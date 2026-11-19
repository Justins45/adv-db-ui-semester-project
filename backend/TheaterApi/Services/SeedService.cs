using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TheaterApi.Data;
using TheaterApi.Models;
using Microsoft.AspNetCore.Identity;

namespace TheaterApi.Services;

public sealed class SeedService(IConfiguration config, AppDbContext db, IWebHostEnvironment env, FirestoreSeeder firestore)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        if (await db.People.AnyAsync(ct)) return;
        var source = Path.Combine(env.ContentRootPath, "docs", "sources", "AdvDataPhase2SeedData.sql");
        var output = Path.Combine(env.ContentRootPath, "seed"); Directory.CreateDirectory(output);
        var postgres = Path.Combine(output, "postgres.sql"); var login = Path.Combine(output, "login_history.json");
        var split = Process.Start(new ProcessStartInfo("python3", $"tools/split_seed.py docs/sources/AdvDataPhase2SeedData.sql seed") { WorkingDirectory = env.ContentRootPath, RedirectStandardOutput = true });
        if (split is not null) { await split.WaitForExitAsync(ct); if (split.ExitCode != 0) throw new InvalidOperationException(await split.StandardOutput.ReadToEndAsync(ct)); }
        await using var conn = new NpgsqlConnection(config.GetConnectionString("NeonDb")); await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await using (var cmd = new NpgsqlCommand(await File.ReadAllTextAsync(postgres, ct), conn, tx)) await cmd.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
        if (!await db.Reviews.AnyAsync(ct)) { var review = Path.Combine(env.ContentRootPath, "docs", "sources", "theater_reviews_with_vectors.sql"); await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(review, ct), ct); }
        await db.Database.ExecuteSqlRawAsync("UPDATE patron SET preferences = '{}'::jsonb WHERE preferences = '{}'::jsonb", ct);
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO order_item (order_id, performance_seat_id, price_at_purchase)
            SELECT o.id, ps.id, t.price FROM \"ORDER\" o
            JOIN cart_item ci ON ci.cart_id = o.cart_id JOIN ticket t ON t.id = ci.ticket_id
            JOIN performance_seat ps ON ps.id = t.performance_seat_id
            WHERE NOT EXISTS (SELECT 1 FROM order_item oi WHERE oi.order_id = o.id);
            UPDATE \"ORDER\" SET status = 'PAID', total_amount = COALESCE((SELECT amount FROM payment p WHERE p.order_id = \"ORDER\".id), 0)
            WHERE status = 'PENDING';
            """, ct);
        await ConfigureDevAccountsAsync(ct);
        var firestoreSeed = Path.Combine(env.ContentRootPath, "firestore_logs_seed.json");
        if (File.Exists(firestoreSeed)) await firestore.SeedAsync(firestoreSeed);
        if (File.Exists(login)) await firestore.SeedAsync(login);
    }

    private async Task ConfigureDevAccountsAsync(CancellationToken ct)
    {
        var hasher = new PasswordHasher<Person>();
        foreach (var (key, role) in new[] { ("Admin", "ADMIN"), ("Staff", "STAFF"), ("Patron", "PATRON") })
        {
            var password = config[$"SeedAccounts:{key}Password"];
            if (string.IsNullOrWhiteSpace(password)) continue;
            var person = await db.People.Include(p => p.Patron).Include(p => p.Staff).Where(p => role == "PATRON" ? p.Staff == null : p.Staff != null && p.Staff.Role.RoleName == role).OrderBy(p => p.Id).FirstOrDefaultAsync(ct);
            if (person is null) continue;
            person.Password = hasher.HashPassword(person, password);
            await db.SaveChangesAsync(ct);
        }
    }
}
