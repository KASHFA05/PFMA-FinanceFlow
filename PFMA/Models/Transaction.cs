using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PFMA.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(10)]
        public string Type { get; set; } = "Expense"; // Income, Expense

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = "Other"; // Food, Transport, Bills, Salary, Misc

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
