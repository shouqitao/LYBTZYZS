# simplify-workspace-architecture 任务清单

## Phase 1: Item类实现接口 (1小时)

### 1.1 ConsultationItem实现接口
- [ ] 添加IDataProvider接口实现
- [ ] 添加IValidatable接口实现
- [ ] 添加ValidationMessage属性
- [ ] 确保ConsultationMapper.Instance可用

### 1.2 PrescriptionItem实现接口
- [ ] 添加IDataProvider接口实现
- [ ] 添加IValidatable接口实现
- [ ] 添加ValidationMessage属性
- [ ] 添加ValidationEnabled属性
- [ ] 确保PrescriptionMapper.Instance可用

### 1.3 删除DataProviderAdapters.cs
- [ ] 搜索所有适配器引用
- [ ] 更新ViewModel中的使用方式
- [ ] 删除DataProviderAdapters.cs文件

### 1.4 验证
- [ ] 编译通过
- [ ] Coordinator的Save/Complete方法正常工作

---

## Phase 2: 合并DataLoader到Coordinator (1小时)

### 2.1 扩展Coordinator
- [ ] 添加CachedMedicalCase属性
- [ ] 添加CachedConsultation属性
- [ ] 添加CachedPrescription属性
- [ ] 添加LoadMedicalCaseAsync方法
- [ ] 添加ClearCache方法

### 2.2 更新ViewModel引用
- [ ] 移除MedicalCaseDataLoader依赖注入
- [ ] 将_dataLoader调用改为_coordinator调用
- [ ] 更新构造函数

### 2.3 删除DataLoader
- [ ] 删除MedicalCaseDataLoader.cs
- [ ] 从DI注册中移除

### 2.4 验证
- [ ] 编译通过
- [ ] 医案加载功能正常

---

## Phase 3: 合并StatusDisplay到WorkspaceState (30分钟)

### 3.1 扩展WorkspaceState
- [ ] 添加ConsultationStatusText属性
- [ ] 添加ConsultationStatusColor属性
- [ ] 添加PrescriptionStatusText属性
- [ ] 添加PrescriptionStatusSummary属性
- [ ] 添加PrescriptionStatusColor属性
- [ ] 添加ShowPrescriptionStatus属性
- [ ] 添加UpdateConsultationStatus方法
- [ ] 添加UpdatePrescriptionStatus方法
- [ ] 扩展Reset方法

### 3.2 更新XAML绑定
- [ ] 搜索StatusDisplay绑定
- [ ] 改为State.xxx绑定

### 3.3 更新ViewModel
- [ ] 移除StatusDisplay属性
- [ ] 移除StatusDisplay初始化
- [ ] 将StatusDisplay调用改为State调用

### 3.4 删除StatusDisplay
- [ ] 删除WorkspaceStatusDisplay.cs

### 3.5 验证
- [ ] 编译通过
- [ ] XAML绑定正常
- [ ] 状态显示正确

---

## Phase 4: 待诊队列逻辑回归ViewModel (1.5小时)

### 4.1 在ViewModel中添加待诊队列逻辑
- [ ] 添加PendingCases属性
- [ ] 添加SelectedPendingCase属性
- [ ] 添加RefreshPendingQueueCommand
- [ ] 添加SelectPendingCaseCommand
- [ ] 实现RefreshPendingQueueAsync方法
- [ ] 实现SelectPendingCaseAsync方法
- [ ] 实现HandleSuspendedCaseAsync方法(如需要)

### 4.2 移除回调委托
- [ ] 移除对WorkspacePendingQueueHandler的依赖
- [ ] 移除相关回调设置代码

### 4.3 删除Handler
- [ ] 删除WorkspacePendingQueueHandler.cs
- [ ] 从DI注册中移除

### 4.4 验证
- [ ] 编译通过
- [ ] 待诊队列加载正常
- [ ] 患者切换正常

---

## Phase 5: 导航逻辑回归ViewModel (1小时)

### 5.1 在ViewModel中添加导航逻辑
- [ ] 添加NavigateBackCommand
- [ ] 实现NavigateBackAsync方法
- [ ] 实现HandleManagementLeaveAsync方法
- [ ] 实现HandleClinicalLeaveAsync方法
- [ ] 添加LeaveResult辅助类(如需要)

### 5.2 移除回调委托
- [ ] 移除对MedicalCaseNavigationHandler的依赖
- [ ] 移除相关回调设置代码

### 5.3 删除Handler
- [ ] 删除MedicalCaseNavigationHandler.cs
- [ ] 从DI注册中移除

### 5.4 验证
- [ ] 编译通过
- [ ] Clinical模式返回导航正常
- [ ] Management模式返回导航正常
- [ ] 离开确认对话框正常

---

## Phase 6: 处方导入简化 (30分钟)

### 6.1 创建扩展方法
- [ ] 创建Extensions目录(如不存在)
- [ ] 创建PrescriptionImportExtensions.cs
- [ ] 添加FormulaDetailDto.ToHerbItemDtos扩展方法
- [ ] 添加List<PrescriptionItemDto>.ToHerbItemDtos扩展方法

### 6.2 更新调用方
- [ ] 搜索PrescriptionImportHandler使用
- [ ] 改为扩展方法调用

### 6.3 删除Handler
- [ ] 删除PrescriptionImportHandler.cs
- [ ] 从DI注册中移除(如有)

### 6.4 验证
- [ ] 编译通过
- [ ] 验方导入功能正常
- [ ] 历史处方复制功能正常

---

## Phase 7: 最终清理和验证 (30分钟)

### 7.1 清理未使用代码
- [ ] 检查未使用的using语句
- [ ] 检查未使用的私有方法
- [ ] 检查空的region

### 7.2 更新DI注册
- [ ] 更新ServiceCollectionExtensions
- [ ] 移除已删除类的注册
- [ ] 确认保留类的注册正确

### 7.3 更新文档
- [ ] 更新MedicalCase模块CLAUDE.md
- [ ] 记录架构变更到Serena记忆

### 7.4 全量验证
- [ ] 全解决方案编译通过
- [ ] Clinical新建医案流程
- [ ] Clinical暂存/完成医案流程
- [ ] Clinical待诊队列切换
- [ ] Clinical返回导航
- [ ] Management查看医案
- [ ] Management编辑/保存
- [ ] 处方打印预览
- [ ] 验方导入

---

## 进度跟踪

| Phase | 状态 | 开始时间 | 完成时间 |
|-------|------|----------|----------|
| Phase 1 | pending | - | - |
| Phase 2 | pending | - | - |
| Phase 3 | pending | - | - |
| Phase 4 | pending | - | - |
| Phase 5 | pending | - | - |
| Phase 6 | pending | - | - |
| Phase 7 | pending | - | - |

---

## 风险检查点

### Phase 1后
- [ ] 确认Mapper.Instance模式可用
- [ ] 确认IDataProvider/IValidatable接口在正确位置

### Phase 4后
- [ ] 确认待诊队列Service注入正确
- [ ] 确认待诊队列UI绑定不受影响

### Phase 5后
- [ ] 确认IConfirmNavigationRequest实现不受影响
- [ ] 确认导航参数传递正确

### 最终
- [ ] 确认代码行数达到目标 (<1700行)
- [ ] 确认类数量达到目标 (5个)
