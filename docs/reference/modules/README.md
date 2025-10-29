# 模块总览文档

**版本**：v5.0 对齐架构版  
**更新时间**：2025-10-29  
**维护团队**：业务开发组  

## 🎯 业务模块导航

凌隐宝堂中医诊所管理系统采用模块化设计，包含8个核心业务模块，覆盖中医诊所的完整业务流程。每个模块都遵循统一的设计标准和开发规范。

### 📋 模块架构概览

| 模块 | 功能描述 | 主要特性 | 技术复杂度 | 业务重要性 |
|------|----------|----------|------------|------------|
| **认证模块** | 用户认证、权限管理 | 双轨认证、JWT令牌、RBAC权限 | 中等 | 核心基础 |
| **用户模块** | 用户管理、角色分配 | 用户信息、角色权限、密码安全 | 中等 | 管理基础 |
| **患者模块** | 患者信息管理 | 患者档案、Excel导入、查询统计 | 中等 | 核心业务 |
| **医案模块** | 医案流程管理 | 状态流转、四诊记录、辨证论治 | 高 | 中医特色 |
| **诊疗模块** | 四诊合参、辨证论治 | 望闻问切、诊断记录、治疗方案 | 高 | 中医特色 |
| **处方模块** | 处方录入、价格计算 | 四种录入方式、药材配伍、自动计价 | 高 | 核心业务 |
| **药材模块** | 药材字典管理 | 拼音码检索、价格管理、质量控制 | 中等 | 基础支撑 |
| **验方模块** | 验方模板、智能推荐 | 验方库、症状匹配、统计分析 | 中等 | 特色功能 |

## 🏗️ 模块架构标准

### 1. 统一模块结构
每个业务模块都遵循统一的目录结构：

```
Modules/{ModuleName}/
├── Server/                    # 服务端实现
│   ├── Controllers/           # API控制器
│   ├── Services/              # 业务服务
│   ├── Repositories/          # 数据仓储
│   ├── Models/                # 数据模型
│   └── Validators/            # 数据验证
├── Client/                    # 客户端实现
│   ├── Views/                 # 用户界面
│   ├── ViewModels/            # 视图模型
│   ├── Models/                # 数据模型
│   ├── Commands/              # 命令处理
│   └── Converters/            # 数据转换
├── Shared/                    # 共享组件
│   ├── Interfaces/            # 接口定义
│   ├── DTOs/                  # 数据传输对象
│   ├── Requests/              # 请求模型
│   ├── Responses/             # 响应模型
│   └── Enums/                 # 枚举定义
├── Tests/                     # 测试代码
│   ├── Unit/                  # 单元测试
│   ├── Integration/           # 集成测试
│   └── Data/                  # 测试数据
└── Docs/                      # 模块文档
    ├── README.md              # 模块说明
    ├── API.md                 # API文档
    ├── BusinessRules.md       # 业务规则
    └── UserGuide.md           # 用户指南
```

### 2. 统一命名规范
- **控制器**: `{ModuleName}Controller`
- **服务**: `{ModuleName}Service`
- **仓储**: `{ModuleName}Repository`
- **视图**: `{ModuleName}View`
- **视图模型**: `{ModuleName}ViewModel`
- **模型**: `{ModuleName}Model` / `{ModuleName}Dto`

### 3. 统一接口标准
每个模块都实现标准的CRUD接口：
- **GET**: 获取列表、获取详情、搜索查询
- **POST**: 创建资源、批量操作
- **PUT**: 更新资源、状态变更
- **DELETE**: 删除资源、批量删除

## 🔐 认证模块 (Auth)

### 功能概述
认证模块是系统的基础模块，负责用户身份认证、权限管理和令牌管理。采用双轨认证机制，支持普通用户认证和超级管理员认证。

### 核心功能
- **双轨认证**: Users表 + AdminSecrets表物理隔离
- **JWT令牌**: AccessToken(2小时) + RefreshToken(7天)
- **权限控制**: 基于角色的访问控制(RBAC)
- **密码安全**: 密码哈希、强度验证、定期更换

### 技术实现
```csharp
// 双轨认证实现
public class AuthService : IAuthService
{
    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        // 普通用户认证
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user != null && VerifyPassword(user, request.Password))
        {
            return await GenerateUserTokens(user);
        }
        
        // 超级管理员认证
        var adminSecret = await _adminSecretRepository.GetBySecretAsync(request.Password);
        if (adminSecret != null && adminSecret.IsActive)
        {
            return await GenerateAdminTokens(adminSecret);
        }
        
        return AuthResult.Failure("认证失败");
    }
}
```

