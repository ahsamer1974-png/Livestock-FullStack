using LivestockMarketplaceApp.Data;
using LivestockMarketplaceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // أضفنا هذه المكتبة للحماية
using System.Security.Claims; // أضفنا هذه المكتبة لقراءة بيانات المستخدم

namespace LivestockMarketplaceApp.Controllers
{
    public class ListingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ListingsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // 1. عرض قائمة الإعلانات (متاحة للجميع)
        // 1. عرض قائمة الإعلانات مع دعم البحث والفلترة
        public async Task<IActionResult> Index(string searchString, string city)
        {
            // أ. نبدأ بجلب الإعلانات كـ Query (استعلام) وليس كقائمة نهائية لكي نتمكن من فلترتها
            var query = _context.Listings
                .Include(l => l.Category)
                .Include(l => l.ListingImages)
                .AsQueryable();

            // ب. إذا أدخل المستخدم كلمة للبحث، نبحث في العنوان أو الوصف
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(l => l.Title.Contains(searchString) || l.Description.Contains(searchString));
            }

            // ج. إذا اختار المستخدم مدينة معينة، نفلتر بناءً عليها
            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(l => l.City == city);
            }

            // د. حفظ قيم البحث الحالية لإعادتها للواجهة (لكي لا تختفي الكلمة من مربع البحث بعد تحديث الصفحة)
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentCity = city;

            // هـ. ترتيب الإعلانات (الأحدث أولاً) ثم جلبها من قاعدة البيانات
            var listings = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

            return View(listings);
        }

        // 2. صفحة إضافة إعلان جديد (محمية)
        [Authorize]
        public IActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // 3. حفظ الإعلان الجديد باسم المستخدم المسجل
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Listing listing, List<IFormFile> images)
        {
            // --- التعديل هنا: جلب رقم المستخدم الحقيقي ---
            var userIdClaim = User.FindFirst("UserId")?.Value;
            listing.UserId = int.Parse(userIdClaim);

            listing.CreatedAt = DateTime.Now;

            ModelState.Remove("User");
            ModelState.Remove("Category");
            ModelState.Remove("ListingImages");

            if (ModelState.IsValid)
            {
                _context.Add(listing);
                await _context.SaveChangesAsync();

                if (images != null && images.Count > 0)
                {
                    string uploadFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "listings");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    foreach (var file in images)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string filePath = Path.Combine(uploadFolder, fileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        _context.ListingImages.Add(new ListingImage
                        {
                            ImageUrl = "/images/listings/" + fileName,
                            ListingId = listing.Id
                        });
                    }
                    await _context.SaveChangesAsync();
                }
                // بعد إضافة الإعلان بنجاح، نحوله لصفحة "إعلاناتي" في الإعدادات
                return RedirectToAction("MyListings", "Settings");
            }
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", listing.CategoryId);
            return View(listing);
        }

        // 4. عرض تفاصيل الإعلان (متاحة للجميع)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var listing = await _context.Listings
                .Include(l => l.Category)
                .Include(l => l.ListingImages)
                .Include(l => l.User) 
                .FirstOrDefaultAsync(m => m.Id == id);

            if (listing == null) return NotFound();

            bool isFavorite = false;
            string? currentUserIdString = null; 

            if (User.Identity.IsAuthenticated)
            {
                currentUserIdString = User.FindFirst("UserId")?.Value;
                int currentUserId = int.Parse(currentUserIdString);

                isFavorite = await _context.Favorites
                    .AnyAsync(f => f.ListingId == id && f.UserId == currentUserId);
            }

            ViewBag.IsFavorite = isFavorite;
            ViewBag.CurrentUserId = currentUserIdString; 

            return View(listing);
        }

        // 5. صفحة تعديل الإعلان (محمية ومقتصرة على صاحب الإعلان)
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var listing = await _context.Listings.FindAsync(id);
            if (listing == null) return NotFound();

            // حماية: التأكد أن المستخدم الحالي هو صاحب الإعلان
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (listing.UserId != int.Parse(userIdClaim))
            {
                return Unauthorized(); // يمنع تعديل إعلانات الآخرين
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", listing.CategoryId);
            return View(listing);
        }

        // 6. حفظ التعديلات
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Listing listing)
        {
            if (id != listing.Id) return NotFound();

            // حماية إضافية وقت الحفظ
            var userIdClaim = User.FindFirst("UserId")?.Value;
            listing.UserId = int.Parse(userIdClaim); // نضمن عدم تغيير المالك

            ModelState.Remove("User");
            ModelState.Remove("Category");
            ModelState.Remove("ListingImages");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(listing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ListingExists(listing.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction("MyListings", "Settings");
            }
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", listing.CategoryId);
            return View(listing);
        }

        // 7. صفحة تأكيد الحذف (محمية)
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var listing = await _context.Listings
                .Include(l => l.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (listing == null) return NotFound();

            // حماية: التأكد أن المستخدم الحالي هو صاحب الإعلان
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (listing.UserId != int.Parse(userIdClaim))
            {
                return Unauthorized();
            }

            return View(listing);
        }

        // 8. تنفيذ الحذف
        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var listing = await _context.Listings
                .Include(l => l.ListingImages)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (listing != null)
            {
                // حماية: تأكيد الملكية قبل الحذف الفعلي
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (listing.UserId != int.Parse(userIdClaim)) return Unauthorized();

                foreach (var img in listing.ListingImages)
                {
                    var filePath = Path.Combine(_hostEnvironment.WebRootPath, img.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                }

                _context.Listings.Remove(listing);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MyListings", "Settings");
        }

        private bool ListingExists(int id)
        {
            return _context.Listings.Any(e => e.Id == id);
        }
    }
}