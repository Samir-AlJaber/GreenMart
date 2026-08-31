using GreenMart.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace GreenMart.Controllers
{
    public class HomeController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }




        public IActionResult Privacy()
        {
            return View();
        }





        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true
        )]
        public IActionResult Error()
        {

            return View(
                new ErrorViewModel
                {
                    RequestId =
                    Activity.Current?.Id
                    ??
                    HttpContext.TraceIdentifier
                }
            );

        }

    }
}
