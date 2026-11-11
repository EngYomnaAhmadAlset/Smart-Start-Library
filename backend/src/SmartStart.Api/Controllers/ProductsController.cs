using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStart.Application.DTOs;
using SmartStart.Application.Interfaces;

namespace SmartStart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _productService.SearchProductsAsync(null));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _productService.GetByIdAsync(id);
        return result != null ? Ok(result) : NotFound("المنتج غير موجود");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        try
        {
            var result = await _productService.CreateProductAsync(dto);
            return CreatedAtAction(nameof(GetByBarcode), new { barcode = result.Barcode }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
    {
        try
        {
            return Ok(await _productService.UpdateProductAsync(id, dto));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _productService.DeleteProductAsync(id);
            return Ok(new { message = "تم حذف المنتج" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("generate-barcodes")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GenerateBarcodes([FromBody] GenerateBarcodesDto dto)
    {
        try
        {
            var codes = await _productService.GenerateBarcodesAsync(dto.Count);
            return Ok(codes);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("suggest-price")]
    [Authorize(Roles = "Admin")]
    public IActionResult SuggestPrice([FromQuery] decimal wholesalePrice, [FromQuery] decimal percent)
    {
        try
        {
            var price = _productService.SuggestSellingPrice(wholesalePrice, percent);
            return Ok(new { wholesalePrice, percent, sellingPrice = price });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("import")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Import([FromBody] IReadOnlyList<ImportProductRowDto> rows)
    {
        try
        {
            return Ok(await _productService.ImportProductsAsync(rows));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("restock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Restock([FromBody] RestockProductDto dto)
    {
        try
        {
            return Ok(await _productService.RestockProductAsync(dto));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/barcode")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangeBarcode(int id, [FromBody] ChangeBarcodeDto dto)
    {
        try
        {
            int? userId = null;
            var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idValue, out var parsed))
                userId = parsed;

            return Ok(await _productService.ChangeBarcodeAsync(id, dto, userId));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/barcode-changes")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetBarcodeChanges(int id)
    {
        try
        {
            return Ok(await _productService.GetBarcodeChangesAsync(id));
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetHistory(int id)
    {
        var result = await _productService.GetHistoryAsync(id);
        return result != null ? Ok(result) : NotFound("المنتج غير موجود");
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<IActionResult> GetByBarcode(string barcode)
    {
        var result = await _productService.GetByBarcodeAsync(barcode);
        return result != null ? Ok(result) : NotFound("المنتج غير موجود");
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q)
    {
        return Ok(await _productService.SearchProductsAsync(q));
    }

    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetLowStock()
    {
        return Ok(await _productService.GetLowStockProductsAsync());
    }
}
