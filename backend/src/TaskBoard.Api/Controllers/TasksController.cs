using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Tasks;

namespace TaskBoard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController : ApiControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _taskService.ListAsync(userId, cancellationToken);

        return FromResult(result, Ok);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _taskService.GetByIdAsync(userId, id, cancellationToken);

        return FromResult(result, Ok);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _taskService.CreateAsync(userId, request, cancellationToken);

        return FromResult(result, value => CreatedAtAction(nameof(Get), new { id = value.Id }, value));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _taskService.UpdateAsync(userId, id, request, cancellationToken);

        return FromResult(result, Ok);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _taskService.DeleteAsync(userId, id, cancellationToken);

        return FromResult(result, NoContent);
    }
}
