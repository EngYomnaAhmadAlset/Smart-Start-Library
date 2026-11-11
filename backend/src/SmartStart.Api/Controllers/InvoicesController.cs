using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStart.Application.DTOs;
using SmartStart.Application.Interfaces;

namespace SmartStart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto)
    {
        try
        {
            return Ok(await _invoiceService.CreateInvoiceAsync(dto));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _invoiceService.GetInvoiceByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpGet("scan")]
    public async Task<IActionResult> Scan([FromQuery] string code)
    {
        try
        {
            return Ok(await _invoiceService.GetInvoiceByScanAsync(code));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 20, [FromQuery] int? hours = null)
    {
        int? cashierId = null;
        if (User.IsInRole("Cashier"))
        {
            var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idValue, out var id))
                cashierId = id;
        }

        if (hours is > 0)
        {
            var since = DateTime.UtcNow.AddHours(-hours.Value);
            return Ok(await _invoiceService.GetInvoicesSinceAsync(since, cashierId));
        }

        if (cashierId is int cid)
            return Ok(await _invoiceService.GetInvoicesSinceAsync(DateTime.UtcNow.AddYears(-10), cid));

        return Ok(await _invoiceService.GetRecentInvoicesAsync(count));
    }
}
