# Desktop Layer WebAPI Integration Test Plan

## 测试目标
创建 Desktop 层集成测试，真实连接 WebAPI，按照 Postman 测试顺序验证核心业务流程。

## 测试范围
- **测试类型**: Desktop ↔ WebAPI 集成测试
- **测试框架**: xUnit + FluentAssertions + NSubstitute
- **测试数据库**: SQLite InMemory (本地) + SQL Server (远程 API)
- **测试夹具**: `UserJourneyFixture` 扩展支持 WebAPI 连接

## API 测试顺序 (按 Postman Collection 顺序)

### Phase 1: 认证与基础 (Authentication & Foundation)
| # | 测试类 | 测试场景 | API 端点 |
|---|--------|----------|----------|
| 1.1 | `AuthIntegrationTests` | 管理员登录成功 | POST /auth/login |
| 1.2 | `AuthIntegrationTests` | Token 验证 | GET /auth/validate |
| 1.3 | `AuthIntegrationTests` | Token 刷新 | POST /auth/refresh |
| 1.4 | `AuthIntegrationTests` | 获取当前用户信息 | GET /users/current |
| 1.5 | `HealthCheckTests` | 健康检查 | GET /health, /health/details |

### Phase 2: 用户管理 (User Management) - AdminOnly
| # | 测试类 | 测试场景 | API 端点 |
|---|--------|----------|----------|
| 2.1 | `UserManagementTests` | 创建医生用户 | POST /users |
| 2.2 | `UserManagementTests` | 获取用户列表 | GET /users |
| 2.3 | `UserManagementTests` | 获取用户详情 | GET /users/{id} |
| 2.4 | `UserManagementTests` | 更新用户信息 | PUT /users/{id} |
| 2.5 | `UserManagementTests` | 重置密码 | POST /users/{id}/reset-password |
| 2.6 | `UserManagementTests` | 切换用户状态 | POST /users/{id}/toggle-status |
| 2.7 | `UserManagementTests` | 删除用户 (软删除) | DELETE /users/{id} |
| 2.8 | `UserManagementTests` | 恢复用户 | POST /users/{id}/restore |

### Phase 3: 患者管理 (Patient Management)
| # | 测试类 | 测试场景 | API 端点 |
|---|--------|----------|----------|
| 3.1 | `PatientManagementTests` | 创建患者 | POST /patients |
| 3.2 | `PatientManagementTests` | 获取患者列表 | GET /patients |
| 3.3 | `PatientManagementTests` | 获取患者详情 | GET /patients/{id} |
| 3.4 | `PatientManagementTests` | 更新患者信息 | PUT /patients/{id} |
| 3.5 | `PatientManagementTests` | 引用检查 | POST /patients/{id}/check-reference |
| 3.6 | `PatientManagementTests` | 删除患者 (软删除) | DELETE /patients/{id} |
| 3.7 | `PatientManagementTests` | 恢复患者 | POST /patients/{id}/restore |

### Phase 4: 药材管理 (Herb Management)
| # | 测试类 | 测试场景 | API 端点 |
|---|--------|----------|----------|
| 4.1 | `HerbManagementTests` | 创建药材 | POST /herbs |
| 4.2 | `HerbManagementTests` | 获取药材列表 | GET /herbs |
| 4.3 | `HerbManagementTests` | 获取药材详情 | GET /herbs/{id} |
| 4.4 | `HerbManagementTests` | 更新药材 | PUT /herbs/{id} |
| 4.5 | `HerbManagementTests` | 引用检查 | POST /herbs/{id}/check-reference |
| 4.6 | `HerbManagementTests` | 删除药材 (软删除) | DELETE /herbs/{id} |

### Phase 5: 验方管理 (Formula Management)
| # | 测试类 | 测试场景 | API 端点 |
|---|--------|----------|----------|
| 5.1 | `FormulaManagementTests` | 创建验方 | POST /formulas |
| 5.2 | `FormulaManagementTests` | 获取验方列表 | GET /formulas |
| 5.3 | `FormulaManagementTests` | 获取验方详情 | GET /formulas/{id} |
| 5.4 | `FormulaManagementTests` | 更新验方 | PUT /formulas/{id} |
| 5.5 | `FormulaManagementTests` | 删除验方 (软删除) | DELETE /formulas/{id} |

### Phase 6: 医案管理 (Medical Case Management) - 核心业务
| # | 测试类 | 测试场景 | API 端点 |
|---|--------|----------|----------|
| 6.1 | `MedicalCaseWorkflowTests` | 创建医案 | POST /medicalcases |
| 6.2 | `MedicalCaseWorkflowTests` | 获取医案详情 | GET /medicalcases/{id} |
| 6.3 | `MedicalCaseWorkflowTests` | 更新诊断信息 | PUT /medicalcases/{id} |
| 6.4 | `MedicalCaseWorkflowTests` | 标记需要处方 | PUT /medicalcases/{id}/prescription-flag |
| 6.5 | `MedicalCaseWorkflowTests` | 添加处方药材 | PUT /medicalcases/{id} |
| 6.6 | `MedicalCaseWorkflowTests` | 完成医案 | PUT /medicalcases/{id}/close |
| 6.7 | `MedicalCaseWorkflowTests` | 获取医案列表 | GET /medicalcases |
| 6.8 | `MedicalCaseWorkflowTests` | 挂起医案 | PUT /medicalcases/{id}/suspend |
| 6.9 | `MedicalCaseWorkflowTests` | 取消医案 | PUT /medicalcases/{id}/cancel |

