# MedicalCase模块设计 - Server端

## 📋 模块概述
**职责**：病历管理（聚合根）、诊疗流程控制、医疗档案管理  
**命名空间**：`LYBT.Module.MedicalCase`  
**API路径**：`/api/v1/medicalcases/*`  
**核心定位**：作为系统的**聚合根（Aggregate Root）**，管理整个诊疗流程，包含诊疗记录（Consultation）和处方（Prescription）

## 🏗️ 架构设计

### 分层结构
```
├── Controllers/           # HTTP控制器 (WebAPI项目)
│   └── MedicalCaseController.cs
├── Services/             # 业务服务
│   └── MedicalCaseService.cs
├── Interfaces/           # 服务接口
│   ├── IMedicalCaseService.cs
│   └── IMedicalCaseRepository.cs
├── Repositories/         # 数据仓储
│   └── MedicalCaseRepository.cs
├── Mapping/             # AutoMapper配置
│   └── MedicalCaseMappingProfile.cs
└── MedicalCaseModule.cs  # 模块注册
```

### 设计理念
- **聚合根模式**：MedicalCase作为聚合根，管理完整诊疗流程
- **Record-Only模式**：简化状态管理，仅记录数据，无复杂流程控制
- **一病案一诊断**：每个病例对应一次诊疗记录 (1:1关系)
- **一病案至多一处方**：每个病例最多关联一张处方 (0..1关系)

## 🔌 API接口设计

### GET /api/v1/medicalcases
**功能**：分页查询医疗案例
```csharp
// Request Query Parameters
{
  "page": 1,
  "pageSize": 20,
  "keyword": "患者姓名或医生姓名"
}

// Response 200
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "guid",
        "patientId": "guid",
        "patientName": "张三",
        "doctorId": "guid", 
        "doctorName": "李医生",
        "consultationId": "guid",
        "prescriptionId": "guid",
        "consultationDate": "2024-12-31T10:00:00Z",
        "caseStatus": "Active",
        "remark": "备注信息",
        "createdAt": "2024-12-31T09:00:00Z"
      }
    ],
    "totalCount": 100,
    "currentPage": 1,
    "pageSize": 20
  }
}
```

### GET /api/v1/medicalcases/{id}
**功能**：根据ID获取医疗案例详情
```csharp
// Response 200
{
  "success": true,
  "data": {
    "id": "guid",
    "patientId": "guid",
    "patientName": "张三",
    "doctorId": "guid",
    "doctorName": "李医生",
    "consultationDate": "2024-12-31T10:00:00Z",
    "caseStatus": "Active",
    "remark": "初诊，主诉头痛",
    // 业务方法
    "priority": 2,
    "isUrgent": false,
    "needsDoctorAttention": true,
    "canStartConsultation": true
  }
}

// Response 404
{
  "success": false,
  "message": "医疗案例不存在"
}
```

### POST /api/v1/medicalcases
**功能**：创建新的医疗案例
```csharp
// Request
{
  "patientId": "guid",
  "doctorId": "guid",
  "diagnosisSummary": "初步诊断摘要",
  "remark": "备注信息"
}

// Response 201
{
  "success": true,
  "data": {
    "id": "newly-created-guid",
    "patientId": "guid",
    "doctorId": "guid",
    "consultationDate": "2024-12-31T10:00:00Z",
    "caseStatus": "Active"
  },
  "message": "医疗案例创建成功"
}
```

### PUT /api/v1/medicalcases/{id}
**功能**：更新医疗案例
```csharp
// Request
{
  "id": "guid",
  "patientId": "guid",
  "doctorId": "guid",
  "diagnosisSummary": "更新的诊断摘要",
  "chiefComplaint": "主诉",
  "presentIllness": "现病史",
  "pastHistory": "既往史",
  "diagnosisResult": "诊断结果",
  "treatmentPlan": "治疗方案",
  "status": "Active",
  "remark": "更新的备注"
}

// Response 200
{
  "success": true,
  "data": { /* 更新后的医案数据 */ },
  "message": "医疗案例更新成功"
}
```