### 业务规则
1. **用户认证**: 邮箱必须验证，密码强度至少8位
2. **超级管理员**: 使用物理隔离的秘密密钥认证
3. **令牌管理**: AccessToken过期后自动使用RefreshToken刷新
4. **权限检查**: 每个API请求都会检查用户权限

### API端点
- `POST /api/auth/login` - 用户登录
- `POST /api/auth/refresh` - 刷新令牌
- `POST /api/auth/logout` - 用户登出
- `GET /api/auth/me` - 获取当前用户信息
- `POST /api/auth/change-password` - 修改密码
- `POST /api/auth/reset-password` - 重置密码

### 相关文档
- **[认证模块详细文档](auth/README.md)**
- **[双轨认证机制设计](auth/dual-track-auth.md)**
- **[JWT令牌管理指南](auth/jwt-management.md)**

## 👥 用户模块 (Users)

### 功能概述
用户模块负责系统用户的管理，包括用户信息维护、角色权限分配、密码安全等功能。

### 核心功能
- **用户管理**: 用户信息CRUD、状态管理
- **角色管理**: 角色定义、权限分配
- **权限管理**: 细粒度权限控制
- **密码管理**: 密码策略、重置流程

### 业务规则
1. **用户创建**: 必须指定角色和权限
2. **权限继承**: 角色权限 + 用户权限
3. **密码策略**: 定期更换、强度验证
4. **状态管理**: 启用/禁用/锁定

### API端点
- `GET /api/users` - 获取用户列表
- `GET /api/users/{id}` - 获取用户详情
- `POST /api/users` - 创建用户
- `PUT /api/users/{id}` - 更新用户
- `DELETE /api/users/{id}` - 删除用户
- `PUT /api/users/{id}/status` - 更新用户状态
- `GET /api/users/roles` - 获取角色列表
- `POST /api/users/{id}/permissions` - 分配权限

### 相关文档
- **[用户模块详细文档](users/README.md)**
- **[权限管理指南](users/permission-guide.md)**
- **[用户管理操作手册](users/user-manual.md)**

## 🏥 患者模块 (Patients)

### 功能概述
患者模块是系统的核心业务模块，负责患者信息的完整管理，包括患者档案、就诊记录、统计报表等。

### 核心功能
- **患者档案**: 基本信息、病史、过敏史
- **数据导入**: Excel批量导入患者信息
- **查询搜索**: 多条件搜索、模糊匹配
- **统计分析**: 患者统计、就诊分析
- **数据导出**: 患者信息导出

### 技术特色
```csharp
// 患者搜索实现
public async Task<PagedResult<PatientDto>> SearchPatientsAsync(SearchRequest request)
{
    var query = _context.Patients.AsQueryable();
    
    if (!string.IsNullOrEmpty(request.Keyword))
    {
        query = query.Where(p => 
            p.Name.Contains(request.Keyword) ||
            p.Phone.Contains(request.Keyword) ||
            p.IdCard.Contains(request.Keyword));
    }
    
    if (request.Gender.HasValue)
    {
        query = query.Where(p => p.Gender == request.Gender.Value);
    }
    
    if (request.MinAge.HasValue)
    {
        var minBirthDate = DateTime.Today.AddYears(-request.MinAge.Value);
        query = query.Where(p => p.BirthDate <= minBirthDate);
    }
    
    return await query.ToPagedResultAsync(request.PageIndex, request.PageSize);
}
```

### 业务规则
1. **唯一性约束**: 手机号、身份证号必须唯一
2. **数据验证**: 身份证号格式、手机号格式验证
3. **隐私保护**: 敏感信息脱敏显示
4. **数据完整性**: 患者删除前检查关联数据

### API端点
- `GET /api/patients` - 获取患者列表
- `GET /api/patients/{id}` - 获取患者详情
- `POST /api/patients` - 创建患者
- `PUT /api/patients/{id}` - 更新患者
- `DELETE /api/patients/{id}` - 删除患者
- `GET /api/patients/search` - 搜索患者
- `POST /api/patients/import` - 导入患者
- `GET /api/patients/export` - 导出患者

### 相关文档
- **[患者模块详细文档](patients/README.md)**
- **[患者数据导入指南](patients/import-guide.md)**
- **[患者搜索使用指南](patients/search-guide.md)**

## 📋 医案模块 (MedicalCases)

