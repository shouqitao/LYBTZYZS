# RC就绪设计需求分析 - 前端部分 (Refit Client → VM层)

**分析日期**: 2026-04-08  
**目标**: 达到Release Candidate条件 - 所有代码测试到位，业务逻辑错误修正，缺失补充

---

## 1. 当前架构概览

### 1.1 分层架构

```
┌─────────────────────────────────────────────────────────────┐
│  View (XAML)                                               │
│  - 患者管理界面                                             │
│  - 药材管理界面                                             │
│  - 医案工作台                                               │
└────────────────────┬────────────────────────────────────────┘
                     │ Data Binding
┌────────────────────▼────────────────────────────────────────┐
│  ViewModel (34个VM)                                        │
│  - MasterDetailViewModels (CRUD操作)                       │
│  - WorkspaceViewModels (复杂业务)                          │
│  - DialogViewModels (模态对话框)                           │
└────────────────────┬────────────────────────────────────────┘
                     │ Call
┌────────────────────▼────────────────────────────────────────┐
│  Service Layer                                             │
│  - IPatientRepository, IHerbRepository...                  │
│  - DesktopCacheManager                                     │
│  - StatusHandlers                                          │
└────────────────────┬────────────────────────────────────────┘
                     │ HTTP/Refit
┌────────────────────▼────────────────────────────────────────┐
│  Refit Client                                              │
│  - IPatientApi, IHerbApi, IAuthApi...                      │
│  - ApiResponse<T> wrapper                                  │
└────────────────────┬────────────────────────────────────────┘
                     │ HTTP
┌────────────────────▼────────────────────────────────────────┐
│  WebAPI                                                    │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. 测试覆盖率分析

### 2.1 已测试的ViewModels ✅

| ViewModel | 测试文件 | 测试数 | 状态 |
|-----------|----------|--------|------|
| PatientMasterDetailViewModel | PatientMasterDetailViewModelTests.cs | 40+ | ✅ 完整 |
| MedicalCaseWorkspaceViewModel | MedicalCaseWorkspaceViewModelTests.cs | 15+ | ✅ 完整 |
| FormulaMasterDetailViewModel | FormulaMasterDetailViewModelTests.cs | 20+ | ✅ 完整 |
| HerbMasterDetailViewModel | HerbMasterDetailViewModelTests.cs | 18+ | ✅ 完整 |
| PrescriptionEditorViewModel | PrescriptionEditorViewModelTests.cs | 12+ | ✅ 完整 |
| RegistrationMasterDetailViewModel | RegistrationMasterDetailViewModelTests.cs | 15+ | ✅ 完整 |

### 2.2 需要补充测试的ViewModels ⚠️

| ViewModel | 优先级 | 测试重点 | 估计工作量 |
|-----------|--------|----------|------------|
| UserMasterDetailViewModel | 高 | CRUD操作、角色权限 | 2-3小时 |
| MedicalCaseCommandsViewModel | 高 | 命令执行、状态机 | 2-3小时 |
| ConsultationEditorViewModel | 中 | 诊断逻辑、望闻问切 | 2小时 |
| SyncViewModel | 中 | 同步逻辑、冲突处理 | 2小时 |
| CardReaderViewModel | 低 | 读卡集成 | 1小时 |
| 各种DialogViewModels | 低 | 简单确认/输入 | 可跳过 |

---

## 3. 业务逻辑错误修正清单

### 3.1 已修复 ✅

1. **Controller状态码映射** (400→422)
   - 文件: ServiceCollectionExtensions.cs:207
   - 修复: UnprocessableEntityObjectResult
   - 验证: 负面测试返回422

2. **测试数据生成器**
   - GenerateIdNumber: 18位身份证号 + ISO 7064校验
   - GeneratePhoneNumber: 11位手机号格式
   - 线程安全: 添加lock和Interlocked

3. **Refit Client契约测试**
   - 12个契约测试覆盖IPatientApi/IAuthApi

### 3.2 需要检查 ⚠️

1. **MedicalCase状态机逻辑**
   - 检查状态转换是否正确
   - Pending → InProgress → Completed → Closed
   - 验证非法状态转换是否被阻止

2. **权限验证逻辑**
   - Doctor只能编辑自己的医案
   - Admin可以管理所有用户
   - Receptionist只能创建挂号

3. **数据一致性检查**
   - 删除患者时检查是否有未完成的挂号
   - 删除药材时检查是否被方剂引用
   - 关闭医案时检查是否有未打印处方

---

## 4. 缺失补充清单

### 4.1 测试补充

#### High Priority (Must Have for RC)

```csharp
// 1. UserMasterDetailViewModelTests.cs - 缺失
// 需要测试:
// - 用户CRUD操作
// - 角色变更验证
// - 密码重置流程

