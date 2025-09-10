---
name: 04-code-quality-improvement
status: backlog
created: 2025-09-08T10:24:53Z
progress: 0%
prd: .claude/prds/04-code-quality-improvement.md
github: [Will be updated when synced to GitHub]
---

# Epic: 04-code-quality-improvement

## Overview

通过消除代码重复、建立统一验证框架、重构过大类结构和建立代码质量检查机制，全面提升代码质量。将代码重复率从15%降至3%，平均类行数控制在200行以下，建立可维护的代码架构基础。

## Architecture Decisions

- **统一验证框架**: FluentValidation + ValidationService，消除分散的验证逻辑
- **全局异常处理**: 统一异常中间件，标准化错误响应格式
- **ViewModel重构**: 大型ViewModel按职责拆分，事件协调机制
- **通用基础设施**: 抽象查询服务基类、CRUD服务基类，减少重复代码
- **代码质量工具**: SonarQube + EditorConfig集成，自动化质量检查

## Technical Approach

### Frontend Components
- **ViewModel重构**: 
  - PatientManagementViewModel拆分为List、Search、Edit、ImportExport四个专门ViewModel
  - 基类抽象: BaseCollectionViewModel、BaseEditViewModel
  - 事件协调机制处理ViewModel间通信
- **验证集成**: 前端验证与后端ValidationService保持一致

### Backend Services
- **统一验证**: 
  - ValidationService提供手机号、身份证、邮箱等通用验证
  - FluentValidation验证器自动化DTO验证
  - 全局验证中间件
- **异常处理**:
  - GlobalExceptionMiddleware统一异常捕获
  - 自定义异常类型(BusinessException、ValidationException等)
  - 结构化异常日志记录
- **基础设施**:
  - BaseQueryService<TEntity, TDto> 通用分页查询基类
  - BaseCrudService<TEntity, TDto, TCreateDto, TUpdateDto> 通用CRUD基类

### Infrastructure
- **代码质量工具**: SonarQube规则配置，代码重复率、复杂度监控
- **开发规范**: EditorConfig统一代码风格，命名规范自动检查
- **持续集成**: 代码质量门禁，覆盖率要求80%

## Implementation Strategy

- **渐进式重构**: 保持向后兼容，逐步迁移到新框架
- **A/B测试**: 功能开关支持新旧实现共存
- **Phase 1** (5天): 验证框架和异常处理基础设施
- **Phase 2** (3天): 全局异常处理中间件
- **Phase 3** (5天): ViewModel重构和职责分离  
- **Phase 4** (2天): 通用基础设施和工具集成

## Task Breakdown Preview

High-level task categories that will be created:
- [ ] **统一验证框架**: ValidationService、FluentValidation集成、验证中间件 (5天)
- [ ] **全局异常处理**: 异常中间件、自定义异常类、统一错误响应 (3天)
- [ ] **ViewModel重构**: 大型类拆分、基类抽象、事件协调 (5天)
- [ ] **通用基础设施**: 查询基类、CRUD基类、工具类库 (2天)

## Dependencies

- **现有业务服务**: 需要与8个业务模块的Service层协调重构
- **前端MVVM架构**: 基于现有Prism.DryIoc框架进行优化
- **FluentValidation**: 引入第三方验证框架，需要NuGet包管理
- **代码分析工具**: SonarQube服务器配置和集成

## Success Criteria (Technical)

- **代码重复率**: 从15%降至3%以下
- **类大小控制**: 90%的类行数 < 200行
- **方法复杂度**: 95%的方法圈复杂度 < 10  
- **测试覆盖率**: 整体覆盖率 > 80%，关键模块 > 90%
- **开发效率**: 基于新框架开发功能效率提升30%
- **维护成本**: 验证类Bug修复时间减少50%

## Tasks Created
- [ ] 001.md - 建立统一验证框架和FluentValidation集成 (parallel: true)
- [ ] 002.md - 实现全局异常处理和标准化响应 (parallel: false)
- [ ] 003.md - ViewModel重构和职责分离优化 (parallel: false)
- [ ] 004.md - 建立通用基础设施和CRUD基类 (parallel: true)
- [ ] 005.md - 集成代码质量工具和自动化检查 (parallel: true)

Total tasks: 5
Parallel tasks: 3
Sequential tasks: 2
Estimated total effort: 114 hours (约14.25工作日)

## Estimated Effort

- **总体工期**: 15工作日
- **开发资源**: 1名高级.NET工程师专职重构
- **代码审查**: 每个重构阶段进行Code Review
- **风险评估**: 低风险，主要关注功能兼容性和重构安全性
- **质量保证**: 重构前后功能完全一致，性能无衰减