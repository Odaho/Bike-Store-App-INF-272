using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using BikeStoreApp.Models;

namespace BikeStoreApp.Controllers
{
    public class MaintainController : Controller
    {
        private readonly BikeStoresEntities1 db = new BikeStoresEntities1();

        // MAIN PAGE - Displays all data
        public async Task<ActionResult> Index()
        {
            ViewBag.Staffs = await db.staffs.Include(s => s.stores).ToListAsync();
            ViewBag.Customers = await db.customers.ToListAsync();
            ViewBag.Products = await db.products
                .Include(p => p.brands)
                .Include(p => p.categories)
                .ToListAsync();

            return View();
        }

        // ========================= STAFF =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditStaff(staffs model)
        {
            if (ModelState.IsValid)
            {
                var staff = await db.staffs.FindAsync(model.staff_id);
                if (staff != null)
                {
                    staff.first_name = model.first_name;
                    staff.last_name = model.last_name;
                    staff.email = model.email;
                    staff.phone = model.phone;
                    await db.SaveChangesAsync();
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteStaff(int staff_id)
        {
            var staff = await db.staffs.FindAsync(staff_id);
            if (staff != null)
            {
                try
                {
                    db.staffs.Remove(staff);
                    await db.SaveChangesAsync();
                    TempData["Message"] = "Staff member deleted successfully.";
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException)
                {
                    TempData["Error"] = "This staff member cannot be deleted because they are linked to existing orders.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "An unexpected error occurred: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }


        // ========================= CUSTOMERS =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditCustomer(customers model)
        {
            if (ModelState.IsValid)
            {
                var customer = await db.customers.FindAsync(model.customer_id);
                if (customer != null)
                {
                    customer.first_name = model.first_name;
                    customer.last_name = model.last_name;
                    customer.email = model.email;
                    customer.phone = model.phone;
                    await db.SaveChangesAsync();
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteCustomer(int customer_id)
        {
            var customer = await db.customers.FindAsync(customer_id);
            if (customer != null)
            {
                db.customers.Remove(customer);
                await db.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        // ========================= PRODUCTS =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditProduct(products model)
        {
            if (ModelState.IsValid)
            {
                var product = await db.products.FindAsync(model.product_id);
                if (product != null)
                {
                    product.product_name = model.product_name;
                    product.model_year = model.model_year;
                    product.list_price = model.list_price;
                    product.image_url = model.image_url; // ✅ new line
                    await db.SaveChangesAsync();
                }
            }
            return RedirectToAction("Index");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteProduct(int product_id)
        {
            var product = await db.products.FindAsync(product_id);
            if (product != null)
            {
                db.products.Remove(product);
                await db.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}