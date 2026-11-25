using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PFMA.Models
{
    public class Budget
    {
        [Key]
        public int BudgetId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount is required")]
        [Range(1, 10000000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // Format: yyyy-MM  (e.g. 2026-10)
        [Required]
        [MaxLength(7)]
        public string Month { get; set; } = DateTime.Now.ToString("yyyy-MM");
    }
}