### 功能概述
医案模块是中医特色模块，负责管理中医诊疗过程中的医案信息，包括四诊记录、辨证论治、诊断结果等。

### 核心功能
- **医案管理**: 医案创建、编辑、查询
- **状态流转**: 新建→进行中→已完成→已归档
- **四诊记录**: 望闻问切完整记录
- **辨证论治**: 中医诊断和治法方案
- **医案模板**: 常见病症模板管理

### 技术特色
```csharp
// 医案状态机实现
public class MedicalCaseStateMachine
{
    public async Task<bool> TransitionStatusAsync(MedicalCase medicalCase, MedicalCaseStatus newStatus)
    {
        var validTransitions = GetValidTransitions(medicalCase.Status);
        
        if (!validTransitions.Contains(newStatus))
        {
            throw new InvalidOperationException($"无法从状态 {medicalCase.Status} 转换到 {newStatus}");
        }
        
        medicalCase.Status = newStatus;
        medicalCase.StatusUpdatedAt = DateTime.UtcNow;
        
        await _medicalCaseRepository.UpdateAsync(medicalCase);
        await PublishStatusChangedEvent(medicalCase, newStatus);
        
        return true;
    }
}
```

### 业务规则
1. **状态流转**: 严格按照状态机规则流转
2. **数据完整性**: 四诊信息必须完整
3. **诊断标准**: 遵循中医诊断标准
4. **医案归档**: 完成后自动归档

### API端点
- `GET /api/MedicalCases` - 获取医案列表
- `GET /api/MedicalCases/{id}` - 获取医案详情
- `POST /api/MedicalCases` - 创建医案
- `PUT /api/MedicalCases/{id}` - 更新医案
- `PUT /api/MedicalCases/{id}/status` - 更新医案状态
- `GET /api/MedicalCases/{id}/history` - 获取医案历史
- `POST /api/MedicalCases/{id}/template` - 应用医案模板

### 相关文档
- **[医案模块详细文档](MedicalCase/README.md)**
- **[医案状态管理指南](MedicalCase/status-guide.md)**
- **[四诊记录规范](MedicalCase/four-examinations.md)**

## 🔍 诊疗模块 (Consultations)

### 功能概述
诊疗模块负责记录具体的诊疗过程，包括四诊合参的详细记录、诊断结果、治疗方案等。

### 核心功能
- **四诊记录**: 望、闻、问、切四诊详细信息
- **诊断结果**: 疾病诊断、证候诊断
- **治疗方案**: 治法、方药、医嘱
- **诊疗记录**: 每次诊疗的详细记录
- **疗效评估**: 治疗效果跟踪评估

### 技术特色
```csharp
// 四诊记录模型
public class FourExaminations
{
    public string Inspection { get; set; }      // 望诊
    public string Auscultation { get; set; }    // 闻诊
    public string Inquiry { get; set; }         // 问诊
    public string Palpation { get; set; }       // 切诊
    
    public string GetSummary()
    {
        return $"望诊：{Inspection}\n闻诊：{Auscultation}\n问诊：{Inquiry}\n切诊：{Palpation}";
    }
}
```

### 业务规则
1. **四诊完整**: 四诊信息必须记录完整
2. **诊断规范**: 遵循中医诊断规范
3. **治疗方案**: 基于辨证论治制定
4. **疗效跟踪**: 定期评估治疗效果

### API端点
- `GET /api/consultations` - 获取诊疗记录
- `GET /api/consultations/{id}` - 获取诊疗详情
- `POST /api/consultations` - 创建诊疗记录
- `PUT /api/consultations/{id}` - 更新诊疗记录
- `GET /api/consultations/patient/{patientId}` - 获取患者诊疗记录
- `POST /api/consultations/{id}/evaluation` - 疗效评估

### 相关文档
- **[诊疗模块详细文档](consultation/README.md)**
- **[四诊记录指南](consultation/four-examinations-guide.md)**
- **[诊断规范文档](consultation/diagnosis-standards.md)**

## 💊 处方模块 (Prescriptions)

### 功能概述
处方模块是系统的核心业务模块，负责中药处方的管理，包括处方录入、药材配伍、价格计算、处方打印等。

### 核心功能
- **四种录入方式**: 手动录入、验方模板、历史处方、智能推荐
- **药材配伍**: 配伍禁忌检查、剂量调整
- **价格计算**: 自动计算处方总价、药材单价管理
- **处方管理**: 处方状态管理、历史记录
- **统计报表**: 处方统计、药材使用统计

