using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TheaterApi.Data;
using TheaterApi.Models;

namespace TheaterApi.Tests.Infrastructure;

public sealed class TestDataFactory(AppDbContext db, TimeProvider clock)
{
    public async Task<(Person Person, string Token)> CreatePatronAsync(string email = "patron@example.test")
    {
        var role = await db.Roles.SingleAsync(x => x.RoleName == "PATRON");
        var person = NewPerson(email);
        person.Password = new PasswordHasher<Person>().HashPassword(person, "test-password-only");
        person.Patron = new Patron { RoleId = role.Id };
        db.People.Add(person);
        await db.SaveChangesAsync();
        return (person, CreateToken(person.Id, "PATRON"));
    }

    public async Task<(Person Person, string Token)> CreateStaffAsync(bool admin = false, string email = "staff@example.test")
    {
        var roleName = admin ? "ADMIN" : "STAFF";
        var role = await db.Roles.SingleAsync(x => x.RoleName == roleName);
        var venue = await db.Venues.OrderBy(x => x.Id).FirstAsync();
        var person = NewPerson(email);
        person.Password = new PasswordHasher<Person>().HashPassword(person, "test-password-only");
        person.Patron = new Patron { RoleId = (await db.Roles.SingleAsync(x => x.RoleName == "PATRON")).Id };
        person.Staff = new Staff { RoleId = role.Id, AssignedVenue = venue.Id, HireDate = clock.GetUtcNow() };
        db.People.Add(person);
        await db.SaveChangesAsync();
        return (person, CreateToken(person.Id, roleName));
    }

    public async Task<Performance> CreatePerformanceAsync(int seatCount, decimal price)
    {
        var venue = new Venue { Name = "Fixture Venue", Address = "1 Test Way", City = "Calgary", Province = "AB", PostalCode = "T2P 0A1" };
        var stage = new Stage { Venue = venue, Name = "Fixture Stage" };
        var section = new Section { Stage = stage, Name = "Fixture Section" };
        for (var i = 1; i <= seatCount; i++) section.Seats.Add(new Seat { Row = "A", SeatNumber = i });
        var genre = new Genre { Name = $"Fixture-{Guid.NewGuid():N}", Active = true };
        var show = new Event { Title = "Fixture Event", Status = "ACTIVE", Genres = [genre] };
        var performance = new Performance { Event = show, Stage = stage, DateTime = clock.GetUtcNow().AddDays(1), Status = "SCHEDULED" };
        db.AddRange(venue, genre, show, performance);
        await db.SaveChangesAsync();
        db.PriceTiers.Add(new PriceTier { EventId = show.Id, Price = price });
        foreach (var seat in section.Seats)
            db.PerformanceSeats.Add(new PerformanceSeat { SeatId = seat.Id, PerformanceId = performance.Id, Status = "AVAILABLE" });
        await db.SaveChangesAsync();
        return performance;
    }

    private static Person NewPerson(string email) => new()
    {
        FirstName = "Test", LastName = "Patron", Email = email,
        Phone = $"+1{Random.Shared.Next(2000000000, int.MaxValue)}", Password = ""
    };

    private string CreateToken(int id, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-only-signing-key-that-is-long-enough-for-hmac-sha256"));
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, id.ToString()), new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Role, role) };
        var token = new JwtSecurityToken("TheaterApi.Tests", "TheaterApi.Tests", claims,
            expires: clock.GetUtcNow().UtcDateTime.AddMinutes(10), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
