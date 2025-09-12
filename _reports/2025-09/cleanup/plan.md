# P1 Safe Dead Code Cleanup - 执行计划

> **生成时间**: 2025-09-12  
> **目标**: 安全清理未被引用的内部代码，保持构建零警告零错误  
> **范围**: Herbs, Formula, Patients, Consultation 模块

## 📋 筛选规则与白名单

### 保留条件（命中任一条即保留）
- ✅ **Public API**: 所有 public 类、接口、方法、属性
- ✅ **DTO/实体**: 继承自实体基类或标记 DataContract 的类
- ✅ **DI注册类型**: 被依赖注入容器注册的服务类
- ✅ **序列化属性**: 带 JsonPropertyName, DataMember 等特性
- ✅ **反射目标**: 可能被 nameof, Activator.CreateInstance 使用
- ✅ **XAML绑定**: WPF 前端绑定目标（本项目不涉及）
- ✅ **测试代码**: 测试夹具、数据生成器、Mock 对象

### 清理目标
- ❌ **未引用的 internal/private 成员**
- ❌ **未使用的 using 语句**
- ❌ **重复的空实现**
- ❌ **条件编译的死代码**

## 🔍 分析结果概览

| 模块 | 接口实现问题 | 未使用方法 | 存根方法 | 过时代码块 | 风险评估 |
|------|-------------|------------|----------|------------|----------|
| **Herbs** | 2个接口未实现 | 3个Repository方法 | 0个 | 0个 | 🟡 中风险 |
| **Formula** | 2个接口未实现 | 0个 | 6个Service存根 | 1个条件编译块 | 🟡 中风险 |
| **Patients** | 0个 | 0个 | 0个 | 0个 | 🟢 无风险 |
| **Consultation** | 0个 | 0个 | 0个 | 0个 | 🟢 无风险 |

## 📊 候选清理项目明细

### LYBT.Module.Herbs

#### 🚨 接口实现不匹配（需修复，非删除）
```
项目: LYBT.Module.Herbs
路径: Interfaces/IHerbQueryService.cs
类型: Interface
问题: 定义但未被 HerbQueryService 类实现
处理: 修复实现声明或删除接口
证据: class HerbQueryService 未继承 IHerbQueryService
```

```
项目: LYBT.Module.Herbs
路径: Interfaces/IHerbBusinessService.cs  
类型: Interface
问题: 定义但未被 HerbBusinessService 类实现
处理: 修复实现声明或删除接口
证据: class HerbBusinessService 未继承 IHerbBusinessService
```

#### ⚠️ Repository 方法候选（仅测试使用）
```
项目: LYBT.Module.Herbs
路径: Repositories/HerbRepository.cs
符号: ExistsByNameAsync
类型: Method (private/internal)
处理: 标记 [Obsolete] 或移除
证据: 仅在单元测试 HerbRepositoryTests 中调用
原因: 未被业务逻辑使用的Repository方法
```

```
项目: LYBT.Module.Herbs
路径: Repositories/HerbRepository.cs
符号: SearchByPinyinAsync
类型: Method (private/internal)  
处理: 标记 [Obsolete] 或移除
证据: 仅在测试中调用，Service层未使用
原因: 测试专用方法，可能为开发期间留存
```

```
项目: LYBT.Module.Herbs
路径: Repositories/HerbRepository.cs
符号: AddRangeAsync
类型: Method (private/internal)
处理: 标记 [Obsolete] 或移除  
证据: 仅在测试数据准备中使用
原因: 批量插入方法未被业务流程使用
```

### LYBT.Module.Formula

#### 🚨 接口实现不匹配（需修复，非删除）
```
项目: LYBT.Module.Formula
路径: Interfaces/IFormulaQueryService.cs
类型: Interface
问题: 定义但未被 FormulaQueryService 类实现
处理: 修复实现声明或删除接口
证据: class FormulaQueryService 未继承 IFormulaQueryService
```

```
项目: LYBT.Module.Formula
路径: Interfaces/IFormulaBusinessService.cs
类型: Interface  
问题: 定义但未被 FormulaBusinessService 类实现
处理: 修复实现声明或删除接口
证据: class FormulaBusinessService 未继承 IFormulaBusinessService
```

#### ❌ 条件编译死代码块
```
项目: LYBT.Module.Formula
路径: Services/FormulaQueryService.cs
符号: GetSmartRecommendationsAsync 及相关方法
类型: Conditional Compilation Block
处理: 删除整个 #if ENABLE_SMART_FEATURES 代码段
证据: ENABLE_SMART_FEATURES 未在项目文件中定义
原因: 过时的推荐算法功能，已标记 [Obsolete]
```

#### ⚠️ 存根方法（空实现）
```
项目: LYBT.Module.Formula  
路径: Services/FormulaService.cs
符号: GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync, ExistsAsync, IsNameDuplicatedAsync
类型: Method (存根实现)
处理: 实现功能或移除方法
证据: 所有方法返回固定失败消息或 false
原因: 未完成的功能实现，需要决策是否保留
```

### LYBT.Module.Patients ✅
```
项目: LYBT.Module.Patients
状态: 架构一致，无死代码发现
接口实现: PatientQueryService : IPatientQueryService ✅
        PatientBusinessService : IPatientBusinessService ✅
```

### LYBT.Module.Consultation ✅  
```
项目: LYBT.Module.Consultation
状态: 架构一致，无死代码发现
接口实现: ConsultationQueryService : IConsultationQueryService ✅  
        ConsultationBusinessService : IConsultationBusinessService ✅
```

## 🎯 执行策略

### 批次一：Herbs / Formula（架构修复）
**重点**: 先修复接口实现问题，再清理死代码
1. 修复接口继承声明
2. 清理条件编译死代码
3. 处理未使用的Repository方法

### 批次二：Patients / Consultation（维护清理）
**重点**: 轻微清理，主要是import整理
1. 清理未使用的 using 语句
2. 格式优化

### 批次三：公共符号标注
**重点**: 不确定的public符号标记为过时
1. 评估接口定义是否需要保留
2. 标记过时API

## 💾 变更追踪
每个批次执行后，更新 `changes.csv` 记录具体变更项目。

## ⚠️ 风险控制
- 每个提交独立验证：dotnet format + build + test
- 遇到构建失败立即回滚对应提交
- 不确定的public符号不删除，仅标记过时
- 保持所有DTO、实体类、DI注册完整性