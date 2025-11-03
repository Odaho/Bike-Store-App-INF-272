using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using BikeStoreApp.Models;

namespace BikeStoreApp.Controllers
{
    public class ReportController : Controller
    {
        private readonly BikeStoresEntities1 db = new BikeStoresEntities1();

        // ----- Simple POCOs for the view -----
        public class PopularProductRow
        {
            public string Product { get; set; }
            public string Brand { get; set; }
            public string Category { get; set; }
            public int Quantity { get; set; }
            public decimal Revenue { get; set; }
        }

        public class ArchiveItem
        {
            public string BaseName { get; set; }     // filename without extension
            public string DisplayName { get; set; }  // what user typed
            public string FileType { get; set; }     // png or csv
            public DateTime SavedAt { get; set; }
            public string DescriptionHtml { get; set; } // from rich text
        }

        private string ReportsRoot
            => Server.MapPath("~/App_Data/Reports");

        // GET: /Report
        public async Task<ActionResult> Index(DateTime? from = null, DateTime? to = null, int top = 10)
        {
            // ✅ Default range automatically uses all available data
            DateTime minOrderDate = db.orders.Min(o => o.order_date);
            DateTime maxOrderDate = db.orders.Max(o => o.order_date);

            DateTime start = from ?? minOrderDate;
            DateTime end = to ?? maxOrderDate;

            // Query: Popular Products over date range (business-meaningful)
            var rows = (from oi in db.order_items
                        join o in db.orders on oi.order_id equals o.order_id
                        join p in db.products on oi.product_id equals p.product_id
                        join b in db.brands on p.brand_id equals b.brand_id
                        join c in db.categories on p.category_id equals c.category_id
                        where o.order_date >= start && o.order_date <= end
                        group new { oi, p, b, c } by new
                        {
                            p.product_id,
                            p.product_name,
                            b.brand_name,
                            c.category_name
                        } into g
                        select new PopularProductRow
                        {
                            Product = g.Key.product_name,
                            Brand = g.Key.brand_name,
                            Category = g.Key.category_name,
                            Quantity = g.Sum(x => x.oi.quantity),
                            Revenue = g.Sum(x => (decimal)x.oi.list_price * x.oi.quantity)
                        })
                        .OrderByDescending(r => r.Quantity)
                        .Take(top)
                        .ToList();

            // Chart payload
            ViewBag.ChartLabels = rows.Select(r => r.Product).ToArray();
            ViewBag.ChartValues = rows.Select(r => r.Quantity).ToArray();

            ViewBag.From = start.ToString("yyyy-MM-dd");
            ViewBag.To = end.ToString("yyyy-MM-dd");
            ViewBag.Top = top;

            // Archive list
            var archive = LoadArchive();
            ViewBag.Archive = archive
                .OrderByDescending(a => a.SavedAt)
                .ToList();

            return View(rows);
        }

        // POST: /Report/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)] // ✅ allows HTML in descriptionHtml
        public ActionResult Save(string fileName, string fileType, string imageData, string descriptionHtml, string csvData)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                TempData["ReportMessage"] = "Please enter a filename.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(fileType))
            {
                TempData["ReportMessage"] = "Please choose a file type.";
                return RedirectToAction("Index");
            }

            Directory.CreateDirectory(ReportsRoot);

            var baseName = MakeSafeFileName(fileName);
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            baseName = $"{baseName}_{stamp}";

            if (fileType.Equals("png", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(imageData) || !imageData.Contains(","))
                {
                    TempData["ReportMessage"] = "Could not capture the report image.";
                    return RedirectToAction("Index");
                }
                var b64 = imageData.Split(',')[1];
                var bytes = Convert.FromBase64String(b64);
                System.IO.File.WriteAllBytes(Path.Combine(ReportsRoot, baseName + ".png"), bytes);
                SaveMeta(baseName, fileName, "png", descriptionHtml);
            }
            else if (fileType.Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(csvData))
                {
                    TempData["ReportMessage"] = "No CSV data was provided.";
                    return RedirectToAction("Index");
                }
                System.IO.File.WriteAllText(Path.Combine(ReportsRoot, baseName + ".csv"), csvData);
                SaveMeta(baseName, fileName, "csv", descriptionHtml);
            }
            else
            {
                TempData["ReportMessage"] = "Unsupported file type.";
            }

            TempData["ReportMessage"] = $"Saved '{fileName}.{fileType}'.";
            return RedirectToAction("Index");
        }


        // GET: /Report/Download
        public ActionResult Download(string name, string ext)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(ext)) return HttpNotFound();
            var full = Path.Combine(ReportsRoot, $"{name}.{ext}");
            if (!System.IO.File.Exists(full)) return HttpNotFound();

            var mime = ext.Equals("png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "text/csv";
            var downloadName = GetMeta(name)?.DisplayName ?? name;
            return File(full, mime, $"{downloadName}.{ext}");
        }

        // POST: /Report/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string baseName)
        {
            if (string.IsNullOrWhiteSpace(baseName))
                return RedirectToAction("Index");

            var metaPath = Path.Combine(ReportsRoot, baseName + ".json");
            var pngPath = Path.Combine(ReportsRoot, baseName + ".png");
            var csvPath = Path.Combine(ReportsRoot, baseName + ".csv");

            if (System.IO.File.Exists(metaPath)) System.IO.File.Delete(metaPath);
            if (System.IO.File.Exists(pngPath)) System.IO.File.Delete(pngPath);
            if (System.IO.File.Exists(csvPath)) System.IO.File.Delete(csvPath);

            TempData["ReportMessage"] = "Report deleted.";
            return RedirectToAction("Index");
        }

        // Helpers
        private static string MakeSafeFileName(string s)
        {
            var bad = Path.GetInvalidFileNameChars();
            return string.Concat(s.Where(c => !bad.Contains(c))).Trim();
        }

        private void SaveMeta(string baseName, string displayName, string fileType, string descriptionHtml)
        {
            var meta = new ArchiveItem
            {
                BaseName = baseName,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? baseName : displayName,
                FileType = fileType.ToLowerInvariant(),
                SavedAt = DateTime.Now,
                DescriptionHtml = descriptionHtml ?? ""
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(meta, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(Path.Combine(ReportsRoot, baseName + ".json"), json);
        }

        private ArchiveItem GetMeta(string baseName)
        {
            var path = Path.Combine(ReportsRoot, baseName + ".json");
            if (!System.IO.File.Exists(path)) return null;
            var json = System.IO.File.ReadAllText(path);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<ArchiveItem>(json);
        }

        private IEnumerable<ArchiveItem> LoadArchive()
        {
            Directory.CreateDirectory(ReportsRoot);
            foreach (var meta in Directory.EnumerateFiles(ReportsRoot, "*.json"))
            {
                ArchiveItem item = null;
                try
                {
                    item = Newtonsoft.Json.JsonConvert.DeserializeObject<ArchiveItem>(System.IO.File.ReadAllText(meta));
                }
                catch { }

                if (item != null)
                {
                    var any = System.IO.File.Exists(Path.Combine(ReportsRoot, item.BaseName + ".png")) ||
                              System.IO.File.Exists(Path.Combine(ReportsRoot, item.BaseName + ".csv"));

                    if (any) yield return item;
                }
            }
        }
    }
}
