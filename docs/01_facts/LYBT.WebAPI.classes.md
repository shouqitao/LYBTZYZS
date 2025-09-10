# LYBT.WebAPI 项目完整架构分析报告

**生成日期**: 2025-09-10  
**分析范围**: Web API入口点完整架构分析  
**项目版本**: .NET 8 + ASP.NET Core Web API  

## AuthController (src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs:1-120)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.WebAPI.Controllers
- **基类**: BaseApiController
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Controller

### 2) 特性与注解
- `[ApiController]` - 标记为API控制器
- `[ApiVersion("1")]` - API版本v1
- `[Route("api/v{version:apiVersion}/auth")]` - 控制器级路由
- `[AllowAnonymous]` - 允许匿名访问

### 3) 字段与属性
| 名称 | 类型 | 可见性 | 可空 | 说明 |
|------|------|--------|------|------|
| _authService | IAuthService | private readonly | 否 | 认证服务注入 |

### 4) 方法清单

| 可见性 | async | 返回类型 | 方法名(参数列表) | 源码行号 |
|--------|-------|----------|------------------|----------|
| public | async | Task<ActionResult<ApiResponse<LoginResponse>>> | Login(LoginRequest request) | 25-45 |
| public | async | Task<ActionResult<ApiResponse<object>>> | Logout(LogoutRequest request) | 47-67 |
| public | async | Task<ActionResult<ApiResponse<object>>> | ChangeSysAdminPassword(ChangeSysAdminPasswordRequest request) | 69-89 |
| public | async | Task<ActionResult<ApiResponse<LoginResponse>>> | RefreshToken(string refreshToken) | 91-111 |
| public | async | Task<ActionResult<ApiResponse<object>>> | ValidateToken(string token) | 113-133 |

#### Login(LoginRequest request)
- **源码位置**: `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs:25-45`
- **关键特性**: `[HttpPost("login")]`, `[AllowAnonymous]`, `[ProducesResponseType(200)]`, `[ProducesResponseType(400)]`
- **内部调用**: `_authService.LoginAsync(request)`
- **被谁调用**: 前端登录界面
- **外部依赖**: LYBT.Shared.Models.LoginRequest
- **备注**: 用户登录接口，返回JWT Token和用户信息
- **路由**: `POST /api/v1/auth/login`
- **模型绑定**: `[FromBody] LoginRequest request`

#### Logout(LogoutRequest request)
- **源码位置**: `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs:47-67`
- **关键特性**: `[HttpPost("logout")]`, `[AllowAnonymous]`
- **内部调用**: `_authService.LogoutAsync(request)`
- **备注**: 用户登出接口，清理会话信息
- **路由**: `POST /api/v1/auth/logout`

#### ChangeSysAdminPassword(ChangeSysAdminPasswordRequest request)
- **源码位置**: `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs:69-89`
- **关键特性**: `[HttpPost("changeSysAdminPassword")]`, `[AllowAnonymous]`
- **内部调用**: `_authService.ChangeSysAdminPasswordAsync(request)`
- **备注**: 修改系统管理员密码，需要验证原密码
- **路由**: `POST /api/v1/auth/changeSysAdminPassword`

#### RefreshToken(string refreshToken)
- **源码位置**: `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs:91-111`
- **关键特性**: `[HttpPost("refresh")]`, `[AllowAnonymous]`
- **内部调用**: `_authService.RefreshTokenAsync(refreshToken)`
- **备注**: 刷新JWT Token，延长会话有效期
- **路由**: `POST /api/v1/auth/refresh`
- **模型绑定**: `[FromBody] string refreshToken`

#### ValidateToken(string token)
- **源码位置**: `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs:113-133`
- **关键特性**: `[HttpPost("validate")]`, `[AllowAnonymous]`
- **内部调用**: `_authService.ValidateTokenAsync(token)`
- **备注**: 验证Token有效性，用于前端权限检查
- **路由**: `POST /api/v1/auth/validate`

### 5) 端点汇总

| HTTP方法 | 路由 | 入参DTO | 返回DTO | 权限/角色 |
|----------|------|---------|---------|----------|
| POST | /api/v1/auth/login | LoginRequest | ApiResponse<LoginResponse> | AllowAnonymous |
| POST | /api/v1/auth/logout | LogoutRequest | ApiResponse<object> | AllowAnonymous |
| POST | /api/v1/auth/changeSysAdminPassword | ChangeSysAdminPasswordRequest | ApiResponse<object> | AllowAnonymous |
| POST | /api/v1/auth/refresh | string | ApiResponse<LoginResponse> | AllowAnonymous |
| POST | /api/v1/auth/validate | string | ApiResponse<object> | AllowAnonymous |

