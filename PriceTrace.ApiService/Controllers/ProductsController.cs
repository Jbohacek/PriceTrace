using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PriceTrace.ApiService.Models;
using PriceTrace.Common.Products;
using PriceTrace.DataEf.UnitOfWork;

namespace PriceTrace.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductsController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll()
    {
        var products = await _unitOfWork.Products.GetAllAsync();
        return Ok(_mapper.Map<IEnumerable<ProductResponse>>(products));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product is null)
            return NotFound();

        return Ok(_mapper.Map<ProductResponse>(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] CreateProductRequest request)
    {
        var product = _mapper.Map<ProductEntity>(request);

        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        var response = _mapper.Map<ProductResponse>(product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product is null)
            return NotFound();

        _mapper.Map(request, product);
        product.UpdatedDate = DateTime.Now;

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync();

        return Ok(_mapper.Map<ProductResponse>(product));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product is null)
            return NotFound();

        _unitOfWork.Products.Remove(product);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }
}
