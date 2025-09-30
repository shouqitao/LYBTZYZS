# Desktop项目结构优化方案

## 1. 概述

本方案旨在重组Desktop项目文件夹结构，参考Server项目的成功架构模式，建立清晰、可维护、可扩展的项目结构。

## 2. 现状分析

### 2.1 Server项目结构（参考标准）
```
src/Server/
├── Core/                    # 核心层
│   ├── LYBT.Core/          # 基础功能
│   ├── LYBT.Core.EventBus/ # 事件总线
│   ├── LYBT.Entities/      # 实体定义
│   ├── LYBT.Infrastructure/# 基础设施
│   └── Documentation/       # 文档
├── Modules/                 # 业务模块层
│   ├── LYBT.Module.Auth/
│   ├── LYBT.Module.Consultation/
│   ├── LYBT.Module.Formula/
│   ├── LYBT.Module.Herbs/
│   ├── LYBT.Module.MedicalCase/
│   ├── LYBT.Module.Patients/
│   ├── LYBT.Module.Prescriptions/
│   └── LYBT.Module.Users/
└── Services/                # 服务层
    └── LYBT.WebAPI/        # Web API服务
```

### 2.2 Desktop项目当前问题
1. **Core文件夹过度臃肿**：包含27个子文件夹，混合了不同层次的概念
2. **资源文件分散**：Assets、Resources、Infrastructure/Themes等多处存放
3. **职责边界模糊**：Infrastructure、Services、Core之间存在功能重叠
4. **模块结构不统一**：各模块内部组织方式不一致
5. **缺少清晰分层**：未像Server那样有明确的Core、Modules、Services分层

## 3. 优化方案

### 3.1 设计理念

#### 工作台与业务模块分离
工作台（Workstations）作为独立层的原因：
1. **聚合性质**：工作台是多个业务模块的聚合器，而非单一业务功能
2. **扩展需求**：后续可能增加更多类型的工作台（如数据分析工作台、报表工作台等）
3. **复杂度管理**：工作台通常包含复杂的布局和导航逻辑，需要独立管理
4. **依赖关系**：工作台依赖多个业务模块，放在同一层级会造成循环依赖
5. **用户角色**：不同角色对应不同工作台，便于权限管理和功能定制

### 3.2 目标结构
```
src/Client/Desktop/
├── Core/                           # 核心层（独立项目）
│   ├── LYBT.Desktop.Core/        # 基础核心
│   │   ├── Interfaces/           # 接口定义
│   │   ├── Enums/               # 枚举
│   │   ├── Constants/           # 常量
│   │   ├── Events/              # 事件定义
│   │   ├── Commands/            # 命令基类
│   │   └── Mvvm/                # MVVM基础类
│   ├── LYBT.Desktop.Infrastructure/ # 基础设施
│   │   ├── Themes/              # 主题资源
│   │   │   ├── Design/         # 设计时资源
│   │   │   ├── Dark/           # 深色主题
│   │   │   └── Light/          # 浅色主题
│   │   ├── Controls/            # 自定义控件
│   │   ├── Converters/          # 值转换器
│   │   ├── Resources/           # 资源文件（图标、图片等）
│   │   └── Utilities/           # 工具类（Helpers、Extensions）
│   ├── LYBT.Desktop.Services/    # 核心服务
│   │   ├── Http/               # HTTP服务
│   │   ├── Authentication/     # 认证服务
│   │   ├── Navigation/         # 导航服务
│   │   ├── Cache/              # 缓存服务
│   │   └── Security/           # 安全服务
│   └── LYBT.Desktop.Models/      # 共享模型
│       ├── DTOs/               # 数据传输对象
│       ├── ViewModels/         # 共享视图模型
│       └── EventArgs/          # 事件参数
├── Modules/                        # 业务模块层
│   ├── LYBT.Desktop.Auth/        # 认证模块
│   │   ├── ViewModels/
│   │   ├── Views/
│   │   ├── Services/
│   │   ├── Models/
│   │   └── AuthModule.cs
│   ├── LYBT.Desktop.Patients/    # 患者管理模块
│   ├── LYBT.Desktop.Prescriptions/ # 处方管理模块
│   ├── LYBT.Desktop.Herbs/       # 药材管理模块
│   └── LYBT.Desktop.Formula/     # 方剂管理模块
├── Workstations/                   # 工作台层（独立于业务模块）
│   ├── LYBT.Desktop.ClinicalWorkstation/ # 诊疗工作台
│   ├── LYBT.Desktop.AdminWorkstation/    # 管理工作台
│   └── README.md                  # 工作台扩展说明
└── Shell/                          # 启动层
    └── LYBT.Desktop.Shell/       # 主程序
        ├── App.xaml
        ├── App.xaml.cs
        ├── Bootstrapper.cs
        └── Configuration/
```

### 3.3 文件迁移映射

