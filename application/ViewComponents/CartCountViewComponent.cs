using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenMart.Data;

namespace GreenMart.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {

        private readonly ApplicationDbContext _context;


        public CartCountViewComponent(
            ApplicationDbContext context
        )
        {
            _context = context;
        }





        public IViewComponentResult Invoke()
        {

            if (UserClaimsPrincipal == null ||
                !UserClaimsPrincipal.Identity.IsAuthenticated)
            {
                return View(0);
            }





            var userId =
                UserClaimsPrincipal
                .FindFirst("UserId")
                ?.Value;



            if (userId == null)
            {
                return View(0);
            }





            int id =
                int.Parse(userId);





            var count =
                _context.CartItems
                .Include(x => x.Cart)
                .Where(
                    x => x.Cart.UserId == id
                )
                .Sum(
                    x => x.Quantity
                );





            return View(count);

        }

    }
}