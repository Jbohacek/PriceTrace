using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PriceTrace.Common.Products;
using PriceTrace.Contracts.Models.ProductModels;
using PriceTrace.DataEf.UnitOfWork;

namespace PriceTrace.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IUnitOfWork unitOfWork, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll()
    {
        var products = await unitOfWork.Products.GetAllAsync();
        return Ok(mapper.Map<IEnumerable<ProductResponse>>(products));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id)
    {
        var product = await unitOfWork.Products.GetByIdAsync(id);
        if (product is null)
            return NotFound();

        return Ok(mapper.Map<ProductResponse>(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] CreateProductRequest request)
    {
        var product = mapper.Map<ProductEntity>(request);

        await unitOfWork.Products.AddAsync(product);
        await unitOfWork.SaveChangesAsync();

        var response = mapper.Map<ProductResponse>(product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        var product = await unitOfWork.Products.GetByIdAsync(id);
        if (product is null)
            return NotFound();

        mapper.Map(request, product);
        product.UpdatedDate = DateTime.Now;

        unitOfWork.Products.Update(product);
        await unitOfWork.SaveChangesAsync();

        return Ok(mapper.Map<ProductResponse>(product));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await unitOfWork.Products.GetByIdAsync(id);
        if (product is null)
            return NotFound();

        unitOfWork.Products.Remove(product);
        await unitOfWork.SaveChangesAsync();

        return NoContent();
    }
}
