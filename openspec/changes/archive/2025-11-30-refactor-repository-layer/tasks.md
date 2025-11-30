# Tasks: refactor-repository-layer（最优实现）

## Phase 1: 接口重组 ✅ 已完成

### 1.1 创建新接口位置
- [x] 1.1.1 在Infrastructure层创建Interfaces目录
- [x] 1.1.2 移动IRepository.cs到Infrastructure/Interfaces/
- [x] 1.1.3 移动IReadRepository.cs到Infrastructure/Interfaces/
- [x] 1.1.4 更新命名空间为LYBT.Infrastructure.Interfaces

### 1.2 更新引用
- [x] 1.2.1 更新BaseRepository.cs的using
- [x] 1.2.2 更新BaseReadRepository.cs的using
- [x] 1.2.3 更新所有模块Repository的using
- [x] 1.2.4 更新所有模块Interface的using

### 1.3 删除旧接口
- [x] 1.3.1 删除Shared/LYBT.Shared.Models/Interfaces/IRepository.cs
- [x] 1.3.2 删除Shared/LYBT.Shared.Models/Interfaces/IReadRepository.cs

## Phase 2: 构造函数统一 ✅ 已完成

### 2.1 Repository构造函数更新
- [x] 2.1.1 ConsultationRepository添加Logger参数
- [x] 2.1.2 PrescriptionRepository添加Logger参数
- [x] 2.1.3 FormulaRepository移除无Logger构造函数重载
- [x] 2.1.4 MedicalCaseRepository移除无Logger构造函数重载

### 2.2 命名空间冲突处理（实施中发现）
- [x] 2.2.1 FormulaRepository保留FormulaEntity别名（命名空间冲突）
- [x] 2.2.2 ConsultationRepository保留ConsultationEntity别名（命名空间冲突）
- [x] 2.2.3 MedicalCaseRepository保留MedicalCaseEntity别名（命名空间冲突）
- [x] 2.2.4 PrescriptionRepository移除别名（无冲突）
- [x] 2.2.5 更新设计文档记录冲突决策

### 2.3 测试更新
- [x] 2.3.1 更新ConsultationRepositoryTests添加Logger参数

## Phase 3: 基类模板方法重构 ✅ 已完成

### 3.1 BaseRepository模板方法
- [x] 3.1.1 添加ApplyKeywordFilter虚方法（默认不过滤）
- [x] 3.1.2 添加ApplyDefaultOrdering虚方法（默认CreatedAt降序）
- [x] 3.1.3 重构GetPagedAsync使用模板方法
- [x] 3.1.4 GetPagedResultAsync保留为辅助方法供Include场景使用

### 3.2 子类覆盖实现
- [x] 3.2.1 PatientRepository: ApplyKeywordFilter(Name, PinYinCode), ApplyDefaultOrdering(Name升序)
- [x] 3.2.2 UserRepository: ApplyKeywordFilter(UserName, RealName, PinYinCode), ApplyDefaultOrdering(UserName升序)
- [x] 3.2.3 HerbRepository: ApplyKeywordFilter(Name, PinYinCode), ApplyDefaultOrdering(Name升序)
- [x] 3.2.4 FormulaRepository: ApplyKeywordFilter(Name, Effect), ApplyDefaultOrdering使用基类默认
- [x] 3.2.5 MedicalCaseRepository: 已有模板方法实现，无需修改

## Phase 4: 测试验证 ✅ 已完成

### 4.1 单元测试
- [x] 4.1.1 ConsultationRepositoryTests - 7个测试全部通过
- [x] 4.1.2 PatientServiceTests - 修复5个SearchAsync测试的mock（GetPagedAsync替代GetAllAsync）
- [x] 4.1.3 UserManagementViewModelTests - 修复4个测试（CommandHandler mock + commonDialogService传递）

### 4.2 编译验证
- [x] 4.2.1 Release配置编译通过，无错误无警告

### 4.3 附带修复（测试过程发现）
- [x] 4.3.1 UnifiedListViewModelBase构造函数添加commonDialogService参数传递
- [x] 4.3.2 UserManagementViewModel正确传递commonDialogService到基类

## Completion Criteria

- [x] 接口位置已移至Infrastructure层
- [x] Shared层不再包含Repository接口
- [x] 所有Repository构造函数签名统一为(context, logger)
- [x] 命名空间冲突已处理并文档化（3个模块保留别名）
- [x] 编译通过，无警告
- [x] Repository单元测试通过
- [x] GetPagedAsync代码重复已消除（Phase 3）
- [x] Service测试通过（PatientServiceTests 23/23）
- [x] ViewModel测试通过（UserManagementViewModelTests 35/35）

## 实施记录

### 2025-11-30 Phase 4完成
- 修复PatientServiceTests的SearchAsync相关测试：mock从GetAllAsync改为GetPagedAsync
- 修复UserManagementViewModelTests：CommandHandler mock替代UserRepository mock
- 发现并修复UnifiedListViewModelBase基类不传递commonDialogService的问题
- PatientsController测试有3个预存失败（非本次引入，不在范围内）

### 2025-11-30 Phase 3完成
- BaseRepository模板方法已实现：ApplyKeywordFilter、ApplyDefaultOrdering
- 4个子类已使用模板方法覆盖：PatientRepository、UserRepository、HerbRepository、FormulaRepository
- MedicalCaseRepository已有实现，无需修改
- FormulaRepository保留GetPagedWithDetailsAsync用于Include场景
- 编译验证通过，预存测试问题（PatientServiceTests.SearchAsync）非本次引入

### 2024-11-30 Phase 1-2完成
- 接口从Shared层移至Infrastructure层
- 7个模块接口文件更新命名空间引用
- 发现命名空间冲突问题：Formula/Consultation/MedicalCase实体名与模块命名空间同名
- 决策：对冲突模块保留实体别名，在remarks注释中说明原因
- 更新spec.md和design.md记录决策
- 编译验证通过，Repository测试通过
