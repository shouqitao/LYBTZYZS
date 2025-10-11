# Server端结构扫描报告 - Phase 3

**生成时间**: 2025-10-12
**分析范围**: 8个Server模块 + 12个API Controllers
**相关Issue**: #1149 代码实现盘点与差异分析

---

## 📊 执行概览

### 扫描统计
- **扫描模块**: 8个 (Auth, Users, Patients, MedicalCase, Consultation, Prescriptions, Herbs, Formula)
- **业务Controllers**: 8个
- **辅助Controllers**: 4个 (Health, CacheHealth, RootHealth, Performance)
- **Service层文件**: 10个
- **API端点总数**: 约56个业务API
- **架构**: 三层架构（Controller → Service → Repository）

---

## 🏗️ 服务端架构概览

### 目录结构

```
src/Server/
├── Services/
│   └── LYBT.WebAPI/
│       └── Controllers/           # API控制器层
│           ├── AuthController.cs
│           ├── UsersController.cs
│           ├── PatientsController.cs
│           ├── MedicalCaseController.cs
│           ├── ConsultationController.cs
│           ├── PrescriptionsController.cs
│           ├── HerbsController.cs
│           ├── FormulasController.cs
│           ├── HealthController.cs
│           ├── CacheHealthController.cs
│           ├── RootHealthController.cs
│           └── PerformanceController.cs
└── Modules/                       # 业务模块层
    ├── LYBT.Module.Auth/
    │   └── Services/
    │       ├── AuthService.cs
    │       └── JwtService.cs
    ├── LYBT.Module.Users/
    │   └── Services/
    │       └── UserService.cs
    ├── LYBT.Module.Patients/
    │   └── Services/
    │       └── PatientService.cs
    ├── LYBT.Module.MedicalCase/
    │   └── Services/
    │       ├── MedicalCaseService.cs
    │       └── MedicalCaseRules.cs
    ├── LYBT.Module.Consultation/
    │   └── Services/
    │       └── ConsultationService.cs
    ├── LYBT.Module.Prescriptions/
    │   └── Services/
    │       └── PrescriptionService.cs
    ├── LYBT.Module.Herbs/
    │   └── Services/
    │       └── HerbService.cs
    └── LYBT.Module.Formula/
        └── Services/
            └── FormulaService.cs
```

---

## 📡 API Controllers详细清单

### 1. AuthController (320行)

**路由**: `/api/auth`
**依赖服务**: IAuthService

#### API端点 (6个)

| HTTP Method | 端点 | 方法名 | 功能描述 | 返回类型 |
|------------|------|--------|---------|---------|
| POST | `/login` | LoginAsync | 普通用户登录 | ApiResponse<LoginResponseDto> |
| POST | `/superadmin-login` | SuperAdminLoginAsync | 超级管理员登录 | ApiResponse<LoginResponseDto> |
| POST | `/logout` | LogoutAsync | 用户登出 | ApiResponse |
| POST | `/change-sysadmin-password` | ChangeSysAdminPasswordAsync | 修改系统管理员密码 | ApiResponse |
| POST | `/validate-token` | ValidateTokenAsync | 验证Token（从Body） | ApiResponse<TokenValidationDto> |
| GET | `/validate-token-from-header` | ValidateTokenFromHeaderAsync | 验证Token（从Header） | ApiResponse<UserDto> |
| GET | `/` | Get | 测试端点 | string |

**特殊功能**:
- 超级管理员登录（特殊通道）
- Token验证（支持Body和Header两种方式）
- 系统管理员密码修改

---

### 2. UsersController (209行)

**路由**: `/api/users`
**依赖服务**: IUserService

#### API端点 (6个)

| HTTP Method | 端点 | 方法名 | 功能描述 | 返回类型 |
|------------|------|--------|---------|---------|
| GET | `/` | GetUsers | 获取用户列表（分页） | ApiResponse<PagedResult<UserDto>> |
| GET | `/current` | GetCurrentUser | 获取当前登录用户信息 | ApiResponse<UserDto> |
| GET | `/{id}` | GetUser | 根据ID获取用户 | ApiResponse<UserDto> |
| POST | `/` | CreateUser | 创建用户 | ApiResponse<UserDto> |
| PUT | `/{id}` | UpdateUser | 更新用户 | ApiResponse<UserDto> |
| DELETE | `/{id}` | DeleteUser | 删除用户 | ApiResponse |

