# 项目依赖关系图分析

**分析时间**: 2025-01-09  
**依赖原则**: UI → Application → Domain ← Infrastructure  
**图表格式**: Mermaid依赖图

---

## 🔗 当前依赖关系图 (Mermaid)

```mermaid
graph TD
    %% UI层 (绿色)
    WebAPI[LYBT.WebAPI<br/>Web API控制器]:::ui
    DesktopApp[LYBT.Desktop.App<br/>WPF主程序]:::ui
    Shell[LYBT.Desktop.Shell<br/>应用外壳]:::ui
    AuthUI[LYBT.Auth.Desktop<br/>认证界面]:::ui
    UsersUI[LYBT.Users.Desktop<br/>用户界面]:::ui
    PatientsUI[LYBT.Patients.Desktop<br/>患者界面]:::ui
    
    %% Application层 (蓝色)
    AuthApp[LYBT.Auth.Server<br/>认证服务]:::app
    UsersApp[LYBT.Users.Server<br/>用户服务]:::app  
    PatientsApp[LYBT.Patients.Server<br/>患者服务]:::app
    DesktopServices[LYBT.Desktop.Services<br/>桌面服务]:::app
    
    %% Domain层 (黄色) - 问题层
    Entities[LYBT.Entities<br/>领域实体]:::domain-problem
    
    %% Infrastructure层 (灰色)
    Infrastructure[LYBT.Infrastructure<br/>数据访问]:::infra
    DesktopInfra[LYBT.Desktop.Infrastructure<br/>桌面基础设施]:::infra
    
    %% Shared层 (橙色) - 问题层  
    SharedModels[LYBT.Shared.Models<br/>共享模型]:::shared-problem
    SharedInterfaces[LYBT.Shared.Interfaces<br/>共享接口]:::shared-problem
    SharedUtils[LYBT.Shared.Utilities<br/>共享工具]:::shared
    
    %% 正确的依赖关系 (绿色箭头)
    WebAPI --> AuthApp
    WebAPI --> UsersApp  
    WebAPI --> PatientsApp
    
    DesktopApp --> Shell
    Shell --> AuthUI
    Shell --> UsersUI
    Shell --> PatientsUI
    
    AuthUI --> DesktopServices
    UsersUI --> DesktopServices
    PatientsUI --> DesktopServices
    
    AuthApp --> Entities
    UsersApp --> Entities
    PatientsApp --> Entities
    
    Infrastructure --> Entities
    
    %% 问题依赖关系 (红色箭头)  
    Entities -.->|❌违规| Infrastructure
    SharedModels -.->|❌混合| Entities
    SharedModels -.->|❌混合| Infrastructure
    SharedInterfaces -.->|❌跨层| AuthApp
    SharedInterfaces -.->|❌跨层| UsersApp
    
    %% 前后端共享依赖 (黄色箭头)
    AuthUI -.->|⚠️共享| SharedModels
    UsersUI -.->|⚠️共享| SharedModels
    AuthApp -.->|⚠️共享| SharedModels
    UsersApp -.->|⚠️共享| SharedModels
    
    %% 样式定义
    classDef ui fill:#e8f5e8,stroke:#4caf50,stroke-width:2px
    classDef app fill:#e3f2fd,stroke:#2196f3,stroke-width:2px  
    classDef domain fill:#fff3e0,stroke:#ff9800,stroke-width:2px
    classDef domain-problem fill:#ffebee,stroke:#f44336,stroke-width:3px
    classDef infra fill:#f3e5f5,stroke:#9c27b0,stroke-width:2px
    classDef shared fill:#e8f5e8,stroke:#4caf50,stroke-width:1px
    classDef shared-problem fill:#fff3e0,stroke:#ff9800,stroke-width:2px
```

---

## 🚨 依赖违规详细分析

### Critical 违规 (必须修复)

#### 1. Domain层反向依赖Infrastructure
```mermaid
graph LR
    Entities[LYBT.Entities<br/>Domain层]:::problem
    EFCore[EntityFrameworkCore<br/>技术框架]:::tech
    
    Entities -.->|❌ 严重违规| EFCore
    
    classDef problem fill:#ffebee,stroke:#f44336,stroke-width:3px
    classDef tech fill:#f5f5f5,stroke:#9e9e9e,stroke-width:2px
```

**违规详情**:
- **问题**: Domain实体直接依赖EF Core注解
- **影响**: Domain层不纯净，违反DDD原则
- **示例**: `[Column("UserName")]`, `[Key]`, `[Required]`等注解

#### 2. Shared层职责混合
```mermaid  
graph TD
    SharedModels[LYBT.Shared.Models]:::problem
    
    DomainEntity[User实体<br/>Domain概念]:::domain
    DTO[UserDto<br/>传输对象]:::contract
    TechModel[ApiResponse&lt;T&gt;<br/>技术模型]:::tech
    
    SharedModels --> DomainEntity
    SharedModels --> DTO  
    SharedModels --> TechModel
    
    classDef problem fill:#fff3e0,stroke:#ff9800,stroke-width:3px
    classDef domain fill:#e8f5e8,stroke:#4caf50,stroke-width:2px
    classDef contract fill:#e3f2fd,stroke:#2196f3,stroke-width:2px
    classDef tech fill:#f5f5f5,stroke:#9e9e9e,stroke-width:2px
```

