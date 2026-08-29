using System.ComponentModel.DataAnnotations;

namespace GreenMart.Models
{
    public class ProfileUpdateModel
    {

        public string? CurrentEmail { get; set; }



        [Required(ErrorMessage = "Name cannot be empty")]
        public string FullName { get; set; }



        [Required(ErrorMessage = "Email cannot be empty")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; }



        [Required(ErrorMessage = "Phone number cannot be empty")]
        [RegularExpression(
            @"^\d{11}$",
            ErrorMessage = "Phone number must contain exactly 11 digits"
        )]
        public string PhoneNumber { get; set; }



        public string? Address { get; set; }



        public string? CurrentPassword { get; set; }



        public string? NewPassword { get; set; }



        public string? ConfirmPassword { get; set; }

    }
}