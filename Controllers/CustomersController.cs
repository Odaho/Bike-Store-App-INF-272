using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;
using BikeStoreApp.Models;

namespace BikeStoreApp.Controllers
{
    public class CustomersController : Controller
    {
        private readonly BikeStoresEntities1 db = new BikeStoresEntities1();

        public async Task<ActionResult> Index()
        {
            var customers = await db.customers.ToListAsync();
            return View(customers);
        }
    }
}