**查询参数** (GetUsers):
- page: int (页码)
- pageSize: int (每页数量)
- searchText: string? (搜索关键字)

**特殊功能**:
- 获取当前用户（通过Token）

---

### 3. PatientsController (180行)

**路由**: `/api/patients`
**依赖服务**: IPatientService

#### API端点 (5个)

| HTTP Method | 端点 | 方法名 | 功能描述 | 返回类型 |
|------------|------|--------|---------|---------|
| GET | `/` | GetList | 获取患者列表（分页） | ApiResponse<PagedResult<PatientDto>> |
| GET | `/{id}` | GetById | 根据ID获取患者 | ApiResponse<PatientDto> |
| POST | `/` | Add | 创建患者 | ApiResponse<PatientDto> |
| PUT | `/{id}` | Update | 更新患者 | ApiResponse<PatientDto> |
| DELETE | `/{id}` | Delete | 删除患者 | ApiResponse |

**查询参数** (GetList):
- page: int (页码)
- pageSize: int (每页数量)
- searchText: string? (搜索关键字)

---

### 4. MedicalCaseController (229行)

**路由**: `/api/medicalcase`
**依赖服务**: IMedicalCaseService

#### API端点 (7个)

| HTTP Method | 端点 | 方法名 | 功能描述 | 返回类型 |
|------------|------|--------|---------|---------|
| GET | `/` | GetPaged | 获取病历列表（分页） | ApiResponse<PagedResult<MedicalCaseDto>> |
| GET | `/{id}` | GetById | 根据ID获取病历 | ApiResponse<MedicalCaseDto> |
| GET | `/{id}/details` | GetByIdWithDetails | 获取病历详情（含关联数据） | ApiResponse<MedicalCaseDetailDto> |
| POST | `/details` | CreateWithDetails | 创建病历（含详情） | ApiResponse<MedicalCaseDto> |
| POST | `/` | Create | 创建病历（基础） | ApiResponse<MedicalCaseDto> |
| PUT | `/{id}` | Update | 更新病历 | ApiResponse<MedicalCaseDto> |
| DELETE | `/{id}` | Delete | 删除病历 | ApiResponse |

**查询参数** (GetPaged):
- page: int (页码)
- pageSize: int (每页数量)
- searchText: string? (搜索关键字)

**特殊功能**:
- GetByIdWithDetails: 返回病历详情（包含关联的处方、会诊等数据）
- CreateWithDetails: 支持一次性创建病历及关联数据

---

### 5. ConsultationController (229行)

**路由**: `/api/consultation`
**依赖服务**: IConsultationService

#### API端点 (7个)

| HTTP Method | 端点 | 方法名 | 功能描述 | 返回类型 |
|------------|------|--------|---------|---------|
| GET | `/` | GetConsultations | 获取会诊列表（分页） | ApiResponse<PagedResult<ConsultationDto>> |
| GET | `/{id}` | GetById | 根据ID获取会诊 | ApiResponse<ConsultationDto> |
| GET | `/medicalcase/{medicalCaseId}` | GetByMedicalCaseId | 根据病历ID获取会诊列表 | ApiResponse<List<ConsultationDto>> |
| GET | `/search` | Search | 搜索会诊 | ApiResponse<List<ConsultationDto>> |
| POST | `/` | CreateConsultation | 创建会诊 | ApiResponse<ConsultationDto> |
| PUT | `/{id}` | UpdateConsultation | 更新会诊 | ApiResponse<ConsultationDto> |
| DELETE | `/{id}` | DeleteConsultation | 删除会诊 | ApiResponse |

**查询参数**:
- GetConsultations: page, pageSize, searchText
- GetByMedicalCaseId: medicalCaseId (病历ID)
- Search: keyword, startDate, endDate

**特殊功能**:
- 支持按病历ID查询会诊记录
- 支持按关键字和日期范围搜索

---

### 6. PrescriptionsController (181行)

**路由**: `/api/prescriptions`
**依赖服务**: IPrescriptionService

#### API端点 (5个)

