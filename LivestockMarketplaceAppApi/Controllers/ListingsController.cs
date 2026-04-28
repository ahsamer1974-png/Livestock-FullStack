using LivestockMarketplaceApp.Data;
using LivestockMarketplaceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;

namespace LivestockMarketplaceApp.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListingsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ListingsApiController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // 1. جلب جميع الإعلانات مع دعم البحث والفلترة بالمدينة والقسم (GET: api/ListingsApi)
        [HttpGet]
        public async Task<IActionResult> GetListings([FromQuery] string? searchString, [FromQuery] string? city, [FromQuery] int? categoryId)
        {
            var query = _context.Listings
                .Include(l => l.Category)
                .Include(l => l.ListingImages)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(l => l.Title.Contains(searchString) || l.Description.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(l => l.City == city);
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(l => l.CategoryId == categoryId.Value);
            }

            // إرجاع البيانات بشكل مرتب ونظيف كـ JSON لتطبيق الجوال
            var listings = await query.OrderByDescending(l => l.CreatedAt)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.Price,
                    l.City,
                    l.Age,
                    CategoryName = l.Category != null ? l.Category.Name : "غير محدد",
                    // 🔥 التعديل هنا: إضافة الساعات والدقائق والثواني
                    DateAdded = l.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    CoverImage = l.ListingImages.Any() ? l.ListingImages.First().ImageUrl : ""
                })
                .ToListAsync();

            return Ok(listings);
        }

        // 2. جلب تفاصيل إعلان واحد (GET: api/ListingsApi/5)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetListingDetails(int id)
        {
            var listing = await _context.Listings
                .Include(l => l.Category)
                .Include(l => l.ListingImages)
                .Include(l => l.User)
                .Where(l => l.Id == id)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.Description,
                    l.Price,
                    l.City,
                    l.Age,
                    CategoryName = l.Category != null ? l.Category.Name : "غير محدد",
                    SellerName = l.User != null ? l.User.FullName : "غير معروف",
                    SellerPhone = l.User != null ? l.User.PhoneNumber : "",
                    SellerId = l.UserId,
                    // 🔥 التعديل هنا أيضاً: إضافة الساعات والدقائق والثواني
                    DateAdded = l.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    Images = l.ListingImages.Select(img => img.ImageUrl).ToList()
                })
                .FirstOrDefaultAsync();

            if (listing == null)
            {
                return NotFound(new { success = false, message = "الإعلان غير موجود" });
            }

            return Ok(new { success = true, data = listing });
        }

        // 3. إضافة إعلان جديد عبر الـ API (POST: api/ListingsApi)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateListing([FromForm] CreateListingRequest request, [FromForm] List<IFormFile> images)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { success = false, message = "يجب تسجيل الدخول أولاً" });
                }

                var listing = new Listing
                {
                    Title = request.Title,
                    Description = request.Description,
                    Price = request.Price,
                    City = request.City,
                    Age = request.Age,
                    CategoryId = request.CategoryId,
                    UserId = int.Parse(userIdClaim),
                    CreatedAt = DateTime.Now // يتم حفظ الوقت الحالي بالكامل في قاعدة البيانات
                };

                _context.Listings.Add(listing);
                await _context.SaveChangesAsync();

                if (images != null && images.Count > 0)
                {
                    string webRootPath = _hostEnvironment.WebRootPath;
                    if (string.IsNullOrWhiteSpace(webRootPath))
                    {
                        webRootPath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot");
                    }

                    string uploadFolder = Path.Combine(webRootPath, "images", "listings");

                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

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

                return Ok(new { success = true, message = "تمت إضافة الإعلان بنجاح", listingId = listing.Id });
            }
            catch (Exception ex)
            {
                string exactError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { success = false, message = "السبب: " + exactError });
            }
        }

        // 4. حذف إعلان عبر الـ API (DELETE: api/ListingsApi/5)
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteListing(int id)
        {
            var listing = await _context.Listings
                .Include(l => l.ListingImages)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (listing == null)
            {
                return NotFound(new { success = false, message = "الإعلان غير موجود" });
            }

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (listing.UserId.ToString() != userIdClaim)
            {
                return Unauthorized(new { success = false, message = "غير مصرح لك بحذف هذا الإعلان" });
            }

            foreach (var img in listing.ListingImages)
            {
                string webRootPath = _hostEnvironment.WebRootPath ?? Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot");
                var filePath = Path.Combine(webRootPath, img.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Listings.Remove(listing);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "تم حذف الإعلان بنجاح" });
        }
    }

    public class CreateListingRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string City { get; set; }
        public string? Age { get; set; }
        public int CategoryId { get; set; }
    }
}