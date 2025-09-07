# 可疑代码清单（可能被间接引用）

**分析时间**: 2025-09-07  
**分析器**: .NET 代码整洁教练  
**项目**: LYBT中医诊所管理系统

## 🔍 分析说明

以下代码项目被标记为"可疑"，因为它们可能通过以下方式被间接引用：
- 反射调用 (Reflection)
- 依赖注入容器 (IoC Container)
- XAML绑定 (Data Binding)
- JSON/XML序列化
- MediatR消息处理
- ASP.NET路由特性

对于这些项目，建议**不直接删除**，而是先添加 `[Obsolete]` 特性观察一段时间。

## 🔍 已发现的可疑项目

### 1. JSON序列化相关类

这些类包含JSON序列化特性，可能被序列化框架间接使用：

#### 1.1 UserDtos.cs
**文件**: `src/Shared/LYBT.Shared.Models/Contracts/Users/UserDtos.cs`  
**原因**: 包含 `[JsonProperty]` 特性  
**风险级别**: 🟡 中等  
**建议**: 保持观察，这些是API契约类

#### 1.2 PagedResult.cs
**文件**: `src/Shared/LYBT.Shared.Models/Contracts/Common/PagedResult.cs`  
**原因**: 包含 `[JsonProperty]` 特性  
**风险级别**: 🟡 中等  
**建议**: 保持观察，这是通用分页类

#### 1.3 ApiResponse.cs  
**文件**: `src/Shared/LYBT.Shared.Models/Contracts/Common/ApiResponse.cs`  
**原因**: 包含 `[JsonProperty]` 特性  
**风险级别**: 🟡 中等  
**建议**: 保持观察，这是API响应包装类

### 2. 工具类和帮助类

这些类可能通过静态调用或反射被使用：

#### 2.1 CommonHelper
**文件**: `src/Shared/LYBT.Shared.Utilities/Helpers/CommonHelper.cs`  
**原因**: 静态帮助类，可能被多处使用但IDE无法检测到所有引用  
**风险级别**: 🟡 中等  
**建议**: 需要手动搜索所有字符串引用

#### 2.2 PasswordHelper
**文件**: `src/Shared/LYBT.Shared.Utilities/Helpers/PasswordHelper.cs`  
**原因**: 安全相关静态类，可能被认证系统使用  
**风险级别**: 🔴 高  
**建议**: 强烈建议保留，这是核心安全组件

#### 2.3 EnumHelper
**文件**: `src/Shared/LYBT.Shared.Utilities/Helpers/EnumHelper.cs`  
**原因**: 枚举操作帮助类，可能被多处使用  
**风险级别**: 🟡 中等  
**建议**: 检查是否有字符串/反射调用

### 3. 系统常量类

#### 3.1 SystemConstants
**文件**: `src/Shared/LYBT.Shared.Models/Constants/SystemConstants.cs`  
**原因**: 常量类，可能通过反射或字符串访问  
**风险级别**: 🟡 中等  
**建议**: 检查是否有动态常量访问

### 4. 基础架构类

#### 4.1 Specification
**文件**: `src/Server/Core/LYBT.Infrastructure/Specification.cs`  
**原因**: 规约模式基类，可能被泛型或反射使用  
**风险级别**: 🟡 中等  
**建议**: 检查泛型约束和反射使用

#### 4.2 BaseService
**文件**: `src/Server/Core/LYBT.Infrastructure/BaseService.cs`  
**原因**: 基础服务类，可能被继承但IDE无法检测  
**风险级别**: 🟡 中等  
**建议**: 搜索继承关系

### 5. 特殊ViewModel

#### 5.1 PrescriptionViewModelRefactored
**文件**: `src/Client/Desktop/Modules/Prescriptions/ViewModels/PrescriptionViewModelRefactored.cs`  
**原因**: 重构版本的ViewModel，可能是实验性代码  
**风险级别**: 🟡 中等  
**建议**: 确认是否为过时版本，可能可以删除

## 🔍 需要手动验证的项目

### 1. XAML绑定检查
以下类型需要在XAML文件中搜索绑定引用：
- 所有ViewModel类
- 常量类的公共字段
- 枚举类型

### 2. 字符串引用检查
需要搜索以下模式的字符串引用：
- `nameof()` 调用
- 反射的Type.GetType()调用  
- 字符串形式的类型名称

### 3. 配置文件检查
检查以下配置文件中的类型引用：
- appsettings.json
- 各种.config文件
- 模块注册代码

## 📋 观察期建议

### 标记方式
为可疑项目添加观察标记：
```csharp
[Obsolete("Under review for removal - will be removed in 14 days if unused", false)]
public class SuspectClass 
{
    // ...
}
```

### 观察期时长
- **高风险项目**: 30天观察期
- **中等风险项目**: 14天观察期  
- **低风险项目**: 7天观察期

### 监控方式
1. 添加编译警告级别的Obsolete特性
2. 在CI/CD中监控相关警告
3. 定期检查是否有新的引用出现

---
**总结**: 发现了若干可疑的代码项目，主要集中在序列化、帮助类和基础架构类。建议采用渐进式清理策略，先标记观察再决定是否删除。