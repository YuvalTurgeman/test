using System.ComponentModel.DataAnnotations;

namespace test.Models
{
    public class RatingModel
    {
        public int Id { get; set; }
    
        [Required]
        public int BookId { get; set; }
    
        [Required]
        public int UserId { get; set; }
    
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Value { get; set; }
    
        public DateTime CreatedAt { get; set; }
    
        // Navigation properties
        [ForeignKey("BookId")]
        public virtual BookModel Book { get; set; }
    
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}