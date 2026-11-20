using Microsoft.EntityFrameworkCore;
using TheaterApi.Models;

namespace TheaterApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>(); public DbSet<Person> People => Set<Person>(); public DbSet<Venue> Venues => Set<Venue>(); public DbSet<Event> Events => Set<Event>(); public DbSet<Genre> Genres => Set<Genre>(); public DbSet<PromoCode> PromoCodes => Set<PromoCode>(); public DbSet<EmailList> EmailLists => Set<EmailList>(); public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>(); public DbSet<Patron> Patrons => Set<Patron>(); public DbSet<Staff> Staff => Set<Staff>(); public DbSet<Performer> Performers => Set<Performer>(); public DbSet<Stage> Stages => Set<Stage>(); public DbSet<Cart> Carts => Set<Cart>(); public DbSet<Section> Sections => Set<Section>(); public DbSet<Performance> Performances => Set<Performance>(); public DbSet<Seat> Seats => Set<Seat>(); public DbSet<PerformanceSeat> PerformanceSeats => Set<PerformanceSeat>(); public DbSet<Ticket> Tickets => Set<Ticket>(); public DbSet<CartItem> CartItems => Set<CartItem>(); public DbSet<Order> Orders => Set<Order>(); public DbSet<OrderItem> OrderItems => Set<OrderItem>(); public DbSet<Payment> Payments => Set<Payment>(); public DbSet<PriceTier> PriceTiers => Set<PriceTier>(); public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasPostgresExtension("vector");
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
                property.SetColumnName(ToSnakeCase(property.Name));
        foreach (var e in b.Model.GetEntityTypes()) e.SetTableName(e.GetTableName()!.ToLowerInvariant());
        b.Entity<Role>().ToTable("role").HasKey(x => x.Id); b.Entity<Role>().Property(x => x.RoleName).HasColumnName("role").HasMaxLength(50);
        b.Entity<Person>().ToTable("person"); b.Entity<Person>().HasIndex(x => x.Email).IsUnique(); b.Entity<Person>().HasIndex(x => x.Phone).IsUnique();
        b.Entity<EmailList>().ToTable("email_list").HasKey(x => x.PersonId); b.Entity<PasswordResetRequest>().ToTable("password_reset_request");
        b.Entity<Patron>().ToTable("patron"); b.Entity<Patron>().Property(x => x.Preferences).HasColumnType("jsonb"); b.Entity<Staff>().ToTable("staff"); b.Entity<Staff>().Property(x => x.EmployeeId).HasColumnName("employee_id").ValueGeneratedOnAddOrUpdate(); b.Entity<Staff>().Property(x => x.AssignedVenue).HasColumnName("assigned_venue"); b.Entity<Performer>().ToTable("performer");
        b.Entity<Venue>().ToTable("venue"); b.Entity<Event>().ToTable("event"); b.Entity<Genre>().ToTable("genre"); b.Entity<PromoCode>().ToTable("promo_code"); b.Entity<Stage>().ToTable("stage"); b.Entity<Section>().ToTable("section"); b.Entity<Performance>().ToTable("performance"); b.Entity<Seat>().ToTable("seat"); b.Entity<PerformanceSeat>().ToTable("performance_seat"); b.Entity<Ticket>().ToTable("ticket"); b.Entity<Cart>().ToTable("cart"); b.Entity<CartItem>().ToTable("cart_item"); b.Entity<Order>().ToTable("ORDER"); b.Entity<OrderItem>().ToTable("order_item"); b.Entity<Payment>().ToTable("payment"); b.Entity<PriceTier>().ToTable("price_tier"); b.Entity<Review>().ToTable("review");
        b.Entity<Review>().Property(x => x.Embedding).HasColumnType("vector(384)");
        b.Entity<Event>().HasMany(x => x.Genres).WithMany(x => x.Events).UsingEntity<Dictionary<string, object>>("event_genre", j => j.HasOne<Genre>().WithMany().HasForeignKey("genre_id"), j => j.HasOne<Event>().WithMany().HasForeignKey("event_id"), j => j.HasKey("genre_id", "event_id"));
        b.Entity<Performance>().HasMany(x => x.Performers).WithMany(x => x.Performances).UsingEntity<Dictionary<string, object>>("performance_cast", j => j.HasOne<Performer>().WithMany().HasForeignKey("performer_id"), j => j.HasOne<Performance>().WithMany().HasForeignKey("performance_id"), j => j.HasKey("performer_id", "performance_id"));
        b.Entity<Person>().HasOne(x => x.Patron).WithOne(x => x.Person).HasForeignKey<Patron>(x => x.Id); b.Entity<Person>().HasOne(x => x.Staff).WithOne(x => x.Person).HasForeignKey<Staff>(x => x.Id); b.Entity<Person>().HasOne(x => x.Performer).WithOne(x => x.Person).HasForeignKey<Performer>(x => x.Id);
        b.Entity<Person>().HasOne(x => x.EmailList).WithOne(x => x.Person).HasForeignKey<EmailList>(x => x.PersonId);
        b.Entity<Patron>().HasOne(x => x.Role).WithMany(x => x.Patrons).HasForeignKey(x => x.RoleId); b.Entity<Staff>().HasOne(x => x.Role).WithMany(x => x.Staff).HasForeignKey(x => x.RoleId); b.Entity<Staff>().HasOne(x => x.Venue).WithMany(x => x.Staff).HasForeignKey(x => x.AssignedVenue);
        b.Entity<Cart>().Property(x => x.Xmin).IsRowVersion();
        b.Entity<PerformanceSeat>().Property(x => x.Xmin).IsRowVersion();
        b.Entity<Stage>().HasOne(x => x.Venue).WithMany(x => x.Stages).HasForeignKey(x => x.VenueId); b.Entity<Section>().HasOne(x => x.Stage).WithMany(x => x.Sections).HasForeignKey(x => x.StageId); b.Entity<Seat>().HasOne(x => x.Section).WithMany(x => x.Seats).HasForeignKey(x => x.SectionId); b.Entity<Performance>().HasOne(x => x.Event).WithMany(x => x.Performances).HasForeignKey(x => x.EventId); b.Entity<Performance>().HasOne(x => x.Stage).WithMany(x => x.Performances).HasForeignKey(x => x.StageId); b.Entity<PerformanceSeat>().HasIndex(x => new { x.SeatId, x.PerformanceId }).IsUnique(); b.Entity<Ticket>().HasIndex(x => x.PerformanceSeatId).IsUnique(); b.Entity<CartItem>().HasIndex(x => x.TicketId).IsUnique(); b.Entity<Order>().HasIndex(x => x.CartId).IsUnique(); b.Entity<Order>().HasIndex(x => x.IdempotencyKey).IsUnique(); b.Entity<Payment>().HasIndex(x => x.OrderId).IsUnique();
    }

    private static string ToSnakeCase(string value) => string.Concat(value.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}
