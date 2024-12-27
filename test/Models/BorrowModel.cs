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

        // Optional actual return date
        public DateTime? ReturnedDate { get; set; }

        // Navigation properties
        [ForeignKey("BookId")]
        public virtual BookModel Book { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        // Many-to-many relationship with Purchases
        public virtual ICollection<PurchaseModel> Purchases { get; set; }
    }
}