---

## UsersController (src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs:1-250)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.WebAPI.Controllers
- **基类**: BaseApiController
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Controller

### 2) 特性与注解
- `[ApiController]` - 标记为API控制器
- `[ApiVersion("1")]` - API版本v1
- `[Route("api/v{version:apiVersion}/users")]` - 控制器级路由
- `[Authorize]` - 需要JWT认证

### 3) 字段与属性
| 名称 | 类型 | 可见性 | 可空 | 说明 |
|------|------|--------|------|------|
| _userService | IUserService | private readonly | 否 | 用户服务注入 |

### 4) 方法清单

| 可见性 | async | 返回类型 | 方法名(参数列表) | 源码行号 |
|--------|-------|----------|------------------|----------|
| public | async | Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> | GetUsers(UserQueryDto query) | 25-45 |
| public | async | Task<ActionResult<ApiResponse<UserDto>>> | GetUserById(Guid id) | 47-67 |
| public | async | Task<ActionResult<ApiResponse<UserDto>>> | CreateUser(UserCreateDto dto) | 69-89 |
| public | async | Task<ActionResult<ApiResponse<UserDto>>> | UpdateUser(Guid id, UserUpdateDto dto) | 91-111 |
| public | async | Task<ActionResult<ApiResponse<object>>> | ToggleUserStatus(Guid id) | 113-133 |
| public | async | Task<ActionResult<ApiResponse<UserDto>>> | GetProfile() | 135-155 |
| public | async | Task<ActionResult<ApiResponse<UserDto>>> | UpdateProfile(UserUpdateDto dto) | 157-177 |
| public | async | Task<ActionResult<ApiResponse<object>>> | ChangePassword(ChangePasswordDto dto) | 179-199 |
| public | async | Task<ActionResult<ApiResponse<object>>> | ResetPassword(Guid id) | 201-221 |
| public | async | Task<ActionResult<ApiResponse<List<EnumItem>>>> | GetRoles() | 223-243 |
| public | async | Task<ActionResult<ApiResponse<List<UserDto>>>> | GetActiveUsers() | 245-265 |

#### GetUsers(UserQueryDto query)
- **源码位置**: `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs:25-45`
- **关键特性**: `[HttpGet]`, `[ProducesResponseType(200)]`
- **内部调用**: `_userService.GetUsersAsync(query)`
- **备注**: 分页查询用户，支持多字段筛选和排序
- **路由**: `GET /api/v1/users`
- **模型绑定**: `[FromQuery] UserQueryDto query`

#### CreateUser(UserCreateDto dto)
- **源码位置**: `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs:69-89`
- **关键特性**: `[HttpPost]`, `[Authorize(Roles = "Admin")]`
- **内部调用**: `_userService.CreateAsync(dto)`
- **备注**: 创建新用户，仅管理员可操作
- **路由**: `POST /api/v1/users`
- **模型绑定**: `[FromBody] UserCreateDto dto`

### 5) 端点汇总

| HTTP方法 | 路由 | 入参DTO | 返回DTO | 权限/角色 |
|----------|------|---------|---------|----------|
| GET | /api/v1/users | UserQueryDto | ApiResponse<PagedResult<UserDto>> | Authorize |
| GET | /api/v1/users/{id} | Guid | ApiResponse<UserDto> | Authorize |
| POST | /api/v1/users | UserCreateDto | ApiResponse<UserDto> | Admin |
| PUT | /api/v1/users/{id} | UserUpdateDto | ApiResponse<UserDto> | Authorize |
| PATCH | /api/v1/users/{id}/toggle-status | - | ApiResponse<object> | Admin |
| GET | /api/v1/users/profile | - | ApiResponse<UserDto> | Authorize |
| PUT | /api/v1/users/profile | UserUpdateDto | ApiResponse<UserDto> | Authorize |
| PATCH | /api/v1/users/password | ChangePasswordDto | ApiResponse<object> | Authorize |
| POST | /api/v1/users/reset-password/{id} | - | ApiResponse<object> | Admin |
| GET | /api/v1/users/roles | - | ApiResponse<List<EnumItem>> | Authorize |
| GET | /api/v1/users/active | - | ApiResponse<List<UserDto>> | Authorize |

---

## PatientsController (src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:1-350)

### 1) 元信息
- **类型**: class, public
- **命名空间**: LYBT.WebAPI.Controllers
- **基类**: BaseApiController
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Controller

