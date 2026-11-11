using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStart.Application.DTOs;
using SmartStart.Application.Interfaces;

namespace SmartStart.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("cashiers")]
    public async Task<IActionResult> GetCashiers()
    {
        return Ok(await _userService.GetCashiersAsync());
    }

    [HttpPost("cashiers")]
    public async Task<IActionResult> CreateCashier([FromBody] CreateCashierDto dto)
    {
        try
        {
            var result = await _userService.CreateCashierAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("cashiers/{id:int}")]
    public async Task<IActionResult> UpdateCashier(int id, [FromBody] UpdateCashierDto dto)
    {
        try
        {
            var result = await _userService.UpdateCashierAsync(id, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
