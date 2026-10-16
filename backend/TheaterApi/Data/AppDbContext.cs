using Microsoft.EntityFrameworkCore;
using TheaterApi.Models;

namespace TheaterApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Test> Tests => Set<Test>();
}