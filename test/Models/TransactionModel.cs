using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace test.Models
{
    public class TransactionModel
    {
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        public string Type { get; set; } // "borrow" or "purchase"

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }
    }
}