# 代码清理白名单（保护列表）

**分析时间**: 2025-09-07  
**分析器**: .NET 代码整洁教练  
**项目**: LYBT中医诊所管理系统

## 🛡️ 保护原则

以下类型的代码**绝对不能删除**，即使IDE显示为"未使用"：

### 1. 对外公共API
- **WebAPI Controllers**: 所有Controller类和Action方法
- **公共SDK接口**: IService等共享接口
- **数据传输对象**: DTO类（可能被序列化使用）

### 2. 依赖注入相关
- **服务注册**: ServiceCollection扩展方法
- **模块注册**: Prism模块类
- **Scrutor扫描**: 约定式注册的类型

### 3. 反射访问
- **配置映射**: AutoMapper Profile类
- **实体模型**: Entity类（EF Core使用）
- **特性标记**: 带有序列化特性的类

### 4. XAML绑定
- **ViewModel类**: WPF数据绑定使用
- **转换器类**: IValueConverter实现
- **资源字典**: 样式和模板定义

### 5. 数据库相关
- **Entity类**: 数据库实体模型
- **迁移文件**: EF Core Migrations
- **配置类**: EntityTypeConfiguration

### 6. 安全和核心功能
- **认证相关**: JWT、密码处理等
- **异常处理**: 全局异常处理器
- **日志记录**: ILogger相关实现

## 📋 具体保护清单

### 核心基础设施（绝对保护）
```
src/Server/Core/LYBT.Infrastructure/
├── Web/BaseApiController.cs           ✅ 控制器基类
├── Web/BaseSystemController.cs        ✅ 系统控制器基类  
├── Data/AppDbContext.cs              ✅ 数据库上下文
├── Repositories/OptimizedBaseRepository.cs ✅ Repository基类
└── ServiceCollectionExtensions.cs     ✅ DI注册扩展
```

### 共享模型（绝对保护）
```
src/Shared/LYBT.Shared.Models/
├── Contracts/**/*.cs                  ✅ 所有DTO类
├── Constants/**/*.cs                  ✅ 系统常量
└── Extensions/**/*.cs                 ✅ 扩展方法
```

### 共享工具（谨慎处理）
```
src/Shared/LYBT.Shared.Utilities/
├── Helpers/PasswordHelper.cs          ✅ 绝对保护（安全）
├── Helpers/CommonHelper.cs            🟡 需要分析引用
└── Helpers/EnumHelper.cs              🟡 需要分析引用
```

### 实体模型（绝对保护）
```
src/Server/Core/LYBT.Entities/
└── **/*.cs                           ✅ 所有实体类
```

### 业务模块核心（绝对保护）
```
src/Server/Modules/*/
├── Interfaces/I*Repository.cs         ✅ Repository接口
├── Interfaces/I*Service.cs            ✅ Service接口
├── Services/*Service.cs               ✅ Service实现
├── Repositories/*Repository.cs        ✅ Repository实现
└── Mapping/*MappingProfile.cs         ✅ AutoMapper配置
```

### WebAPI控制器（绝对保护）
```
src/Server/Services/LYBT.WebAPI/
├── Controllers/**/*.cs                ✅ 所有控制器
├── Program.cs                         ✅ 应用入口点
└── Extensions/**/*.cs                 ✅ 服务注册
```

### WPF应用核心（绝对保护）
```
src/Client/Desktop/Shell/
├── App.xaml.cs                        ✅ 应用程序类
├── MainWindow.xaml.cs                 ✅ 主窗口
└── ViewModels/**/*.cs                 ✅ Shell ViewModels
```

### 活跃业务模块（绝对保护）
```
src/Client/Desktop/Modules/
├── Auth/**/*.cs                       ✅ 认证模块
├── Users/**/*.cs                      ✅ 用户管理
├── Patients/**/*.cs                   ✅ 患者管理  
├── Consultation/**/*.cs               ✅ 看诊模块
├── Prescriptions/**/*.cs              ✅ 处方模块
├── MedicalCase/**/*.cs               ✅ 医疗案例
├── Herbs/**/*.cs                     ✅ 药材管理
└── Formula/**/*.cs                   ✅ 验方管理
```

### 活跃工作台（绝对保护）
```
src/Client/Desktop/Workbenches/
├── ConsultationWorkbench/**/*.cs      ✅ 看诊工作台
├── SystemWorkbench/**/*.cs           ✅ 系统管理工作台
└── Core/**/*.cs                      ✅ 工作台核心
```

## ❌ 确认可删除的项目

以下项目经过分析，确认可以安全删除：

### 未使用的工作台模块
```
src/Client/Desktop/Workbenches/
├── TherapistWorkbench/               ❌ 完全未使用
├── PharmacistWorkbench/              ❌ 完全未使用  
├── CashierWorkbench/                 ❌ 完全未使用
└── ReceptionistWorkbench/            ❌ 完全未使用
```

### 解决方案引用清理
```
*.sln files                           ❌ 移除死项目引用
```

### 相关文档清理
```
docs/ related to deleted workbenches  ❌ 清理相关文档
README files in deleted directories   ❌ 清理说明文档
```

## 🔍 需要进一步分析的项目

### 1. 测试项目
- 所有测试项目保持现状，不在此次清理范围内
- 除非明确指定 INCLUDE_TESTS=true

### 2. 生成文件
- *.Designer.cs 文件 - 自动排除
- *.g.cs 文件 - 自动排除
- Migration 文件 - 自动排除

### 3. 配置文件
- appsettings*.json - 保护
- *.config 文件 - 保护
- web.config 文件 - 保护

---
**总结**: 建立了完整的保护清单，确保只删除真正的死代码，避免误删重要的系统组件。重点保护API、依赖注入、数据绑定和核心业务逻辑相关的代码。