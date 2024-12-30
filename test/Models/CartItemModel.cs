using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace test.Models
{
    public class CartItemModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }

        [Required]
        public int ShoppingCartId { get; set; }

        [Required]
        public bool IsBorrow { get; set; }  // true for borrow, false for buy

        [Required]
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        public int? DiscountId { get; set; }

        [Range(0, 10000)]
        public decimal? FinalPrice { get; set; }  // Price after discount

        // Navigation properties
        [ForeignKey("BookId")]
        public virtual BookModel Book { get; set; }

        [ForeignKey("ShoppingCartId")]
        public virtual ShoppingCartModel ShoppingCart { get; set; }

        [ForeignKey("DiscountId")]
        public virtual DiscountModel Discount { get; set; }
    }
}