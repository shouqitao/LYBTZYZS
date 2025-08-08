# INITIAL.md - 凌隐宝堂中医诊所系统项目初始化文档

## FEATURE: 纯中医诊所管理系统

### 核心功能描述
凌隐宝堂中医诊所诊疗系统是一个专为中医诊所设计的综合管理平台，完全基于中医理论和诊疗流程，不包含任何西医检查和诊断功能。系统涵盖患者管理、中医四诊、辨证论治、处方开具的核心诊疗流程。

### 主要特性
1. **纯中医诊疗体系** - 完全基于中医理论，支持望闻问切四诊
2. **辨证论治** - 支持中医证型分析和治疗原则制定
3. **验方管理** - 经典方剂模板管理和快速应用
4. **中药材管理** - 完整的中药材库存和价格管理
5. **处方管理** - 中药处方开具、打印和管理
6. **患者档案** - 完整的患者信息和病历管理
7. **工作流管理** - 简化的诊疗流程，以医疗案例为核心

## EXAMPLES: 项目示例和参考

### 1. API调用示例 (`tests/api/`)

```python
# API认证示例
def login_api(username="sysadmin", password="Admin@123456"):
    """用户登录获取JWT Token"""
    response = requests.post(
        f"{BASE_URL}/api/v1/auth/login",
        json={
            "username": username,
            "password": password,
            "rememberMe": False
        }
    )
    return response.json()["data"]["token"]

# 患者管理API示例
def get_patients(token):
    """获取患者列表"""
    headers = {"Authorization": f"Bearer {token}"}
    response = requests.get(
        f"{BASE_URL}/api/v1/patients",
        headers=headers
    )
    return response.json()
```

### 2. 前端服务调用示例 (`src/Frontend/Desktop/Services/`)

```csharp
// 看诊服务示例
public class ConsultationService : IConsultationService
{
    public async Task<ConsultationDto> StartConsultationAsync(Guid patientId)
    {
        var startDto = new ConsultationStartDto
        {
            PatientId = patientId,
            MedicalCaseId = Guid.NewGuid()
        };
        
        var response = await _apiService.StartConsultationAsync(startDto);
        return response.Content;
    }
}
```

### 3. 数据模型示例 (`src/Shared/LYBT.Shared.Models/`)

```csharp
// 中医诊断模型
public class TCMDiagnosisModel
{
    public string Inspection { get; set; }           // 望诊
    public string AuscultationOlfaction { get; set; } // 闻诊
    public string Inquiry { get; set; }               // 问诊
    public string Palpation { get; set; }             // 切诊
    public string TongueInspection { get; set; }      // 舌诊
    public string PulseCondition { get; set; }        // 脉象
    public string TCMDiagnosis { get; set; }          // 中医诊断
    public string TreatmentPrinciple { get; set; }    // 治疗原则
}
```

### 4. 处方打印示例 (`src/Frontend/Desktop/Modules/Consultation/`)

```csharp
// 处方打印内容构建
private string BuildPrescriptionContent()
{
    var content = new StringBuilder();
    content.AppendLine("凌隐宝堂中医诊所");
    content.AppendLine("═════════════════════════════════════");
    content.AppendLine($"患者姓名：{patient.Name}");
    content.AppendLine($"中医诊断：{diagnosis}");
    content.AppendLine("处方：");
    foreach (var herb in prescriptionItems)
    {
        content.AppendLine($"  {herb.Name} {herb.Quantity}{herb.Unit}");
    }
    content.AppendLine("用法：水煎服，一日一剂，分早晚两次温服。");
    return content.ToString();
}
```

## DOCUMENTATION: 关键文档参考

### 开发文档
1. **[开发规范](docs/开发规范.md)** - 完整的编码规范和最佳实践
2. **[API响应标准](docs/API响应标准.md)** - RESTful API设计规范
3. **[前后端契约规范](docs/前后端契约规范.md)** - 前后端接口约定
4. **[项目修复进度报告](docs/development/项目修复进度报告-20250108.md)** - 最新的开发进度

