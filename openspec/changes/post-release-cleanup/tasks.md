# Tasks: Post-Release代码清理与优化

## Phase 1: 过期Management组件移除

### 1.1 Formula模块
- [ ] 删除FormulaManagementView.xaml
- [ ] 删除FormulaManagementView.xaml.cs
- [ ] 删除FormulaManagementViewModel.cs
- [ ] 更新FormulaModule.cs移除注册
- [ ] 编译验证

### 1.2 Herb模块
- [ ] 删除HerbManagementView.xaml
- [ ] 删除HerbManagementView.xaml.cs
- [ ] 删除HerbManagementViewModel.cs
- [ ] 更新HerbsModule.cs移除注册
- [ ] 编译验证

### 1.3 Patient模块
- [ ] 删除PatientManagementView.xaml
- [ ] 删除PatientManagementView.xaml.cs
- [ ] 删除PatientManagementViewModel.cs
- [ ] 更新PatientsModule.cs移除注册
- [ ] 编译验证

### 1.4 User模块
- [ ] 删除UserManagementView.xaml
- [ ] 删除UserManagementView.xaml.cs
- [ ] 删除UserManagementViewModel.cs
- [ ] 更新UsersModule.cs移除注册
- [ ] 编译验证

### 1.5 MedicalCase模块
- [ ] 删除MedicalCaseManagementView.xaml
- [ ] 删除MedicalCaseManagementView.xaml.cs
- [ ] 删除MedicalCaseManagementViewModel.cs
- [ ] 删除MedicalCaseDetailView.xaml
- [ ] 删除MedicalCaseDetailView.xaml.cs
- [ ] 删除MedicalCaseDetailViewModel.cs
- [ ] 更新MedicalCaseModule.cs移除注册
- [ ] 编译验证

## Phase 2: 过期DTO清理

### 2.1 识别过期DTO
- [ ] 搜索所有*Legacy类
- [ ] 搜索所有*QueryDto类
- [ ] 搜索所有*SearchDto类
- [ ] 生成待删除清单

### 2.2 删除过期DTO
- [ ] 删除识别的Legacy类
- [ ] 删除识别的QueryDto类
- [ ] 删除识别的SearchDto类
- [ ] 更新引用
- [ ] 编译验证

### 2.3 清理AutoMapper配置
- [ ] 移除旧DTO映射配置
- [ ] 验证新映射正确性
- [ ] 编译验证

## Phase 3: 服务层DTO迁移

### 3.1 User模块服务层
- [ ] UserController迁移到UserListDto/UserDetailDto
- [ ] IUserService接口更新
- [ ] UserService实现更新
- [ ] 测试验证

### 3.2 其他模块服务层 (按需)
- [ ] Formula模块服务层迁移
- [ ] Patient模块服务层迁移
- [ ] Herb模块服务层迁移
- [ ] MedicalCase模块服务层迁移

## Phase 4: MedicalCase API端点优化

### 4.1 查询端点合并
- [ ] 合并GetList和GetMedicalCasesList为GET /
- [ ] 合并GetById和GetMedicalCaseByIdWithDetails为GET /{id} (添加include参数)
- [ ] 合并患者查询端点为GET /patient/{patientId} (添加filter参数)
- [ ] 删除GetConsultationList和GetPrescriptionList

### 4.2 状态端点统一
- [ ] 创建PATCH /{id}/status统一端点
- [ ] 迁移CloseMedicalCase到新端点
- [ ] 迁移CancelMedicalCase到新端点
- [ ] 迁移UpdateStatus到新端点
- [ ] 删除SetPrescriptionFlag端点

### 4.3 处方端点清理
- [ ] 删除独立的CreatePrescription端点
- [ ] 删除独立的UpdatePrescription端点
- [ ] 删除独立的DeletePrescription端点
- [ ] 删除UpdateConsultation端点

### 4.4 Client端同步
- [ ] 更新IMedicalCaseApi接口
- [ ] 更新MedicalCaseRepository
- [ ] 更新调用点
- [ ] 编译验证

## Phase 5: 验证与测试

### 5.1 编译验证
- [ ] dotnet build LYBT.All.sln (0 errors, 0 warnings)

### 5.2 单元测试
- [ ] 运行所有Server模块测试
- [ ] 运行所有Client模块测试
- [ ] 确保测试通过率100%

### 5.3 功能回归测试
- [ ] Formula模块MasterDetail功能
- [ ] Herb模块MasterDetail功能
- [ ] Patient模块MasterDetail功能
- [ ] User模块MasterDetail功能
- [ ] MedicalCase模块MasterDetail功能
- [ ] 看诊工作流完整测试

### 5.4 文档更新
- [ ] 更新API文档
- [ ] 更新CHANGELOG
- [ ] 归档提案

## 执行优先级

| Phase | 优先级 | 风险 | 建议 |
|-------|--------|------|------|
| Phase 1 | P1 | Low | 先执行，影响最小 |
| Phase 2 | P1 | Low | 与Phase 1同步执行 |
| Phase 3 | P2 | Medium | 可选优化 |
| Phase 4 | P2 | High | 需要全面Client重构 |
| Phase 5 | P0 | - | 每个Phase完成后执行 |

## 完成标准

- [ ] 所有[Obsolete]代码已删除
- [ ] 编译0错误0警告
- [ ] 所有测试通过
- [ ] 功能回归测试通过
- [ ] 文档已更新
