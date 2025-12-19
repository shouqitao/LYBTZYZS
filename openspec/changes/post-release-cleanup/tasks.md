# Tasks: Post-Release代码清理与优化

## Phase 1: 过期Management组件移除

### 1.1 Formula模块
- [x] 删除FormulaManagementView.xaml
- [x] 删除FormulaManagementView.xaml.cs
- [x] 删除FormulaManagementViewModel.cs
- [x] 更新FormulaModule.cs移除注册
- [x] 编译验证

### 1.2 Herb模块
- [x] 删除HerbManagementView.xaml
- [x] 删除HerbManagementView.xaml.cs
- [x] 删除HerbManagementViewModel.cs
- [x] 更新HerbsModule.cs移除注册
- [x] 编译验证

### 1.3 Patient模块
- [x] 删除PatientManagementView.xaml
- [x] 删除PatientManagementView.xaml.cs
- [x] 删除PatientManagementViewModel.cs
- [x] 更新PatientsModule.cs移除注册
- [x] 编译验证

### 1.4 User模块
- [x] 删除UserManagementView.xaml
- [x] 删除UserManagementView.xaml.cs
- [x] 删除UserManagementViewModel.cs
- [x] 更新UsersModule.cs移除注册
- [x] 编译验证

### 1.5 MedicalCase模块
- [x] 删除MedicalCaseManagementView.xaml
- [x] 删除MedicalCaseManagementView.xaml.cs
- [x] 删除MedicalCaseManagementViewModel.cs
- [x] 删除MedicalCaseDetailView.xaml
- [x] 删除MedicalCaseDetailView.xaml.cs
- [x] 删除MedicalCaseDetailViewModel.cs
- [x] 更新MedicalCaseModule.cs移除注册
- [x] 编译验证

## Phase 2: 过期DTO清理

### 2.1 识别过期DTO
- [x] 搜索所有*Legacy类
- [x] 搜索所有*QueryDto类
- [x] 搜索所有*SearchDto类
- [x] 生成待删除清单

### 2.2 删除过期DTO
- [x] 删除识别的Legacy类
- [x] 删除识别的QueryDto类
- [x] 删除识别的SearchDto类
- [x] 更新引用
- [x] 编译验证

### 2.3 清理AutoMapper配置
- [x] 移除旧DTO映射配置
- [x] 验证新映射正确性
- [x] 编译验证

## Phase 3: 服务层DTO迁移

### 3.1 User模块服务层
- [x] UserController迁移到UserListDto/UserDetailDto
- [x] IUserService接口更新
- [x] UserService实现更新
- [x] 测试验证

### 3.2 其他模块服务层 (按需)
- [x] Formula模块服务层迁移
- [x] Patient模块服务层迁移
- [x] Herb模块服务层迁移
- [x] MedicalCase模块服务层迁移

## Phase 4: MedicalCase API端点优化

### 4.1 查询端点合并
- [x] 合并GetList和GetMedicalCasesList为GET /
- [N/A] 合并GetById和GetMedicalCaseByIdWithDetails为GET /{id} (添加include参数) - DEFERRED
- [N/A] 合并患者查询端点为GET /patient/{patientId} (添加filter参数) - DEFERRED
- [N/A] 删除GetConsultationList和GetPrescriptionList - DEFERRED

### 4.2 状态端点统一
- [N/A] 创建PATCH /{id}/status统一端点 - DEFERRED
- [N/A] 迁移CloseMedicalCase到新端点 - DEFERRED
- [N/A] 迁移CancelMedicalCase到新端点 - DEFERRED
- [N/A] 迁移UpdateStatus到新端点 - DEFERRED
- [N/A] 删除SetPrescriptionFlag端点 - DEFERRED

### 4.3 处方端点清理
- [N/A] 删除独立的CreatePrescription端点 - DEFERRED
- [N/A] 删除独立的UpdatePrescription端点 - DEFERRED
- [N/A] 删除独立的DeletePrescription端点 - DEFERRED
- [N/A] 删除UpdateConsultation端点 - DEFERRED

### 4.4 Client端同步
- [x] 更新IMedicalCaseApi接口
- [x] 更新MedicalCaseRepository
- [x] 更新调用点
- [x] 编译验证

## Phase 5: 验证与测试

### 5.1 编译验证
- [x] dotnet build LYBT.All.sln (0 errors, 0 warnings)

### 5.2 单元测试
- [x] 运行所有Server模块测试
- [x] 运行所有Client模块测试
- [x] 确保测试通过率100%

### 5.3 功能回归测试
- [x] Formula模块MasterDetail功能
- [x] Herb模块MasterDetail功能
- [x] Patient模块MasterDetail功能
- [x] User模块MasterDetail功能
- [x] MedicalCase模块MasterDetail功能
- [x] 看诊工作流完整测试

### 5.4 文档更新
- [x] 更新API文档
- [x] 更新CHANGELOG
- [x] 归档提案

## 执行优先级

| Phase | 优先级 | 风险 | 状态 |
|-------|--------|------|------|
| Phase 1 | P1 | Low | 完成 |
| Phase 2 | P1 | Low | 完成 |
| Phase 3 | P2 | Medium | 完成 |
| Phase 4 | P2 | High | 核心完成，DEFERRED项待定 |
| Phase 5 | P0 | - | 完成 |

## 完成标准

- [x] 所有[Obsolete]代码已删除
- [x] 编译0错误0警告
- [x] 所有测试通过
- [x] 功能回归测试通过
- [x] 文档已更新

## 完成日期

2025-12-19
