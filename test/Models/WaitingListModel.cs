using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace test.Models
{
    public class WaitingListModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;

        [Required]
        public int Position { get; set; }

        public bool IsNotified { get; set; } = false;

        // Navigation properties
        [ForeignKey("BookId")]
        public virtual BookModel Book { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}