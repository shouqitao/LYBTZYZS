# API路由规范分析报告

## 一、当前各模块路由对比

### 1. 用户模块 (UsersController)
```
POST   /users/paged           - 分页查询 ✅
POST   /users/add             - 新增用户 ✅
PUT    /users/update          - 更新用户 ⚠️ (应该是 PUT /users/{id})
POST   /users/disable/{id}    - 禁用用户 ⚠️ (应该是 PATCH)
POST   /users/enable/{id}     - 启用用户 ⚠️ (应该是 PATCH)
GET    /users/{id}            - 根据ID获取 ✅
POST   /users                 - 创建用户 (标准REST)
PUT    /users/{id}            - 更新用户 (标准REST)
DELETE /users/{id}            - 删除用户 ❌ (违反软删除策略)
```

### 2. 患者模块 (PatientsController)
```
POST   /patients              - 新增患者 ⚠️ (应该是 POST /patients/add)
PUT    /patients/{id}         - 更新患者 ✅
PATCH  /patients/{id}/enable  - 启用患者 ✅
PATCH  /patients/{id}/disable - 禁用患者 ✅
```

### 3. 药材模块 (HerbsController)
```
GET    /herbs                 - 获取列表 ✅
POST   /herbs/paged           - 分页查询 ✅
GET    /herbs/{id}            - 根据ID获取 ✅
POST   /herbs                 - 新增药材 ⚠️ (应该是 POST /herbs/add)
PUT    /herbs                 - 更新药材 ⚠️ (应该是 PUT /herbs/{id})
DELETE /herbs/{id}            - 删除药材 ❌ (违反软删除策略)
PATCH  /herbs/batch-status    - 批量更新状态 ✅
```

### 4. 医生模块 (DoctorsController)
```
POST   /doctors/paged         - 分页查询 ✅
GET    /doctors/search        - 搜索医生 ⚠️
GET    /doctors/active        - 获取活跃医生 ⚠️
GET    /doctors/{id}          - 根据ID获取 ✅
POST   /doctors               - 新增医生 ⚠️ (应该是 POST /doctors/add)
PUT    /doctors               - 更新医生 ⚠️ (应该是 PUT /doctors/{id})
PATCH  /doctors/{id}/disable  - 禁用医生 ✅
PATCH  /doctors/{id}/enable   - 启用医生 ✅
```

## 二、发现的问题

### 1. HTTP方法使用不一致
- **启用/禁用操作**：
  - ✅ 正确：患者、医生模块使用 `PATCH`
  - ❌ 错误：用户模块使用 `POST`

### 2. 路由命名不一致
- **新增操作**：
  - ✅ 规范：用户模块 `POST /users/add`
  - ❌ 不规范：患者、药材、医生模块直接 `POST /xxx`

- **更新操作**：
  - ✅ 规范：患者模块 `PUT /patients/{id}`
  - ❌ 不规范：药材、医生模块 `PUT /xxx` (缺少ID)

### 3. 违反软删除策略
- 用户、药材模块仍有 `DELETE` 方法
- 应该只使用 `enable/disable` 操作

### 4. 查询接口不统一
- 用户、医生、药材：使用 `POST /xxx/paged`
- 医生模块额外的：`GET /search`, `GET /active`

## 三、建议的统一API路由规范

### 基础CRUD操作
```
POST   /{controller}/paged         - 分页查询（带条件）
POST   /{controller}/add           - 新增资源
GET    /{controller}/{id}          - 根据ID获取
PUT    /{controller}/{id}          - 更新资源
PATCH  /{controller}/{id}/enable   - 启用资源
PATCH  /{controller}/{id}/disable  - 禁用资源
```

### 批量操作
```
POST   /{controller}/batch-add     - 批量新增
PATCH  /{controller}/batch-enable  - 批量启用
PATCH  /{controller}/batch-disable - 批量禁用
```

### 特殊查询
```
GET    /{controller}/active        - 获取活跃资源列表
POST   /{controller}/search        - 高级搜索（复杂条件）
GET    /{controller}/statistics    - 获取统计信息
```

## 四、修复建议

### 1. 高优先级修复
1. 移除所有 `DELETE` 方法
2. 统一启用/禁用使用 `PATCH` 方法
3. 统一新增操作使用 `POST /{controller}/add`

### 2. 中优先级修复
1. 统一更新操作使用 `PUT /{controller}/{id}`
2. 整合查询接口，避免过多的特殊查询端点

### 3. 低优先级优化
1. 考虑使用 RESTful 风格，但保持项目一致性
2. 添加 API 版本管理策略文档

## 五、示例：标准化的控制器模板

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class StandardController : ControllerBase
{
    // 查询
    [HttpPost("paged")]
    public async Task<IActionResult> GetPaged([FromBody] QueryDto query) { }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) { }
    
    [HttpGet("active")]
    public async Task<IActionResult> GetActive() { }
    
    // 创建
    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] CreateDto dto) { }
    
    // 更新
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDto dto) { }
    
    // 软删除
    [HttpPatch("{id}/enable")]
    public async Task<IActionResult> Enable(Guid id) { }
    
    [HttpPatch("{id}/disable")]
    public async Task<IActionResult> Disable(Guid id) { }
    
    // 批量操作
    [HttpPatch("batch-enable")]
    public async Task<IActionResult> BatchEnable([FromBody] BatchOperationDto dto) { }
    
    [HttpPatch("batch-disable")]
    public async Task<IActionResult> BatchDisable([FromBody] BatchOperationDto dto) { }
}
```

## 六、实施计划

1. **第一阶段**：创建基础控制器类，提取公共方法
2. **第二阶段**：逐个模块修改路由，保持向后兼容
3. **第三阶段**：更新前端调用，移除旧路由
4. **第四阶段**：更新API文档和测试用例