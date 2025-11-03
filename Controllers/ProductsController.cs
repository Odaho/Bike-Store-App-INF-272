using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;
using BikeStoreApp.Models;

namespace BikeStoreApp.Controllers
{
    public class ProductsController : Controller
    {
        private readonly BikeStoresEntities1 db = new BikeStoresEntities1();

        public async Task<ActionResult> Index()
        {
            var products = await db.products
                .Include(p => p.brands)
                .Include(p => p.categories)
                .ToListAsync();
            return View(products);
        }
    }
}
