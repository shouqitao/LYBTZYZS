# Consultation 模块优化方案

## 优化概览

**模块名称**: 看诊管理（Consultation）  
**当前状态**: 功能完整但接口风格不统一  
**优化目标**: 符合 RESTful 规范，提高 API 一致性

## 现状分析

### 当前接口列表

1. **POST /api/v1/consultation/paged** - 分页查询看诊记录 ❌ (应使用 GET)
2. **GET /api/v1/consultation/{id}** - 获取看诊详情 ✅
3. **GET /api/v1/consultation/medical-case/{medicalCaseId}** - 根据医疗案例获取 ✅
4. **POST /api/v1/consultation/start** - 开始看诊 ✅
5. **PUT /api/v1/consultation/{id}** - 更新看诊信息 ✅
6. **POST /api/v1/consultation/{id}/complete** - 完成看诊 ✅
7. **GET /api/v1/consultation/doctor/{doctorId}/today** - 获取医生今日看诊 ✅
8. **GET /api/v1/consultation/patient/{patientId}/history** - 获取患者历史 ✅
9. **GET /api/v1/consultation/doctor/{doctorId}/count** - 统计看诊数量 ✅
10. **DELETE /api/v1/consultation/{id}** - 删除看诊记录（软删除）✅

### 发现的问题

1. **分页查询使用 POST**: 违反 RESTful 规范
2. **缺少标准 GET 列表接口**: 应该有 GET /api/v1/consultation
3. **缺少状态切换接口**: 看诊有多种状态，需要统一管理
4. **响应格式不统一**: 部分接口返回包装对象，部分直接返回

## 优化方案

### 1. 接口优化

#### 1.1 修改分页查询接口

**修改前**:
```csharp
[HttpPost("paged")]
public async Task<IActionResult> GetPaged([FromBody] ConsultationPagedQueryDto query)
```

**修改后**:
```csharp
[HttpGet]
public async Task<IActionResult> GetConsultations(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? keyword = null,
    [FromQuery] Guid? doctorId = null,
    [FromQuery] Guid? patientId = null,
    [FromQuery] DateTime? startDate = null,
    [FromQuery] DateTime? endDate = null,
    [FromQuery] ConsultationStatus? status = null)
```

#### 1.2 添加状态管理接口

```csharp
[HttpPost("{id}/update-status")]
public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusDto dto)
{
    // 支持状态流转：待看诊 → 看诊中 → 已完成/已取消
}
```

#### 1.3 统一响应格式

- GET 请求: 直接返回数据
- POST/PUT: 返回创建/更新的对象
- DELETE: 返回 `{ message: "操作成功" }`
- 错误: 使用 ProblemDetails

### 2. 前端服务更新

#### 2.1 更新 IConsultationApiService

```csharp
public interface IConsultationApiService
{
    // 修改：使用 GET 请求进行分页查询
    [Get("/api/v1/consultation")]
    Task<PaginatedResult<ConsultationDto>> GetConsultationsAsync(
        [Query] int page = 1,
        [Query] int pageSize = 10,
        [Query] string? keyword = null,
        [Query] Guid? doctorId = null,
        [Query] Guid? patientId = null,
        [Query] DateTime? startDate = null,
        [Query] DateTime? endDate = null,
        [Query] ConsultationStatus? status = null);
        
    // 新增：状态更新接口
    [Post("/api/v1/consultation/{id}/update-status")]
    Task<ConsultationDto> UpdateStatusAsync(Guid id, [Body] UpdateStatusDto dto);
}
```

#### 2.2 更新 ConsultationService

- 修改 GetPagedAsync 方法适配新接口
- 添加 UpdateStatusAsync 方法
- 统一错误处理逻辑

### 3. 数据模型优化

#### 3.1 看诊状态枚举

```csharp
public enum ConsultationStatus
{
    Waiting = 0,      // 待看诊
    InProgress = 1,   // 看诊中
    Completed = 2,    // 已完成
    Cancelled = 3     // 已取消
}
```

#### 3.2 状态流转规则

```
待看诊 → 看诊中 → 已完成
  ↓        ↓
已取消   已取消
```

### 4. 测试计划

#### 4.1 单元测试

- ConsultationController 所有接口
- ConsultationService 业务逻辑
- 状态流转规则验证

#### 4.2 集成测试

- 完整看诊流程
- 与处方模块的集成
- 与缴费模块的联动

### 5. 实施步骤

1. **后端改造** (2小时)
   - 修改分页查询接口
   - 添加状态管理接口
   - 统一响应格式

2. **前端更新** (2小时)
   - 更新 API 服务接口
   - 修改服务层实现
   - 更新相关 ViewModel

3. **测试编写** (2小时)
   - 编写单元测试
   - 更新集成测试

4. **文档更新** (1小时)
   - 更新 API 文档
   - 更新开发指南

## 预期效果

- ✅ 100% 符合 RESTful 规范
- ✅ 接口一致性提高
- ✅ 状态管理更加清晰
- ✅ 前端调用更加简单
- ✅ 测试覆盖率提升

## 风险评估

1. **兼容性风险**: 低 - 保留原有功能，只是优化接口
2. **性能风险**: 低 - GET 请求可以利用缓存
3. **测试风险**: 中 - 需要全面测试看诊流程