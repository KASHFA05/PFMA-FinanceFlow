using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PFMA.Models
{
    public class BillReminder
    {
        [Key]
        public int ReminderId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "Utility"; // Utility, Housing, Entertainment

        [Required]
        [MaxLength(100)]
        public string Provider { get; set; } = string.Empty; // e.g. Comcast, Clean Energy Co.

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Upcoming"; // Overdue, Upcoming, Paid
    }
}
