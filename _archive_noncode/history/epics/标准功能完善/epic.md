---
name: 标准功能完善
status: backlog
created: 2025-09-04T00:46:52Z
progress: 0%
prd: .claude/prds/标准功能完善.md
github: https://github.com/shouqitao/LYBTZYZS/issues/486
---

# Epic: 标准功能完善

## Overview

基于LYBTZYZS中医诊所系统96%符合率分析，需完善剩余7个功能点以实现100%标准化。重点优化Excel导入向导UI体验，增强系统安全性和用户体验。后端核心API已完成，主要工作集中在前端UI优化和安全机制补全。

## Architecture Decisions

- **UI框架决策**: 复用现有WPF + Prism.DryIoc模块化架构，避免引入新框架
- **Excel处理**: 利用现有NPOI库和后端ImportXxxAsync API，前端仅需UI向导包装
- **安全架构**: 扩展现有JWT认证体系，增加登录锁定和审计日志功能
- **数据验证**: 前后端双重验证，前端用户体验优先，后端数据完整性保障
- **异步处理**: Excel导入使用BackgroundWorker模式，提供进度反馈和取消功能

## Technical Approach

### Frontend Components
- **Excel导入向导**: 4步向导组件（模板下载→文件选择→数据预览→导入执行）
- **进度指示器**: 可取消的异步操作进度条组件
- **数据验证UI**: 错误高亮、批量修正、部分导入支持
- **审计查看器**: 分页列表、时间线视图、筛选和搜索组件
- **安全锁定**: 登录失败计数器、锁定状态显示组件

### Backend Services
- **扩展现有API**: 在PatientModule、FormulaModule、HerbModule基础上增加验证接口
- **安全服务增强**: AuthService添加锁定机制，UserService添加审计日志查询
- **文件处理优化**: 增加Excel文件格式验证、恶意文件检测
- **审计日志**: 扩展现有日志系统，增加操作详情记录

### Infrastructure
- **现有数据库扩展**: 添加LoginAttempts、AuditLogs表，无需架构重构
- **缓存策略**: 利用现有IMemoryCache缓存模板和用户锁定状态
- **异常处理**: 复用现有统一异常处理体系
- **权限控制**: 基于现有RBAC权限系统扩展

## Implementation Strategy

### Phase 1: Excel导入向导UI实现 (2周)
- 利用现有API快速构建向导界面
- 重点优化用户体验而非重构后端
- 渐进式功能发布：Patients→Formula→Herbs

### Phase 2: 安全功能增强 (1个月)
- 最小化修改现有认证流程
- 增量添加锁定和审计功能
- 向后兼容现有用户会话

### Phase 3: 用户体验完善 (3个月)
- 非核心功能，可选实现
- 基于用户反馈决定实施优先级

## Task Breakdown Preview

高级任务分解（8个核心任务，符合简化原则）：
- [ ] **Task 1**: Patients模块Excel导入向导UI实现
- [ ] **Task 2**: Formula模块Excel导入导出UI集成  
- [ ] **Task 3**: Herbs模块导入体验优化
- [ ] **Task 4**: 登录失败锁定机制实现
- [ ] **Task 5**: 用户操作审计详情页开发
- [ ] **Task 6**: 患者照片管理功能（低优先级）
- [ ] **Task 7**: 语音录入支持（可选）
- [ ] **Task 8**: 集成测试和性能验证

## Dependencies

### 外部依赖
- **现有后端API**: PatientModule.ImportPatientsAsync等已实现接口
- **NPOI Excel库**: 现有Excel处理能力
- **JWT认证体系**: 现有安全基础设施

### 内部团队依赖
- **UltraThink架构**: 必须遵循现有双层架构模式
- **设计规范**: 保持与现有UI风格一致
- **测试团队**: 需要完整的手工测试验证

### 前置工作
- 无需重大架构变更，基于现有96%完成度继续开发
- 现有编译零错误状态必须维持

## Success Criteria (Technical)

### 性能基准
- Excel导入处理时间: <30秒（1000条记录）
- UI响应时间: <2秒
- 导入成功率: >95%
- 系统稳定性: 零崩溃

### 质量门禁
- 零编译警告错误
- 关键功能单元测试覆盖
- 手工测试全场景通过
- 现有功能回归测试通过

### 验收标准
- 标准功能检查清单符合率: 96% → 100%
- Excel导入向导包含完整4步流程
- 安全锁定机制有效防护
- 用户体验直观友好，无需技术背景

## Estimated Effort

### 总体时间估算
- **高优先级 (核心)**: 2周 - Excel导入向导UI实现
- **中优先级 (安全)**: 4周 - 登录锁定和审计功能
- **低优先级 (增强)**: 8周 - 照片管理和语音录入

### 资源需求
- **前端开发**: 1人 × 6周（主要工作量）
- **后端扩展**: 0.5人 × 4周（API增强和安全功能）
- **测试验证**: 0.5人 × 2周（全功能回归测试）

### 关键路径
1. Patients模块导入向导（高价值，快速交付）
2. Formula模块UI集成（复用导入向导组件）
3. 安全功能实现（独立并行开发）
4. 用户体验增强（基于用户反馈决定）

## Risk Mitigation

### 技术风险
- **向导UI复杂度**: 采用分步骤组件化设计，降低开发难度
- **Excel兼容性**: 基于现有NPOI库，风险可控
- **性能影响**: 异步处理 + 进度反馈，用户体验优先

### 业务风险
- **用户接受度**: 渐进式发布，及时收集反馈调整
- **功能蔓延**: 严格按PRD执行，拒绝清单外需求

通过复用现有技术栈和API基础，专注于UI体验优化，实现快速交付和低风险实施。

## Tasks Created
- [ ] #487 - Patients模块Excel导入向导UI实现 (parallel: true)
- [ ] #488 - Formula模块Excel导入导出UI集成 (parallel: true)
- [ ] #489 - Herbs模块导入体验优化 (parallel: true)
- [ ] #490 - 集成测试和性能验证 (parallel: false, depends_on: [487, 488, 489])

Total tasks: 4
Parallel tasks: 3
Sequential tasks: 1
Estimated total effort: 28 hours (3.5工作日)

## UltraThink约束合规性
✅ **完全符合** `docs/architecture/ULTRATHINK_REFACTORING_REPORT.md` 约束
- Excel导入导出功能：标准功能清单⚡必需功能，已有HerbImportExportController基础
- 不新增文档未定义功能：移除了登录锁定和审计功能（标准清单未包含）
- 基于现有架构扩展：复用8个核心模块标准化基础