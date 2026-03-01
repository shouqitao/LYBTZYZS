# LYBT.Shared.Primitives

> 零依赖基础层 | 错误码定义 | 验证常量 | 全项目共享

## 项目定位

- **层级**: Shared (最底层)
- **职责**: 提供全系统共享的基础类型定义，包括错误码枚举、验证常量、错误消息模板
- **状态**: Active

## 目录结构

```
LYBT.Shared.Primitives/
├── ErrorCodes/
│   ├── ErrorCategory.cs        # 错误类别枚举
│   ├── ErrorCode.cs            # MCCEE 分区错误码 (0xxxx~7xxxx)
│   ├── ErrorCodeExtensions.cs  # 错误码扩展 (HTTP映射/格式化)
│   └── ErrorMessages.cs        # 用户友好错误消息
└── Validation/
    └── ValidationConstants.cs  # 长度/范围/正则/消息模板
```

## 核心接口

| 名称 | 说明 |
|------|------|
| ErrorCode | MCCEE 分区错误码枚举 (模块-类别-序号) |
| ErrorCategory | 错误类别 (General/User/Patient/MedicalCase...) |
| ErrorCodeExtensions | HTTP 状态码映射、格式化字符串 |
| ValidationConstants | 长度限制、数值范围、正则表达式、错误消息模板 |

## 设计依据

- 零外部依赖设计，确保可被所有层级引用
- MCCEE 错误码分区 (M=模块 CC=子类别 EE=序号) 支持模块化扩展
- 验证常量集中管理，避免硬编码散落各处

## 依赖关系

### 依赖
- System.ComponentModel.Annotations (NuGet)

### 被依赖
- LYBT.Shared.Models (错误码引用)
- LYBT.Shared.ExceptionHandling (异常类使用错误码)
- LYBT.Shared.Logging (错误码日志)
- LYBT.Desktop.Models, Desktop.Herbs/Formula/MedicalCase/Patients/Users/Sync (验证常量)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 创建 README |
| 2026-02 | 审计遗留项清理 (AuthErrorCode 枚举删除) |
| 2025-12 | MCCEE 错误码分区体系建立 |
