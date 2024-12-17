using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> users { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Map UserPermission enum to string
            modelBuilder.Entity<User>()
                .Property(u => u.Permission)
                .HasConversion<string>()
                .IsRequired();

            base.OnModelCreating(modelBuilder);
        }
    }
}