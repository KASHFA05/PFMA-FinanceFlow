using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PFMA.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        // "Expense" or "Income"
        [Required]
        [MaxLength(10)]
        public string Type { get; set; } = "Expense";

        // Font Awesome class, e.g. "fa-mug-hot"
        [Required]
        [MaxLength(50)]
        public string Icon { get; set; } = "fa-tag";
    }
}
