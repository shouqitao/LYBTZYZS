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
- `01-dual-mode-architecture.md` — 双模式架构（远程/本地）
- `02-embedded-kestrel.md` — 嵌入式 Kestrel
- `03-single-window-architecture.md` — 单窗口架构
- `04-workspace-modes.md` — 工作区模式（临床/管理）
- `05-clinical-vs-management-mode.md` — 临床 vs 管理模式
- `06-clinical-workflow.md` — 临床工作流
- `07-authentication.md` — 认证
- `08-authorization-policies.md` — 授权策略
- `09-password-management.md` — 密码管理策略
- `10-sensitive-data.md` — 敏感数据分类

### 业务流程
- `11-error-handling.md` — 错误处理
- `12-exception-hierarchy.md` — 异常层次结构
- `13-api-response-envelope.md` — API 响应信封
- `14-feature-toggles.md` — 功能开关
- `15-caching-strategy.md` — 缓存策略
- `16-herb-cache-strategy.md` — 药材缓存策略
- `17-memory-cache-management.md` — 内存缓存管理
- `18-mvvm-prism.md` — MVVM Prism 模式
- `19-mapperly.md` — Mapperly 映射
- `20-startup-pipeline.md` — 启动管线
- `21-edit-mode-state-machine.md` — 编辑模式状态机
- `22-menu-visibility-matrix.md` — 菜单可见性矩阵
- `23-cross-module-communication.md` — 跨模块通信
- `24-testing-strategy.md` — 测试策略
- `25-zero-mock-strategy.md` — 零 Mock 策略
- `26-print-protection.md` — 打印保护
- `27-pinyin-search.md` — 拼音搜索实现
- `28-formula-validation-workflow.md` — 验方验证工作流
- `29-prescription-completeness-checker.md` — 处方完整性检查
- `30-patient-status-lifecycle.md` — 患者状态生命周期
- `31-medical-case-locking-rules.md` — 医案锁定规则
- `32-registration-lifecycle.md` — 挂号生命周期
- `33-sync-conflict-resolution.md` — 同步冲突解决
- `34-batch-operation-pattern.md` — 批量操作模式
- `35-validator-architecture.md` — 验证器架构

## 模块文档 (modules/)

9 个业务模块概述：Auth、Users、Patient、Herb、Formula、MedicalCase、Registration、Sync、Printing。

## 开发指南 (development/)

- `build-and-run.md` — 构建与运行
- `common-pitfalls.md` — 常见陷阱
- `terminology.md` — 术语表
