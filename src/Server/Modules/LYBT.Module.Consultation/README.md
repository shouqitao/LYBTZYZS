# LYBT.Module.Consultation - 看诊模块

## 📋 模块概述

**看诊模块 (Consultation Module)** 是凌隐宝堂中医诊所系统的核心业务模块，专门处理中医看诊过程中的四诊合参（望、闻、问、切）诊断记录管理。基于分层架构设计，提供完整的看诊生命周期管理功能。

### 🎯 核心功能

- **四诊合参记录**：系统化记录中医四诊信息（望诊、闻诊、问诊、切诊）
- **看诊生命周期管理**：从开始看诊到诊断完成的完整工作流
- **患者就诊历史**：多维度查询患者历史就诊记录
- **中医诊断记录**：规范化的中医诊断信息管理

### 🏗️ 架构特点

- **分层架构**：QueryService + BusinessService 专业分离
- **纯委托模式**：主服务完全委托给专业服务层，职责清晰
- **缓存优化**：继承OptimizedBaseRepository，提供智能缓存机制
- **AutoMapper集成**：完整的实体-DTO映射配置

## 📁 项目结构

```
LYBT.Module.Consultation/
├── ConsultationModule.cs              # 🔧 模块注册类
├── Interfaces/                        # 📋 接口定义
│   ├── IConsultationRepository.cs     # 数据仓储接口
│   ├── IConsultationQueryService.cs   # 查询服务接口
│   └── IConsultationBusinessService.cs # 业务服务接口
├── Services/                          # 🎯 服务实现层
│   ├── ConsultationService.cs         # 主服务（纯委托）
│   ├── ConsultationQueryService.cs    # 查询专业服务
│   └── ConsultationBusinessService.cs # 业务专业服务
├── Repositories/                      # 💾 数据访问层
│   └── ConsultationRepository.cs      # 仓储实现
├── Mapping/                          # 🔄 映射配置
│   └── ConsultationMappingProfile.cs  # AutoMapper配置
└── README.md                         # 📖 模块文档
```

## 🔧 核心组件详解

### 1. 模块注册 (ConsultationModule.cs)

```csharp
/// <summary>
/// 看诊模块注册 - UltraThink标准化重构
/// 负责注册看诊相关的所有服务、仓储和映射配置
/// 采用UltraThink双层架构：QueryService + BusinessService 专业分离
/// </summary>
public static class ConsultationModule
{
    public static IServiceCollection AddConsultationModule(this IServiceCollection services)
    {
        // 仓储层
        services.AddScoped<IConsultationRepository, ConsultationRepository>();

        // UltraThink双层架构服务
        services.AddScoped<IConsultationQueryService, ConsultationQueryService>();
        services.AddScoped<IConsultationBusinessService, ConsultationBusinessService>();

        // 主服务 - 纯委托模式
        services.AddScoped<IConsultationService, ConsultationService>();

        // AutoMapper配置
        services.AddAutoMapper(cfg => cfg.AddProfile<ConsultationMappingProfile>());

        return services;
    }
}
```

### 2. 主服务层 (ConsultationService.cs)

**纯委托模式**：不包含任何业务逻辑，完全委托给专业服务

```csharp
/// <summary>
/// 看诊服务 - UltraThink双层架构纯委托模式
/// </summary>
public class ConsultationService : IConsultationService
{
    // 查询操作委托给QueryService
    public async Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);

    // 业务操作委托给BusinessService
    public async Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto dto)
        => await _businessService.StartAsync(dto);
}
```

### 3. 查询服务层 (ConsultationQueryService.cs)

**专业职责**：分页查询、搜索筛选、看诊查询、历史记录获取

#### 🔍 核心查询功能

- **分页查询**：`GetPagedAsync()` - 支持关键词搜索的分页查询
- **患者查询**：`GetByPatientIdAsync()` - 按患者ID查询就诊记录
- **医生查询**：`GetByDoctorIdAsync()` - 按医生ID查询看诊记录
- **医案查询**：`GetByMedicalCaseIdAsync()` - 按医疗案例查询
- **历史查询**：`GetPatientHistoryAsync()` - 获取患者历史就诊记录
- **四诊查询**：`GetFourDiagnosisByMedicalCaseIdAsync()` - 获取四诊数据

#### 🎯 四诊数据结构

```csharp
var fourDiagnosis = new
{
    // 望诊 - 观察
    Looking = new { Inspection = consultation.Inspection },

    // 闻诊 - 听声音、嗅气味  
    Listening = new { AuscultationOlfaction = consultation.AuscultationOlfaction },

    // 问诊 - 询问病情
    Asking = new 
    {
        ChiefComplaint = consultation.ChiefComplaint,
        PresentIllness = consultation.PresentIllness,
        Inquiry = consultation.Inquiry
    },

    // 切诊 - 脉诊等
    Palpation = new { Palpation = consultation.Palpation }
};
```

