using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TheaterApi.Data;
using TheaterApi.Logging;
using TheaterApi.Models;

namespace TheaterApi.Services;

public interface IMockEmailSender { Task SendPasswordResetAsync(string email, string token, CancellationToken ct); }
public sealed class MockEmailSender : IMockEmailSender { public Task SendPasswordResetAsync(string email, string token, CancellationToken ct) => Task.CompletedTask; }

public sealed class AuthService(AppDbContext db, IConfiguration config, ILogSink logs, IMockEmailSender emailSender)
{
    private readonly PasswordHasher<Person> _hasher = new();
    private readonly ConcurrentDictionary<string, (string Token, DateTimeOffset Expires)> _resetTokens = new(StringComparer.OrdinalIgnoreCase);

    public async Task<Person> RegisterAsync(string firstName, string lastName, string email, string phone, string password, CancellationToken ct)
    {
        email = email.Trim().ToLowerInvariant();
        if (await db.People.AnyAsync(p => p.Email == email || p.Phone == phone, ct)) throw new InvalidOperationException("Email or phone is already registered.");
        var role = await db.Roles.SingleAsync(r => r.RoleName == "PATRON", ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var person = new Person { FirstName = firstName.Trim(), LastName = lastName.Trim(), Email = email, Phone = phone.Trim() };
        person.Password = _hasher.HashPassword(person, password);
        person.Patron = new Patron { RoleId = role.Id, Role = role };
        db.People.Add(person); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return person;
    }

    public async Task<(Person? Person, string? Token)> LoginAsync(string email, string password, string ip, CancellationToken ct)
    {
        var person = await db.People.Include(p => p.Patron).ThenInclude(p => p!.Role).Include(p => p.Staff).ThenInclude(s => s!.Role).SingleOrDefaultAsync(p => p.Email == email.Trim().ToLower(), ct);
        var valid = person is not null && IsValidRole(person) && _hasher.VerifyHashedPassword(person, person.Password, password) != PasswordVerificationResult.Failed;
        await logs.LoginAttemptAsync(person?.Id, ip, valid);
        return valid ? (person, CreateToken(person!)) : (null, null);
    }

    public string Role(Person p) => p.Staff?.Role.RoleName ?? p.Patron?.Role.RoleName ?? "PATRON";
    private static bool IsValidRole(Person p) => (p.Staff is null || p.Staff.Role.RoleName is "STAFF" or "ADMIN") && (p.Patron is null || p.Patron.Role.RoleName == "PATRON");
    public async Task<string?> RequestResetAsync(string email, CancellationToken ct)
    {
        var person = await db.People.SingleOrDefaultAsync(p => p.Email == email.Trim().ToLower(), ct);
        if (person is null) return null;
        var token = Convert.ToHexString(Guid.NewGuid().ToByteArray()); _resetTokens[email] = (token, DateTimeOffset.UtcNow.AddMinutes(30));
        var request = new PasswordResetRequest { PersonId = person.Id, Time = DateTimeOffset.UtcNow, Status = "PENDING", Success = false }; db.PasswordResetRequests.Add(request); await db.SaveChangesAsync(ct);
        await emailSender.SendPasswordResetAsync(email, token, ct); return token;
    }
    public async Task<bool> ConfirmResetAsync(string email, string token, string newPassword, CancellationToken ct)
    {
        if (!_resetTokens.TryGetValue(email, out var reset) || reset.Expires < DateTimeOffset.UtcNow || !CryptographicEquals(reset.Token, token)) return false;
        var person = await db.People.SingleOrDefaultAsync(p => p.Email == email.Trim().ToLower(), ct); if (person is null) return false;
        person.Password = _hasher.HashPassword(person, newPassword); var request = await db.PasswordResetRequests.Where(r => r.PersonId == person.Id && r.Status == "PENDING").OrderByDescending(r => r.Id).FirstOrDefaultAsync(ct); if (request is not null) { request.Status = "COMPLETED"; request.Success = true; }
        await db.SaveChangesAsync(ct); _resetTokens.TryRemove(email, out _); return true;
    }
    private string CreateToken(Person p)
    {
        var key = config["Jwt:SigningKey"]; if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, p.Id.ToString()), new Claim(ClaimTypes.NameIdentifier, p.Id.ToString()), new Claim(ClaimTypes.Email, p.Email), new Claim(ClaimTypes.Role, Role(p)) };
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims, expires: DateTime.UtcNow.AddHours(2), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256)));
    }
    private static bool CryptographicEquals(string a, string b) => System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
