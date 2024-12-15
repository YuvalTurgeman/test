using test.Models;
namespace test.Data;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<BookModel> Books { get; set; }
    public DbSet<TransactionModel> Transactions { get; set; }
    public DbSet<ReviewModel> Reviews { get; set; }

}