### 技术文档
1. **Entity Framework Core 8文档** - 数据访问层ORM
2. **Prism.DryIoc文档** - WPF MVVM框架和依赖注入
3. **AutoMapper 15文档** - 对象映射（注意需要ILoggerFactory参数）
4. **Refit文档** - 类型安全的HTTP客户端

### 业务文档
1. **中医四诊规范** - 望闻问切标准流程
2. **中药材国家标准** - 药材名称、规格、用量规范
3. **处方格式标准** - 中医处方书写规范

## OTHER CONSIDERATIONS: 特殊注意事项

### 1. 常见AI助手易错点

#### AutoMapper 15配置问题
```csharp
// ❌ 错误：AutoMapper 15不支持单参数构造函数
var config = new MapperConfiguration(cfg => { });

// ✅ 正确：必须提供ILoggerFactory参数
var config = new MapperConfiguration(cfg => { }, NullLoggerFactory.Instance);
```

#### 数据库迁移位置
```bash
# ❌ 错误：在错误的项目中添加迁移
dotnet ef migrations add Init --project LYBT.Module.Users

# ✅ 正确：只能在Infrastructure项目中添加迁移
dotnet ef migrations add Init --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI
```

#### 中文编码问题
```csharp
// ❌ 错误：未设置UTF-8编码
File.WriteAllText(path, content);

// ✅ 正确：明确指定UTF-8编码
File.WriteAllText(path, content, Encoding.UTF8);
```

### 2. 项目特定约束

1. **纯中医系统** - 绝对不能添加任何西医检查项目（血压、血糖、CT、MRI等）
2. **共享数据上下文** - 所有模块必须使用统一的AppDbContext
3. **API响应格式** - 必须使用ApiResponse<T>包装所有响应
4. **前端框架** - 必须使用WPF + Prism，不使用其他UI框架
5. **依赖注入** - 必须使用构造函数注入，不使用属性注入

### 3. 性能考虑

1. **分页查询** - 列表API必须支持分页，默认每页20条
2. **异步操作** - 所有数据库操作必须使用async/await
3. **缓存策略** - 频繁访问的数据（如药材列表）应实现缓存
4. **批量操作** - 支持批量更新和删除以提高效率

### 4. 安全要求

1. **JWT认证** - 所有API必须进行身份验证（除了登录接口）
2. **密码加密** - 使用ASP.NET Core Identity的密码哈希
3. **SQL注入防护** - 使用参数化查询，禁止字符串拼接SQL
4. **敏感信息** - 不在日志中记录密码、Token等敏感信息

### 5. 部署注意

1. **环境变量** - 生产环境配置通过环境变量或appsettings.Production.json
2. **数据库连接** - 生产环境使用独立的SQL Server实例
3. **HTTPS** - 生产环境必须使用HTTPS
4. **日志级别** - 生产环境设置为Warning或Error级别

### 6. 开发工具要求

- **IDE**: Visual Studio 2022（推荐）或 VS Code
- **数据库**: SQL Server 2019+ 或 SQL Server LocalDB（开发）
- **.NET SDK**: .NET 8.0.100+
- **Node.js**: 18.x+（用于运行测试脚本）
- **Python**: 3.8+（用于API测试脚本）

### 7. 测试覆盖要求

- 核心业务逻辑：>80%覆盖率
- API控制器：>70%覆盖率
- 工具类：>90%覆盖率
- UI层：手工测试为主

### 8. 版本控制规范

- 主分支：master（生产代码）
- 开发分支：develop（日常开发）
- 功能分支：feature/功能名称
- 修复分支：fix/问题描述
- 每次提交必须有清晰的commit message

---

**文档版本**: 1.0.0  
**创建日期**: 2025年1月8日  
**维护团队**: 凌隐宝堂开发团队