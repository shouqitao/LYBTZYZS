# Tasks: refactor-service-layer

## Phase 1: 统一返回值类型

### 1.1 Result<T>增强
- [x] 1.1.1 为Result<T>添加静态工厂方法FromException(Exception ex)
- [x] 1.1.2 为Result<T>添加隐式转换操作符（可选）
- [x] 1.1.3 确保Errors和ErrorMessage兼容（单错误时自动填充Errors）

### 1.2 Auth模块迁移
- [x] 1.2.1 更新IAuthService接口返回值为Result<T>
- [x] 1.2.2 更新AuthService实现
- [x] 1.2.3 更新IJwtService和JwtService
- [x] 1.2.4 更新AuthController处理Result<T>
- [x] 1.2.5 更新Auth模块单元测试

### 1.3 ServiceResult废弃
- [x] 1.3.1 标记ServiceResult<T>为[Obsolete]
- [x] 1.3.2 确认无其他引用后删除ServiceResult.cs（可延后）

### 1.4 验证
- [x] 1.4.1 运行Auth模块单元测试
- [x] 1.4.2 运行集成测试验证登录流程
- [x] 1.4.3 Release编译验证

## Phase 2: 引入Service基类

### 2.1 创建BaseService
- [x] 2.1.1 在Infrastructure层创建Services/BaseService.cs
- [x] 2.1.2 实现ExecuteAsync<T>统一错误处理方法
- [x] 2.1.3 实现ValidateAsync<TDto>统一验证方法
- [x] 2.1.4 添加单元测试验证基类行为

### 2.2 PatientService迁移
- [x] 2.2.1 PatientService继承BaseService<Patient>
- [x] 2.2.2 重构CRUD方法使用ExecuteAsync
- [x] 2.2.3 验证PatientServiceTests全部通过

### 2.3 UserService迁移
- [x] 2.3.1 UserService继承BaseService<User>
- [x] 2.3.2 重构CRUD方法使用ExecuteAsync
- [x] 2.3.3 验证UserServiceTests全部通过

### 2.4 HerbService迁移
- [x] 2.4.1 HerbService继承BaseService<Herb>
- [x] 2.4.2 重构CRUD方法使用ExecuteAsync
- [x] 2.4.3 验证HerbServiceTests全部通过

### 2.5 FormulaService迁移
- [x] 2.5.1 FormulaService继承BaseService<Formula>
- [x] 2.5.2 重构CRUD方法使用ExecuteAsync

### 2.6 验证
- [x] 2.6.1 运行所有Service单元测试
- [x] 2.6.2 Release编译验证
- [x] 2.6.3 检查代码重复率降低

## Phase 3: MedicalCaseService直接拆分（无兼容性包袱）

### 3.1 删除原God Class/Interface
- [x] 3.1.1 备份IMedicalCaseService接口定义（仅供参考）
- [x] 3.1.2 删除IMedicalCaseService.cs
- [x] 3.1.3 删除MedicalCaseService.cs

### 3.2 创建职责单一的新接口
- [x] 3.2.1 创建IMedicalCaseCommandService接口（Create, Update, Delete）
- [x] 3.2.2 创建IMedicalCaseQueryService接口（GetById, GetPaged, GetPending, Search）
- [x] 3.2.3 创建IMedicalCaseStateService接口（Complete, Cancel, SaveDraft, UpdateStatus）

### 3.3 实现新Service
- [x] 3.3.1 实现MedicalCaseCommandService（继承BaseService）
- [x] 3.3.2 实现MedicalCaseQueryService（继承BaseService）
- [x] 3.3.3 实现MedicalCaseStateService（继承BaseService）

### 3.4 Controller同步更新
- [x] 3.4.1 更新MedicalCaseController注入（3个Service替代1个）
- [x] 3.4.2 更新Controller方法调用对应Service

### 3.5 DI注册更新
- [x] 3.5.1 移除MedicalCaseService注册
- [x] 3.5.2 注册MedicalCaseCommandService
- [x] 3.5.3 注册MedicalCaseQueryService
- [x] 3.5.4 注册MedicalCaseStateService

### 3.6 测试更新
- [x] 3.6.1 删除MedicalCaseServiceTests
- [x] 3.6.2 创建MedicalCaseCommandServiceTests
- [x] 3.6.3 创建MedicalCaseQueryServiceTests
- [x] 3.6.4 创建MedicalCaseStateServiceTests
- [x] 3.6.5 更新MedicalCaseController集成测试

### 3.7 验证
- [x] 3.7.1 运行所有新Service单元测试
- [x] 3.7.2 运行集成测试
- [x] 3.7.3 Release编译验证

## Phase 4: 验证统一化

### 4.1 创建缺失的Validators
- [x] 4.1.1 FormulaInputDtoValidator
- [x] 4.1.2 ConsultationInputDtoValidator（如需要）
- [x] 4.1.3 MedicalCaseCreateRequestValidator
- [x] 4.1.4 LoginRequestValidator（Auth模块）

### 4.2 集成Validators到Services
- [x] 4.2.1 FormulaService添加验证调用
- [x] 4.2.2 MedicalCaseCommandService添加验证调用
- [x] 4.2.3 AuthService添加验证调用

### 4.3 移除手工验证代码
- [x] 4.3.1 识别并移除Service中的手工验证逻辑
- [x] 4.3.2 确保FluentValidation覆盖所有验证场景

### 4.4 验证
- [x] 4.4.1 运行所有Service单元测试
- [x] 4.4.2 验证验证错误返回格式统一

## Completion Criteria

- [x] 统一使用Result<T>返回值类型
- [x] 所有Service继承BaseService基类
- [x] MedicalCaseService拆分为子服务
- [x] 所有CRUD Service使用FluentValidation
- [x] 编译通过，测试全绿
- [x] 代码行数减少（消除catch块重复）

## 依赖关系

```
Phase 1 ──→ Phase 2 ──→ Phase 3
              │
              └──→ Phase 4

Phase 1: 必须先完成，统一返回值类型
Phase 2: 依赖Phase 1，需要Result<T>
Phase 3: 依赖Phase 2，子服务需要基类
Phase 4: 与Phase 3可并行，仅依赖Phase 2
```

## 风险缓解

| 风险 | 缓解措施 |
|------|---------|
| Auth模块迁移影响登录 | 先在测试环境验证完整登录流程 |
| MedicalCaseService拆分破坏功能 | 保持Facade模式，API不变 |
| 测试覆盖不足 | 每个Phase后运行完整测试套件 |
| 代码冲突 | 及时提交，避免长期分支 |

## Completion Notes (2025-11-30)

所有任务已完成，测试全部通过：
- Infrastructure.Tests: 54/54 通过
- Formula.Tests: 22/22 通过
- Patients.Tests: 54/54 通过
- Herbs.Tests: 34/34 通过
- 所有其他模块测试通过
