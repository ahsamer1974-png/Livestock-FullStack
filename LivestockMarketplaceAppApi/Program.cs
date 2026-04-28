using LivestockMarketplaceApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var contectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(contectionString));

builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.Cookie.Name = "MyCookieAuth";
        options.LoginPath = "/Settings/Login";

        // 🔥 الإضافة لمنع ظهور كود HTML في الـ API
        options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                // إذا كان الطلب قادم من تطبيق الجوال (الرابط يبدأ بـ api)
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                // أما إذا كان من متصفح عادي للموقع، حوله لصفحة الدخول
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection(); // موقفه عمداً عشان المحاكي يقبل الـ HTTP

// ==========================================
// 🔥 التعديل هنا: تفعيل قراءة الصور من مجلد الـ API نفسه
// ==========================================
app.UseStaticFiles(); // 👈 هذا السطر السحري بيحل المشكلة ويظهر الصور للفلاتر

// الكود حقك القديم (خليناه موجود بشكل آمن عشان لو مشروع الـ MVC يحتاجه)
var mvcPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "LivestockMarketplaceApp", "wwwroot");
if (Directory.Exists(mvcPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(mvcPath)
    });
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Listings}/{action=Index}/{id?}");

app.MapControllers();

app.Run();