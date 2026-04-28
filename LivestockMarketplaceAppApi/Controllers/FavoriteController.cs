using LivestockMarketplaceApp.Data;
using LivestockMarketplaceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LivestockMarketplaceApp.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 👈 حماية كاملة: يجب تسجيل الدخول لاستخدام المفضلة
    public class FavoritesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FavoritesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. جلب قائمة إعلاناتي المفضلة (GET: api/FavoritesApi)
        [HttpGet]
        public async Task<IActionResult> GetMyFavorites()
        {
            // جلب رقم المستخدم الحقيقي
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { success = false, message = "غير مصرح لك" });
            }
            int currentUserId = int.Parse(userIdClaim);

            var favorites = await _context.Favorites
                .Include(f => f.Listing)
                    .ThenInclude(l => l.Category)
                .Include(f => f.Listing)
                    .ThenInclude(l => l.ListingImages)
                .Where(f => f.UserId == currentUserId)
                .Select(f => new
                {
                    FavoriteId = f.Id, // رقم سجل المفضلة (للحذف المباشر إن أردت)
                    Listing = new
                    {
                        f.Listing.Id,
                        f.Listing.Title,
                        f.Listing.Price,
                        f.Listing.City,
                        CategoryName = f.Listing.Category != null ? f.Listing.Category.Name : "غير محدد",
                        CoverImage = f.Listing.ListingImages.Any() ? f.Listing.ListingImages.First().ImageUrl : "",
                        DateAdded = f.Listing.CreatedAt.ToString("yyyy/MM/dd")
                    }
                })
                .ToListAsync();

            return Ok(new { success = true, data = favorites });
        }

        // 2. إضافة أو إزالة الإعلان من المفضلة (POST: api/FavoritesApi/Toggle/5)
        [HttpPost("Toggle/{listingId}")]
        public async Task<IActionResult> ToggleFavorite(int listingId)
        {
            // جلب رقم المستخدم الحقيقي
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { success = false, message = "غير مصرح لك" });
            }
            int currentUserId = int.Parse(userIdClaim);

            // التحقق مما إذا كان الإعلان موجوداً فعلاً في قاعدة البيانات
            var listingExists = await _context.Listings.AnyAsync(l => l.Id == listingId);
            if (!listingExists)
            {
                return NotFound(new { success = false, message = "الإعلان غير موجود" });
            }

            // البحث في المفضلة
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.ListingId == listingId && f.UserId == currentUserId);

            bool isFavorited; // لمعرفة حالة الإعلان بعد العملية

            if (existingFavorite != null)
            {
                // إذا كان موجوداً، نحذفه
                _context.Favorites.Remove(existingFavorite);
                isFavorited = false;
            }
            else
            {
                // إذا لم يكن موجوداً، نضيفه
                var favorite = new Favorite
                {
                    ListingId = listingId,
                    UserId = currentUserId
                };
                _context.Add(favorite);
                isFavorited = true;
            }

            await _context.SaveChangesAsync();

            // نرجع النتيجة لكي يغير تطبيق الجوال لون أيقونة القلب فوراً
            string message = isFavorited ? "تمت الإضافة للمفضلة" : "تمت الإزالة من المفضلة";
            return Ok(new { success = true, isFavorited = isFavorited, message = message });
        }
    }
}