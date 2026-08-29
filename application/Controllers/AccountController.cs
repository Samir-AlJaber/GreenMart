using Microsoft.AspNetCore.Mvc;
using GreenMart.Data;
using GreenMart.Models;
using System.Text.RegularExpressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using BCrypt.Net;

namespace GreenMart.Controllers
{
    public class AccountController : Controller
    {

        private readonly ApplicationDbContext _context;



        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }




        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }





        [HttpPost]
        public IActionResult Register(User user)
        {

            ValidateUser(user);



            if (ModelState.IsValid)
            {

                user.PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        user.PasswordHash
                    );



                user.Role = "User";



                user.IsActive = true;



                _context.Users.Add(user);



                _context.SaveChanges();



                return View("RegisterSuccess");

            }



            return View(user);

        }







        [HttpGet]
        public IActionResult Login()
        {

            if (User.Identity != null &&
                User.Identity.IsAuthenticated)
            {
                return RedirectToAction(
                    "Index",
                    "Home"
                );
            }



            return View();

        }








        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }



            var user =
                _context.Users
                .FirstOrDefault(
                    x => x.Email == model.Email
                );





            if (user == null ||
                !BCrypt.Net.BCrypt.Verify(
                    model.Password,
                    user.PasswordHash
                ))
            {

                ModelState.AddModelError(
                    "",
                    "Invalid email or password"
                );


                return View(model);

            }





            if (!user.IsActive)
            {

                ModelState.AddModelError(
                    "",
                    "Your account has been disabled."
                );


                return View(model);

            }





            await SignInUser(user);




            return RedirectToAction(
                "LoginSuccess"
            );

        }







        [HttpGet]
        public IActionResult LoginSuccess()
        {
            return View();
        }





        [HttpGet]
        public IActionResult RegisterSuccess()
        {
            return View();
        }







        [HttpGet]
        public JsonResult CheckEmail(string email)
        {

            bool exists =
                _context.Users
                .Any(
                    x => x.Email == email
                );



            return Json(new
            {
                exists
            });

        }








        [HttpGet]
        public IActionResult Profile()
        {

            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }




            var userId =
                int.Parse(
                    User.FindFirst("UserId").Value
                );



            var user =
                _context.Users
                .FirstOrDefault(
                    x => x.UserId == userId
                );



            if (user == null)
            {
                return RedirectToAction("Login");
            }





            var model =
                new ProfileViewModel
                {

                    UserId = user.UserId,

                    FullName = user.FullName,

                    Email = user.Email,

                    PhoneNumber = user.PhoneNumber,

                    Address = user.Address,

                    Role = user.Role,

                    CreatedAt = user.CreatedAt,

                    IsActive = user.IsActive,


                    UpdatedFullName = user.FullName,

                    UpdatedPhoneNumber = user.PhoneNumber,

                    UpdatedAddress = user.Address

                };



            return View(model);

        }









        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileUpdateModel model)
        {

            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }





            var userId =
                int.Parse(
                    User.FindFirst("UserId").Value
                );





            var user =
                _context.Users
                .FirstOrDefault(
                    x => x.UserId == userId
                );




            if (user == null)
            {
                return RedirectToAction("Login");
            }





            if (string.IsNullOrWhiteSpace(model.FullName))
            {

                TempData["ErrorMessage"] =
                    "Name cannot be empty.";

                return RedirectToAction("Profile");

            }





            if (string.IsNullOrWhiteSpace(model.Email) ||
                !model.Email.EndsWith("@gmail.com"))
            {

                TempData["ErrorMessage"] =
                    "Email must be a valid Gmail address.";

                return RedirectToAction("Profile");

            }







            if (!Regex.IsMatch(
                model.PhoneNumber,
                @"^\d{11}$"
            ))
            {

                TempData["ErrorMessage"] =
                    "Phone number must contain exactly 11 digits.";

                return RedirectToAction("Profile");

            }







            if (model.Email != user.Email)
            {

                bool exists =
                    _context.Users.Any(
                        x =>
                        x.Email == model.Email &&
                        x.UserId != user.UserId
                    );



                if (exists)
                {

                    TempData["ErrorMessage"] =
                        "This email is already registered.";

                    return RedirectToAction("Profile");

                }

            }





            user.FullName =
                model.FullName;



            user.Email =
                model.Email;



            user.PhoneNumber =
                model.PhoneNumber;



            user.Address =
                model.Address;



            _context.SaveChanges();



            await SignInUser(user);



            TempData["SuccessMessage"] =
                "✓ Profile updated successfully";



            return RedirectToAction("Profile");

        }








        [HttpPost]
        public async Task<IActionResult> UpdatePassword(ProfileViewModel model)
        {

            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }





            var userId =
                int.Parse(
                    User.FindFirst("UserId").Value
                );





            var user =
                _context.Users
                .FirstOrDefault(
                    x => x.UserId == userId
                );





            if (user == null)
            {
                return RedirectToAction("Login");
            }







            if (!BCrypt.Net.BCrypt.Verify(
                model.CurrentPassword,
                user.PasswordHash))
            {

                TempData["ErrorMessage"] =
                    "Current password is incorrect.";

                return RedirectToAction("Profile");

            }







            if (!Regex.IsMatch(
                model.NewPassword,
                @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{8,}$"
            ))
            {

                TempData["ErrorMessage"] =
                    "Password must contain uppercase, lowercase, digit and minimum 8 characters.";

                return RedirectToAction("Profile");

            }






            if (model.NewPassword != model.ConfirmPassword)
            {

                TempData["ErrorMessage"] =
                    "Passwords do not match.";

                return RedirectToAction("Profile");

            }







            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    model.NewPassword
                );



            _context.SaveChanges();



            await HttpContext.SignOutAsync(
                "GreenMartCookie"
            );



            TempData["SuccessMessage"] =
                "✓ Password updated successfully. Please login again.";





            return RedirectToAction("Login");

        }









        [HttpGet]
        public async Task<IActionResult> Logout()
        {

            await HttpContext.SignOutAsync(
                "GreenMartCookie"
            );



            return RedirectToAction(
                "Index",
                "Home"
            );

        }








        private async Task SignInUser(User user)
        {

            var claims =
                new List<Claim>
                {

                    new Claim(
                        ClaimTypes.Name,
                        user.FullName
                    ),



                    new Claim(
                        ClaimTypes.Email,
                        user.Email
                    ),



                    new Claim(
                        "UserId",
                        user.UserId.ToString()
                    ),



                    new Claim(
                        ClaimTypes.Role,
                        user.Role
                    )

                };




            var identity =
                new ClaimsIdentity(
                    claims,
                    "GreenMartCookie"
                );




            var principal =
                new ClaimsPrincipal(identity);




            await HttpContext.SignInAsync(
                "GreenMartCookie",
                principal
            );

        }








        private void ValidateUser(User user)
        {


            if (!string.IsNullOrEmpty(user.Email))
            {


                if (!user.Email.EndsWith("@gmail.com"))
                {

                    ModelState.AddModelError(
                        "Email",
                        "Email must end with @gmail.com"
                    );

                }




                bool exists =
                    _context.Users.Any(
                        x => x.Email == user.Email
                    );




                if (exists)
                {

                    ModelState.AddModelError(
                        "Email",
                        "This email is already registered"
                    );

                }

            }






            if (!string.IsNullOrEmpty(user.PasswordHash))
            {


                if (!Regex.IsMatch(
                    user.PasswordHash,
                    @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{8,}$"
                ))
                {

                    ModelState.AddModelError(
                        "PasswordHash",
                        "Password must contain uppercase, lowercase, digit and minimum 8 characters"
                    );

                }

            }

        }

    }
}