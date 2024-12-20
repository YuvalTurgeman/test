namespace test.Models;
using System;

public class BookModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; }

    [MaxLength(500)]
    public string CoverImage { get; set; } // URL or file path to the cover image

    [MaxLength(50)]
    public string Genre { get; set; }

    [Required]
    [MaxLength(100)]
    public string Author { get; set; }

    [MaxLength(100)]
    public string Publisher { get; set; }

    [Range(0, 10000)]
    public decimal? PurchasePrice { get; set; } // Nullable for flexibility

    [Range(0, 1000)]
    public decimal? BorrowPrice { get; set; } // Nullable for flexibility

    [Range(1000, 9999)]
    public int? YearPublished { get; set; } // Nullable to allow missing years

    [MaxLength(10)]
    public string AgeLimit { get; set; } // Example: "18+"

    public bool IsBuyOnly { get; set; } // Flag for purchase-only books

    [MaxLength(100)]
    public string Formats { get; set; } // Example: "EPUB, PDF, MOBI"
}