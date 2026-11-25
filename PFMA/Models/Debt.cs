using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PFMA.Models
{
    public class Debt
    {
        [Key]
        public int DebtId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        // e.g. "Borrowed from Ali", "Lent to Sara", "University Fee Loan"
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        // "To Pay" (I owe) or "To Receive" (they owe me)
        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = "To Pay";

        [Required(ErrorMessage = "Total amount is required")]
        [Range(1, 100000000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Range(0, 100000000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ClearedAmount { get; set; }

        [Required]
        public DateTime DueDate { get; set; } = DateTime.Now.AddMonths(1);

        [NotMapped]
        public decimal Remaining => Math.Max(0, TotalAmount - ClearedAmount);

        [NotMapped]
        public bool IsCleared => ClearedAmount >= TotalAmount;
    }
}