### DELETE /api/v1/medicalcases/{id}
**功能**：删除医疗案例（软删除）
```csharp
// Response 200
{
  "success": true,
  "message": "删除成功"
}
```

## 🔧 核心服务

### IMedicalCaseService（实际实现）
**职责**：医疗案例业务逻辑，包含聚合操作
```csharp
public interface IMedicalCaseService
{
    // 基础CRUD操作
    Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(
        int page = 1, int pageSize = 20, string? keyword = null);
    Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto);
    Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId);
    
    // 聚合操作（重要）
    Task<ServiceResult<MedicalCaseDto>> CreateWithDetailsAsync(
        MedicalCaseCreateDto caseDto, 
        ConsultationCreateDto consultationDto, 
        PrescriptionCreateDto prescriptionDto = null);
    Task<ServiceResult<MedicalCaseDetailDto>> GetByIdWithDetailsAsync(Guid id);
}
```

### IMedicalCaseRepository
**职责**：医疗案例数据访问
```csharp
public interface IMedicalCaseRepository : IRepository<MedicalCaseEntity>
{
    Task<List<MedicalCaseEntity>> GetByPatientIdAsync(Guid patientId);
    Task<MedicalCaseEntity> GetByIdWithDetailsAsync(Guid id);
    Task<PagedResult<MedicalCaseEntity>> GetPagedWithDetailsAsync(
        int pageNumber, int pageSize, string keyword = null);
    Task<List<MedicalCaseEntity>> GetByDoctorIdAsync(Guid doctorId);
}
```

## 📊 数据模型与实体

### 核心实体：MedicalCase（聚合根）
**数据表**：`MedicalCases`
```csharp
public class MedicalCase : BaseEntity  // 继承BaseEntity提供审计字段
{
    // 基础属性
    public Guid PatientId { get; set; }           // 患者ID
    public string PatientName { get; set; }       // 患者姓名（冗余存储）
    public Guid DoctorId { get; set; }            // 医生ID（非UserId）
    public string DoctorName { get; set; }        // 医生姓名（冗余存储）
    public DateTime ConsultationDate { get; set; } // 诊疗时间
    public MedicalCaseStatus Status { get; set; }  // 状态
    public string? Remark { get; set; }           // 备注
    
    // 导航属性（聚合关系）
    public virtual Consultation? Consultation { get; set; }  // 诊疗记录 (1:1)
    public virtual Prescription? Prescription { get; set; }  // 处方信息 (1:0..1)
    
    // 业务方法（实际实现）
    public bool CanEdit(bool isAdmin, Guid? currentUserId = null)
    {
        if (isAdmin) return true;
        if (currentUserId.HasValue && DoctorId == currentUserId.Value)
        {
            return CreatedAt.Date == DateTime.Today; // 当天可改
        }
        return false;
    }
    
    public bool IsLocked => CreatedAt.Date < DateTime.Today; // 过期锁定
}
```

### 状态枚举：MedicalCaseStatus（实际实现）
```csharp
public enum MedicalCaseStatus
{
    Active = 0,     // 活动状态（默认）
    Completed = 1,  // 已完成
    Cancelled = 2   // 已取消
}
```

### DTO模型层次结构
```
MedicalCaseDto (基础DTO)
├── MedicalCaseDetailDto (详情DTO)
├── MedicalCaseCreateDto (创建DTO)
├── MedicalCaseEditDto (编辑DTO)
├── MedicalCaseUpdateDto (更新DTO)
├── MedicalCaseQueryDto (查询DTO)
└── MedicalCaseSearchDto (搜索DTO)
```

## 🔄 数据验证规则

### 输入验证
- **PatientId**: 必填，有效GUID
- **DoctorId**: 必填，有效GUID
- **PatientName**: 必填，最大50字符
- **DoctorName**: 必填，最大50字符
- **Remark**: 可选，最大500字符
- **DiagnosisSummary**: 可选，最大200字符

