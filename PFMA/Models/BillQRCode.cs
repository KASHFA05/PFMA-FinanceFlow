using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PFMA.Models
{
    public class BillQRCode
    {
        [Key]
        public int QRCodeId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required(ErrorMessage = "Bill title is required")]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Consumer number / reference is required")]
        [MaxLength(100)]
        public string ConsumerNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount is required")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
    }
}
