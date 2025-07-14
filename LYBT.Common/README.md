## AGENTS.md — 通用模块（LYBT.Common）

### 1. Agent 概述

通用模块提供系统级通用类型、基础枚举、统一响应模型、工具方法等，支撑各业务模块开发、数据交换和基础约定，作为所有功能模块的“工具箱”。

### 2. 核心能力

- 定义系统基础枚举（性别、状态、角色、挂号/诊疗/账单/处方等状态）
- 统一 API 响应对象/通用返回结构（如 ApiResponse、PagedResult 等）
- 通用扩展方法（如枚举转中文、时间转换等）
- 常用校验、格式化、映射工具
- 枚举映射与下拉数据源生成

### 3. 输入输出规范

#### 输入

- 通用模块多为静态方法或类型，无单独业务输入
- 作为参数被其他模块调用（如：Gender、Status 枚举类型）

#### 输出

- 枚举定义、响应类型、扩展方法结果
- 用于业务接口的标准数据结构

### 4. 协作与依赖模块

- **全部业务模块**：依赖基础枚举和通用类型
- **UI/WPF 前端**：通过 EnumHelper 等生成绑定数据源、下拉列表
- **基础设施/模型层**：引用通用枚举定义字段

### 5. 示例场景

#### 响应结构封装

```csharp
var result = ApiResponse.Success(data: list, message: "操作成功");
```

#### 获取枚举下拉项

```csharp
var items = EnumHelper.BuildComboBoxSource<Gender>(); // 用于前端下拉
```

#### 校验与格式化

```csharp
var phone = ExcelUtils.FormatPhone("13312345678");
var isOk = ExcelUtils.CheckIdNumber("440101199912121234");
```

### 6. 典型类型/工具列表

- `Gender`、`UserStatus`、`RegistrationStatus`、`PrescriptionStatus`、`BillingStatus` 等基础枚举
- `ApiResponse<T>`、`PagedResult<T>` 通用返回对象
- `EnumHelper` 等扩展方法类
- `ExcelUtils` 通用工具类（字符串格式化、校验、网络状态等）
- 枚举转中文辅助（`.ToChinese()`）
- `LogHelper` 基础日志工具

