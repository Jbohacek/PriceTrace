using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PriceTrace.Common.Users;
using PriceTrace.Contracts.Models.UserModels;
using PriceTrace.DataEf.UnitOfWork;

namespace PriceTrace.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUnitOfWork unitOfWork, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll()
    {
        var users = await unitOfWork.Users.GetAllAsync();
        return Ok(mapper.Map<IEnumerable<UserResponse>>(users));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return NotFound();

        return Ok(mapper.Map<UserResponse>(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request)
    {
        var user = mapper.Map<UserEntity>(request);

        await unitOfWork.Users.AddAsync(user);
        await unitOfWork.SaveChangesAsync();

        var response = mapper.Map<UserResponse>(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserResponse>> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return NotFound();

        mapper.Map(request, user);

        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync();

        return Ok(mapper.Map<UserResponse>(user));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return NotFound();

        unitOfWork.Users.Remove(user);
        await unitOfWork.SaveChangesAsync();

        return NoContent();
    }
}
