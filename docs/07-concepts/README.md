# 07 - 技术概念与模块文档

> 从 LLM Wiki 同步的技术概念页面，覆盖架构、业务规则、开发规范等核心知识。

## 目录结构

| 目录 | 内容 | 文件数 |
|------|------|--------|
| `/` | 核心技术概念 | 35 |
| `modules/` | 业务模块概述 | 9 |
| `development/` | 开发指南 | 3 |

## 核心概念

### 架构
- `dual-mode-architecture.md` — 双模式架构（远程/本地）
- `single-window-architecture.md` — 单窗口架构
- `embedded-kestrel-architecture.md` — 嵌入式 Kestrel
- `startup-pipeline.md` — 启动管线
- `workspace-modes.md` — 工作区模式（临床/管理）
- `clinical-vs-management-mode.md` — 临床 vs 管理模式

### 业务流程
- `clinical-workflow.md` — 临床工作流
- `patient-status-lifecycle.md` — 患者状态生命周期
- `registration-lifecycle.md` — 挂号生命周期
- `medical-case-locking-rules.md` — 医案锁定规则
- `edit-mode-state-machine.md` — 编辑模式状态机
- `prescription-completeness-checker.md` — 处方完整性检查
- `sync-conflict-resolution.md` — 同步冲突解决
- `print-protection.md` — 打印保护

### 安全
- `authentication.md` — 认证
- `authorization-policies.md` — 授权策略
- `sensitive-data-classification.md` — 敏感数据分类
- `password-management-strategy.md` — 密码管理策略

### 技术模式
- `caching-strategy.md` — 缓存策略
- `memory-cache-management.md` — 内存缓存管理
- `herb-cache-strategy.md` — 药材缓存策略
- `batch-operation-pattern.md` — 批量操作模式
- `api-response-envelope.md` — API 响应信封
- `exception-hierarchy.md` — 异常层次结构
- `error-handling.md` — 错误处理
- `feature-toggles.md` — 功能开关
- `mvvm-prism.md` — MVVM Prism 模式
- `mapperly.md` — Mapperly 映射
- `validator-architecture.md` — 验证器架构
- `cross-module-communication.md` — 跨模块通信
- `pinyin-search-implementation.md` — 拼音搜索实现
- `formula-validation-workflow.md` — 验方验证工作流
- `testing-strategy.md` — 测试策略
- `zero-mock-strategy.md` — 零 Mock 策略

### UI
- `menu-visibility-matrix.md` — 菜单可见性矩阵

## 模块文档 (modules/)

9 个业务模块概述：Auth、Users、Patient、Herb、Formula、MedicalCase、Registration、Sync、Printing。

## 开发指南 (development/)

- `build-and-run.md` — 构建与运行
- `common-pitfalls.md` — 常见陷阱
- `terminology.md` — 术语表
