using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PFMA.Models
{
    public class AdvisorClient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AdvisorId { get; set; }

        [ForeignKey("AdvisorId")]
        public User? Advisor { get; set; }

        [Required]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public User? Client { get; set; }
    }
}
