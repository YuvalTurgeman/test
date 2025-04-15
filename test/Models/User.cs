using Microsoft.AspNetCore.Mvc.ModelBinding;
using test.Enums;

namespace test.Models
{
    public class User
    {
        public User()
        {
            // Initialize collections in constructor
            Purchases = new List<PurchaseModel>();
            Borrows = new List<BorrowModel>();
            Reviews = new List<ReviewModel>();
        }
        
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        [RegularExpression(@"(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[#$^+=!*()@%&]).{8,}",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
        public string Password { get; set; }
        
        [Required]
        [EnumDataType(typeof(UserPermission))]
        public UserPermission Permission { get; set; }
        
        [BindNever]
        public string? Salt { get; set; }
        
        // Forgot Password fields
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }
        
        // Navigation properties
        public virtual ICollection<PurchaseModel> Purchases { get; set; }
        public virtual ICollection<BorrowModel> Borrows { get; set; }
        public virtual ICollection<ReviewModel> Reviews { get; set; }
    }
}