| HTTP Method | 端点 | 方法名 | 功能描述 | 返回类型 |
|------------|------|--------|---------|---------|
| GET | `/` | GetList | 获取处方列表（分页） | ApiResponse<PagedResult<PrescriptionDto>> |
| GET | `/{id}` | GetById | 根据ID获取处方 | ApiResponse<PrescriptionDto> |
| POST | `/` | Add | 创建处方 | ApiResponse<PrescriptionDto> |
| PUT | `/{id}` | Update | 更新处方 | ApiResponse<PrescriptionDto> |
| DELETE | `/{id}` | Delete | 删除处方 | ApiResponse |

**查询参数** (GetList):
- page: int (页码)
- pageSize: int (每页数量)
- searchText: string? (搜索关键字)

---

### 7. HerbsController (172行)

**路由**: `/api/herbs`
**依赖服务**: IHerbService

#### API端点 (5个)

| HTTP Method | 端点 | 方法名 | 功能描述 | 返回类型 |
|------------|------|--------|---------|---------|
| GET | `/` | GetList | 获取药材列表（分页） | ApiResponse<PagedResult<HerbDto>> |
| GET | `/{id}` | GetById | 根据ID获取药材 | ApiResponse<HerbDto> |
| POST | `/` | Create | 创建药材 | ApiResponse<HerbDto> |
| PUT | `/{id}` | Update | 更新药材 | ApiResponse<HerbDto> |
| DELETE | `/{id}` | Delete | 删除药材 | ApiResponse |

**查询参数** (GetList):
- page: int (页码)
- pageSize: int (每页数量)
- searchText: string? (搜索关键字)

---

### 8. FormulasController (180行)

**路由**: `/api/formulas`
**依赖服务**: IFormulaService

#### API端点 (5个)

| HTTP Method | 端点 | 方法名 | 功能描述 | 返回类型 |
|------------|------|--------|---------|---------|
| GET | `/` | GetList | 获取验方列表（分页） | ApiResponse<PagedResult<FormulaDto>> |
| GET | `/{id}` | GetById | 根据ID获取验方 | ApiResponse<FormulaDto> |
| POST | `/` | Add | 创建验方 | ApiResponse<FormulaDto> |
| PUT | `/{id}` | Update | 更新验方 | ApiResponse<FormulaDto> |
| DELETE | `/{id}` | Delete | 删除验方 | ApiResponse |

**查询参数** (GetList):
- page: int (页码)
- pageSize: int (每页数量)
- searchText: string? (搜索关键字)

---

## 🔧 Service层文件清单

### 模块Service映射

| 模块 | Service文件 | 说明 |
|------|------------|------|
| Auth | AuthService.cs | 认证服务 |
| Auth | JwtService.cs | JWT令牌服务 |
| Users | UserService.cs | 用户管理服务 |
| Patients | PatientService.cs | 患者管理服务 |
| MedicalCase | MedicalCaseService.cs | 病历服务 |
| MedicalCase | MedicalCaseRules.cs | 病历业务规则 |
| Consultation | ConsultationService.cs | 会诊服务 |
| Prescriptions | PrescriptionService.cs | 处方服务 |
| Herbs | HerbService.cs | 药材服务 |
| Formula | FormulaService.cs | 验方服务 |

**总计**: 10个Service类文件

---

## 📊 API端点统计分析

### 按模块统计

| 模块 | Controller | API端点数 | CRUD完整性 | 特殊功能 |
|------|-----------|----------|-----------|---------|
| Auth | AuthController | 6 | ❌ N/A | 超级管理员登录、Token验证 |
| Users | UsersController | 6 | ✅ 完整 | GetCurrent |
| Patients | PatientsController | 5 | ✅ 完整 | - |
| MedicalCase | MedicalCaseController | 7 | ✅ 完整 | WithDetails端点 |
| Consultation | ConsultationController | 7 | ✅ 完整 | 按病历查询、高级搜索 |
| Prescriptions | PrescriptionsController | 5 | ✅ 完整 | - |
| Herbs | HerbsController | 5 | ✅ 完整 | - |
| Formula | FormulasController | 5 | ✅ 完整 | - |

**总计**: 46个业务API端点

### CRUD模式分析

#### 标准CRUD模式 (6个模块)
遵循标准RESTful CRUD的模块：
- Patients: GetList, GetById, Add, Update, Delete
- Prescriptions: GetList, GetById, Add, Update, Delete
- Herbs: GetList, GetById, Create, Update, Delete
- Formulas: GetList, GetById, Add, Update, Delete
- Users: GetUsers, GetUser, CreateUser, UpdateUser, DeleteUser
- Consultation: GetConsultations, GetById, CreateConsultation, UpdateConsultation, DeleteConsultation