#### Core层重组
| 原路径 | 新路径 |
|--------|--------|
| Core/Interfaces/ | LYBT.Desktop.Core/Interfaces/ |
| Core/Enums/, Core/Constants/ | LYBT.Desktop.Core/ |
| Core/Events/, Core/Commands/ | LYBT.Desktop.Core/ |
| Core/ViewModels/Base/ | LYBT.Desktop.Core/Mvvm/ |
| Core/Controls/, Core/Converters/, Core/Templates/ | LYBT.Desktop.Infrastructure/Controls/ |
| Core/Helpers/, Core/Extensions/ | LYBT.Desktop.Infrastructure/Utilities/ |
| Infrastructure/Themes/ | LYBT.Desktop.Infrastructure/Themes/ |
| Assets/, Resources/ | LYBT.Desktop.Infrastructure/Resources/ |
| Core/Http/, Core/Security/ | LYBT.Desktop.Services/ |
| Core/Services/ | LYBT.Desktop.Services/ |
| Core/Models/ | LYBT.Desktop.Models/ |

#### 清理项目
- 删除Workbenches文件夹（如果还存在）
- 删除重复的Services文件夹
- 合并Configuration到Shell项目

### 3.4 Solution文件组织
```xml
<Solution>
  <SolutionFolder Name="Core">
    <Project>LYBT.Desktop.Core</Project>
    <Project>LYBT.Desktop.Infrastructure</Project>
    <Project>LYBT.Desktop.Services</Project>
    <Project>LYBT.Desktop.Models</Project>
  </SolutionFolder>
  <SolutionFolder Name="Modules">
    <Project>LYBT.Desktop.Auth</Project>
    <Project>LYBT.Desktop.Patients</Project>
    <Project>LYBT.Desktop.Prescriptions</Project>
    <Project>LYBT.Desktop.Herbs</Project>
    <Project>LYBT.Desktop.Formula</Project>
  </SolutionFolder>
  <SolutionFolder Name="Workstations">
    <Project>LYBT.Desktop.ClinicalWorkstation</Project>
    <Project>LYBT.Desktop.AdminWorkstation</Project>
  </SolutionFolder>
  <SolutionFolder Name="Shell">
    <Project>LYBT.Desktop.Shell</Project>
  </SolutionFolder>
</Solution>
```

### 3.5 项目依赖关系
```
LYBT.Desktop.Shell
    ├── 所有Modules项目
    ├── 所有Workstations项目
    └── 所有Core项目

Modules项目
    ├── LYBT.Desktop.Core
    ├── LYBT.Desktop.Infrastructure
    ├── LYBT.Desktop.Services
    └── LYBT.Desktop.Models

Workstations项目
    ├── LYBT.Desktop.Core
    ├── LYBT.Desktop.Infrastructure
    ├── LYBT.Desktop.Services
    ├── LYBT.Desktop.Models
    └── 相关Modules项目（按需引用）

LYBT.Desktop.Services
    └── LYBT.Desktop.Core

LYBT.Desktop.Infrastructure
    └── LYBT.Desktop.Core

LYBT.Desktop.Models
    └── LYBT.Desktop.Core
```

## 4. 实施步骤

### 第一阶段：创建Core层项目
1. 创建4个Core层独立项目
2. 迁移相关文件到对应项目
3. 调整命名空间
4. 更新项目引用

### 第二阶段：重组Modules层
1. 统一模块内部结构
2. 移除重复的Services文件夹
3. 更新模块间依赖

### 第三阶段：整理Shell层
1. 将启动相关文件移到Shell项目
2. 更新模块注册逻辑
3. 调整配置文件位置

### 第四阶段：验证与测试
1. 编译整个解决方案
2. 运行单元测试
3. 验证功能完整性

## 5. 预期收益

### 5.1 结构优势
- **清晰分层**：与Server保持一致的多层架构（Core、Modules、Workstations、Shell）
- **职责分明**：每层有明确的职责边界，避免功能重叠
- **工作台独立**：工作台作为聚合模块单独管理，便于扩展和维护
- **易于维护**：模块化设计，每个模块独立管理

### 5.2 开发效率
- **降低复杂度**：从Core的27个子文件夹减少到4个独立项目
- **资源统一**：所有资源文件集中管理
- **依赖清晰**：项目间依赖关系简单明了

### 5.3 团队协作
- **统一标准**：与Server端保持一致的项目组织哲学
- **降低学习成本**：新成员更容易理解项目结构
- **并行开发**：模块独立，便于团队并行工作

## 6. 风险与对策

### 6.1 潜在风险
1. **大规模文件迁移**可能导致引用错误
2. **命名空间变更**影响现有代码
3. **Solution文件**需要重新配置

### 6.2 缓解措施
1. 使用Git分支进行重组，保留原始版本
2. 使用IDE的重构工具批量更新命名空间
3. 分阶段实施，每阶段验证编译通过
4. 保留完整的迁移日志，便于回滚

## 7. 后续优化建议

1. **引入Directory.Build.props**统一管理NuGet包版本
2. **创建项目模板**便于新模块开发
3. **完善模块间通信机制**使用Prism EventAggregator
4. **建立模块加载策略**支持按需加载和插件化

## 8. 总结

本优化方案通过参考Server项目的成功架构，为Desktop项目建立了清晰的三层结构。这不仅提高了代码的可维护性和可扩展性，还统一了前后端的项目组织哲学，有助于团队协作和知识共享。