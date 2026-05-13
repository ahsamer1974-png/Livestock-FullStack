using LivestockMarketplaceApp.Data;
using LivestockMarketplaceAppApi.DTOs;
using LivestockMarketplaceAppMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

[Route("api/[controller]")]
[ApiController]
public class DriversApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DriversApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetDrivers(
        [FromQuery] string? carType,
        [FromQuery] string? region)
    {
        var query = _context.Drivers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(carType))
            query = query.Where(x => x.CarType.Contains(carType));

        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(x => x.Region.Contains(region));

        var drivers = await query
            .OrderByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                FullName = "سائق",
                x.CarType,
                x.Region
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = drivers
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddDriver([FromBody] AddDriverDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CarType) || string.IsNullOrWhiteSpace(dto.Region))
        {
            return BadRequest(new
            {
                success = false,
                message = "نوع السيارة والمنطقة مطلوبة"
            });
        }

        var userId = 1; // مؤقتاً، بعدين خذها من تسجيل الدخول

        var driver = new Driver
        {
            UserId = userId,
            CarType = dto.CarType,
            Region = dto.Region
        };

        _context.Drivers.Add(driver);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "تمت إضافة السائق بنجاح"
        });
    }
}