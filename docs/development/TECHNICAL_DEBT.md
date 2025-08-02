# 技术债务清单

## 创建日期
2025-08-02

## 1. 路由不一致性

### 1.1 需要修复的控制器
以下控制器不符合我们的REST路由标准：

#### 高优先级（核心业务）
- **RegistrationController** - 挂号管理
  - 使用 `POST` 而非 `POST /add`
  - 使用 `POST /cancel/{id}` 而非 `PATCH /{id}/cancel`
  - 包含 `DELETE` 方法（应该移除）

- **PrescriptionsController** - 处方管理
  - 使用 `POST` 而非 `POST /add`
  - 使用 `PUT` 而非 `PUT /{id}`
  - 使用 `POST /void/{id}` 而非 `PATCH /{id}/void`
  - 包含 `DELETE` 方法（应该移除）

- **BillingController** - 账单管理
  - 使用 `POST` 而非 `POST /add`
  - 使用 `PUT` 而非 `PUT /{id}`
  - 多个状态更新使用 `POST` 而非 `PATCH`
  - 包含 `DELETE` 方法（应该移除）

#### 中优先级（辅助业务）
- **PharmacyController** - 药房管理
  - 使用 `POST` 而非 `POST /add`
  - 使用 `PUT` 而非 `PUT /{id}`
  - 使用 `POST /{id}/prepared` 而非 `PATCH /{id}/prepared`
  - 包含 `DELETE` 方法（应该移除）

- **QueueingController** - 排队管理
  - 使用 `POST` 而非 `POST /add`
  - 使用 `PUT` 而非 `PUT /{id}`
  - 状态更新使用 `POST` 而非 `PATCH`
  - 包含 `DELETE` 方法（应该移除）

### 1.2 已符合标准的控制器
- ✅ UsersController
- ✅ PatientsController
- ✅ HerbsController
- ✅ DoctorsController

### 1.3 迁移策略
根据渐进式迁移原则：
1. **保持现状** - 不主动修复，避免破坏现有功能
2. **按需修复** - 当模块需要功能更新时顺带修复
3. **向后兼容** - 修复时考虑保留旧路由作为过渡

## 2. DTO架构不一致

### 2.1 现状
- 部分模块使用共享DTO（Shared.Models）
- 部分模块使用本地DTO（Module内部）
- 药材模块存在临时DTO映射代码

### 2.2 标准
已制定DTO使用标准文档，采用渐进式迁移策略

### 2.3 待处理
- 药材模块的临时DTO映射（低优先级）
- 其他模块的DTO标准化（按需处理）

## 3. AutoMapper配置

### 3.1 已完成
- ✅ 为患者模块添加了PatientDto用于列表显示
- ✅ 为药材模块添加了HerbDto用于列表显示
- ✅ 修复了药材模块的DTO引用歧义问题

### 3.2 待处理
- 其他模块可能需要类似的列表DTO
- 部分模块仍使用本地DTO（如医生模块）

### 3.3 优先级
中 - 基本功能已修复，剩余为优化

## 4. 缺失功能

### 4.1 分页查询
- 以下控制器缺少 `/paged` 端点：
  - RegistrationController
  - PrescriptionsController
  - BillingController
  - PharmacyController
  - QueueingController
- 建议：按需添加，当数据量大时再实现

### 4.2 批量操作
- 缺少批量启用/禁用功能
- 缺少批量删除功能

## 5. 代码质量

### 5.1 日志记录
- 部分控制器日志不够详细
- 缺少操作审计日志

### 5.2 错误处理
- 错误信息不够友好
- 缺少详细的错误代码

## 6. 性能优化

### 6.1 缓存
- BillingController使用了内存缓存
- 其他控制器未实现缓存策略

### 6.2 查询优化
- 部分查询可能存在N+1问题
- 缺少异步批量查询

## 处理建议

### 立即行动
1. 完善AutoMapper配置（高优先级）
2. 实现缺失的分页查询接口

### 短期计划（1-2周）
1. 统一错误处理机制
2. 增强日志记录

### 长期计划（1-2月）
1. 逐步统一路由标准（按需）
2. 实现统一缓存策略
3. 性能优化

## 风险评估

### 高风险
- 大规模路由修改会影响前端
- AutoMapper配置错误会导致数据问题

### 中风险
- DTO迁移可能引入新的映射问题
- 缓存策略可能导致数据不一致

### 低风险
- 日志增强
- 错误信息优化

## 结论

采用渐进式改进策略，优先处理高影响低风险的问题，对于路由不一致等高风险改动采取"按需修复"原则，确保系统稳定性。