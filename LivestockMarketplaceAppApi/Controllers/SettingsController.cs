using LivestockMarketplaceApp.Data;
using LivestockMarketplaceApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LivestockMarketplaceApp.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SettingsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. تسجيل الدخول لتطبيق الجوال (POST: api/SettingsApi/Login)
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { success = false, message = "الرجاء إدخال البريد الإلكتروني وكلمة المرور" });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Password == request.Password);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim("UserId", user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // لحفظ الدخول حتى بعد إغلاق التطبيق
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                };

                await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

                // إرجاع بيانات المستخدم (بدون كلمة المرور لأسباب أمنية)
                return Ok(new
                {
                    success = true,
                    message = "تم تسجيل الدخول بنجاح",
                    userData = new
                    {
                        user.Id,
                        user.FullName,
                        user.Email,
                        user.City,
                        JoinedDate = user.JoinedDate.ToString("yyyy/MM/dd")
                    }
                });
            }

            return Unauthorized(new { success = false, message = "البريد الإلكتروني أو كلمة المرور غير صحيحة" });
        }

        // 2. إنشاء حساب جديد من تطبيق الجوال (POST: api/SettingsApi/Register)
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            // التحقق من أن الإيميل غير مسجل مسبقاً
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (existingUser != null)
            {
                return BadRequest(new { success = false, message = "هذا البريد الإلكتروني مسجل مسبقاً!" });
            }

            user.JoinedDate = DateTime.Now;

            // حفظ المستخدم
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "تم إنشاء الحساب بنجاح",
                userId = user.Id
            });
        }

        // 3. تعديل الملف الشخصي من الجوال (PUT: api/SettingsApi/EditProfile/5)
        [HttpPut("EditProfile/{id}")]
        public async Task<IActionResult> EditProfile(int id, [FromBody] EditProfileRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { success = false, message = "المستخدم غير موجود" });
            }

            // تحديث البيانات المسموح بها فقط
            user.FullName = request.FullName ?? user.FullName;
            user.City = request.City ?? user.City;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "تم تحديث البيانات بنجاح",
                updatedData = new { user.FullName, user.City }
            });
        }

        // 4. جلب إعلانات المستخدم الحالي (GET: api/SettingsApi/MyListings)
        [HttpGet("MyListings")]
        [Authorize]
        public async Task<IActionResult> GetMyListings()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { success = false, message = "يجب تسجيل الدخول أولاً" });
            }

            int userId = int.Parse(userIdClaim);

            var myListings = await _context.Listings
                .Include(l => l.Category)
                .Include(l => l.ListingImages)
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.Price,
                    l.City,
                    l.Age,
                    CategoryName = l.Category != null ? l.Category.Name : "غير محدد",
                    DateAdded = l.CreatedAt.ToString("yyyy/MM/dd"),
                    CoverImage = l.ListingImages.Any() ? l.ListingImages.First().ImageUrl : ""
                })
                .ToListAsync();

            return Ok(myListings);
        }
    }

    // كلاسات مساعدة لاستقبال البيانات من الجوال بشكل مرتب (DTOs)
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class EditProfileRequest
    {
        public string? FullName { get; set; }
        public string? City { get; set; }
    }
}