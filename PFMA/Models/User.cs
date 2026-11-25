using System.ComponentModel.DataAnnotations;

namespace PFMA.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "User"; // User, Advisor, Admin

        [MaxLength(20)]
        public string AccountType { get; set; } = "Standard"; // Standard, Premium

        [MaxLength(255)]
        public string? ProfilePictureUrl { get; set; } = "/images/default-avatar.png";

        public bool TwoFactorEnabled { get; set; } = false;
        public bool EmailNotificationsEnabled { get; set; } = false;
        public bool LoginAlertsEnabled { get; set; } = false;
    }
}
