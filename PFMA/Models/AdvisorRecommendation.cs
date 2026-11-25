using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PFMA.Models
{
    public class AdvisorRecommendation
    {
        [Key]
        public int RecommendationId { get; set; }

        [Required]
        public int AdvisorId { get; set; }

        [ForeignKey("AdvisorId")]
        public User? Advisor { get; set; }

        [Required]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public User? Client { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
