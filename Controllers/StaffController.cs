using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;
using BikeStoreApp.Models;

namespace BikeStoreApp.Controllers
{
    public class StaffController : Controller
    {
        private readonly BikeStoresEntities1 db = new BikeStoresEntities1();

        public async Task<ActionResult> Index()
        {
            var staffs = await db.staffs.Include(s => s.stores).ToListAsync();
            return View(staffs);
        }
    }
}
