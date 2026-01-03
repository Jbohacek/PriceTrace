using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PriceTrace.ApiService.Models;
using PriceTrace.Common.Users;
using PriceTrace.DataEf.UnitOfWork;

namespace PriceTrace.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UsersController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return Ok(_mapper.Map<IEnumerable<UserResponse>>(users));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return NotFound();

        return Ok(_mapper.Map<UserResponse>(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request)
    {
        var user = _mapper.Map<UserEntity>(request);

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var response = _mapper.Map<UserResponse>(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserResponse>> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return NotFound();

        _mapper.Map(request, user);
        user.UpdatedDate = DateTime.Now;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(_mapper.Map<UserResponse>(user));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return NotFound();

        _unitOfWork.Users.Remove(user);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }
}
