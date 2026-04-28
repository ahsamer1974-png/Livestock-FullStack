using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockMarketplaceApp.Data;
using LivestockMarketplaceApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection; // 🔴 مهم جداً للمهام في الخلفية

namespace LivestockMarketplaceApp.Controllers
{
    [Authorize]
    public class TransportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider _serviceProvider; // 🔴 إضافة لعمل التحديث التلقائي

        public TransportController(ApplicationDbContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        // 1. المشتري يطلب الشراء
        [HttpPost]
        public async Task<IActionResult> RequestTransport(int listingId, string address, decimal fee)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Settings");

            var request = new TransportRequest
            {
                ListingId = listingId,
                BuyerId = userId,
                DeliveryAddress = address,
                TransportFee = fee,
                Status = 0, // 0 = قيد المراجعة
                CreatedAt = DateTime.Now
            };

            _context.TransportRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إرسال طلب الشراء للبائع بنجاح!";
            return RedirectToAction("MyOrders"); // يحوله مباشرة لصفحة طلباته
        }

        // 2. صفحة المشتري
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");
            var orders = await _context.TransportRequests
                .Include(t => t.Listing)
                .Where(t => t.BuyerId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return View(orders);
        }

        // 3. صفحة البائع
        public async Task<IActionResult> IncomingOrders()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");

            int currentUserId = 0;
            int.TryParse(userIdString, out currentUserId);

            var orders = await _context.TransportRequests
                .Include(t => t.Listing)
                .Where(t => t.Listing.UserId == currentUserId) 
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // 4. البائع يقبل الطلب (وهنا يبدأ السحر والتحديث التلقائي)
        [HttpPost]
        public async Task<IActionResult> StartDelivery(int id)
        {
            var request = await _context.TransportRequests.FindAsync(id);
            if (request != null && request.Status == 0)
            {
                request.Status = 1; // 1 = عند شركة التوصيل
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم استلام الطلب! سيقوم النظام بتحديث الحالة للمشتري تلقائياً.";

                // 🔥 تشغيل مؤقت في الخلفية بدون ما يوقف الموقع
                _ = Task.Run(async () =>
                {
                    // 💡 ملاحظة: خليتها 5 دقائق زي ما طلبت. 
                    // إذا جيت تعرضها للدكتور، غير الرقم 5 إلى 1 عشان ما ينتظر الدكتور 10 دقايق!
                    await Task.Delay(TimeSpan.FromMinutes(5));

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var reqUpdate1 = await db.TransportRequests.FindAsync(id);
                        if (reqUpdate1 != null && reqUpdate1.Status == 1)
                        {
                            reqUpdate1.Status = 2; // 2 = في الطريق إليك
                            await db.SaveChangesAsync();

                            // ننتظر 5 دقائق أخرى
                            await Task.Delay(TimeSpan.FromMinutes(5));

                            var reqUpdate2 = await db.TransportRequests.FindAsync(id);
                            if (reqUpdate2 != null && reqUpdate2.Status == 2)
                            {
                                reqUpdate2.Status = 3; // 3 = تم التوصيل
                                await db.SaveChangesAsync();
                            }
                        }
                    }
                });
            }
            return RedirectToAction(nameof(IncomingOrders));
        }
    }
}