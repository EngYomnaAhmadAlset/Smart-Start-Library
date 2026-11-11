using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SmartStart.Application.DTOs;
using SmartStart.Application.Interfaces;

namespace SmartStart.Api.Controllers;

[ApiController]
[Route("api/photocopy")]
[Authorize]
public class PhotocopyController : ControllerBase
{
    private readonly IPhotocopyService _service;

    public PhotocopyController(IPhotocopyService service)
    {
        _service = service;
    }

    [HttpGet("settings")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSettings() =>
        Ok(await _service.GetSettingsAsync());

    [HttpPut("overhead")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOverhead([FromBody] UpdatePhotocopyOverheadDto dto)
    {
        try { return Ok(await _service.UpdateOverheadAsync(dto)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("papers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePaper([FromBody] CreatePhotocopyPaperDto dto)
    {
        try { return Ok(await _service.CreatePaperAsync(dto)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("papers/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePaper(int id, [FromBody] UpdatePhotocopyPaperDto dto)
    {
        try { return Ok(await _service.UpdatePaperAsync(id, dto)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("papers/{id:int}/restock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RestockPaper(int id, [FromBody] RestockPhotocopyPaperDto dto)
    {
        try { return Ok(await _service.RestockPaperAsync(id, dto)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("ink")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateInk([FromBody] UpdatePhotocopyInkDto dto)
    {
        try { return Ok(await _service.UpdateInkAsync(dto)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("cashiers/{id:int}/access")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetAccess(int id, [FromBody] SetPhotocopyAccessDto dto)
    {
        try
        {
            await _service.SetCashierAccessAsync(id, dto.CanAccessPhotocopy);
            return Ok(new { message = "تم التحديث" });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog()
    {
        var userId = UserId();
        if (userId is null) return Unauthorized();
        return Ok(await _service.GetCatalogAsync(userId.Value));
    }

    [HttpPost("sales")]
    [Authorize(Roles = "Cashier")]
    public async Task<IActionResult> CreateSale([FromBody] CreatePhotocopySaleDto dto)
    {
        var userId = UserId();
        if (userId is null) return Unauthorized();
        try { return Ok(await _service.CreateSaleAsync(userId.Value, dto)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    private int? UserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }
}
