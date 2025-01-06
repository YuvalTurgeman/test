using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace test.Models
{
    public class PurchaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public int Quantity { get; set; } = 1;
        [Required]
        public decimal FinalPrice { get; set; }

        // Navigation properties
        [ForeignKey("BookId")]
        public virtual BookModel Book { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public int? DiscountId { get; set; }

        [ForeignKey("DiscountId")]
        public virtual DiscountModel Discount { get; set; }

        // Many-to-many relationship with Borrows
        public virtual ICollection<BorrowModel> Borrows { get; set; }
        
        public bool IsHidden { get; set; } = false;
    }
}