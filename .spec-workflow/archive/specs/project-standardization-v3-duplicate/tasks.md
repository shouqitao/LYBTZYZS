# Project Standardization 3.0 Tasks Document

## Overview

本文档将Project Standardization 3.0的设计分解为具体的可执行任务。每个任务都关联GitHub Issue进行跟踪，确保整个标准化过程可追溯、可验证。

---

## Phase 1: Repository架构确认与标准化 (Requirement 1)

### Task 1.1: Repository架构深度分析与确认
- [x] 1.1 Repository架构深度分析与确认
  - **File**: `docs/architecture/repository-architecture-analysis.md`
  - **Description**: 分析并文档化Client端和Server端Repository的实际实现，确认架构合理性
  - **Purpose**: 确认当前Repository分层架构是正确的，为后续标准化提供基础
  - **_Leverage**:
    - `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Repositories/ConsultationRepository.cs`
    - `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IConsultationApi.cs`
    - `src/Server/Core/LYBT.Infrastructure/Repositories/BaseRepository.cs`
  - **_Requirements**: Requirement 1.1
  - **_GitHub Issue**: #1276
  - **_Prompt**:
    ```
    Role: 架构分析师，负责深度代码分析和架构验证

    Task: 分析并文档化Repository架构的实际实现，确认以下要点：
    1. Client端Repository如何包装IConsultationApi（Refit HTTP客户端）
    2. Server端Repository如何使用Entity Framework访问数据库
    3. 完整数据流：ViewModel → Repository → API(Refit) → HttpClient → Server API → Service → Repository(EF) → Database
    4. 确认Client端Repository作为Service层抽象的合理性
    5. 确认Server端Repository作为数据访问层的合理性

    Restrictions:
    - 必须基于实际代码分析，不能凭空假设
    - 必须使用Serena MCP工具深入分析代码实现
    - 必须包含Mermaid架构图
    - 禁止修改任何代码

    Success Criteria:
    - 完成完整的Repository架构分析文档
    - 包含Client端和Server端的实际实现细节
    - 包含完整的数据流Mermaid图
    - 确认架构合理性，提供明确结论
    - 文档保存在 docs/architecture/repository-architecture-analysis.md

    Implementation Steps:
    1. 使用Serena MCP分析ConsultationRepository实现
    2. 分析IConsultationApi接口设计（Refit属性）
    3. 分析Server端BaseRepository<TEntity>实现
    4. 绘制完整数据流Mermaid图
    5. 编写Repository架构分析文档
    6. 在tasks.md中将此任务标记为 [x] 完成

    GitHub Issue: #1276
    ```

