using LivestockMarketplaceApp.Data;
using LivestockMarketplaceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace LivestockMarketplaceApp.Controllers
{
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. صفحة الإعدادات (محمية - لا يدخلها إلا المسجلون)
        [Authorize]
        public async Task<IActionResult> Index()
        {
            // جلب رقم المستخدم الحقيقي من الجلسة (Cookie) 
            var userIdClaim = User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users
                .Include(u => u.Listings)
                .Include(u => u.Favorites)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            return View(user);
        }

        // 2. صفحة تسجيل الدخول (عرض الواجهة)
        public IActionResult Login()
        {
            // إذا كان مسجلاً بالفعل، لا داعي لظهور صفحة الدخول
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Listings");
            }
            return View();
        }

        // 3. التحقق من بيانات الدخول (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginAction(string email, string password)
        {
            // البحث عن المستخدم في قاعدة البيانات
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                // إذا تم العثور عليه، نقوم بإنشاء "هوية" له في المتصفح
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("UserId", user.Id.ToString()) // حفظ الـ ID لنستخدمه لاحقاً
                };

                var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");

                // تسجيل الدخول فعلياً وحفظ الـ Cookie
                await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Listings"); // التوجه للرئيسية بعد النجاح
            }

            // إذا فشل الدخول (الإيميل أو الرقم السري خطأ)
            ViewBag.Error = "البريد الإلكتروني أو كلمة المرور غير صحيحة";
            return View("Login");
        }

        // 4. صفحة التسجيل الجديد (عرض الواجهة)
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Listings");
            }
            return View();
        }

        // 5. استقبال بيانات التسجيل الجديد وحفظها في قاعدة البيانات (الإضافة الجديدة)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user)
        {
            // التحقق من أن الإيميل غير مسجل مسبقاً
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (existingUser != null)
            {
                ViewBag.Error = "هذا البريد الإلكتروني مسجل مسبقاً!";
                return View(user);
            }

            // إذا كانت البيانات سليمة، نحفظ المستخدم في قاعدة البيانات
            if (ModelState.IsValid)
            {
                user.JoinedDate = DateTime.Now; // تسجيل وقت الانضمام
                _context.Add(user);
                await _context.SaveChangesAsync();

                // بعد نجاح التسجيل، نوجهه لصفحة الدخول
                return RedirectToAction(nameof(Login));
            }

            // إذا كان هناك خطأ في الإدخال (مثلاً نقص في البيانات)
            return View(user);
        }

        // 6. تسجيل الخروج
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Login", "Settings");
        }
        // 7. صفحة تعديل الملف الشخصي (عرض)
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            int currentUserId = int.Parse(userIdClaim);

            var user = await _context.Users.FindAsync(currentUserId);
            return View(user);
        }

        // 8. حفظ التعديلات في قاعدة البيانات (POST)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(User updatedUser)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            int currentUserId = int.Parse(userIdClaim);

            var user = await _context.Users.FindAsync(currentUserId);
            if (user != null)
            {
                // نحدث فقط الحقول المسموح بتعديلها
                user.FullName = updatedUser.FullName;
                user.PhoneNumber = updatedUser.PhoneNumber;
                user.City = updatedUser.City;
                // ملاحظة: البريد وكلمة المرور عادة نضع لها صفحة تغيير مستقلة للأمان

                await _context.SaveChangesAsync();
                return RedirectToAction("Index"); // العودة لصفحة الإعدادات بعد الحفظ
            }
            return View(updatedUser);
        }

        // 9. صفحة عرض إعلانات المستخدم فقط
        [Authorize]
        public async Task<IActionResult> MyListings()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            int currentUserId = int.Parse(userIdClaim);

            // جلب الإعلانات مع الأقسام والصور
            var myAds = await _context.Listings
                .Include(l => l.Category)
                .Include(l => l.ListingImages) 
                .Where(l => l.UserId == currentUserId)
                .ToListAsync();

            return View(myAds);
        }
        // 10. صفحة عرض طلباتي (المشتريات)
        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            int currentUserId = int.Parse(userIdClaim);

            // جلب الطلبات التي قام بها هذا المستخدم (كمشتري)
            var myOrders = await _context.Orders
                .Include(o => o.Listing) // لجلب تفاصيل الإعلان المرتبط بالطلب
                .Where(o => o.BuyerId == currentUserId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(myOrders);
        }
    }
}