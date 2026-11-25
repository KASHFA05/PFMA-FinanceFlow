using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PFMA.Models
{
    public class SavingsGoal
    {
        [Key]
        public int GoalId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [MaxLength(100)]
        public string GoalName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TargetAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentAmount { get; set; }

        public DateTime? TargetDate { get; set; }
    }
}