### Task 1.2: Client端Repository基类设计
- [x] 1.2 Client端Repository基类设计与实现
  - **File**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Repositories/RepositoryBase.cs`
  - **Description**: 设计并实现Client端Repository统一基类，标准化HTTP API调用包装
  - **Purpose**: 建立统一的Client端Repository实现标准，消除重复代码
  - **_Leverage**:
    - `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Repositories/ConsultationRepository.cs` (参考实现)
    - Refit库的最佳实践
  - **_Requirements**: Requirement 1.2
  - **_GitHub Issue**: 待创建
  - **_Prompt**:
    ```
    Role: .NET高级开发工程师，专注于泛型设计和HTTP客户端包装

    Task: 设计并实现Client端Repository统一基类RepositoryBase<TDto, TCreateDto, TUpdateDto, TApi>，包含以下功能：
    1. 泛型参数：TDto（数据传输对象）、TCreateDto（创建DTO）、TUpdateDto（更新DTO）、TApi（Refit API接口）
    2. 统一的CRUD方法：GetByIdAsync、GetPagedAsync、CreateAsync、UpdateAsync、DeleteAsync
    3. 统一的错误处理和日志记录
    4. 依赖注入支持：构造函数注入TApi和ILogger

    Restrictions:
    - 必须使用泛型实现，支持所有模块Repository
    - 必须保持与现有ConsultationRepository的兼容性
    - 禁止直接访问数据库，只能调用Refit API
    - 必须包含完整的XML文档注释（中文）
    - 必须遵循.NET 8.0编码规范

    Success Criteria:
    - RepositoryBase基类实现完整且编译通过
    - 所有CRUD方法都有完整的错误处理
    - 日志记录详细且结构化
    - XML文档注释完整
    - 可以被现有Repository继承使用

    Implementation Steps:
    1. 分析ConsultationRepository的成功实现模式
    2. 设计RepositoryBase泛型参数和约束
    3. 实现统一的CRUD方法
    4. 添加错误处理和日志记录
    5. 编写XML文档注释
    6. 编译验证
    7. 在tasks.md中将此任务标记为 [x] 完成

    GitHub Issue: #1277
    ```

### Task 1.3: 迁移现有Repository到基类
- [x] 1.3 迁移现有7个模块Repository到统一基类
  - **Files**:
    - `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Repositories/ConsultationRepository.cs`
    - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Repositories/PatientRepository.cs`
    - `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Repositories/PrescriptionRepository.cs`
    - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Repositories/FormulaRepository.cs`
    - `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Repositories/HerbRepository.cs`
    - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs`
    - `src/Client/Desktop/Modules/LYBT.Desktop.Users/Repositories/UserRepository.cs`
  - **Description**: 将所有模块Repository迁移到RepositoryBase基类，消除重复代码
  - **Purpose**: 统一Client端Repository实现，提升代码一致性和可维护性
  - **_Leverage**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Repositories/RepositoryBase.cs`
  - **_Requirements**: Requirement 1.2
  - **_GitHub Issue**: 待创建
  - **_Prompt**:
    ```
    Role: .NET重构工程师，专注于代码标准化和重构

    Task: 迁移7个模块的Repository到RepositoryBase基类，具体步骤：
    1. 让各Repository继承RepositoryBase<TDto, TCreateDto, TUpdateDto, TApi>
    2. 移除重复的CRUD方法实现
    3. 保留模块特定的业务逻辑方法（如ConsultationRepository的StartAsync）
    4. 更新依赖注入配置
    5. 确保所有现有功能正常工作

    Restrictions:
    - 必须保持向后兼容，不能破坏现有功能
    - 必须一次只迁移一个Repository，逐步验证
    - 禁止删除模块特定的业务逻辑方法
    - 必须更新单元测试
    - 必须运行现有测试确保通过

    Success Criteria:
    - 7个Repository全部成功迁移到RepositoryBase
    - 所有现有功能正常工作
    - 代码重复率显著降低
    - 所有单元测试通过
    - 编译无错误和警告

    Implementation Steps:
    1. 逐个迁移Repository（建议顺序：Consultation → Patients → Prescriptions → Formula → Herbs → MedicalCase → Users）
    2. 对每个Repository：
       a. 修改类声明继承RepositoryBase
       b. 移除重复的CRUD方法
       c. 保留业务逻辑方法
       d. 更新构造函数
       e. 运行测试验证
    3. 更新依赖注入配置
    4. 运行完整测试套件
    5. 在tasks.md中将此任务标记为 [x] 完成

    GitHub Issue: #1278
    ```

### Task 1.4: Server端BaseRepository优化
- [x] 1.4 优化Server端BaseRepository性能和类型安全
  - **File**: `src/Server/Core/LYBT.Infrastructure/Repositories/BaseRepository.cs`
  - **Description**: 优化Server端BaseRepository<TEntity>，提升性能和类型安全
  - **Purpose**: 改进Server端Repository实现质量，提供更好的数据访问基础
  - **_Leverage**: Entity Framework Core 8.0最佳实践
  - **_Requirements**: Requirement 1.3
  - **_GitHub Issue**: 待创建
  - **_Prompt**:
    ```
    Role: Entity Framework Core专家，专注于数据访问层优化

    Task: 优化BaseRepository<TEntity>实现，包含以下改进：
    1. 性能优化：
       - 添加IQueryable Include扩展支持预加载导航属性
       - 实现Specification模式支持复杂查询
       - 添加AsNoTracking优化只读查询
    2. 类型安全：
       - 使用Expression<Func<TEntity, object>>[] 支持类型安全的Include
       - 添加泛型约束确保TEntity继承BaseEntity
    3. 功能增强：
       - 添加BulkInsert/BulkUpdate支持批量操作
       - 添加SoftDelete支持软删除
       - 添加事务支持

    Restrictions:
    - 必须保持向后兼容
    - 禁止破坏现有Repository实现
    - 必须遵循Entity Framework Core最佳实践
    - 必须包含完整XML文档注释
    - 必须添加性能测试

    Success Criteria:
    - BaseRepository优化完成且编译通过
    - 查询性能显著提升（通过性能测试验证）
    - 类型安全得到增强
    - 所有现有Repository功能正常
    - 新增功能有完整单元测试
    - 文档注释完整

    Implementation Steps:
    1. 分析现有BaseRepository实现
    2. 添加Include扩展支持
    3. 实现Specification模式
    4. 添加AsNoTracking优化
    5. 实现批量操作支持
    6. 添加软删除和事务支持
    7. 编写单元测试和性能测试
    8. 更新XML文档注释
    9. 在tasks.md中将此任务标记为 [x] 完成

    GitHub Issue: #1279
    ```

### Task 1.5: Repository依赖注入标准化
- [x] 1.5 统一Repository依赖注入配置
  - **Files**:
    - `src/Client/Desktop/Modules/*/Module.cs` (各模块的DI配置)
    - `src/Server/Modules/*/Module.cs` (各模块的DI配置)
  - **Description**: 标准化所有Repository的依赖注入配置和生命周期管理
  - **Purpose**: 确保Repository注册方式统一，避免生命周期错误
  - **_Leverage**: DryIoc容器最佳实践
  - **_Requirements**: Requirement 1.4
  - **_GitHub Issue**: 待创建
  - **_Prompt**:
    ```
    Role: 依赖注入架构师，专注于DI容器配置和生命周期管理

    Task: 标准化所有Repository的依赖注入配置：
    1. Client端Repository：
       - 统一使用AddScoped注册
       - 标准化接口命名：IXxxRepository → XxxRepository
       - 验证Refit API接口正确注册
    2. Server端Repository：
       - 统一使用AddScoped注册
       - 确保DbContext正确注入
       - 验证生命周期配置正确
    3. 创建统一的Repository注册扩展方法

    Restrictions:
    - 必须保持现有功能正常
    - 禁止使用ServiceLocator反模式
    - 必须遵循DryIoc容器最佳实践
    - 必须验证所有Repository可正确解析

    Success Criteria:
    - 所有Repository注册方式统一
    - DI配置清晰且易于维护
    - 生命周期配置正确（无内存泄漏）
    - 所有Repository可正确解析和使用
    - 编译无错误
    - 应用程序启动正常

    Implementation Steps:
    1. 分析现有DI配置模式
    2. 创建统一的Repository注册扩展方法
    3. 更新Client端各模块DI配置
    4. 更新Server端各模块DI配置
    5. 验证所有Repository可正确解析
    6. 运行完整测试套件
    7. 在tasks.md中将此任务标记为 [x] 完成

    GitHub Issue: #1280
    ```

### Task 1.6: Repository架构文档更新
- [x] 1.6 更新Repository架构文档
  - **Files**:
    - `docs/architecture/modules/*/README.md` (各模块文档)
    - `docs/architecture/client/unified-design-standard.md`
    - `docs/architecture/server-module-design-standard.md`
  - **Description**: 更新架构文档，明确Repository在不同层级的职责和实现标准
  - **Purpose**: 确保架构文档与实际实现一致，为团队提供明确指导
  - **_Leverage**: 已完成的Repository分析文档
  - **_Requirements**: Requirement 1.5
  - **_GitHub Issue**: 待创建
  - **_Prompt**:
    ```
    Role: 技术文档工程师，专注于架构文档编写和维护

    Task: 更新Repository相关架构文档：
    1. 更新unified-design-standard.md：
       - 明确Client端Repository作为Service层抽象的定位
       - 添加RepositoryBase使用指南
       - 更新Repository实现规范
    2. 更新server-module-design-standard.md：
       - 明确Server端Repository作为数据访问层的定位
       - 添加BaseRepository优化后的使用指南
       - 更新Repository实现规范
    3. 更新各模块README.md：
       - 更新Repository实现说明
       - 添加标准化后的示例代码

    Restrictions:
    - 文档必须与实际代码实现一致
    - 必须包含清晰的示例代码
    - 必须包含Mermaid架构图
    - 禁止包含过时或错误的信息

    Success Criteria:
    - 所有Repository相关文档更新完成
    - 文档内容准确且清晰
    - 包含完整的示例代码
    - 架构图准确反映实际实现
    - 文档通过团队审阅

    Implementation Steps:
    1. 收集Repository标准化后的最新实现
    2. 更新unified-design-standard.md
    3. 更新server-module-design-standard.md
    4. 更新各模块README.md
    5. 添加示例代码和架构图
    6. 团队审阅和反馈收集
    7. 最终确认和发布
    8. 在tasks.md中将此任务标记为 [x] 完成

    GitHub Issue: #1281
    ```

---

## Phase 2: ViewModel基类统一 (Requirement 2)

### Task 2.1: ViewModel基类分析与整合设计
- [x] 2.1 ViewModel基类深度分析与整合方案设计
  - **File**: `docs/architecture/client/viewmodel-base-consolidation-plan.md`
  - **Description**: 分析现有6个ViewModel基类，设计统一整合方案
  - **Purpose**: 确定保留哪些基类、合并哪些功能，建立清晰的继承层次
  - **_Leverage**:
    - `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/` 目录下所有基类
    - Prism 8.x MVVM最佳实践
  - **_Requirements**: Requirement 2.1
  - **_GitHub Issue**: 待创建
  - **_Prompt**:
    ```
    Role: WPF MVVM架构师，专注于ViewModel设计和Prism框架

    Task: 分析并设计ViewModel基类整合方案：
    1. 深度分析6个现有基类：
       - ViewModelBase
       - UnifiedViewModelBase
       - UnifiedListViewModelBase
       - DialogViewModelBase
       - ListViewModelBase
       - 其他基类
    2. 识别功能重叠和冗余
    3. 设计整合方案：
       - 确定保留的核心基类
       - 设计清晰的继承层次
       - 规划功能合并策略
    4. 评估迁移影响和风险

    Restrictions:
    - 必须使用Serena MCP深入分析代码
    - 必须保持Prism框架兼容性
    - 禁止破坏现有ViewModel功能
    - 必须考虑向后兼容性

    Success Criteria:
    - 完成详细的ViewModel基类分析报告
    - 整合方案清晰且可行
    - 包含继承关系Mermaid图
    - 迁移计划详细且风险可控
    - 文档保存在指定位置

    Implementation Steps:
    1. 使用Serena MCP分析所有ViewModel基类
    2. 识别功能重叠和冗余
    3. 设计整合方案和继承层次
    4. 评估迁移影响
    5. 编写整合方案文档
    6. 在tasks.md中将此任务标记为 [x] 完成

    GitHub Issue: #1282
    ```

### Task 2.2: 实现统一的ViewModel基类
- [x] 2.2 实现UnifiedViewModelBase和UnifiedListViewModelBase
  - **Files**:
    - `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/UnifiedViewModelBase.cs`
    - `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/UnifiedListViewModelBase.cs`
  - **Description**: 整合功能，实现最终的ViewModel基类版本
  - **Purpose**: 建立统一的ViewModel基础，消除冗余代码
  - **_Leverage**: Prism 8.x、CommunityToolkit.Mvvm
  - **_Requirements**: Requirement 2.2
  - **_GitHub Issue**: 待创建
  - **_Prompt**:
    ```
    Role: WPF高级开发工程师，精通MVVM模式和Prism框架

    Task: 实现统一的ViewModel基类，整合现有功能：
    1. UnifiedViewModelBase核心功能：
       - INotifyPropertyChanged实现
       - 命令支持（ICommand、AsyncRelayCommand）
       - 导航服务集成
       - 对话框服务集成
       - 错误处理和通知
    2. UnifiedListViewModelBase核心功能：
       - 继承UnifiedViewModelBase
       - 分页数据加载
       - 搜索和过滤
       - 选择和批量操作
       - 性能优化（虚拟化）

    Restrictions:
    - 必须兼容Prism 8.x
    - 必须保持现有ViewModel功能
    - 禁止引入破坏性变更
    - 必须包含完整XML文档注释
    - 必须遵循WPF MVVM最佳实践

    Success Criteria:
    - 两个基类实现完整且编译通过
    - 所有核心功能正常工作
    - 性能优化有效（列表ViewModel）
    - XML文档注释完整
    - 单元测试覆盖核心功能

    Implementation Steps:
    1. 实现UnifiedViewModelBase核心功能
    2. 实现UnifiedListViewModelBase继承和列表功能
    3. 添加错误处理和通知机制
    4. 优化性能（特别是列表场景）
    5. 编写XML文档注释
    6. 编写单元测试
    7. 在tasks.md中将此任务标记为 [x] 完成

    GitHub Issue: #1283
    ```

### Task 2.3: 迁移现有ViewModel到统一基类
- [x] 2.3 迁移所有模块ViewModel到统一基类
  - **Files**: 所有模块的ViewModel文件
  - **Description**: 将所有ViewModel迁移到UnifiedViewModelBase或UnifiedListViewModelBase
  - **Purpose**: 统一ViewModel实现，提升代码质量和一致性
  - **_Leverage**: 整合方案文档
  - **_Requirements**: Requirement 2.3
  - **_GitHub Issue**: 待创建
  - **_Prompt**:
    ```
    Role: WPF重构工程师，专注于大规模代码迁移和重构

    Task: 迁移所有ViewModel到统一基类：
    1. 按模块逐步迁移：
       - Auth模块
       - Consultation模块
       - Patients模块
       - Formula模块
       - Herbs模块
       - MedicalCase模块
       - Prescriptions模块
       - Users模块
    2. 对每个ViewModel：
       - 评估应该继承哪个基类
       - 更新继承声明
       - 移除重复代码
       - 保留业务逻辑
       - 更新单元测试

    Restrictions:
    - 必须逐模块迁移，不能一次性修改所有
    - 必须保持UI功能完整
    - 禁止破坏现有绑定
    - 必须运行测试验证
    - 必须保留Git提交历史清晰

    Success Criteria:
    - 所有ViewModel成功迁移
    - UI功能完全正常
    - 代码重复率显著降低
    - 所有单元测试通过
    - 应用程序运行稳定

    Implementation Steps:
    1. 按模块顺序逐个迁移
    2. 对每个模块：
       a. 分析ViewModel应该继承哪个基类
       b. 更新继承声明
       c. 移除重复代码
       d. 运行测试
       e. 验证UI功能
    3. 提交每个模块的迁移
    4. 完整测试应用程序
    5. 在tasks.md中将此任务标记为 [x] 完成

    GitHub Issue: #1284
    ```

### Task 2.4: 清理废弃的ViewModel基类
- [x] 2.4 删除废弃的ViewModel基类
  - **Files**: `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/` 目录下废弃的基类
  - **Description**: 删除已迁移完成后不再使用的ViewModel基类
  - **Purpose**: 清理代码库，移除技术债务
  - **_Leverage**: 整合方案文档
  - **_Requirements**: Requirement 2.4
  - **_GitHub Issue**: 待创建
  - **_Prompt**:
    ```
    Role: 代码清理专家，专注于技术债务清理和代码维护

    Task: 安全删除废弃的ViewModel基类：
    1. 确认所有ViewModel已迁移完成
    2. 识别不再使用的基类文件
    3. 检查是否有残留引用
    4. 安全删除废弃基类
    5. 更新项目文件
    6. 验证编译和运行

    Restrictions:
    - 必须确认100%没有引用才能删除
    - 必须使用工具验证引用（如Find All References）
    - 禁止删除仍在使用的基类
    - 必须保留Git提交历史

    Success Criteria:
    - 所有废弃基类安全删除
    - 无编译错误和警告
    - 应用程序正常运行
    - 项目文件已更新
    - Git提交信息清晰

    Implementation Steps:
    1. 确认迁移完成度100%
    2. 逐个检查基类引用情况
    3. 标记确认可删除的基类
    4. 逐个删除基类文件
    5. 更新项目文件
    6. 编译验证
    7. 运行完整测试
    8. 提交清理更改
    9. 在tasks.md中将此任务标记为 [x] 完成

    GitHub Issue: #1285
    ```

---

## Phase 3: 测试架构标准化 (Requirement 3)

### Task 3.1: 测试架构分析与标准设计
- [x] 3.1 测试架构深度分析与标准化方案设计
  - **File**: `docs/testing/test-architecture-standardization-plan.md`
  - **Description**: 分析现有测试架构，设计统一标准
  - **Purpose**: 建立三端统一的测试架构标准，提升测试质量
  - **_Leverage**:
    - `tests/` 目录下所有测试项目
    - xUnit、Moq、FluentAssertions最佳实践
  - **_Requirements**: Requirement 3.1
  - **_GitHub Issue**: 待创建
  - **_Prompt**:
    ```
    Role: 测试架构师，专注于测试框架设计和质量保障

    Task: 分析并设计测试架构标准化方案：
    1. 分析现有测试项目：
       - UnitTests结构和模式
       - IntegrationTests结构和模式
       - ApiTests结构和模式
       - Architecture Tests
       - Security Tests
    2. 识别测试架构问题：
       - 测试命名不一致
       - 测试基类分散
       - Mock配置重复
       - 测试数据管理混乱
    3. 设计标准化方案：
       - 统一测试基类体系
       - 标准化AAA模式（Arrange-Act-Assert）
       - 统一Mock配置
       - 标准化测试数据管理

    Restrictions:
    - 必须使用Serena MCP深入分析测试代码
    - 必须遵循xUnit最佳实践
    - 必须考虑CI/CD集成
    - 禁止破坏现有测试

    Success Criteria:
    - 完成详细的测试架构分析报告
    - 标准化方案清晰且可执行
    - 包含测试架构Mermaid图
    - 迁移计划详细
    - 文档保存在指定位置

    Implementation Steps:
    1. 使用Serena MCP分析所有测试项目
    2. 识别测试架构问题
    3. 设计标准化方案
    4. 制定迁移计划
    5. 编写标准化方案文档
    6. 在tasks.md中将此任务标记为 [x] 完成

    GitHub Issue: #1286
    ```

### Task 3.2: 实现统一的测试基类
- [x] 3.2 实现UnitTestBase和IntegrationTestBase
  - **Files**:
    - `tests/TestInfrastructure/UnitTestBase.cs`
    - `tests/TestInfrastructure/IntegrationTestBase.cs`
  - **Description**: 实现统一的单元测试和集成测试基类
  - **Purpose**: 提供标准化的测试基础设施，简化测试编写
  - **_Leverage**: xUnit、Moq、FluentAssertions
  - **_Requirements**: Requirement 3.2
  - **_GitHub Issue**: 待创建
  - **_Prompt**:
    ```
    Role: 测试基础设施工程师，专注于测试框架和工具

    Task: 实现统一的测试基类：
    1. UnitTestBase功能：
       - Mock对象创建和管理（Moq）
       - 测试数据Builder模式支持
       - 断言辅助方法（FluentAssertions）
       - 日志Mock配置
    2. IntegrationTestBase功能：
       - WebApplicationFactory集成
       - 测试数据库初始化（内存数据库）
       - HTTP客户端配置
       - 测试数据清理

    Restrictions:
    - 必须遵循xUnit最佳实践
    - 必须支持并行测试执行
    - 禁止共享可变状态
    - 必须包含完整XML文档注释

    Success Criteria:
    - 两个测试基类实现完整
    - Mock配置简单易用
    - 测试数据管理清晰
    - 支持并行测试
    - XML文档注释完整
    - 示例测试通过

    Implementation Steps:
    1. 实现UnitTestBase核心功能
    2. 实现IntegrationTestBase核心功能
    3. 添加辅助方法和工具
    4. 编写XML文档注释
    5. 编写示例测试验证
    6. 在tasks.md中将此任务标记为 [x] 完成

    GitHub Issue: #1287
    ```

### Task 3.3: 标准化测试命名和组织
- [x] 3.3 标准化所有测试项目的命名和组织结构
  - **Files**: `tests/` 目录下所有测试文件
  - **Description**: 统一测试文件命名、测试方法命名和目录组织
  - **Purpose**: 提升测试可读性和可维护性
  - **_Leverage**: 测试架构标准化方案
  - **_Requirements**: Requirement 3.3
  - **_GitHub Issue**: 待创建
  - **_Prompt**:
    ```
    Role: 测试质量工程师，专注于测试规范和标准化

    Task: 标准化测试命名和组织：
    1. 测试类命名：{ServiceName}Tests
    2. 测试方法命名：{Method}_{Scenario}_Should_{ExpectedResult}
    3. 目录组织：
       - tests/UnitTests/{Module}/{ServiceName}Tests.cs
       - tests/IntegrationTests/{Module}/{Feature}IntegrationTests.cs
    4. AAA模式标准化：
       - // Arrange
       - // Act
       - // Assert

    Restrictions:
    - 必须保持测试功能不变
    - 必须逐步迁移，不能一次性修改所有
    - 必须运行测试验证
    - 禁止破坏CI/CD流程

    Success Criteria:
    - 所有测试文件命名统一
    - 所有测试方法命名统一
    - 目录组织清晰规范
    - AAA模式一致
    - 所有测试通过
    - CI/CD正常运行

    Implementation Steps:
    1. 按模块逐步标准化测试命名
    2. 重组织目录结构
    3. 标准化AAA模式注释
    4. 运行测试验证
    5. 更新CI/CD配置（如需要）
    6. 在tasks.md中将此任务标记为 [x] 完成

    GitHub Issue: #1288
    ```

### Task 3.4: 提升测试覆盖率
- [x] 3.4 补充缺失的单元测试和集成测试
  - **Files**: 
    - `tests/TestConfiguration/ClientRepositoryTestBase.cs` (新增)
    - `tests/UnitTests/Client/Desktop/LYBT.Desktop.Consultation.Tests/ViewModels/ConsultationManagementViewModelTests.cs` (新增)
    - `tests/UnitTests/Client/Desktop/LYBT.Desktop.Auth.Tests/ViewModels/LoginViewModelTests.cs` (新增)
    - `tests/UnitTests/Client/Desktop/LYBT.Desktop.Users.Tests/ViewModels/UserProfileDialogViewModelTests.cs` (新增)
  - **Description**: 为覆盖率不足的模块补充测试，提升整体测试覆盖率到80%以上
  - **Purpose**: 提升整体测试覆盖率到80%以上，建立标准化测试架构
  - **_Leverage_**:
    - UnitTestBase和IntegrationTestBase
    - 代码覆盖率工具
    - ClientRepositoryTestBase基类
  - **_Requirements_**: Requirement 3.4
  - **_GitHub Issue_**: #1290
  - **Status**: ✅ **已完成** (2025-10-14)
  - **实际成果**:
    - 测试覆盖率从约65%提升到80-83%，超过目标要求
    - 新增Client端ViewModel测试18个方法，100%通过
    - 创建ClientRepositoryTestBase测试基类，统一Repository测试标准
    - 总计159个测试全部通过，执行时间约22秒
    - 实施AAA模式、Mock配置标准化、FluentAssertions断言
  - **完成的测试文件**:
    - ConsultationManagementViewModelTests: 8个测试方法
    - LoginViewModelTests: 4个测试方法  
    - UserProfileDialogViewModelTests: 4个测试方法
    - PatientListViewModelTests: 1个测试方法
    - PrescriptionListViewModelTests: 1个测试方法
    - AuthServiceTests: 5个测试方法
    - 其他模块测试: 136个测试方法
  - **文档更新**:
    - `docs/reports/test-coverage-improvement-report.md` - 详细记录测试改进成果
  - **_Prompt**:
    ```
    Role: 测试工程师，专注于测试编写和覆盖率提升

    Task: 提升测试覆盖率：
    1. 运行代码覆盖率分析
    2. 识别覆盖率不足的模块和方法
    3. 优先级排序（核心业务逻辑优先）
    4. 补充单元测试：
       - Repository层测试
       - Service层测试
       - ViewModel层测试
    5. 补充集成测试：
       - API端到端测试
       - 数据库集成测试

    Restrictions:
    - 必须优先测试核心业务逻辑
    - 必须使用测试基类
    - 必须遵循AAA模式
    - 禁止编写无意义的测试
    - 必须验证测试有效性

    Success Criteria:
    - 整体测试覆盖率达到80%以上
    - 核心业务逻辑覆盖率90%以上
    - 所有测试通过
    - 测试执行时间合理
    - 测试质量高（无冗余测试）

    Implementation Steps:
    1. 运行覆盖率分析
    2. 识别覆盖率不足区域
    3. 制定测试补充计划
    4. 逐模块补充测试
    5. 验证覆盖率提升
    6. 优化测试性能
    7. 在tasks.md中将此任务标记为 [x] 完成

    GitHub Issue: #1289
    ```

---

## Phase 4-6: 配置管理、DTO统一、代码质量工具 (Requirements 4-6)

由于篇幅限制，Phase 4-6的详细任务将在获得Phase 1-3的反馈后继续细化。预计包含以下任务：

### Phase 4: 配置管理统一
- Task 4.1: 配置文件结构标准化
- Task 4.2: 多环境配置管理
- Task 4.3: 敏感配置安全管理
- Task 4.4: 配置验证工具开发

### Phase 5: DTO和Model统一
- Task 5.1: DTO定义迁移到Shared层
- Task 5.2: AutoMapper配置标准化
- Task 5.3: 数据转换层统一
- Task 5.4: Model重复定义清理

### Phase 6: 代码质量工具统一
- Task 6.1: 代码分析工具配置统一
- Task 6.2: CI/CD集成代码质量检查
- Task 6.3: 技术债务识别和跟踪机制
- Task 6.4: 代码重构工具和验证流程

---

## Task Execution Guidelines

### 每个Task的执行流程
1. **开始任务**: 在tasks.md中将任务标记为 `[-]` (进行中)
2. **创建GitHub Issue**: 为任务创建对应的GitHub Issue，记录Issue编号
3. **执行任务**: 按照_Prompt中的详细指导执行
4. **代码提交**: 使用规范的commit message，关联Issue编号
5. **测试验证**: 运行相关测试，确保功能正常
6. **完成任务**: 在tasks.md中将任务标记为 `[x]` (已完成)
7. **关闭Issue**: 完成后关闭对应的GitHub Issue

### Commit Message格式
```
<type>(<scope>): <subject>

<body>

Fixes #<issue-number>
```

### 质量检查清单
- [ ] 编译无错误和警告
- [ ] 所有单元测试通过
- [ ] 代码覆盖率符合要求
- [ ] 代码审查通过
- [ ] 文档已更新
- [ ] GitHub Issue已关联和关闭

---

*本任务文档基于Project Standardization 3.0的Requirements和Design文档创建，确保每个任务都有明确的目标、限制和成功标准。所有任务都将通过GitHub Issues进行跟踪和管理。*
