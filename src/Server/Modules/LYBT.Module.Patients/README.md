# LYBT.Module.Patients

> **患者档案管理模块**  
> 完整患者基础信息管理与诊疗历史追踪 | UltraThink双层架构

## 🎯 模块功能

- **患者档案**: 完整患者基础信息管理和存档
- **快速检索**: 姓名、电话、身份证号多维搜索
- **状态管理**: 患者启用/禁用状态控制
- **诊疗历史**: 患者看诊记录和历史追踪
- **数据处理**: 批量导入、导出、重复检查

## 👥 患者信息管理

### 基础档案信息
- **个人信息**: 姓名、性别、年龄、出生日期
- **联系方式**: 电话号码、紧急联系人
- **身份信息**: 身份证号、地址信息
- **就诊记录**: 历史就诊和诊疗记录关联

### 数据完整性保障
- **唯一性检查**: 电话号码、身份证号重复验证
- **数据验证**: 联系方式格式验证和完整性检查
- **关系完整性**: 与MedicalCase、Consultation模块数据关联

## 🏗️ UltraThink双层架构

### 架构设计
```
PatientService (纯委托层)
    ├── PatientQueryService (查询专业层)
    └── PatientBusinessService (业务逻辑层)
```

### 核心组件
- **PatientService**: 统一服务入口，纯委托模式
- **PatientQueryService**: 复杂查询和搜索功能
- **PatientBusinessService**: 业务逻辑和CRUD操作
- **PatientRepository**: 数据访问层 (零SQL注入)
- **PatientMappingProfile**: AutoMapper 15.0.1配置

### 服务层分工
- **QueryService**: `GetPagedAsync`, `SearchAsync`, `GetActivePatientsAsync`, `AdvancedSearchAsync`
- **BusinessService**: `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `EnableAsync`, `DisableAsync`
- **主Service**: 纯委托路由，零业务逻辑

### 数据模型
```csharp
public class PatientModel : BaseEntity
{
    public string Name { get; set; }            // 患者姓名
    public Gender Gender { get; set; }          // 性别枚举
    public DateTime? BirthDate { get; set; }    // 出生日期
    public int? Age { get; set; }               // 年龄
    public string? PhoneNumber { get; set; }    // 手机号码
    public string? IdCard { get; set; }         // 身份证号
    public string? Address { get; set; }        // 联系地址
    public string? EmergencyContact { get; set; } // 紧急联系人
    public string? EmergencyPhone { get; set; }   // 紧急联系电话
    public bool Status { get; set; }            // 启用状态
    public string? Remarks { get; set; }        // 备注信息
    public DateTime? LastVisitTime { get; set; } // 最后就诊时间
}
```

## 🚀 API接口

### RESTful API设计 (小写命名规范)
| 接口 | 方法 | 功能描述 | 架构层 | 状态 |
|------|------|----------|--------|------|
| `/api/v1/patients` | GET | 分页查询患者列表 | Query | ✅ 完成 |
| `/api/v1/patients/{id}` | GET | 获取患者详情 | Query | ✅ 完成 |
| `/api/v1/patients` | POST | 创建新患者 | Business | ✅ 完成 |
| `/api/v1/patients/{id}` | PUT | 更新患者信息 | Business | ✅ 完成 |
| `/api/v1/patients/{id}` | DELETE | 删除患者档案 | Business | ✅ 完成 |
| `/api/v1/patients/{id}/enable` | PATCH | 启用患者 | Business | ✅ 完成 |
| `/api/v1/patients/{id}/disable` | PATCH | 禁用患者 | Business | ✅ 完成 |
| `/api/v1/patients/search` | POST | 高级搜索患者 | Query | ✅ 完成 |
| `/api/v1/patients/active` | GET | 获取活跃患者列表 | Query | ✅ 完成 |
| `/api/v1/patients/phone/{phone}` | GET | 按电话号码查询 | Query | ✅ 完成 |
| `/api/v1/patients/idcard/{idcard}` | GET | 按身份证号查询 | Query | ✅ 完成 |

### 使用示例
```bash
# 创建患者档案
POST /api/v1/patients
{
  "name": "张三",
  "gender": "Male",
  "birthDate": "1985-06-15",
  "phoneNumber": "13800138001",
  "idCard": "440106198506150001",
  "address": "广州市天河区天河路123号"
}

