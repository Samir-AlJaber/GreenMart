using System.ComponentModel.DataAnnotations;

namespace GreenMart.Models
{
    public class ProfileUpdateModel
    {
        public string CurrentEmail { get; set; }


        [Required]
        public string FullName { get; set; }


        [EmailAddress]
        public string Email { get; set; }


        public string? PhoneNumber { get; set; }


        public string? Address { get; set; }



        public string CurrentPassword { get; set; }


        public string NewPassword { get; set; }


        public string ConfirmPassword { get; set; }

    }
}