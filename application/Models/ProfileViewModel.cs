using System.ComponentModel.DataAnnotations;

namespace GreenMart.Models
{
    public class ProfileViewModel
    {
        public int UserId { get; set; }

        [Required]
        public string FullName { get; set; }

        public string Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string Role { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; }


        [Required(ErrorMessage = "Name is required")]
        public string UpdatedFullName { get; set; }

        public string? UpdatedPhoneNumber { get; set; }

        public string? UpdatedAddress { get; set; }

        public string? CurrentPassword { get; set; }

        public string? NewPassword { get; set; }

        public string? ConfirmPassword { get; set; }
    }
}