### 4. 业务服务层 (ConsultationBusinessService.cs)

**专业职责**：业务逻辑处理、工作流管理、状态变更、中医四诊处理

#### 🔄 核心业务功能

- **开始看诊**：`StartAsync()` - 创建新的看诊记录
- **更新看诊**：`UpdateAsync()` - 更新看诊信息和四诊数据
- **保存四诊**：`SaveFourDiagnosisAsync()` - 专门的四诊数据保存
- **删除看诊**：`DeleteAsync()` - 软删除看诊记录

#### 🔒 业务规则

- **唯一性检查**：每个医疗案例只能有一个进行中的看诊
- **状态验证**：已完成的看诊不能修改
- **数据完整性**：必填字段验证（患者ID、医疗案例ID、医生ID）

### 5. 数据访问层 (ConsultationRepository.cs)

**继承OptimizedBaseRepository**：获得缓存和性能优化

#### 🚀 缓存策略

```csharp
// 患者看诊记录缓存
var cacheKey = $"{CacheKeyPrefix}patient:{patientId}";

// 医生看诊记录缓存  
var cacheKey = $"{CacheKeyPrefix}doctor:{doctorId}";

// 医案看诊记录缓存
var cacheKey = $"{CacheKeyPrefix}medicalcase:{medicalCaseId}";
```

#### 📊 专业查询方法

- `GetByPatientIdAsync()` - 患者看诊记录（带缓存）
- `GetByDoctorIdAsync()` - 医生看诊记录（带缓存）
- `GetByMedicalCaseIdAsync()` - 医案看诊记录（带缓存）
- `GetByDateRangeAsync()` - 日期范围查询（待v2.0完善）

### 6. 映射配置 (ConsultationMappingProfile.cs)

**AutoMapper配置**：处理实体与DTO之间的复杂映射

#### 🔄 关键映射规则

```csharp
// 字段名映射
.ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.UserId))
.ForMember(dest => dest.Diagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))
.ForMember(dest => dest.Auscultation, opt => opt.MapFrom(src => src.AuscultationOlfaction))

// 状态映射
.ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => src.Status == CommonStatus.Disabled))

// 导航属性忽略
.ForMember(dest => dest.Patient, opt => opt.Ignore())
.ForMember(dest => dest.User, opt => opt.Ignore())
```

## 🎯 业务流程

### 1. 开始看诊流程

```
1. 验证输入数据（患者ID、医案ID、医生ID）
2. 检查是否已存在进行中的看诊
3. 创建新的看诊记录
4. 设置初始状态为Enable
5. 返回看诊DTO
```

### 2. 四诊记录流程

```
1. 获取现有看诊记录
2. 验证看诊状态（不能是已完成）
3. 解析四诊数据（JSON格式）
4. 更新对应的四诊字段
5. 保存到数据库
```

### 3. 查询流程

```
1. 检查缓存是否存在
2. 如果缓存命中，直接返回
3. 查询数据库
4. 更新缓存
5. 返回结果
```

## 📋 接口定义

### IConsultationService

```csharp
// 查询操作
Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id);
Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query);
Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId);
Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId);
Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword);
Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId);
Task<ServiceResult<object>> GetFourDiagnosisByMedicalCaseIdAsync(Guid medicalCaseId);

// 业务操作
Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto dto);
Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationDetailDto dto);
Task<ServiceResult<bool>> DeleteAsync(Guid id);
Task<ServiceResult<bool>> SaveFourDiagnosisAsync(Guid consultationId, object fourDiagnosisData);
```

## 🔄 数据传输对象 (数据传输对象（数据传输对象（DTO））)

### ConsultationDto（基础DTO）

- **基本信息**：Id, PatientId, MedicalCaseId, DoctorId
- **四诊信息**：Inspection, AuscultationOlfaction, Inquiry, Palpation 
- **诊断信息**：ChiefComplaint, PresentIllness, Diagnosis
- **时间信息**：ConsultationTime
- **显示信息**：DoctorName

### ConsultationDetailDto（详细DTO）

继承ConsultationDto，增加：

- **扩展信息**：PatientName, StartTime, EndTime
- **状态信息**：Status, IsCompleted
- **医嘱信息**：MedicalAdvice

### ConsultationStartDto（启动DTO）

- **必需信息**：PatientId, MedicalCaseId, UserId
- **初始信息**：InitialComplaint

## 📊 数据库映射

### 实体字段映射

| 实体字段 | DTO字段 | 说明 |
| --------------------- | --------------------------- | ------ |
| UserId | DoctorId | 医生ID映射 |
| TCMDiagnosis | Diagnosis | 中医诊断映射 |
| AuscultationOlfaction | Auscultation | 闻诊字段映射 |
| Status (CommonStatus) | Status (ConsultationStatus) | 状态类型转换 |