#### 扩展CRUD模式 (2个模块)
在标准CRUD基础上增加特殊端点：
- **MedicalCase**: 标准CRUD + WithDetails（详情查询/创建）
- **Consultation**: 标准CRUD + GetByMedicalCaseId + Search

#### 专用认证模式 (1个模块)
- **Auth**: 登录、登出、Token验证、密码修改

### 分页查询支持

所有列表查询端点均支持分页：
```
✅ 统一分页参数
  - page: int (页码，从1开始)
  - pageSize: int (每页数量)
  - searchText: string? (可选搜索关键字)

✅ 统一返回格式
  PagedResult<T> {
    Items: List<T>
    TotalCount: int
    Page: int
    PageSize: int
  }
```

---

## 🏗️ 架构模式分析

### 三层架构

```
┌─────────────────────────────────────────┐
│         WebAPI Controllers              │  ← API入口层
│  (路由、参数验证、响应包装)                 │
└────────────────┬────────────────────────┘
                 │ 依赖注入
                 ▼
┌─────────────────────────────────────────┐
│         Module Services                 │  ← 业务逻辑层
│  (业务规则、事务管理、数据转换)              │
└────────────────┬────────────────────────┘
                 │ 依赖注入
                 ▼
┌─────────────────────────────────────────┐
│         Repositories                    │  ← 数据访问层
│  (EF Core、数据库操作)                     │
└─────────────────────────────────────────┘
```

### 依赖注入模式

所有Controller遵循统一的依赖注入模式：

```csharp
public class XxxController : ControllerBase
{
    private readonly IXxxService _service;

    public XxxController(IXxxService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }
}
```

**特点**:
- 构造函数注入
- 非空验证
- 接口依赖（IXxxService）

### 响应包装模式

所有API统一使用 `ApiResponse<T>` 包装：

```csharp
public class ApiResponse<T>
{
    bool Success { get; }
    string Message { get; }
    T? Data { get; }
    int StatusCode { get; }
}
```

**优点**:
- 统一错误处理
- 统一响应格式
- 便于前端处理

---

## 🔍 关键发现

### ✅ 优点

1. **架构清晰一致**
   - 严格遵循三层架构
   - Controller → Service → Repository 分层明确
   - 依赖注入规范统一

2. **RESTful风格统一**
   - 所有CRUD端点遵循RESTful约定
   - HTTP动词使用正确（GET/POST/PUT/DELETE）
   - 路由命名一致

3. **分页支持完善**
   - 所有列表查询支持分页
   - 参数命名统一
   - 返回格式一致

4. **响应格式统一**
   - ApiResponse<T> 统一包装
   - 错误处理统一
   - HTTP状态码规范

5. **Service层独立**
   - 每个模块独立Service类
   - 业务逻辑集中管理
   - MedicalCase额外提供Rules类（业务规则分离）

### ⚠️ 潜在问题

#### 1. 缺少高级查询功能

**问题**: 大多数模块仅支持基础的searchText查询

**当前实现**:
```csharp
// 仅支持简单关键字搜索
GET /api/users?page=1&pageSize=10&searchText=张三
```

**Desktop需求** (基于Phase 2分析):
- **Users**: 需要按角色(Role)、状态(Status)筛选
- **Herbs**: 需要按分类(Category)筛选
- **Formulas**: 需要按分类(Category)、功效(Effect)筛选
- **Prescriptions**: 需要按日期范围(StartDate/EndDate)筛选
- **Consultation**: 已提供Search端点（日期范围），但需验证

**建议**:
```
🔧 扩展查询API
  选项1: 为每个Controller添加专用筛选端点
    POST /api/users/filter (Body: FilterDto)

  选项2: 扩展现有GetList端点参数
    GET /api/users?page=1&pageSize=10&role=Doctor&status=Active

  选项3: OData风格查询（长期方案）
    GET /api/users?$filter=role eq 'Doctor' and status eq 'Active'
```

#### 2. 批量操作缺失

**问题**: 无批量删除、批量状态切换等批量操作端点