### 业务验证
- **患者存在性**: 验证PatientId对应的患者记录存在
- **医生存在性**: 验证DoctorId对应的医生记录存在
- **状态转换**: 仅允许Active ↔ Closed状态转换
- **关联约束**: 删除时检查是否存在关联的诊疗记录或处方

### 数据完整性
- **一病案一诊断**: 一个MedicalCase最多关联一个Consultation
- **一病案至多一处方**: 一个MedicalCase最多关联一个Prescription
- **软删除**: 使用IsDeleted标记，保留历史数据

## 🛡️ 权限与安全

### 访问控制
- **认证要求**: 所有API需要JWT认证
- **角色权限**: 
  - 医生：可访问自己的病例 + 查看权限内的其他病例
  - 护士：可协助录入和查看
  - 管理员：完全访问权限

### 数据安全
- **敏感信息保护**: 患者隐私信息加密存储
- **操作审计**: 继承BaseEntity，自动记录创建/修改信息
- **数据脱敏**: 日志中屏蔽患者敏感信息

### API安全
- **输入验证**: 所有输入参数严格验证
- **SQL注入防护**: 使用EF Core参数化查询
- **授权检查**: 基于角色和数据所有权的访问控制

## 📌 架构修正记录

### 2025-01-09: Issue #1093 - 聚合根模式强化
**问题**：Desktop端代码以Consultation为中心创建诊疗记录，违反了聚合根架构原则。

**修正内容**：
1. **Server端强化聚合根创建**：
   - 修改 `MedicalCaseService.CreateAsync()`：创建MedicalCase时自动创建关联的Consultation（共享主键：`Consultation.Id == MedicalCase.Id`）
   - 修改 `ConsultationService.CreateAsync()`：添加聚合根校验和一对一约束校验，防止孤立创建Consultation
   - 标记 `ConsultationController.CreateAsync()` 为 `[Obsolete]`：引导开发者通过MedicalCaseController创建

2. **Desktop端修正业务流程**：
   - 修改 `ConsultationMainViewModel`：从"创建Consultation"改为"加载并更新Consultation"
   - 移除 `MedicalCaseId ?? Guid.NewGuid()` 回退逻辑
   - 添加 `LoadExistingConsultationAsync()` 方法：加载MedicalCase创建时自动生成的Consultation

3. **架构约束强化**：
   - 共享主键约束：Consultation.Id 必须等于 MedicalCase.Id
   - 聚合根模式：MedicalCase 是聚合根，Consultation 是依赖部分
   - 业务流程：Patient → MedicalCase (自动创建 Consultation) → 更新 Consultation → 可选 Prescription

**影响范围**：
- `LYBT.Module.MedicalCase.Services.MedicalCaseService` (CreateAsync 方法)
- `LYBT.Module.Consultation.Services.ConsultationService` (CreateAsync 方法，新增 IMedicalCaseRepository 依赖)
- `LYBT.WebAPI.Controllers.ConsultationController` (CreateAsync 标记为过时)
- `LYBT.Desktop.Consultation.ViewModels.ConsultationMainViewModel` (OnNavigatedTo 和 SaveConsultationAsync 方法)
- `LYBT.Module.Consultation.csproj` (新增对 MedicalCase 模块的项目引用)

**验收结果**：
- ✅ Server端编译成功（0警告，0错误）
- ✅ Desktop端编译成功（0警告，0错误）
- ✅ 聚合根模式正确实现
- ✅ 共享主键约束正确执行

**参考资料**：
- Issue: #1093
- PR: [待创建]
- 相关文档: `docs/requirements/business-requirements-2025-09-27.md`

## 📝 实现状态

### ✅ 已实现
- **核心CRUD**: 完整的增删改查功能
- **分页查询**: 支持关键字搜索的分页列表
- **关联查询**: 预加载Consultation和Prescription，解决N+1问题
- **AutoMapper映射**: 完整的Entity ↔ DTO映射配置
- **软删除**: 基于BaseEntity的软删除机制
- **异常处理**: 统一的异常处理和日志记录