**违规详情**:
- **问题**: 一个项目包含了三种不同层次的模型
- **影响**: 跨层依赖，职责不清，维护困难

### Warning 违规 (建议修复)

#### 3. 前后端耦合共享
```mermaid
graph LR
    FrontEnd[前端WPF<br/>客户端]:::ui
    BackEnd[后端API<br/>服务器]:::app
    SharedModels[共享模型<br/>耦合点]:::shared
    
    FrontEnd --> SharedModels
    BackEnd --> SharedModels
    
    classDef ui fill:#e8f5e8,stroke:#4caf50,stroke-width:2px
    classDef app fill:#e3f2fd,stroke:#2196f3,stroke-width:2px
    classDef shared fill:#fff3e0,stroke:#ff9800,stroke-width:2px
```

**问题分析**:
- **现状**: 前后端直接共享实体模型
- **风险**: 前端变更影响后端，后端变更影响前端
- **建议**: 建立独立的Contract层

---

## 🎯 理想依赖关系图

### 目标架构 (推荐)
```mermaid
graph TD
    %% UI层
    WebAPI[Web Controllers]:::ui
    WPFApp[WPF Applications]:::ui
    
    %% Application层
    AppServices[Application Services]:::app
    
    %% Domain层 (纯净)
    DomainCore[Domain Core<br/>纯业务逻辑]:::domain
    
    %% Infrastructure层
    DataAccess[Data Access<br/>EF Core实现]:::infra
    
    %% Contract层 (新增)
    Contracts[Contracts<br/>API契约]:::contract
    
    %% 正确的依赖方向
    WebAPI --> AppServices
    WPFApp --> AppServices
    
    AppServices --> DomainCore
    AppServices --> Contracts
    
    DataAccess --> DomainCore
    
    %% 前后端通过Contract解耦
    WebAPI --> Contracts
    WPFApp --> Contracts
    
    classDef ui fill:#e8f5e8,stroke:#4caf50,stroke-width:2px
    classDef app fill:#e3f2fd,stroke:#2196f3,stroke-width:2px
    classDef domain fill:#fff3e0,stroke:#ff9800,stroke-width:2px
    classDef infra fill:#f3e5f5,stroke:#9c27b0,stroke-width:2px
    classDef contract fill:#e1f5fe,stroke:#00bcd4,stroke-width:2px
```

### 重构路径
1. **阶段1**: 创建纯净的`LYBT.Domain.Core`项目
2. **阶段2**: 建立独立的`LYBT.Contracts`项目  
3. **阶段3**: 重构`LYBT.Infrastructure`移除Domain污染
4. **阶段4**: 拆分`LYBT.Shared.Models`到对应层次

---

## 📊 依赖健康度评估

### 层次依赖合规率
| 依赖类型 | 当前状态 | 合规项 | 违规项 | 合规率 |
|---------|----------|--------|--------|--------|
| UI → Application | 🟢 良好 | 8个 | 0个 | 100% |
| Application → Domain | 🟡 部分违规 | 6个 | 2个 | 75% |
| Domain ← Infrastructure | 🔴 严重违规 | 0个 | 3个 | 0% |
| 前后端解耦 | 🟡 待改进 | 1个 | 4个 | 20% |

### 问题严重性统计
- **🔴 Critical (3个)**: Domain层污染，必须立即修复
- **🟡 Warning (4个)**: 前后端耦合，建议重构  
- **🟢 Good (8个)**: UI到Application依赖正确

---

## 🛠️ 修复建议优先级

### P0 - 立即修复 (1-2周)
1. **移除Domain层技术依赖**
   ```
   操作: 将EF Core注解移至Infrastructure层
   文件: LYBT.Entities项目中的所有实体类
   影响: 需要调整EF Core配置方式
   ```

2. **拆分Shared.Models项目**
   ```
   拆分为:
   - LYBT.Domain.Models (纯领域模型)
   - LYBT.Contracts (API契约)  
   - LYBT.Common (通用工具)
   ```

### P1 - 规划重构 (1个月内)
3. **建立Contract层解耦前后端**
4. **统一架构文档和规范**
5. **完善依赖注入配置**

---

## 🔍 已知缺口 / 需人工确认

### 技术确认项
1. **EF Core配置迁移**: FluentAPI配置的技术可行性？
2. **前后端解耦策略**: 是否使用AutoMapper处理模型转换？
3. **依赖注入影响**: 项目重构对DI容器配置的影响？

### 业务确认项
1. **重构范围**: 是否一次性重构所有模块？
2. **向后兼容**: API契约变更对现有客户端的影响？
3. **测试覆盖**: 重构过程中的测试保证策略？

### 流程确认项
1. **重构节奏**: 渐进式 vs 大爆炸式重构？
2. **团队协调**: 前后端团队的协作机制？
3. **发布计划**: 依赖调整的发布和回滚策略？

---

**依赖分析结论**: 系统存在明确的依赖方向违规问题，主要集中在Domain层污染和Shared层职责混合。通过系统性重构可以建立清晰的分层依赖关系。

**修复复杂度**: 🟡 **中等** - 需要系统性重构，但有明确的解决路径