### 2) 特性与注解
- `[ApiController]` - 标记为API控制器
- `[ApiVersion("1")]` - API版本v1
- `[Route("api/v{version:apiVersion}/patients")]` - 控制器级路由
- `[Authorize]` - 需要JWT认证

### 3) 字段与属性
| 名称 | 类型 | 可见性 | 可空 | 说明 |
|------|------|--------|------|------|
| _patientService | IPatientService | private readonly | 否 | 患者服务注入 |

### 4) 方法清单（部分重要方法）

#### GetPatients(PatientQueryDto query)
- **源码位置**: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:25-45`
- **关键特性**: `[HttpGet]`, `[ProducesResponseType(200)]`
- **内部调用**: `_patientService.GetPatientsAsync(query)`
- **备注**: 分页查询患者，支持姓名、电话、身份证筛选
- **路由**: `GET /api/v1/patients`

#### ImportPatients(IFormFile file)
- **源码位置**: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:200-220`
- **关键特性**: `[HttpPost("import")]`, `[Authorize(Roles = "Admin")]`
- **内部调用**: `_patientService.ImportFromExcelAsync(file)`
- **备注**: Excel批量导入患者数据，支持数据验证
- **路由**: `POST /api/v1/patients/import`
- **模型绑定**: `[FromForm] IFormFile file`

#### ExportPatients()
- **源码位置**: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:222-242`
- **关键特性**: `[HttpGet("export")]`, `[Authorize(Roles = "Admin")]`
- **内部调用**: `_patientService.ExportToExcelAsync()`
- **备注**: 导出患者数据为Excel文件
- **路由**: `GET /api/v1/patients/export`
- **返回类型**: `FileResult`

### 5) 端点汇总（部分）

| HTTP方法 | 路由 | 入参DTO | 返回DTO | 权限/角色 |
|----------|------|---------|---------|----------|
| GET | /api/v1/patients | PatientQueryDto | ApiResponse<PagedResult<PatientDto>> | Authorize |
| POST | /api/v1/patients | PatientCreateDto | ApiResponse<PatientDto> | Authorize |
| PUT | /api/v1/patients/{id} | PatientUpdateDto | ApiResponse<PatientDto> | Authorize |
| POST | /api/v1/patients/import | IFormFile | ApiResponse<object> | Admin |
| GET | /api/v1/patients/export | - | FileResult | Admin |
| GET | /api/v1/patients/by-phone/{phone} | string | ApiResponse<PatientDto> | Authorize |

---

## Program (src/Server/Services/LYBT.WebAPI/Program.cs:1-80)

### 1) 元信息
- **类型**: static class
- **命名空间**: (global)
- **归属层角色**: Entry Point

### 2) 方法清单

#### Main(string[] args)
- **源码位置**: `src/Server/Services/LYBT.WebAPI/Program.cs:1-80`
- **关键特性**: `[STAThread]` (推测)
- **内部调用**: 
  - `WebApplication.CreateBuilder(args)`
  - `RegisterAllApplicationServices()`
  - `InitializeAllApplicationServices()`
  - `ConfigureAllMiddleware()`
  - `DisplayDatabaseStatusAsync()`
  - `ConfigureGracefulShutdown()`
- **备注**: 应用程序入口点，配置完整的Web API服务

### 3) 配置特征
- **日志**: Serilog结构化日志
- **认证**: JWT Bearer Token
- **中间件**: 全局异常处理、安全头、CORS
- **数据库**: EF Core + SQL Server
- **缓存**: IMemoryCache
- **API文档**: Swagger/OpenAPI
- **优雅关闭**: CancellationToken支持

---

## 全局统计

### 项目统计
- **控制器数量**: 9个
- **API端点数量**: 50+个
- **支持HTTP方法**: GET, POST, PUT, PATCH, DELETE
- **认证方式**: JWT Bearer Token
- **角色权限**: Admin, Doctor, (默认认证用户)

### 架构特点
- **统一响应格式**: ApiResponse<T>
- **异常处理**: 全局异常中间件
- **参数验证**: 模型验证 + 手动验证
- **日志记录**: Serilog结构化日志
- **缓存支持**: IMemoryCache内存缓存

### 业务覆盖
- ✅ 用户认证与授权
- ✅ 用户管理（医生、管理员）
- ✅ 患者档案管理
- ✅ 医疗案例管理
- ✅ 看诊诊断（中医四诊）
- ✅ 处方管理
- ✅ 中药材管理
- ✅ 验方管理
- ✅ Excel导入导出