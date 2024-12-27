using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace test.Models
{
    public class DiscountModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal DiscountAmount { get; set; }  // Percentage or fixed amount

        [Required]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        // Navigation property
        [ForeignKey("BookId")]
        public virtual BookModel Book { get; set; }
    }
}