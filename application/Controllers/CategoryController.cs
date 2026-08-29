using Microsoft.AspNetCore.Mvc;
using GreenMart.Data;
using GreenMart.Models;

namespace GreenMart.Controllers
{
    public class CategoryController : Controller
    {

        private readonly ApplicationDbContext _context;



        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }





        [HttpGet]
        public IActionResult Index()
        {

            var categories =
                _context.Categories
                .Where(x => x.IsActive)
                .ToList();



            return View(categories);

        }





        [HttpGet]
        public IActionResult Create()
        {

            if (!User.IsInRole("Admin"))
            {
                return Unauthorized();
            }



            return View();

        }






        [HttpPost]
        public IActionResult Create(Category category)
        {

            if (!User.IsInRole("Admin"))
            {
                return Unauthorized();
            }




            if (!ModelState.IsValid)
            {
                return View(category);
            }




            category.IsActive = true;



            _context.Categories.Add(category);



            _context.SaveChanges();



            return RedirectToAction("Index");

        }





        [HttpPost]
        public IActionResult Delete(int id)
        {

            if (!User.IsInRole("Admin"))
            {
                return Unauthorized();
            }





            var category =
                _context.Categories
                .FirstOrDefault(
                    x => x.CategoryId == id
                );



            if (category == null)
            {
                return NotFound();
            }




            category.IsActive = false;



            _context.SaveChanges();



            return RedirectToAction("Index");

        }

    }
}