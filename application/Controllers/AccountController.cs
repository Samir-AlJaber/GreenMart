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
        public IActionResult Register(User user)
        {
            ValidateUser(user);


            if (ModelState.IsValid)
            {
                user.PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        user.PasswordHash
                    );


                _context.Users.Add(user);

                _context.SaveChanges();


                return View("RegisterSuccess");
            }


            return View(user);
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }



            var user = _context.Users
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




            var claims = new List<Claim>
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


            return RedirectToAction(
                "LoginSuccess",
                "Account"
            );

        }


        [HttpGet]
        public IActionResult RegisterSuccess()
        {
            return View();
        }


        [HttpGet]
        public IActionResult LoginSuccess()
        {
            return View();
        }


        [HttpGet]
        public JsonResult CheckEmail(string email)
        {

            bool exists =
                _context.Users
                .Any(x => x.Email == email);



            return Json(new
            {
                exists = exists
            });

        }

        [HttpGet]
        public IActionResult Profile()
        {
            if (User.Identity == null ||
               !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }


            var email =
                User.FindFirst(
                    ClaimTypes.Email
                )?.Value;


            var user =
                _context.Users
                .FirstOrDefault(
                    x => x.Email == email
                );


            if (user == null)
            {
                return RedirectToAction("Login");
            }


            var model = new ProfileViewModel
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
            if (User.Identity == null ||
                !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }


            var oldEmail =
                User.FindFirst(
                    ClaimTypes.Email
                )?.Value;



            var user =
                _context.Users
                .FirstOrDefault(
                    x => x.Email == oldEmail
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



            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                TempData["ErrorMessage"] =
                    "Phone number cannot be empty.";

                return RedirectToAction("Profile");
            }



            if (model.PhoneNumber.Length != 11 ||
               !Regex.IsMatch(model.PhoneNumber, "^[0-9]+$"))
            {
                TempData["ErrorMessage"] =
                    "Phone number must contain exactly 11 digits.";

                return RedirectToAction("Profile");
            }



            if (string.IsNullOrWhiteSpace(model.Email))
            {
                TempData["ErrorMessage"] =
                    "Email cannot be empty.";

                return RedirectToAction("Profile");
            }



            if (!model.Email.EndsWith("@gmail.com"))
            {
                TempData["ErrorMessage"] =
                    "Email must end with @gmail.com";

                return RedirectToAction("Profile");
            }

            if (model.Email != user.Email)
            {

                bool emailExists =
                    _context.Users.Any(
                        x => x.Email == model.Email &&
                             x.UserId != user.UserId
                    );


                if (emailExists)
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



            var claims = new List<Claim>
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



            TempData["SuccessMessage"] =
                "✓ Profile updated successfully";


            return RedirectToAction("Profile");
        }


        [HttpPost]
        public async Task<IActionResult> UpdatePassword(ProfileViewModel model)
        {
            if (User.Identity == null ||
                !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }



            var email =
                User.FindFirst(
                    ClaimTypes.Email
                )?.Value;



            var user =
                _context.Users
                .FirstOrDefault(
                    x => x.Email == email
                );



            if (user == null)
            {
                return RedirectToAction("Login");
            }



            if (string.IsNullOrWhiteSpace(model.CurrentPassword))
            {
                TempData["ErrorMessage"] =
                    "Current password cannot be empty.";

                return RedirectToAction("Profile");
            }



            if (!BCrypt.Net.BCrypt.Verify(
                    model.CurrentPassword,
                    user.PasswordHash))
            {
                TempData["ErrorMessage"] =
                    "Current password is incorrect.";

                return RedirectToAction("Profile");
            }



            if (string.IsNullOrWhiteSpace(model.NewPassword))
            {
                TempData["ErrorMessage"] =
                    "New password cannot be empty.";

                return RedirectToAction("Profile");
            }



            if (model.NewPassword.Length < 8)
            {
                TempData["ErrorMessage"] =
                    "Password must be at least 8 characters.";

                return RedirectToAction("Profile");
            }



            if (!Regex.IsMatch(
                model.NewPassword,
                "[A-Z]"
            ))
            {
                TempData["ErrorMessage"] =
                    "Password must contain at least one uppercase letter.";

                return RedirectToAction("Profile");
            }



            if (!Regex.IsMatch(
                model.NewPassword,
                "[a-z]"
            ))
            {
                TempData["ErrorMessage"] =
                    "Password must contain at least one lowercase letter.";

                return RedirectToAction("Profile");
            }



            if (!Regex.IsMatch(
                model.NewPassword,
                "[0-9]"
            ))
            {
                TempData["ErrorMessage"] =
                    "Password must contain at least one digit.";

                return RedirectToAction("Profile");
            }



            if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["ErrorMessage"] =
                    "New password and confirm password do not match.";

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



            return RedirectToAction(
                "Login"
            );
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


                bool emailExists =
                    _context.Users
                    .Any(x => x.Email == user.Email);



                if (emailExists)
                {

                    ModelState.AddModelError(
                        "Email",
                        "This email is already registered"
                    );

                }

            }

            if (!string.IsNullOrEmpty(user.PasswordHash))
            {


                if (user.PasswordHash.Length < 8)
                {

                    ModelState.AddModelError(
                        "PasswordHash",
                        "Password must be at least 8 characters"
                    );

                }

                if (!Regex.IsMatch(
                    user.PasswordHash,
                    "[A-Z]"
                ))
                {

                    ModelState.AddModelError(
                        "PasswordHash",
                        "Password must contain at least one uppercase letter"
                    );

                }

                if (!Regex.IsMatch(
                    user.PasswordHash,
                    "[a-z]"
                ))
                {

                    ModelState.AddModelError(
                        "PasswordHash",
                        "Password must contain at least one lowercase letter"
                    );

                }

                if (!Regex.IsMatch(
                    user.PasswordHash,
                    "[0-9]"
                ))
                {

                    ModelState.AddModelError(
                        "PasswordHash",
                        "Password must contain at least one digit"
                    );

                }

            }

        }

    }
}