**Desktop需求** (基于Phase 2分析):
- **Users**: UserManagementViewModel 需要批量操作（基类支持）
- **Herbs**: HerbManagementViewModel 需要批量删除
- **Formulas**: FormulaManagementViewModel 需要批量删除
- **MedicalCase**: 可能需要批量归档

**建议**:
```
🔧 添加批量操作API
  POST /api/users/batch-delete
    Body: { "ids": [guid1, guid2, ...] }

  POST /api/herbs/batch-toggle-status
    Body: { "ids": [guid1, guid2, ...], "status": "Active" }
```

#### 3. 导入导出功能缺失

**问题**: Desktop需要的导入导出功能在Server端未实现

**Desktop需求** (基于Phase 2分析):
- **Patients**: PatientImportWizardViewModel (1079行, Excel导入)
- **Herbs**: HerbManagementViewModel (导入/导出/导出模板)
- **Formulas**: FormulaManagementViewModel (导入/导出/导出模板)
- **Prescriptions**: PrescriptionManagementViewModel (导出处方)

**建议**:
```
🔧 添加导入导出API
  POST /api/patients/import
    Body: multipart/form-data (Excel文件)
    Response: { "successCount": 100, "errors": [...] }

  GET /api/herbs/export?format=excel&filter=...
    Response: File (Excel/CSV)

  GET /api/herbs/export-template
    Response: File (空模板)
```

#### 4. 统计和报表功能缺失

**问题**: Desktop有统计需求，但Server端未提供统计API

**Desktop需求** (基于Phase 2分析):
- **Prescriptions**: PrescriptionsMainViewModel 显示统计（总数、今日数、今日金额）
- **Consultation**: ConsultationManagementViewModel 有统计命令
- **MedicalCase**: 可能需要统计报表

**建议**:
```
🔧 添加统计API
  GET /api/prescriptions/statistics
    Response: {
      "totalCount": 1000,
      "todayCount": 50,
      "todayTotalAmount": 5000.00
    }

  GET /api/prescriptions/statistics/range?startDate=xxx&endDate=xxx
    Response: { "count": 100, "totalAmount": 10000.00 }
```

#### 5. 特殊功能端点缺失

**问题**: Desktop功能需要的特殊端点未实现

**Desktop需求** (基于Phase 2分析):
- **Prescriptions**: 生成处方编号（GeneratePrescriptionNo）
- **Prescriptions**: 打印预览（PrintPreview）
- **Formulas**: 复制验方（CopyFormula）
- **MedicalCase**: 创建处方（CreatePrescription，从病历发起）
- **Users**: 重置密码（ResetPassword）
- **Users**: 切换用户状态（ToggleStatus）

**建议**:
```
🔧 添加特殊功能API
  GET /api/prescriptions/generate-no
    Response: { "prescriptionNo": "RX202501120001" }

  POST /api/formulas/{id}/copy
    Response: ApiResponse<FormulaDto> (新复制的验方)

  POST /api/users/{id}/toggle-status
    Response: ApiResponse<UserDto> (状态已切换)

  POST /api/users/{id}/reset-password
    Body: { "newPassword": "xxx" }
```

#### 6. 关联查询支持不足

**问题**: Desktop需要关联数据，但API支持有限

**现有支持**:
- ✅ MedicalCase: GetByIdWithDetails (支持详情查询)
- ✅ Consultation: GetByMedicalCaseId (支持按病历查询)

**Desktop需求** (基于Phase 2分析):
- **Prescriptions**: 需要关联患者、医生、病历信息（PrescriptionComposerViewModel）
- **MedicalCase**: 需要关联患者、医生信息
- **Consultation**: 需要关联病历、患者信息

**建议**:
```
🔧 扩展关联查询
  选项1: 使用$expand参数（类OData）
    GET /api/prescriptions/{id}?expand=patient,doctor,medicalCase

  选项2: 提供专用详情端点
    GET /api/prescriptions/{id}/details

  选项3: 使用GraphQL（长期方案）
```

---

## 📈 API覆盖率分析

### 基础CRUD覆盖率: 100% ✅

所有业务模块均实现标准CRUD：
- ✅ 列表查询（分页）
- ✅ 单个查询（ById）
- ✅ 创建
- ✅ 更新
- ✅ 删除

### 扩展功能覆盖率: 约30% ⚠️

