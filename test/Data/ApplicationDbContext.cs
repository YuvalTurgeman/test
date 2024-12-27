using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> users { get; set; }
        public DbSet<BookModel> Books { get; set; }
        public DbSet<DiscountModel> Discounts { get; set; }
        public DbSet<PurchaseModel> Purchases { get; set; }
        public DbSet<BorrowModel> Borrows { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map UserPermission enum to string
            modelBuilder.Entity<User>()
                .Property(u => u.Permission)
                .HasConversion<string>()
                .IsRequired();

            // Configure many-to-many relationship between Purchase and Borrow
            modelBuilder.Entity<PurchaseModel>()
                .HasMany(p => p.Borrows)
                .WithMany(b => b.Purchases)
                .UsingEntity(j => j.ToTable("PurchaseBorrows"));

            // Configure one-to-many relationship between Book and Purchase
            modelBuilder.Entity<BookModel>()
                .HasMany(b => b.Purchases)
                .WithOne(p => p.Book)
                .HasForeignKey(p => p.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between Book and Borrow
            modelBuilder.Entity<BookModel>()
                .HasMany(b => b.Borrows)
                .WithOne(b => b.Book)
                .HasForeignKey(b => b.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between User and Purchase
            modelBuilder.Entity<User>()
                .HasMany(u => u.Purchases)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between User and Borrow
            modelBuilder.Entity<User>()
                .HasMany(u => u.Borrows)
                .WithOne(b => b.User)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between Book and Discount
            modelBuilder.Entity<BookModel>()
                .HasMany(b => b.Discounts)
                .WithOne(d => d.Book)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}