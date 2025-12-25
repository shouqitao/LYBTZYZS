# Tasks: sync-entity-dto-fields

## Phase 1: 字段映射规范定义
- [ ] 1.1 定义ListDto字段选择标准（标识、名称、关键业务字段、状态）
- [ ] 1.2 定义DetailDto字段选择标准（Entity全部业务字段）
- [ ] 1.3 定义InputDto字段选择标准（用户可编辑字段，排除系统管理字段）
- [ ] 1.4 更新dto-architecture spec添加字段同步要求

## Phase 2: Entity字段审计
- [ ] 2.1 审计Herbs模块Entity字段属性（类型、必填、DisplayName）
- [ ] 2.2 审计Users模块Entity字段属性
- [ ] 2.3 审计Patients模块Entity字段属性
- [ ] 2.4 审计Formula模块Entity字段属性
- [ ] 2.5 审计MedicalCase模块Entity字段属性
- [ ] 2.6 审计Prescriptions模块Entity字段属性

## Phase 3: DTO同步
- [ ] 3.1 同步Herbs模块DTO（ListDto、DetailDto、InputDto）
- [ ] 3.2 同步Users模块DTO
- [ ] 3.3 同步Patients模块DTO
- [ ] 3.4 同步Formula模块DTO
- [ ] 3.5 同步MedicalCase模块DTO
- [ ] 3.6 同步Prescriptions模块DTO

## Phase 4: Validator同步
- [ ] 4.1 同步Herbs模块Validator与Entity/DTO一致
- [ ] 4.2 同步Users模块Validator
- [ ] 4.3 同步Patients模块Validator
- [ ] 4.4 同步Formula模块Validator
- [ ] 4.5 同步MedicalCase模块Validator
- [ ] 4.6 同步Prescriptions模块Validator

## Phase 5: 前端同步
- [ ] 5.1 同步Herbs模块DependencyProperty类型与验证
- [ ] 5.2 同步Herbs模块XAML标签文本
- [ ] 5.3 同步Users模块前端
- [ ] 5.4 同步Patients模块前端
- [ ] 5.5 同步Formula模块前端
- [ ] 5.6 同步MedicalCase模块前端
- [ ] 5.7 同步Prescriptions模块前端

## Phase 6: 验证
- [ ] 6.1 编译全解决方案
- [ ] 6.2 运行单元测试
- [ ] 6.3 手动验证各模块CRUD功能
- [ ] 6.4 更新viewmodel-conventions spec

## Completion Criteria
- 所有任务标记为完成 `[x]`
- 编译无错误无警告
- 基本功能测试通过