// 2. MedicalCaseCommandsViewModelTests.cs - 缺失
// 需要测试:
// - 状态机转换
// - 命令CanExecute逻辑
// - 批量操作

// 3. SyncViewModelTests.cs - 缺失
// 需要测试:
// - 同步触发逻辑
// - 冲突处理
// - 进度报告
```

#### Medium Priority (Should Have)

```csharp
// 4. ConsultationEditorViewModelTests.cs - 部分覆盖
// 补充: 诊断逻辑、TCM四诊验证

// 5. PatientCardReaderViewModelTests.cs
// 补充: 读卡流程、患者匹配
```

### 4.2 业务逻辑补充

1. **输入验证统一**
   - 所有VM应该使用相同的验证模式
   - 目前有的用FluentValidation，有的用手动验证
   - 建议: 统一使用FluentValidation

2. **错误处理标准化**
   - 统一错误提示方式
   - 统一日志记录格式
   - 统一用户友好错误消息

3. **缓存策略优化**
   - Patient缓存已实施
   - Herb/Formula缓存需要评估
   - MedicalCase缓存策略需要明确

---

## 5. RC就绪检查清单

### 5.1 测试覆盖率要求

| 模块 | 当前覆盖率 | 目标覆盖率 | 状态 |
|------|-----------|-----------|------|
| Refit Client | 90%+ | 90% | ✅ |
| ViewModels | 60% | 80% | ⚠️ 需补充 |
| Business Logic | 70% | 85% | ⚠️ 需补充 |
| E2E Workflows | 75% | 80% | ✅ |

### 5.2 关键路径测试

- [x] 患者管理完整流程
- [x] 药材方剂管理
- [x] 医案诊断开方
- [ ] 用户权限管理 (部分)
- [x] 数据同步基础

### 5.3 性能基准

- [ ] ViewModel初始化 < 100ms
- [ ] 列表加载 < 500ms (100条)
- [ ] 保存操作 < 300ms
- [ ] 内存无泄漏 (长时间运行)

---

## 6. 实施建议

### 6.1 立即执行 (Blocking RC)

1. **补充UserMasterDetailViewModel测试** (2-3小时)
   - 创建测试文件
   - 覆盖所有公共方法
   - 验证权限逻辑

2. **补充MedicalCaseCommandsViewModel测试** (2-3小时)
   - 状态机测试
   - 命令执行测试

3. **业务逻辑审查** (2小时)
   - 走查所有VM的Save/Delete逻辑
   - 确认错误处理完整性

### 6.2 短期优化 (Post-RC)

1. 提升整体测试覆盖率到85%
2. 统一验证框架
3. 性能基准测试自动化
4. 内存泄漏检测

---

## 7. 当前状态评估

**RC就绪度**: ~85%

**已完成**:
- ✅ Refit Client层完整测试
- ✅ 主要MasterDetailViewModels测试
- ✅ E2E工作流测试
- ✅ Controller错误处理修复

**待完成** (约8小时工作):
- ⚠️ UserMasterDetailViewModel测试 (3h)
- ⚠️ MedicalCaseCommandsViewModel测试 (3h)
- ⚠️ 权限边界测试完善 (2h)

**建议**: 完成上述3项后即可达到RC条件。
