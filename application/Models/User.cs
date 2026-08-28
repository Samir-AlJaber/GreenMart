using System.ComponentModel.DataAnnotations;

namespace GreenMart.Models
{
	public class User
	{

		public int UserId { get; set; }


		[Required(ErrorMessage = "Full name is required")]
		public string FullName { get; set; }


		[Required(ErrorMessage = "Email is required")]
		[EmailAddress(ErrorMessage = "Please enter a valid email address")]
		public string Email { get; set; }



		[Required(ErrorMessage = "Password is required")]

		[MinLength(
			8,
			ErrorMessage = "Password must be at least 8 characters"
		)]

		public string PasswordHash { get; set; }


		[Required(ErrorMessage = "Phone number is required")]

		[RegularExpression(
			@"^\d{11}$",
			ErrorMessage = "Phone number must have 11 digits"
		)]

		public string PhoneNumber { get; set; }

		public string? Address { get; set; }


		public string Role { get; set; } = "User";


		public DateTime CreatedAt { get; set; }
			= DateTime.Now;


		public bool IsActive { get; set; }
			= true;


	}
}