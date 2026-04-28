using LivestockMarketplaceApp.Data;
using LivestockMarketplaceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LivestockMarketplaceApp.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FavoritesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. عرض قائمة المفضلة
        public async Task<IActionResult> Index()
        {
            // ملاحظة: نستخدم UserId = 3 مؤقتاً كما فعلنا في الإعلانات
            int currentUserId = 3;

            var favorites = await _context.Favorites
                .Include(f => f.Listing)
                .ThenInclude(l => l.ListingImages) // جلب الصور لعرضها في المفضلة
                .Where(f => f.UserId == currentUserId)
                .ToListAsync();

            return View(favorites);
        }

        // 2. إضافة أو إزالة من المفضلة (Toggle)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int listingId)
        {
            int currentUserId = 3; // مؤقتاً

            // التحقق إذا كان الإعلان مضافاً مسبقاً للمفضلة
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.ListingId == listingId && f.UserId == currentUserId);

            if (existingFavorite != null)
            {
                // إذا كان موجوداً، نقوم بحذفه (إزالة من المفضلة)
                _context.Favorites.Remove(existingFavorite);
            }
            else
            {
                // إذا لم يكن موجوداً، نقوم بإضافته
                var favorite = new Favorite
                {
                    ListingId = listingId,
                    UserId = currentUserId
                };
                _context.Add(favorite);
            }

            await _context.SaveChangesAsync();

            // العودة إلى الصفحة التي جاء منها المستخدم (سواء كانت Index أو Details)
            return Redirect(Request.Headers["Referer"].ToString());
        }

        // 3. حذف من صفحة المفضلة مباشرة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var favorite = await _context.Favorites.FindAsync(id);
            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}