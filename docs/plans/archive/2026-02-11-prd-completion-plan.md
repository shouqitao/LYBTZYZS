# PRD文档全面补全 -- 实施计划

## 概览

基于 `2026-02-11-prd-completion-design.md`，拆分为4个Phase、17个Task。

---

## Phase 1: 新增5个PRD文档 (核心交付)

### Task 1.1: 创建 health-diagnostics.md

- **文件**: `docs/02-requirements/15-health-diagnostics.md`
- **内容**: FR-SYS-001~007，完整PRD格式
- **信息源**:
  - `src/Server/Services/LYBT.WebAPI/Controllers/HealthController.cs` (端点定义)
  - `src/Server/Services/LYBT.WebAPI/Controllers/DiagnosticsController.cs` (端点定义)
  - `docs/04-api-reference/11-health.md` (已有API参考)
  - `docs/04-api-reference/12-diagnostics.md` (已有API参考)
- **包含**: 概述、用户角色、7个FR条目(含双模式)、数据模型、错误码、配置参数、决策记录、变更记录
- **验证**: 与 HealthController/DiagnosticsController 代码逐端点对照

### Task 1.2: 创建 error-handling.md

- **文件**: `docs/02-requirements/13-error-handling.md`
- **内容**: FR-ERR-001~005，完整PRD格式
- **信息源**:
  - `src/Shared/LYBT.Shared.ExceptionHandling/` 目录下所有文件
  - `src/Server/Services/LYBT.WebAPI/` 中的异常处理中间件
  - `src/Client/Desktop/Shell/` 中的 DesktopExceptionHandler
- **包含**: 异常分级体系、ProblemDetails格式、客户端处理策略、严重度枚举、全局错误码注册表

### Task 1.3: 创建 logging.md

- **文件**: `docs/02-requirements/14-logging.md`
- **内容**: FR-LOG-001~004，完整PRD格式
- **信息源**:
  - `src/Shared/LYBT.Shared.Logging/` 目录下所有文件
  - `src/Server/Core/LYBT.Entities/Common/SecurityAuditLog.cs`
  - `src/Server/Core/LYBT.Entities/Common/SystemLog.cs`
- **包含**: 结构化日志体系、安全审计日志实体、脱敏规则、运行时级别管理

### Task 1.4: 创建 desktop-shell.md

- **文件**: `docs/02-requirements/12-desktop-shell.md`
- **内容**: FR-SHELL-001~007，完整PRD格式
- **信息源**:
  - `src/Client/Desktop/Shell/` 目录
  - `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/` (导航、对话框)
  - `src/Client/Desktop/Core/LYBT.Desktop.Foundation/` (基类)
- **包含**: 启动流水线、会话管理、导航系统、菜单、对话框、诊断监控、账户设置

### Task 1.5: 创建 configuration.md

- **文件**: `docs/02-requirements/11-configuration.md`
- **内容**: FR-CFG-001~003，完整PRD格式
- **信息源**:
  - `src/Shared/LYBT.Shared.Configuration/` (所有Options类)
  - `src/Server/Services/LYBT.WebAPI/appsettings.json`
  - `docs/06-operations/02-configuration.md` (已有运维配置文档)
- **包含**: 服务端参数总表、客户端参数总表、环境分层策略、默认值、约束范围

---

## Phase 2: 现有PRD错误码补全 (8个文档)

### Task 2.1: 补充 users.md 错误码

- **信息源**: `src/Server/Modules/LYBT.Module.Users/` Service层异常
- **新增章节**: "错误码" 表格

### Task 2.2: 补充 patients.md 错误码

- **信息源**: `src/Server/Modules/LYBT.Module.Patients/` Service层异常

### Task 2.3: 补充 herbs.md 错误码

- **信息源**: `src/Server/Modules/LYBT.Module.Herbs/` Service层异常

### Task 2.4: 补充 formulas.md 错误码

- **信息源**: `src/Server/Modules/LYBT.Module.Formula/` Service层异常

### Task 2.5: 补充 medical-cases.md 错误码

- **信息源**: `src/Server/Modules/LYBT.Module.MedicalCase/` Service层异常

### Task 2.6: 补充 sync.md / printing.md / card-reader.md 错误码

- **信息源**: 对应模块的异常定义

---

## Phase 3: 验收标准细化 + 产品层修正

### Task 3.1: 细化全部9个PRD的验收标准

- 遍历所有 `- [ ] xxx` 条目
- 改为 `- [ ] [场景] -> [预期结果]` 格式
- 关联已有测试文件 (如有)

### Task 3.2: vision.md 补充版本路线图

- **文件**: `docs/01-product/01-vision.md`
- **新增章节**: "版本路线图"
  - v1.0 Scope: 120个FR清单
  - v2.0 规划: 汇总各模块 v2.0 条目

### Task 3.3: user-roles.md 修正 Receptionist

- **文件**: `docs/01-product/04-user-roles.md`
- **修改**: Receptionist 描述从 "患者登记、预约管理" 修正为实际权限

---

## Phase 4: README索引更新 + 验证

### Task 4.1: 更新 02-requirements/README.md

- 新增5个模块索引
- 更新总功能数 (94 -> 120)
- 更新FR编号规则 (新增模块缩写)

### Task 4.2: 全文档交叉验证

- 检查所有新文档的内部链接
- 验证FR编号无冲突/无跳号
- 确认角色权限矩阵在所有文档中一致

---

## 依赖关系

```
Phase 1 (1.1~1.5 可并行)
  ↓
Phase 2 (2.1~2.6 可并行，1.2 FR-ERR-005全局错误码依赖2.x的错误码收集)
  ↓
Phase 3 (3.1依赖Phase 1+2完成，3.2/3.3可并行)
  ↓
Phase 4 (依赖Phase 1~3全部完成)
```

## 风险

| 风险 | 缓解 |
|------|------|
| 代码中异常类型命名不统一 | 以实际代码为准，PRD统一规范后标注差异 |
| Desktop Shell 内部实现变化快 | PRD聚焦用户可感知行为，不过度描述内部细节 |
| 错误码可能有遗漏 | 通过 Grep 搜索所有 throw/Exception 确保覆盖 |

---

创建时间: 2026-02-11
状态: 待执行
