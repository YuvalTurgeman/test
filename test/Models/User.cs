using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using test.Enums;

namespace test.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; }

        [Required]
        [EnumDataType(typeof(UserPermission))]
        public UserPermission Permission { get; set; }
        
        // Navigation properties
        public virtual ICollection<PurchaseModel> Purchases { get; set; }
        public virtual ICollection<BorrowModel> Borrows { get; set; }
    }
    
}