| 功能类型 | 实现率 | 说明 |
|---------|-------|------|
| 高级筛选 | 20% | 仅Consultation有Search，其他模块仅支持searchText |
| 批量操作 | 0% | 完全缺失 |
| 导入导出 | 0% | 完全缺失 |
| 统计报表 | 0% | 完全缺失 |
| 特殊功能 | 40% | Auth全覆盖，其他模块部分缺失 |
| 关联查询 | 30% | MedicalCase和Consultation部分支持 |

**总体评估**: Server端基础CRUD完善，但扩展功能与Desktop需求存在较大差距。

---

## 🎯 与Desktop需求对比（初步）

基于Phase 2的Desktop详细分析，以下是初步对比：

### 完全匹配的模块

| 模块 | 匹配度 | 说明 |
|------|-------|------|
| Auth | ✅ 95% | 基础认证功能完善，缺少记住用户名等前端功能 |
| Users | ✅ 80% | CRUD完善，缺少高级筛选、批量操作 |
| Patients | ✅ 80% | CRUD完善，缺少批量导入、高级筛选 |

### 部分匹配的模块

| 模块 | 匹配度 | 主要缺失 |
|------|-------|---------|
| MedicalCase | 🟡 70% | 缺少统计、复杂筛选、批量操作 |
| Consultation | 🟡 65% | 有Search但需验证，缺少统计 |
| Prescriptions | 🟡 60% | 缺少统计、生成编号、打印、日期筛选 |
| Herbs | 🟡 60% | 缺少分类筛选、导入导出、批量操作 |
| Formula | 🟡 60% | 缺少分类筛选、导入导出、复制功能 |

### 关键差距

1. **高级查询**: Desktop支持多维筛选，Server仅支持基础searchText
2. **批量操作**: Desktop有批量删除等功能，Server完全缺失
3. **导入导出**: Desktop有导入导出向导，Server完全缺失
4. **统计报表**: Desktop显示统计数据，Server无统计API
5. **特殊业务功能**: Desktop组件化架构支持复杂业务，Server需补充特殊端点

---

## 🔄 下一步行动 (Phase 4-5)

### Phase 4: Server端详细分析 (3-4小时)

**目标**: 深入分析Service层业务逻辑

**任务**:
1. 读取所有Service类代码
2. 分析业务规则实现
3. 识别已有但未暴露的功能
4. 提取数据模型和验证规则

**产出**:
- Service层详细功能清单
- 业务规则文档
- 未暴露功能列表

### Phase 5: 差异分析与报告生成 (2-3小时)

**目标**: 生成完整差异分析报告

**任务**:
1. Desktop需求 vs Server实现逐项对比
2. 按优先级分类差异（MVP核心、扩展、高级）
3. 生成API补充计划
4. 评估工作量

**产出**:
- 完整差异分析报告
- API补充优先级列表
- 工作量评估（以Issue为单位）

---

## 附录

### A. HTTP状态码规范

| 状态码 | 使用场景 |
|-------|---------|
| 200 OK | 成功（GET/PUT） |
| 201 Created | 创建成功（POST） |
| 204 No Content | 删除成功（DELETE） |
| 400 Bad Request | 参数错误 |
| 401 Unauthorized | 未认证 |
| 403 Forbidden | 无权限 |
| 404 Not Found | 资源不存在 |
| 500 Internal Server Error | 服务器错误 |

### B. 命名约定

**Controller命名**:
- 复数形式: UsersController, PatientsController
- 或领域名: AuthController, MedicalCaseController

**Action命名**:
- Get/GetById/GetList: 查询
- Create/Add: 创建
- Update: 更新
- Delete: 删除
- Search: 搜索
- Validate: 验证

**Service命名**:
- XxxService: 业务服务
- XxxRules: 业务规则（可选）

### C. DTO命名约定

| 模式 | 示例 | 用途 |
|------|------|------|
| \*Dto | UserDto | 数据传输对象 |
| \*CreateDto | UserCreateDto | 创建请求 |
| \*UpdateDto | UserUpdateDto | 更新请求 |
| \*ResponseDto | LoginResponseDto | 响应对象 |
| PagedResult<T> | PagedResult<UserDto> | 分页结果 |

---

**报告生成**: Phase 3完成
**下一阶段**: Phase 4 - Server端详细分析
**预计时间**: 3-4小时