### 四诊字段结构

| 中医术语 | 实体字段 | 说明 |
| ---- | --------------------------------------- | -------- |
| 望诊 | Inspection | 观察患者神色形态 |
| 闻诊 | AuscultationOlfaction | 听声音、嗅气味 |
| 问诊 | Inquiry, ChiefComplaint, PresentIllness | 询问症状和病史 |
| 切诊 | Palpation | 脉诊、按诊等 |

## 🚀 性能优化

### 1. 缓存策略

- **多级缓存**：基于不同查询维度的缓存键
- **智能失效**：缓存自动过期和手动清理
- **命中率优化**：针对高频查询的缓存预热

### 2. 查询优化

- **排序策略**：使用Id排序替代时间字段排序
- **分页优化**：Skip/Take方式实现高效分页
- **条件筛选**：基于索引字段的高效WHERE条件

### 3. 数据库访问

- **异步操作**：全部使用async/await模式
- **连接复用**：DbContext生命周期管理
- **批量操作**：减少数据库往返次数

## 🔒 安全与验证

### 1. 输入验证

- **Guid验证**：检查空Guid和有效性
- **状态检查**：确保业务状态的合法性
- **权限验证**：医生只能操作自己的看诊记录

### 2. 异常处理

- **结构化日志**：详细的操作日志记录
- **友好错误**：面向用户的错误消息
- **异常传播**：ServiceResult统一异常处理

## 📝 使用示例

### 开始看诊

```csharp
var startDto = new ConsultationStartDto
{
    PatientId = patientId,
    MedicalCaseId = medicalCaseId,
    UserId = doctorId,
    InitialComplaint = "患者主诉症状"
};

var result = await _consultationService.StartAsync(startDto);
```

### 保存四诊数据

```csharp
var fourDiagnosisData = new
{
    inspection = "面色潮红，舌红苔黄",
    auscultationOlfaction = "语声洪亮，呼吸急促",
    inquiry = "头痛发热，口渴喜冷饮",
    palpation = "脉象滑数有力"
};

var result = await _consultationService.SaveFourDiagnosisAsync(consultationId, fourDiagnosisData);
```

### 查询患者历史

```csharp
var historyResult = await _consultationService.GetPatientHistoryAsync(patientId);
```

## 📈 版本历史

### v2.0.0 (当前版本)

- ✅ **分层架构**：完整重构为QueryService + BusinessService
- ✅ **纯委托模式**：主服务完全委托，职责清晰分离
- ✅ **缓存优化**：继承OptimizedBaseRepository，性能大幅提升
- ✅ **AutoMapper完善**：解决字段更新不完整问题
- ✅ **四诊合参**：规范化的中医四诊数据管理
- ✅ **异常处理**：完整的异常处理和日志记录

### v1.0.0 (已废弃)

- 🗑️ 单一服务架构
- 🗑️ 直接数据库访问
- 🗑️ 缺少缓存机制

## 🛠️ 开发指南

### 环境要求

- .NET 8.0+
- 实体（实体（Entity）） Framework Core 8.0.17+
- AutoMapper 13.0.1+
- SQL Server

### 依赖项目

- LYBT.基础设施（基础设施（Infrastructure））（数据访问基础）
- LYBT.Shared.Models（DTO定义）
- LYBT.Shared.Interfaces（接口定义）
- LYBT.Entities（实体定义）

### 部署注意事项

1. **数据库迁移**：确保Consultation表结构正确
2. **缓存配置**：配置MemoryCache服务
3. **日志配置**：配置结构化日志输出
4. **性能监控**：监控查询性能和缓存命中率

## 📞 技术支持

如有技术问题或改进建议，请联系开发团队。

---

*本文档基于实际代码结构生成，确保与代码实现完全一致。*

## 🎯 项目概述
- [待补充] 简要描述 LYBT.Module.Consultation 的职责、边界及与其他模块关系。

## 🛠 技术栈
- [待补充] 框架/库/运行时示例：.NET 8、ASP.NET Core、EF Core、Prism、Refit、AutoMapper 等。

## 🚀 快速开始
- [待补充] 基本操作：dotnet restore/build/test；如何运行/调试当前模块。

## 🔌 API 接口
- [待补充] API 路由前缀：/api/v1/consultation
- [待补充] 控制器与端点：列出主要 Controller 与示例端点
- 参考 WebAPI：src/Server/Services/LYBT.WebAPI/README.md

## 📚 相关文档
- docs/architecture/overview.md
- docs/api/README.md
- docs/modules/index.md
- [待补充] 本模块相关的设计/实现文档链接
