using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockMarketplaceApp.Data;
using LivestockMarketplaceApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace LivestockMarketplaceApp.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // تأمين الـ API بالكامل
    public class TransportApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        public TransportApiController(ApplicationDbContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        // كلاس صغير (DTO) لاستقبال البيانات بشكل نظيف وآمن
        public class TransportRequestDto
        {
            public int ListingId { get; set; }
            public string Address { get; set; }
            public decimal Fee { get; set; }
        }

        // 1. المشتري يطلب الشراء
        // POST: api/transport/request
        [HttpPost("request")]
        public async Task<IActionResult> RequestTransport([FromBody] TransportRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { success = false, message = "يجب تسجيل الدخول أولاً" });

            var request = new TransportRequest
            {
                ListingId = dto.ListingId,
                BuyerId = userId,
                DeliveryAddress = dto.Address,
                TransportFee = dto.Fee,
                Status = 0, // 0 = قيد المراجعة
                CreatedAt = DateTime.Now
            };

            _context.TransportRequests.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "تم إرسال طلب الشراء للبائع بنجاح!", orderId = request.Id });
        }

        // 2. جلب طلبات المشتري (مشترياتي)
        // GET: api/transport/my-orders
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var orders = await _context.TransportRequests
                .Include(t => t.Listing)
                .Where(t => t.BuyerId == userId)
                .OrderByDescending(t => t.CreatedAt)
                // نستخدم Select عشان نرجع JSON نظيف وما يصير فيه مشكلة التداخل (Circular Reference)
                .Select(t => new {
                    id = t.Id,
                    listingId = t.ListingId,
                    listingTitle = t.Listing.Title,
                    listingImage = t.Listing.ListingImages.FirstOrDefault().ImageUrl,
                    deliveryAddress = t.DeliveryAddress,
                    fee = t.TransportFee,
                    status = t.Status,
                    createdAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = orders });
        }

        // 3. جلب الطلبات الواردة للبائع
        // GET: api/transport/incoming-orders
        [HttpGet("incoming-orders")]
        public async Task<IActionResult> GetIncomingOrders()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            if (!int.TryParse(userIdString, out int currentUserId))
                return BadRequest(new { success = false, message = "معرف المستخدم غير صالح" });

            var orders = await _context.TransportRequests
                .Include(t => t.Listing)
                .Where(t => t.Listing.UserId == currentUserId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new {
                    id = t.Id,
                    listingId = t.ListingId,
                    listingTitle = t.Listing.Title,
                    buyerId = t.BuyerId,
                    deliveryAddress = t.DeliveryAddress,
                    status = t.Status,
                    createdAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = orders });
        }

        // 4. البائع يقبل الطلب ويبدأ التوصيل الأوتوماتيكي
        // PUT: api/transport/start-delivery/5
        [HttpPut("start-delivery/{id}")]
        public async Task<IActionResult> StartDelivery(int id)
        {
            var request = await _context.TransportRequests.FindAsync(id);

            if (request == null)
                return NotFound(new { success = false, message = "الطلب غير موجود" });

            if (request.Status != 0)
                return BadRequest(new { success = false, message = "حالة الطلب الحالية لا تسمح ببدء التوصيل" });

            request.Status = 1; // 1 = عند شركة التوصيل
            await _context.SaveChangesAsync();

            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(5));

                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var reqUpdate1 = await db.TransportRequests.FindAsync(id);
                    if (reqUpdate1 != null && reqUpdate1.Status == 1)
                    {
                        reqUpdate1.Status = 2; // 2 = في الطريق إليك
                        await db.SaveChangesAsync();

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

            return Ok(new { success = true, message = "تم استلام الطلب! سيقوم النظام بتحديث الحالة للمشتري تلقائياً." });
        }
    }
}