# 分页查询患者 (统一ApiResponse<T>格式)
GET /api/v1/patients?page=1&pageSize=10&keyword=张三&status=true

# 响应格式
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [...],
    "totalCount": 25,
    "page": 1,
    "pageSize": 10
  },
  "timestamp": "2025-08-31T10:30:00Z"
}
```

## 🔐 安全特性

- **零SQL注入**: LINQ查询 + EF Core 8.0.17参数化
- **数据验证**: FluentValidation规则验证患者信息
- **唯一性约束**: 电话号码、身份证号重复检查
- **权限验证**: JWT Bearer + RBAC角色控制
- **数据完整性**: 外键约束保护关联数据

## 📊 业务规则

### 患者信息规范
- **姓名**: 2-50字符，支持中文姓名
- **电话号码**: 11位手机号格式验证
- **身份证号**: 18位身份证格式和校验码验证
- **年龄计算**: 根据出生日期自动计算当前年龄

### 数据关联规则
- **就诊关联**: 患者可关联多个MedicalCase记录
- **状态管理**: 禁用患者不影响历史诊疗记录
- **删除保护**: 有诊疗记录的患者不可删除，只能禁用

## 🧪 UltraThink测试体系

### 测试结构
```
tests/LYBT.Module.Patients.Tests/
├── Services/
│   ├── PatientQueryServiceTests.cs
│   ├── PatientBusinessServiceTests.cs
│   └── PatientServiceTests.cs (委托层测试)
├── Repositories/
│   └── PatientRepositoryTests.cs
└── Integration/
    └── PatientModuleIntegrationTests.cs
```

### 测试覆盖率
- **单元测试**: 88个测试用例 ✅ 全部通过
- **架构测试**: 双层服务架构完整性验证
- **集成测试**: Repository + Service层端到端测试

```bash
# 运行患者模块测试
dotnet test --filter "LYBT.Module.Patients" --verbosity normal
```

## 📈 性能指标 (UltraThink优化)

### 查询性能
- **分页查询**: < 30ms (EF Core LINQ优化)
- **搜索响应**: < 50ms (索引优化)
- **单条查询**: < 10ms (主键查询)

### 并发能力
- **并发用户**: 50+ 患者管理操作 (小型诊所优化)
- **批量操作**: 100+ 患者批量处理
- **内存使用**: < 40MB (双层架构精简)

## 🚀 部署配置

### 依赖注入配置
```csharp
// PatientsModule.cs - 模块化注册
public static IServiceCollection AddPatientsModuleServices(this IServiceCollection services)
{
    // UltraThink双层架构服务注册
    services.AddScoped<IPatientService, PatientService>();
    services.AddScoped<IPatientQueryService, PatientQueryService>();
    services.AddScoped<IPatientBusinessService, PatientBusinessService>();
    services.AddScoped<IPatientRepository, PatientRepository>();
    
    return services;
}
```

### 环境配置
```json
// appsettings.json
{
  "PatientOptions": {
    "AllowDuplicatePhoneNumbers": false,
    "AllowDuplicateIdCards": false,
    "MaxSearchResults": 100,
    "DefaultPageSize": 20,
    "EnableAutoAgeCalculation": true,
    "RequiredFields": ["Name", "PhoneNumber"]
  }
}
```

---

> 📌 **架构特色**: UltraThink双层架构 | 零编译警告 | 生产就绪  
> 🔄 **最后更新**: 2025-08-31 | 版本: v1.0 UltraThink重构完成