### 技术特色
```csharp
// 处方价格计算
public class PrescriptionPricingService
{
    public async Task<PrescriptionPriceDto> CalculatePriceAsync(Prescription prescription)
    {
        var totalAmount = 0m;
        var herbPrices = new List<HerbPriceDto>();
        
        foreach (var herb in prescription.Herbs)
        {
            var unitPrice = await _herbService.GetUnitPriceAsync(herb.HerbId);
            var herbTotal = unitPrice * herb.Dosage;
            
            totalAmount += herbTotal;
            herbPrices.Add(new HerbPriceDto
            {
                HerbId = herb.HerbId,
                HerbName = herb.HerbName,
                Dosage = herb.Dosage,
                UnitPrice = unitPrice,
                TotalPrice = herbTotal
            });
        }
        
        return new PrescriptionPriceDto
        {
            TotalAmount = totalAmount,
            HerbPrices = herbPrices,
            ServiceFee = CalculateServiceFee(totalAmount),
            FinalAmount = totalAmount + CalculateServiceFee(totalAmount)
        };
    }
}
```

### 业务规则
1. **配伍禁忌**: 自动检查药材配伍禁忌
2. **剂量规范**: 遵循中药剂量规范
3. **价格计算**: 实时计算处方价格
4. **处方审核**: 处方保存前需要审核

### API端点
- `GET /api/prescriptions` - 获取处方列表
- `GET /api/prescriptions/{id}` - 获取处方详情
- `POST /api/prescriptions` - 创建处方
- `PUT /api/prescriptions/{id}` - 更新处方
- `POST /api/prescriptions/calculate` - 计算处方价格
- `GET /api/prescriptions/template` - 获取处方模板
- `POST /api/prescriptions/{id}/print` - 打印处方

### 相关文档
- **[处方模块详细文档](prescriptions/README.md)**
- **[处方录入指南](prescriptions/input-guide.md)**
- **[配伍禁忌检查](prescriptions/compatibility-check.md)**

## 🌿 药材模块 (Herbs)

### 功能概述
药材模块负责中药材的管理，包括药材字典、价格管理、质量控制等。

### 核心功能
- **药材字典**: 药材基本信息、性味归经、功能主治
- **拼音码检索**: 支持拼音快速检索
- **价格管理**: 药材价格、价格历史
- **质量控制**: 药材质量等级管理

### 技术特色
```csharp
// 拼音码检索实现
public class PinyinSearchService
{
    public async Task<IEnumerable<HerbDto>> SearchByPinyinAsync(string pinyin)
    {
        var herbs = await _herbRepository.GetAllAsync();
        
        return herbs.Where(herb => 
            herb.Pinyin.Contains(pinyin, StringComparison.OrdinalIgnoreCase) ||
            herb.Name.Contains(pinyin, StringComparison.OrdinalIgnoreCase))
            .Select(herb => new HerbDto
            {
                Id = herb.Id,
                Name = herb.Name,
                Pinyin = herb.Pinyin,
                Category = herb.Category,
                Price = herb.Price
            });
    }
}
```

### 业务规则
1. **价格管理**: 价格变更需要审批
2. **质量控制**: 药材质量等级管理
3. **数据完整性**: 药材信息必须完整且准确

### API端点
- `GET /api/herbs` - 获取药材列表
- `GET /api/herbs/{id}` - 获取药材详情
- `POST /api/herbs` - 创建药材
- `PUT /api/herbs/{id}` - 更新药材
- `GET /api/herbs/search` - 搜索药材

### 相关文档
- **[药材模块详细文档](herbs/README.md)**
- **[拼音码检索指南](herbs/pinyin-search.md)**

## 📚 验方模块 (Formulas)

### 功能概述
验方模块负责中医验方的管理，包括验方模板、智能推荐、统计分析等功能。

### 核心功能
- **验方模板**: 经典验方、自拟验方管理
- **智能推荐**: 基于症状的验方推荐
- **验方分析**: 验方组成分析、功效分析
- **临床应用**: 临床应用案例、疗效统计
- **验方统计**: 验方使用统计、效果分析

