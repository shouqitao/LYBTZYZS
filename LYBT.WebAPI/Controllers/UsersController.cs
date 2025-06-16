using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using LYBT.Module.Users.Dtos;

/// <summary>
/// 用户管理控制器，提供RESTful API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase {
    private readonly IUserService _userService;

    public UserController(IUserService userService) {
        _userService = userService;
    }

    /// <summary>
    /// 分页查找用户（关键词、角色、状态筛选）
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] UserQueryDto query) {
        var (users, total) = await _userService.SearchAsync(query);
        return Ok(new { total, users });
    }

    /// <summary>
    /// 新增用户
    /// </summary>
    [HttpPost("AddUser")]
    public async Task<IActionResult> Add([FromBody] UserCreateDto dto) {
        // 从Token/Session等获取操作人信息
        Guid operatorId = Guid.NewGuid(); // 实际开发应取登录管理员ID
        string operatorName = "管理员A";
        var result = await _userService.AddAsync(dto, operatorId, operatorName);
        return result ? Ok(new { success = true }) : BadRequest(new { success = false });
    }

    /// <summary>
    /// 编辑用户
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UserEditDto dto) {
        Guid operatorId = Guid.NewGuid();
        string operatorName = "管理员A";
        dto.Id = id;
        var result = await _userService.UpdateAsync(dto, operatorId, operatorName);
        return result ? Ok(new { success = true }) : BadRequest(new { success = false });
    }

    /// <summary>
    /// 禁用用户
    /// </summary>
    [HttpPut("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id) {
        Guid operatorId = Guid.NewGuid();
        string operatorName = "管理员A";
        var result = await _userService.DisableAsync(id, operatorId, operatorName);
        return result ? Ok(new { success = true }) : BadRequest(new { success = false });
    }

    /// <summary>
    /// 启用用户
    /// </summary>
    [HttpPut("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id) {
        Guid operatorId = Guid.NewGuid();
        string operatorName = "管理员A";
        var result = await _userService.EnableAsync(id, operatorId, operatorName);
        return result ? Ok(new { success = true }) : BadRequest(new { success = false });
    }
}
