# Tasks: optimize-desktop-core

## Phase 0: Core层纯净化 (高优先级)

### 0.1 代码重复清理

- [ ] 0.1.1 合并ExcelHelper
  - 比较Infrastructure和Utilities版本差异
  - 保留功能更完整的版本到Infrastructure
  - 删除Utilities中的ExcelHelper.cs
  - 更新所有引用

- [ ] 0.1.2 合并ClientErrorMessageMapper
  - 比较两个版本差异
  - 统一到Infrastructure
  - 删除重复版本
  - 更新所有引用

- [ ] 0.1.3 评估Utilities项目存废
  - 分析Utilities剩余内容
  - 如无独特功能，合并到Infrastructure后删除项目
  - 更新LYBT.All.sln

### 0.2 业务代码迁移

- [ ] 0.2.1 迁移HerbItemViewModelBase
  - 目标: Core/Models/ViewModels/Base/ → Herbs/ViewModels/Base/
  - 创建Herbs/ViewModels/Base/目录
  - 移动HerbItemViewModelBase.cs
  - 更新命名空间: LYBT.Desktop.Herbs.ViewModels.Base
  - 更新所有引用

- [ ] 0.2.2 迁移业务Item类 (8个文件)
  - ConsultationItem.cs → Consultation/Models/
  - FormulaItem.cs → Formula/Models/
  - FormulaHerbItem.cs → Formula/Models/
  - HerbItem.cs → Herbs/Models/
  - MedicalCaseItem.cs → MedicalCase/Models/
  - PatientItem.cs → Patients/Models/
  - PrescriptionHerbItem.cs → Prescriptions/Models/
  - UserItem.cs → Users/Models/

- [ ] 0.2.3 清理Core/Models/Items/目录
  - 确认所有Item类已迁移
  - 删除空目录

### 0.3 Core层验证

- [ ] 0.3.1 验证Core层纯净度
  - 扫描Core层是否还有业务逻辑
  - 确认只包含框架级代码

- [ ] 0.3.2 编译验证
  - dotnet build LYBT.All.sln
  - 0错误0警告

## Phase 1: ViewModel继承扁平化 (架构优化)

### 1.1 分析并设计新基类体系

- [ ] 1.1.1 设计扁平化继承结构
  ```
  目标: BindableBase → ViewModelCore → 具体ViewModel (3层)

  ViewModelCore: 最小化核心功能
    - INotifyPropertyChanged (来自BindableBase)
    - IDisposable
    - ILogger
    - IsLoading/IsBusy (状态)

  Mixins/Interfaces (组合方式):
    - INavigatable: 导航相关
    - IValidatable: 验证相关
    - ISessionAware: 会话相关
  ```

- [ ] 1.1.2 创建ViewModelCore基类
  - 位置: Core/Models/ViewModels/Base/ViewModelCore.cs
  - 继承: BindableBase
  - 职责: 最小化核心功能 (~100行)

- [ ] 1.1.3 创建Mixin接口和默认实现
  - INavigatable + NavigatableMixin
  - IValidatable + ValidatableMixin
  - IHttpErrorHandler + HttpErrorHandlerMixin

### 1.2 逐步迁移现有ViewModel

- [ ] 1.2.1 试点模块迁移 (Herbs)
  - HerbMasterDetailViewModel使用新基类
  - 验证功能正常

- [ ] 1.2.2 推广到其他模块
  - Users模块
  - Formula模块
  - 其他模块

### 1.3 清理遗留代码

- [ ] 1.3.1 标记旧基类为Obsolete
  - UnifiedViewModelBase
  - MasterDetailViewModelBase (逐步替换)

## Phase 2: Herbs模块Components化

### 2.1 Components设计

- [ ] 2.1.1 设计Component架构
  - HerbCommandHandler: 命令执行
  - HerbDataProvider: 数据加载/导入导出
  - HerbValidator: 验证逻辑

### 2.2 Components实现

- [ ] 2.2.1 创建Components目录
  - Herbs/ViewModels/Components/

- [ ] 2.2.2 实现HerbCommandHandler
  - 提取Add/Edit/Delete/BatchDelete命令
  - 提取Enable/Disable命令

- [ ] 2.2.3 实现HerbDataProvider
  - 提取LoadData/RefreshList
  - 提取Import/Export逻辑

- [ ] 2.2.4 实现HerbValidator
  - 提取ValidateHerb
  - 提取CheckReference

### 2.3 ViewModel重构

- [ ] 2.3.1 重构HerbMasterDetailViewModel
  - 注入Components
  - 委托职责
  - 目标: < 400行 (高于500行标准)

- [ ] 2.3.2 更新HerbsModule DI注册

## Phase 3: Users模块Components化

### 3.1 Components实现

- [ ] 3.1.1 创建Components目录
  - Users/ViewModels/Components/

- [ ] 3.1.2 实现UserCommandHandler
- [ ] 3.1.3 实现UserDataProvider
- [ ] 3.1.4 实现UserValidator

### 3.2 ViewModel重构

- [ ] 3.2.1 重构UserMasterDetailViewModel
  - 目标: < 400行

- [ ] 3.2.2 更新UsersModule DI注册

## Phase 4: 规范与文档更新

### 4.1 规范更新

- [ ] 4.1.1 更新viewmodel-conventions规范
  - 添加继承层级限制 (max 3层)
  - 添加Mixin/组合模式指南
  - 添加标准Components列表

- [ ] 4.1.2 更新client-layer-architecture规范
  - Core层职责明确定义
  - 数据流标准化规范
  - Modules目录结构示例

### 4.2 文档更新

- [ ] 4.2.1 更新架构文档
  - 新基类体系说明
  - 迁移指南

## 验证检查点

### Phase 0 验证
- [ ] Core层零业务代码
- [ ] 零代码重复
- [ ] 编译通过 (0错误0警告)

### Phase 1 验证
- [ ] 新基类体系可用
- [ ] 继承层级 ≤ 3层
- [ ] 试点模块功能正常

### Phase 2 验证
- [ ] HerbMasterDetailViewModel < 400行
- [ ] Components正确注入
- [ ] 功能验证通过

### Phase 3 验证
- [ ] UserMasterDetailViewModel < 400行
- [ ] Components正确注入
- [ ] 功能验证通过

### 最终验证
- [ ] dotnet build LYBT.All.sln (0错误0警告)
- [ ] 所有ViewModel < 500行
- [ ] 数据流清晰 (API → Repository → ViewModel → View)
- [ ] 新开发者架构理解测试
