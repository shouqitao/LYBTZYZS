# Tasks: consolidate-code-quality

## Phase 1: 高复杂度代码重构

### 1.1 BaseApiController.GetOperator() (CC: 37 → <15)
- [ ] 分析现有实现和调用点
- [ ] 提取角色检查为独立方法
- [ ] 使用策略模式重构条件分支
- [ ] 添加/更新单元测试
- [ ] 验证CC降低

### 1.2 PatientImportExecutor.ImportWorker_RunWorkerCompleted() (CC: 30 → <15)
- [ ] 分析异步回调逻辑
- [ ] 提取验证逻辑到`ValidateImportResult()`
- [ ] 提取转换逻辑到`ProcessImportedRows()`
- [ ] 提取错误处理到`HandleImportErrors()`
- [ ] 添加/更新单元测试
- [ ] 验证CC降低

### 1.3 MedicalCaseRepository.UpdateAsync() (CC: 28 → <15)
- [ ] 分析更新逻辑分支
- [ ] 提取处方更新逻辑到`UpdatePrescriptions()`
- [ ] 提取诊断更新逻辑到`UpdateConsultation()`
- [ ] 简化条件判断
- [ ] 添加/更新单元测试
- [ ] 验证CC降低

### 1.4 ExcelHelper 重构 (CC: 25+22 → <10)
- [ ] 分析`ConvertValueToPropertyType`类型转换分支
- [ ] 分析`SetCellValue`类型处理分支
- [ ] 创建类型转换器字典
- [ ] 使用策略模式重构
- [ ] 添加/更新单元测试
- [ ] 验证CC降低

### 1.5 MedicalCaseCommandService.SaveAsync() (CC: 23 → <15)
- [ ] 分析保存逻辑
- [ ] 拆分为`CreateMedicalCaseAsync()`和`UpdateMedicalCaseAsync()`
- [ ] 提取验证逻辑到`ValidateMedicalCase()`
- [ ] 提取处方处理到`ProcessPrescriptions()`
- [ ] 添加/更新单元测试
- [ ] 验证CC降低

### 1.6 PatientImportDataMapper.CreatePatientDtoFromRow() (CC: 21 → <10)
- [ ] 分析字段映射逻辑
- [ ] 创建字段映射配置
- [ ] 简化转换逻辑
- [ ] 添加/更新单元测试
- [ ] 验证CC降低

## Phase 2: EF迁移整合

### 2.1 迁移目录统一
- [ ] 备份当前迁移文件
- [ ] 分析`Data/Migrations/`中5个迁移的依赖关系
- [ ] 将迁移文件移动到`Migrations/`目录
- [ ] 更新命名空间引用
- [ ] 删除空的`Data/Migrations/`目录
- [ ] 验证`dotnet ef migrations list`正确显示所有迁移
- [ ] 验证`dotnet ef database update`可正常执行

### 2.2 迁移压缩 (可选 - v1.0后执行)
- [ ] 创建数据库完整备份
- [ ] 记录当前迁移历史
- [ ] 执行`dotnet ef migrations remove`删除历史迁移
- [ ] 执行`dotnet ef migrations add InitialCreate`创建压缩迁移
- [ ] 验证压缩迁移与原数据库Schema一致
- [ ] 更新`__EFMigrationsHistory`表
- [ ] 验证应用程序正常运行

## 验证清单

- [ ] 运行Code Metrics验证CC降低
- [ ] 所有单元测试通过
- [ ] 所有集成测试通过
- [ ] 编译无警告
- [ ] 应用程序启动正常
- [ ] 核心功能冒烟测试通过