### Phase 7: 数据同步 (Data Sync) - 本地/远程双模式
| # | 测试类 | 测试场景 | API 端点 |
|---|--------|----------|----------|
| 7.1 | `DataSyncTests` | 获取同步元数据 | GET /sync/metadata |
| 7.2 | `DataSyncTests` | 比对差异 | POST /sync/compare |
| 7.3 | `DataSyncTests` | 上传本地数据 | POST /sync/upload |
| 7.4 | `DataSyncTests` | 下载服务端数据 | POST /sync/download |

### Phase 8: 端到端场景 (End-to-End Scenarios)
| # | 测试类 | 测试场景 | 覆盖流程 |
|---|--------|----------|----------|
| 8.1 | `ClinicalWorkflowE2ETests` | 完整诊疗流程 | 登录→创建患者→创建医案→诊断→开方→完成 |
| 8.2 | `AdminWorkflowE2ETests` | 管理员工作流 | 登录→创建医生→创建药材→创建验方→管理患者 |
| 8.3 | `DataSyncE2ETests` | 数据同步流程 | 本地数据→上传→下载→验证一致性 |

## 测试基础设施需求

### 1. WebAPI 测试夹具扩展
```csharp
public class WebApiTestFixture : UserJourneyFixture
{
    // WebAPI 基础 URL (从配置读取)
    public string ApiBaseUrl { get; }
    
    // HttpClient 工厂
    public HttpClient CreateAuthenticatedClient(string token);
    
    // 认证令牌管理
    public AuthTokens AdminTokens { get; private set; }
    public AuthTokens DoctorTokens { get; private set; }
    
    // 测试数据清理
    public async Task CleanupTestDataAsync();
}
```

### 2. 认证令牌管理
```csharp
public class AuthTokens
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string AutoLoginToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string Role { get; set; }
}
```

### 3. 测试数据工厂扩展
```csharp
public static class TestDataFactory
{
    // API 请求 DTO 创建
    public static LoginRequestDto CreateLoginRequest(string username, string password);
    public static UserInputDto CreateUserInput(string role = "Doctor");
    public static PatientInputDto CreatePatientInput();
    public static HerbInputDto CreateHerbInput();
    public static FormulaInputDto CreateFormulaInput();
    public static MedicalCaseInputDto CreateMedicalCaseInput(Guid patientId);
    
    // 唯一性生成器
    public static string GenerateUniqueUserName();
    public static string GenerateUniquePatientName();
    public static string GenerateUniqueHerbName();
}
```

### 4. API 响应断言
```csharp
public static class ApiResponseAssertions
{
    public static void ShouldBeSuccess<T>(this ApiResponse<T> response);
    public static void ShouldHaveStatusCode(this HttpResponseMessage response, HttpStatusCode expected);
    public static T ShouldHaveData<T>(this ApiResponse<T> response);
    public static void ShouldHaveErrorCode(this ApiResponse response, string errorCode);
}
```

## 测试配置

### appsettings.Test.json
```json
{
  "WebApi": {
    "BaseUrl": "https://localhost:5001",
    "Timeout": 30
  },
  "TestData": {
    "AdminUserName": "sysadmin",
    "AdminPassword": "Admin@123",
    "CleanupAfterTest": true
  },
  "RetryPolicy": {
    "MaxRetries": 3,
    "DelayMilliseconds": 1000
  }
}
```

## 执行顺序与依赖

```
Phase 1: 认证与基础 (无依赖)
    ↓
Phase 2: 用户管理 (依赖 Phase 1)
    ↓
Phase 3: 患者管理 (依赖 Phase 1)
    ↓
Phase 4: 药材管理 (依赖 Phase 1)
    ↓
Phase 5: 验方管理 (依赖 Phase 1, 4)
    ↓
Phase 6: 医案管理 (依赖 Phase 1, 2, 3, 4)
    ↓
Phase 7: 数据同步 (依赖 Phase 1, 3, 4)
    ↓
Phase 8: 端到端场景 (依赖 1-7)
```

## 测试 trait 分类

```csharp
[Trait("Category", "WebApiIntegration")]
[Trait("Phase", "1")]
[Trait("Module", "Auth")]
[Trait("Priority", "High")]
```

## 进度跟踪

| Phase | 状态 | 完成度 | 备注 |
|-------|------|--------|------|
| Phase 1 | ⏳ 计划中 | 0% | 基础认证测试 |
| Phase 2 | ⏳ 计划中 | 0% | 用户管理 CRUD |
| Phase 3 | ⏳ 计划中 | 0% | 患者管理 CRUD |
| Phase 4 | ⏳ 计划中 | 0% | 药材管理 CRUD |
| Phase 5 | ⏳ 计划中 | 0% | 验方管理 CRUD |
| Phase 6 | ⏳ 计划中 | 0% | 医案工作流 (核心) |
| Phase 7 | ⏳ 计划中 | 0% | 数据同步 |
| Phase 8 | ⏳ 计划中 | 0% | 端到端场景 |

## 风险与注意事项

1. **并发执行**: 测试数据需唯一命名，避免并行冲突
2. **测试数据清理**: 每个测试类结束后清理创建的数据
3. **网络超时**: 添加重试策略，处理偶发网络问题
4. **数据库状态**: WebAPI 使用真实 SQL Server，注意数据一致性
5. **认证令牌过期**: 测试期间自动刷新 Token

## 下一步行动

1. [ ] 创建 WebApiTestFixture 扩展类
2. [ ] 实现认证令牌管理
3. [ ] 扩展 TestDataFactory (API DTO)
4. [ ] 实现 Phase 1 认证测试
5. [ ] 实现 Phase 2 用户管理测试
6. [ ] 实现 Phase 3 患者管理测试
7. [ ] 实现 Phase 6 医案核心测试
8. [ ] 实现 Phase 8 端到端场景

---
*创建时间: 2026-04-04*
*计划版本: v1.0*
