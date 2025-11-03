using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using BikeStoreApp.Models;

namespace BikeStoreApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly BikeStoresEntities1 db = new BikeStoresEntities1();

        // Home page - merges Staff, Customers, and Products
        public async Task<ActionResult> Index(
            int staffPage = 1, int customerPage = 1, int productPage = 1,
            int? brandId = null, int? categoryId = null)
        {
            int pageSize = 6; // slightly larger lists for bigger panels

            // ===================== STAFF =====================
            var staffQuery = db.staffs.Include(s => s.stores).OrderBy(s => s.first_name);
            int totalStaff = await staffQuery.CountAsync();
            var staffPaged = await staffQuery
                .Skip((staffPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            ViewBag.Staffs = staffPaged;
            ViewBag.StaffCurrentPage = staffPage;
            ViewBag.StaffTotalPages = (int)System.Math.Ceiling((double)totalStaff / pageSize);

            // ===================== CUSTOMERS =====================
            var customerQuery = db.customers.OrderBy(c => c.first_name);
            int totalCustomers = await customerQuery.CountAsync();
            var customerPaged = await customerQuery
                .Skip((customerPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            ViewBag.Customers = customerPaged;
            ViewBag.CustomerCurrentPage = customerPage;
            ViewBag.CustomerTotalPages = (int)System.Math.Ceiling((double)totalCustomers / pageSize);
            ViewBag.Stores = await db.stores.ToListAsync();


            // ===================== PRODUCTS (with filters) =====================
            // ===================== PRODUCTS (with filters) =====================
            ViewBag.Brands = await db.brands.ToListAsync();
            ViewBag.Categories = await db.categories.ToListAsync();

            var productQuery = db.products
                .Include(p => p.brands)
                .Include(p => p.categories);

            if (brandId.HasValue)
                productQuery = productQuery.Where(p => p.brand_id == brandId);

            if (categoryId.HasValue)
                productQuery = productQuery.Where(p => p.category_id == categoryId);

            // Apply sorting AFTER filters
            productQuery = productQuery.OrderBy(p => p.product_name);

            int totalProducts = await productQuery.CountAsync();
            var productPaged = await productQuery
                .Skip((productPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Products = productPaged;
            ViewBag.ProductCurrentPage = productPage;
            ViewBag.ProductTotalPages = (int)System.Math.Ceiling((double)totalProducts / pageSize);

            return View();
        }

        // Async Create Staff
        [HttpPost]
        public async Task<ActionResult> CreateStaff(staffs staff)
        {
            if (ModelState.IsValid)
            {
                db.staffs.Add(staff);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return new HttpStatusCodeResult(400);
        }

        // Async Create Customer
        [HttpPost]
        public async Task<ActionResult> CreateCustomer(customers customer)
        {
            if (ModelState.IsValid)
            {
                db.customers.Add(customer);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return new HttpStatusCodeResult(400);
        }
    }
}
