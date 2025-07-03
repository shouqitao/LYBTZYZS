using LYBT.Module.Users.Dtos;
using LYBT.Module.Users;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// 用户管理控制器，提供RESTful API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase {
    private readonly IUserService _userService;

    public UsersController(IUserService userService) {
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
    /// 新增用户，密码将设为配置的默认值
    /// </summary>
    [HttpPost("add")]
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
    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] UserEditDto dto) {
        Guid operatorId = Guid.NewGuid();
        string operatorName = "管理员A";
        var result = await _userService.UpdateAsync(dto, operatorId, operatorName);
        return result ? Ok(new { success = true }) : BadRequest(new { success = false });
    }

    /// <summary>
    /// 禁用用户
    /// </summary>
    [HttpPost("disable/{id}")]
    public async Task<IActionResult> Disable(Guid id) {
        Guid operatorId = Guid.NewGuid();
        string operatorName = "管理员A";
        var result = await _userService.DisableAsync(id, operatorId, operatorName);
        return result ? Ok(new { success = true }) : BadRequest(new { success = false });
    }

    /// <summary>
    /// 启用用户
    /// </summary>
    [HttpPost("enable/{id}")]
    public async Task<IActionResult> Enable(Guid id) {
        Guid operatorId = Guid.NewGuid();
        string operatorName = "管理员A";
        var result = await _userService.EnableAsync(id, operatorId, operatorName);
        return result ? Ok(new { success = true }) : BadRequest(new { success = false });
    }

    /// <summary>
    /// 批量禁用
    /// </summary>
    [HttpPost("batchDisable")]
    public async Task<IActionResult> BatchDisable([FromBody] BatchIdsDto dto) {
        Guid operatorId = Guid.NewGuid();
        string operatorName = "管理员A";
        var count = await _userService.BatchDisableAsync(dto.Ids, operatorId, operatorName);
        return Ok(new { success = true, count });
    }

    /// <summary>
    /// 批量启用
    /// </summary>
    [HttpPost("batchEnable")]
    public async Task<IActionResult> BatchEnable([FromBody] BatchIdsDto dto) {
        Guid operatorId = Guid.NewGuid();
        string operatorName = "管理员A";
        var count = await _userService.BatchEnableAsync(dto.Ids, operatorId, operatorName);
        return Ok(new { success = true, count });
    }

    /// <summary>
    /// 管理员重置密码，需要明确提供新密码
    /// </summary>
    [HttpPost("resetPassword/{id}")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordDto dto) {
        Guid operatorId = Guid.NewGuid();
        string operatorName = "管理员A";
        var result = await _userService.ResetPasswordAsync(id, dto.NewPassword, operatorId, operatorName);
        return result ? Ok(new { success = true }) : BadRequest(new { success = false });
    }

    /// <summary>
    /// 用户修改密码
    /// </summary>
    [HttpPost("changePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto) {
        var result = await _userService.ChangePasswordAsync(dto.UserId, dto.OldPassword, dto.NewPassword);
        return result ? Ok(new { success = true }) : BadRequest(new { success = false });
    }

    /// <summary>
    /// 获取所有角色
    /// </summary>
    [HttpGet("getRoles")]
    public IActionResult GetRoles() {
        var roles = _userService.GetRoles();
        return Ok(roles);
    }

    /// <summary>
    /// 根据Id获取用户详情
    /// </summary>
    [HttpGet("getById/{id}")]
    public async Task<IActionResult> GetById(Guid id) {
        var user = await _userService.GetByIdAsync(id);
        return user == null ? NotFound() : Ok(user);
    }
}