### 🔄 待优化
- **高级搜索**: 多条件组合搜索（按日期范围、状态、诊断等）
- **统计分析**: 病例统计、趋势分析、医生工作量统计
- **批量操作**: 批量状态更新、批量导出功能
- **缓存策略**: 热点数据缓存，提升查询性能
- **API版本管理**: 支持多版本API兼容

### 🚧 未实现功能
- **复杂查询**: 诊断关键字搜索、患者就诊历史分析
- **报表生成**: 医生工作报表、患者统计报表
- **数据导入导出**: Excel格式的批量导入导出
- **审计日志**: 详细的操作历史记录
- **通知机制**: 病例状态变更通知

## 🧪 测试覆盖

### 单元测试
- **MedicalCaseService**: 业务逻辑测试
- **MedicalCaseRepository**: 数据访问测试
- **AutoMapper配置**: 映射配置测试
- **DTO验证**: 输入验证规则测试

### 集成测试
- **API端到端**: 完整的HTTP API测试
- **数据库集成**: EF Core数据访问测试
- **关联数据**: Consultation和Prescription关联测试

### 性能测试
- **分页查询**: 大数据量分页性能
- **N+1查询**: Include策略验证
- **并发操作**: 多用户并发访问测试

## 🔗 依赖关系

### 依赖模块
- **LYBT.Entities.MedicalCase**: 核心实体定义
- **LYBT.Entities.Consultation**: 诊疗记录实体
- **LYBT.Entities.Prescriptions**: 处方实体
- **LYBT.Infrastructure**: 数据库上下文和基础仓储
- **LYBT.Shared.Models**: DTO定义和枚举
- **LYBT.Shared.Interfaces**: 服务接口定义

### 被依赖模块
- **Consultation模块**: 诊疗记录关联
- **Prescription模块**: 处方信息关联
- **Patient模块**: 患者信息引用
- **User模块**: 医生信息引用
- **WebAPI**: HTTP接口暴露

### 关联关系图
```
MedicalCase (1:1) ← Consultation
MedicalCase (0:1) ← Prescription
MedicalCase (N:1) → Patient
MedicalCase (N:1) → User (Doctor)
```

## 📈 性能考虑

### 查询优化
- **Include策略**: 预加载关联数据，避免N+1查询
- **分页查询**: 支持大数据量的高效分页
- **索引设计**: 在PatientId、DoctorId、ConsultationDate上建立索引
- **查询缓存**: 对热点查询结果进行缓存

### 内存优化
- **DTO投影**: 仅加载需要的字段，减少内存占用
- **分页加载**: 避免一次性加载大量数据
- **连接池**: 数据库连接池优化

### 并发处理
- **乐观锁**: 使用RowVersion防止并发更新冲突
- **异步操作**: 所有I/O操作使用async/await
- **事务管理**: 关联数据的一致性保证

## 🔧 待优化项

### 功能增强
1. **高级搜索功能**
   - 按诊断关键字搜索
   - 日期范围筛选
   - 多状态组合查询
   - 医生和患者组合筛选

2. **统计分析模块**
   - 医生工作量统计
   - 患者就诊频次分析
   - 诊断分布统计
   - 月度/年度趋势分析

3. **批量操作功能**
   - 批量状态更新
   - 批量数据导出
   - 批量删除（软删除）

### 性能优化
1. **查询性能提升**
   - 实现Redis缓存
   - 优化复杂查询SQL
   - 添加必要的数据库索引

2. **API性能优化**
   - 响应数据压缩
   - 分页预加载策略
   - 查询结果缓存

3. **并发性能**
   - 读写分离支持
   - 异步处理优化
   - 连接池配置调优

### 系统集成
1. **消息通知集成**
   - 状态变更通知
   - 紧急病例提醒
   - 医生任务提醒

2. **报表系统集成**
   - 病例统计报表
   - 医生绩效报表
   - 患者就诊报表

3. **审计系统增强**
   - 详细操作日志
   - 数据变更追踪
   - 合规性审计支持