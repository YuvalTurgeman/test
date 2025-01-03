using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace test.Models
{
    public class BorrowModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsReturned { get; set; } = false;

        [Required]
        public decimal BorrowPrice { get; set; }

        public DateTime? ReturnedDate { get; set; }

        // Navigation properties
        [ForeignKey("BookId")]
        public virtual BookModel Book { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public virtual ICollection<PurchaseModel> Purchases { get; set; }
        
        public BorrowModel()
        {
            Purchases = new List<PurchaseModel>();
            EndDate = StartDate.AddDays(30); // Default 30-day borrow period
        }
    }
}