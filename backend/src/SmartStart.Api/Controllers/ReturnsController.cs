using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStart.Application.DTOs;
using SmartStart.Application.Interfaces;

namespace SmartStart.Api.Controllers;

[ApiController]
[Route("api/returns")]
[Authorize]
public class ReturnsController : ControllerBase
{
    private readonly IReturnService _returnService;

    public ReturnsController(IReturnService returnService)
    {
        _returnService = returnService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> List([FromQuery] int hours = 24)
    {
        var since = DateTime.UtcNow.AddHours(-Math.Max(1, hours));
        return Ok(await _returnService.GetSinceAsync(since));
    }

    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "أدخل اسم المنتج أو الباركود" });

        var result = await _returnService.PreviewAsync(q);
        return result != null ? Ok(result) : NotFound(new { message = "لا يوجد بيع قابل للإرجاع لهذا المنتج خلال 24 ساعة" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReturnDto dto)
    {
        try
        {
            return Ok(await _returnService.ProcessReturnAsync(dto));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("by-query")]
    public async Task<IActionResult> CreateByQuery([FromBody] CreateReturnByQueryDto dto)
    {
        try
        {
            return Ok(await _returnService.ProcessByQueryAsync(dto));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
