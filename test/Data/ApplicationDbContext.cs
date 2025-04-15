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
        public DbSet<CartItemModel> CartItems { get; set; }
        public DbSet<ShoppingCartModel> ShoppingCarts { get; set; }
        public DbSet<WaitingListModel> WaitingList { get; set; }
        public DbSet<ReviewModel> WebsiteReviews { get; set; } 
        public DbSet<RatingModel> Ratings { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User relationships
            modelBuilder.Entity<User>()
                .Property(u => u.Permission)
                .HasConversion<string>()
                .IsRequired();
            
            modelBuilder.Entity<User>()
                .Property(u => u.Salt)
                .HasColumnName("salt");
            
            modelBuilder.Entity<User>()
                .HasMany(u => u.Purchases)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Borrows)
                .WithOne(b => b.User)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne<ShoppingCartModel>()
                .WithOne(sc => sc.User)
                .HasForeignKey<ShoppingCartModel>(sc => sc.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Reviews) 
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Book relationships
            modelBuilder.Entity<BookModel>()
                .HasMany(b => b.Purchases)
                .WithOne(p => p.Book)
                .HasForeignKey(p => p.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BookModel>()
                .HasMany(b => b.Borrows)
                .WithOne(b => b.Book)
                .HasForeignKey(b => b.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BookModel>()
                .HasMany(b => b.Discounts)
                .WithOne(d => d.Book)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            // Purchase relationships
            modelBuilder.Entity<PurchaseModel>()
                .HasOne(p => p.Discount)
                .WithMany()
                .HasForeignKey(p => p.DiscountId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Shopping Cart relationships
            modelBuilder.Entity<ShoppingCartModel>()
                .HasMany(sc => sc.CartItems)
                .WithOne(ci => ci.ShoppingCart)
                .HasForeignKey(ci => ci.ShoppingCartId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cart Item relationships
            modelBuilder.Entity<CartItemModel>()
                .HasOne(ci => ci.Book)
                .WithMany()
                .HasForeignKey(ci => ci.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CartItemModel>()
                .HasOne(ci => ci.Discount)
                .WithMany()
                .HasForeignKey(ci => ci.DiscountId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Review relationships
            modelBuilder.Entity<ReviewModel>() // Added
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Performance Indexes
            modelBuilder.Entity<BookModel>()
                .HasIndex(b => b.Title);

            modelBuilder.Entity<BookModel>()
                .HasIndex(b => b.Author);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<DiscountModel>()
                .HasIndex(d => new { d.BookId, d.StartDate, d.EndDate });

            modelBuilder.Entity<BorrowModel>()
                .HasIndex(b => new { b.BookId, b.UserId, b.IsReturned });

            modelBuilder.Entity<PurchaseModel>()
                .HasIndex(p => new { p.BookId, p.UserId });

            modelBuilder.Entity<CartItemModel>()
                .HasIndex(ci => new { ci.ShoppingCartId, ci.BookId });

            modelBuilder.Entity<ReviewModel>() // Added
                .HasIndex(r => r.UserId);
        }
    }
}
