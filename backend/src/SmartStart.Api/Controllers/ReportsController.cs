using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStart.Application.Interfaces;

namespace SmartStart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("profit")]
    public async Task<IActionResult> GetProfitReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var start = ToUtc(startDate) ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = ToUtc(endDate) ?? DateTime.UtcNow;

        return Ok(await _reportService.GetProfitReportAsync(start, end));
    }

    [HttpGet("top-selling")]
    public async Task<IActionResult> GetTopSelling(
        [FromQuery] int count = 10,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        return Ok(await _reportService.GetTopSellingProductsAsync(count, ToUtc(startDate), ToUtc(endDate)));
    }

    private static DateTime? ToUtc(DateTime? value)
    {
        if (value is null) return null;
        var d = value.Value;
        return d.Kind switch
        {
            DateTimeKind.Utc => d,
            DateTimeKind.Local => d.ToUniversalTime(),
            _ => DateTime.SpecifyKind(d, DateTimeKind.Utc)
        };
    }
}