### 技术特色
```csharp
// 验方智能推荐
public class FormulaRecommendationService
{
    public async Task<IEnumerable<FormulaRecommendationDto>> RecommendFormulasAsync(
        IEnumerable<string> symptoms)
    {
        var formulas = await _formulaRepository.GetAllAsync();
        var recommendations = new List<FormulaRecommendationDto>();
        
        foreach (var formula in formulas)
        {
            var matchScore = CalculateMatchScore(formula, symptoms);
            if (matchScore > 0.5) // 匹配度阈值
            {
                recommendations.Add(new FormulaRecommendationDto
                {
                    FormulaId = formula.Id,
                    FormulaName = formula.Name,
                    MatchScore = matchScore,
                    Indications = formula.Indications,
                    Usage = formula.Usage
                });
            }
        }
        
        return recommendations.OrderByDescending(r => r.MatchScore);
    }
    
    private double CalculateMatchScore(Formula formula, IEnumerable<string> symptoms)
    {
        // 实现基于症状的匹配度计算算法
        var formulaSymptoms = ParseSymptoms(formula.Indications);
        var commonSymptoms = symptoms.Intersect(formulaSymptoms);
        
        return (double)commonSymptoms.Count() / symptoms.Count();
    }
}
```

### 业务规则
1. **验方分类**: 按功效分类管理
2. **智能推荐**: 基于症状匹配推荐
3. **验方审核**: 新验方需要专家审核
4. **效果跟踪**: 跟踪验方使用效果

### API端点
- `GET /api/formulas` - 获取验方列表
- `GET /api/formulas/{id}` - 获取验方详情
- `POST /api/formulas` - 创建验方
- `PUT /api/formulas/{id}` - 更新验方
- `GET /api/formulas/recommend` - 智能推荐验方
- `GET /api/formulas/statistics` - 获取验方统计

### 相关文档
- **[验方模块详细文档](formula/README.md)**
- **[验方推荐算法](formula/recommendation-algorithm.md)**
- **[验方分类管理](formula/category-management.md)**

## 🔄 模块间集成

### 1. 业务流程集成
```
患者管理 → 医案管理 → 诊疗记录 → 处方管理 → 收费管理
    ↓         ↓         ↓         ↓         ↓
基础信息   病症记录   四诊记录   药材配伍   价格计算
```

### 2. 数据关联关系
- **患者 → 医案**: 一对多关系
- **医案 → 诊疗**: 一对多关系
- **诊疗 → 处方**: 一对多关系
- **处方 → 药材**: 多对多关系
- **验方 → 处方**: 一对多关系

### 3. 状态同步
- **患者状态**: 影响后续业务流程
- **医案状态**: 控制诊疗和处方操作
- **处方状态**: 影响发药和收费
- **药材状态**: 影响处方开立

## 📊 模块统计

### 开发进度统计
| 模块 | 完成度 | 开发状态 | 测试覆盖度 | 文档完整度 |
|------|--------|----------|------------|------------|
| 认证模块 | 100% | ✅ 已完成 | 95% | 100% |
| 用户模块 | 100% | ✅ 已完成 | 90% | 100% |
| 患者模块 | 100% | ✅ 已完成 | 90% | 100% |
| 医案模块 | 100% | ✅ 已完成 | 85% | 100% |
| 诊疗模块 | 100% | ✅ 已完成 | 85% | 100% |
| 处方模块 | 100% | ✅ 已完成 | 90% | 100% |
| 药材模块 | 100% | ✅ 已完成 | 85% | 100% |
| 验方模块 | 100% | ✅ 已完成 | 80% | 100% |

### 功能统计
- **API端点**: 81个
- **数据表**: 25个
- **业务规则**: 156条
- **测试用例**: 342个

## 🔗 相关文档

### 架构文档
- **[架构总览](../explanation/architecture/README.md)** - 三层对齐架构设计原理
- **[模块设计指南](../explanation/architecture/module-design-guide.md)** - 业务模块化设计标准

### 开发文档
- **[开发指南总览](../how-to-guides/README.md)** - 开发规范和流程指导
- **[Server端开发指南](../how-to-guides/server/README.md)** - 后端开发规范和实践
- **[Client端开发指南](../how-to-guides/client/README.md)** - WPF客户端开发指南

### API文档
- **[API总览](../reference/api/README.md)** - 12个控制器完整API文档
- **[认证API](../reference/api/auth/)** - 双轨认证、JWT验证、超级管理员隔离

### 业务文档
- **[业务流程文档](business/workflows.md)** - 完整业务流程说明
- **[数据模型文档](data/models.md)** - 实体关系和业务规则
- **[用户操作手册](user/manual.md)** - 系统使用指南

---

**文档维护**：业务开发组 | **最后更新**：2025-10-29  
**适用版本**：v5.0 对齐架构版 | **审核状态**：已审核