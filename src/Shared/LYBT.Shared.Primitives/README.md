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

## 开发笔记

# LYBT.Shared.Primitives 代码知识

最底层基础类型库，定义统一错误码体系和验证常量，无外部依赖，被 ExceptionHandling/Models/Infrastructure 等多层引用。

## 代码文件结构

```
ErrorCodes/
├── ErrorCategory.cs        # 错误类别和严重程度枚举
├── ErrorCode.cs            # 统一错误码枚举 (MCCEE 分区)
├── ErrorMessages.cs        # 错误码中英文消息映射
└── ErrorCodeExtensions.cs  # 错误码扩展方法 (HTTP映射/类别映射)
Validation/
└── ValidationConstants.cs  # 统一验证规则常量
```

### ErrorCodes/ErrorCategory.cs
**ErrorCategory** (enum) | 错误分类，值: General/Validation/Authentication/Authorization/Resource/Business/Concurrency/System/External/Configuration/Network/Unknown

**ErrorSeverity** (enum) | 错误严重程度，值: Info/Warning/Error/Critical/Fatal

### ErrorCodes/ErrorCode.cs
**ErrorCode** (enum) | 统一错误码定义，MCCEE 编码规则 (M=模块, CC=子类别, EE=序号)

分区规则:
- 0xxxx: 通用错误 (Unknown/InvalidRequest/NotFound/ValidationFailed 等 13 项)
- 1xxxx: 用户模块 (UserNotFound/UserNameExists 等 + 101xx~103xx Auth MCCEE)
- 2xxxx: 患者模块 (PatientNotFound 等 + 207xx 业务规则 + 208xx 导入错误)
- 3xxxx: 医案模块 (MedicalCaseNotFound 等 + 301xx~306xx MCCEE)
- 4xxxx: 处方模块 (PrescriptionNotFound 等 7 项)
- 5xxxx: 草药模块 (HerbNotFound 等 + 501xx~503xx MCCEE)
- 6xxxx: 配方模块 (FormulaNotFound 等 + 601xx~603xx MCCEE)
- 7xxxx: 同步模块 (701xx~705xx 服务端/客户端错误)

### ErrorCodes/ErrorMessages.cs
**ErrorMessages** (static class) | 错误码到中英文消息的映射，覆盖全部 ErrorCode 值

| 方法 | 说明 |
|------|------|
| Get(ErrorCode, bool useEnglish) | 获取错误消息，默认中文 |
| GetFormatted(ErrorCode, bool, params object[]) | 获取格式化的错误消息 |
| GetUserMessage(ErrorCode) | 获取用户友好消息 (中文) |
| GetTechnicalMessage(ErrorCode) | 获取技术消息 (英文) |
| GetEnglish(ErrorCode) | 获取英文消息 (别名) |

### ErrorCodes/ErrorCodeExtensions.cs
**ErrorCodeExtensions** (static class) | ErrorCode 扩展方法

| 方法 | 说明 |
|------|------|
| ToHttpStatusCode() | 错误码映射到 HTTP 状态码 (400/401/403/404/409/422/429/500/503) |
| ToCategory() | 错误码映射到 ErrorCategory |
| GetModuleName() | 根据数值区间返回模块名称 (General/Users/Patients 等) |
| ToFormattedString() | 格式化为 "ERR-30001" 格式 |

### Validation/ValidationConstants.cs
**ValidationConstants** (static class) | 集中管理验证规则常量

包含:
- 长度限制: NameMaxLength(100)/RemarkMaxLength(1000)/PhoneMaxLength(20)/IdCardMaxLength(18) 等 13 项
- 数值范围: DosageCountMinValue(1)/HerbDosageMinValue(0.1)/PriceMinValue(0.01)/AgeMaxValue(200) 等 8 项
- 正则表达式: IdCardRegex/PhoneRegex/EmailRegex
- 错误消息模板: RequiredErrorMessage/MaxLengthErrorMessage/RangeErrorMessage 等 9 项
- 业务规则: PrescriptionDetailsMinCount(1)/PrescriptionDetailsMaxCount(50)/DefaultDosageCount(3)

## 死代码与废弃标记

| 类型/方法 | 状态 | 替代方案 | 清理计划 |
|-----------|------|----------|----------|
| ErrorCodeExtensions.GetModuleName() | [SUSPECT] | 无 | src/ 中无调用，仅测试项目引用，考虑是否保留 |
| ErrorSeverity | [SUSPECT] | 无 | 仅自身定义和 Models/ExceptionHandling 引用，实际 Service 层未使用 |

## 设计分析

| 文件/目录 | 问题 | 分析 | 建议 |
|-----------|------|------|------|
| ErrorCodeExtensions.cs | ToHttpStatusCode/ToCategory 使用巨型 switch 表达式 | 每新增 ErrorCode 需要在 3 处同步维护 (ErrorCode + ErrorMessages + ErrorCodeExtensions)，易遗漏 | 可考虑属性标注或字典映射方式减少维护负担 |
| ErrorCode.cs | 旧编码 (30001~30008) 与新 MCCEE 编码 (301xx~306xx) 并存 | OpenSpec T3-X1-12 标注保留兼容 | 完成迁移后统一到 MCCEE 编码 |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| 新增 ErrorCode 后忘记同步 ErrorMessages 和 ErrorCodeExtensions | 三处定义需要手动同步 | 编译不会报错，运行时 Get() 会回退到 ToString()，开发时需逐一检查 |
| ValidationConstants 中的常量被 Server 和 Desktop 两端共用 | 修改常量值影响全局验证行为 | 修改前确认两端影响范围 |
