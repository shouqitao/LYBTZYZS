# Shared 层

> 前后端共享组件库，提供统一的 DTO 契约、枚举、验证器、工具类

## 架构概览

Shared 层是 Server 和 Desktop 的公共依赖，实现契约驱动设计。
所有 DTO 采用分层继承体系: BaseDto -> StatusDto，查询类继承 PagedQueryBaseDto。
统一响应格式 ApiResponse<T> 和 ServiceResult<T> 确保前后端数据契约一致。

DTO 已完成三阶段优化: 查询命名标准化、操作结果基类抽取、继承层次优化。

## 项目列表

| 项目 | 职责 | 状态 |
|------|------|------|
| LYBT.Shared.Models | DTO、响应模型、枚举、常量 | 稳定 |
| LYBT.Shared.Primitives | 核心原语（基类、接口定义） | 稳定 |
| LYBT.Shared.Validators | FluentValidation 验证器 | 稳定 |
| LYBT.Shared.Utilities | 扩展方法、帮助类 | 稳定 |
| LYBT.Shared.Components | 共享 UI 组件 | 稳定 |
| LYBT.Shared.Configuration | 配置管理共享逻辑 | 稳定 |
| LYBT.Shared.ExceptionHandling | 统一异常处理 | 稳定 |
| LYBT.Shared.Logging | 日志基础设施 | 稳定 |

## 目录结构

```
src/Shared/
├── LYBT.Shared.Models/
├── LYBT.Shared.Primitives/
├── LYBT.Shared.Validators/
├── LYBT.Shared.Utilities/
├── LYBT.Shared.Components/
├── LYBT.Shared.Configuration/
├── LYBT.Shared.ExceptionHandling/
└── LYBT.Shared.Logging/
```

## 依赖关系

```
Server.Modules  -> Shared.Models / Shared.Validators / Shared.Utilities
Desktop.Modules -> Shared.Models / Shared.Components / Shared.Utilities
```

- **被依赖**: Server 层和 Desktop 层均引用
- **自身无外部层依赖**: 仅依赖 .NET BCL 和第三方库 (FluentValidation, System.Text.Json)

## DTO 继承体系

```
BaseDto (Id)
  └── StatusDto (+ Status)
PagedQueryBaseDto (PageIndex, PageSize, OrderBy, Keyword)
UserInputBaseDto / PatientInputBaseDto ... (共享输入字段)
```

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 精简 README，详细内容迁移至 CLAUDE.md |
| 2025-09-20 | DTO 三阶段优化完成 |
