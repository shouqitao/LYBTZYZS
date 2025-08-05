# 系统管理模块完成执行计划

## 一、总体目标
按照统一的开发模板，完成系统管理模块的所有功能，包括重构已有模块和开发新模块。

## 二、执行阶段

### 第一阶段：补充和重构部分实现的模块（3天）

#### 1.1 医生管理完善（1天）
**当前状态**：70%完成，对话框使用占位实现
**需要完成**：
- [ ] 重构 DoctorManagementViewModel 使用 BaseManagementViewModel
- [ ] 实现 EditDoctorDialog 和 EditDoctorDialogViewModel
- [ ] 实现 ViewDoctorDialog 和 ViewDoctorDialogViewModel
- [ ] 更新服务层接口以适配基类要求

#### 1.2 病历管理完成（1天）
**当前状态**：40%完成，只有列表功能
**需要完成**：
- [ ] 重构 RecordManagementViewModel 使用 BaseManagementViewModel
- [ ] 实现 AddRecordDialog 和 AddRecordDialogViewModel
- [ ] 实现 EditRecordDialog 和 EditRecordDialogViewModel
- [ ] 实现 ViewRecordDialog 和 ViewRecordDialogViewModel
- [ ] 添加病历模板支持

#### 1.3 中药材管理补充（0.5天）
**当前状态**：90%完成，缺少查看功能
**需要完成**：
- [ ] 实现 ViewHerbDialog 和 ViewHerbDialogViewModel
- [ ] 评估是否需要使用 BaseManagementViewModel 重构

### 第二阶段：开发核心业务模块（5天）

#### 2.1 角色权限管理（2天）
**当前状态**：20%框架实现
**需要完成**：
- [ ] 创建 RoleManagementViewModel（基于BaseManagementViewModel）
- [ ] 实现 RoleManagementView
- [ ] 实现 AddRoleDialog 和 AddRoleDialogViewModel
- [ ] 实现 EditRoleDialog 和 EditRoleDialogViewModel
- [ ] 实现权限分配界面 AssignPermissionsDialog
- [ ] 创建 IRoleService 接口和实现
- [ ] 集成权限验证到系统

#### 2.2 处方管理（3天）
**当前状态**：20%框架实现
**需要完成**：
- [ ] 创建 PrescriptionManagementViewModel（基于BaseManagementViewModel）
- [ ] 实现 PrescriptionManagementView
- [ ] 实现 AddPrescriptionDialog（处方开具界面）
- [ ] 实现 EditPrescriptionDialog 和 EditPrescriptionDialogViewModel
- [ ] 实现 ViewPrescriptionDialog（处方详情和打印预览）
- [ ] 实现处方模板选择功能
- [ ] 实现处方打印功能
- [ ] 创建 IPrescriptionService 接口和实现

### 第三阶段：开发辅助功能模块（4天）

#### 3.1 系统日志（1.5天）
**当前状态**：20%框架实现
**需要完成**：
- [ ] 创建 SystemLogsViewModel（只读模式，不需要继承BaseManagementViewModel）
- [ ] 实现 SystemLogsView（查询、筛选、导出）
- [ ] 实现日志详情查看 ViewLogDialog
- [ ] 实现日志统计分析功能
- [ ] 创建 ISystemLogService 接口和实现

#### 3.2 系统设置（1.5天）
**当前状态**：20%框架实现
**需要完成**：
- [ ] 创建 SystemSettingsViewModel
- [ ] 实现 SystemSettingsView（Tab页形式）
  - [ ] 基础设置（诊所信息、营业时间等）
  - [ ] 打印设置（打印机选择、模板设置）
  - [ ] 系统参数（缓存、性能等）
  - [ ] 接口配置（第三方接口）
- [ ] 创建 ISystemSettingsService 接口和实现

#### 3.3 数据备份（1天）
**当前状态**：20%框架实现
**需要完成**：
- [ ] 创建 BackupViewModel
- [ ] 实现 BackupView
- [ ] 实现手动备份功能
- [ ] 实现自动备份计划设置
- [ ] 实现备份还原功能
- [ ] 创建 IBackupService 接口和实现

### 第四阶段：评估和优化已完成模块（2天）

#### 4.1 评估是否重构（1天）
- [ ] 评估用户管理是否需要使用 BaseManagementViewModel
- [ ] 评估挂号管理是否需要使用 BaseManagementViewModel
- [ ] 评估验方模板管理是否需要使用 BaseManagementViewModel

#### 4.2 统一优化（1天）
- [ ] 统一所有模块的UI风格
- [ ] 优化性能（虚拟化、延迟加载等）
- [ ] 添加快捷键支持
- [ ] 完善权限控制

## 三、技术要点

### 3.1 使用基类的标准流程
```csharp
public class XxxManagementViewModel : BaseManagementViewModel<XxxInfo, IXxxService>
{
    protected override string ModuleName => "Xxx管理";
    
    // 实现三个抽象方法
    protected override Task<ServiceResult<PagedResult<XxxInfo>>> LoadDataFromServiceAsync(PaginationRequest request);
    protected override Task<ServiceResult<bool>> DeleteFromServiceAsync(XxxInfo item);
    protected override string GetItemDisplayName(XxxInfo item);
}
```

### 3.2 服务接口标准
```csharp
public interface IXxxService
{
    Task<ServiceResult<PagedResult<XxxInfo>>> GetPagedAsync(PaginationRequest request);
    Task<ServiceResult<XxxInfo>> GetByIdAsync(Guid id);
    Task<ServiceResult<XxxInfo>> CreateAsync(XxxCreateDto createDto);
    Task<ServiceResult<XxxInfo>> UpdateAsync(XxxUpdateDto updateDto);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
}
```

### 3.3 对话框标准
- 所有对话框使用统一的布局结构
- 必填字段标记红色星号
- 保存前进行验证
- 操作后给出反馈

## 四、风险和应对

### 4.1 可能的风险
1. **服务层接口不一致**：需要适配或重构服务层
2. **权限系统复杂**：需要仔细设计权限模型
3. **打印功能兼容性**：需要测试不同打印机

### 4.2 应对措施
1. 创建服务适配器层
2. 参考成熟的RBAC模型
3. 使用标准的打印接口

## 五、时间安排

- **第一阶段**：3天（医生1天 + 病历1天 + 中药材0.5天 + 测试0.5天）
- **第二阶段**：5天（角色权限2天 + 处方3天）
- **第三阶段**：4天（系统日志1.5天 + 系统设置1.5天 + 数据备份1天）
- **第四阶段**：2天（评估1天 + 优化1天）
- **总计**：14个工作日

## 六、验收标准

### 6.1 功能验收
- [ ] 所有模块的CRUD功能正常
- [ ] 搜索和分页功能正常
- [ ] 对话框操作流畅
- [ ] 数据验证完整

### 6.2 技术验收
- [ ] 代码符合模板规范
- [ ] 使用基类减少重复代码
- [ ] 服务接口统一
- [ ] 错误处理完善

### 6.3 用户体验验收
- [ ] 界面风格统一
- [ ] 操作流程一致
- [ ] 响应速度良好
- [ ] 错误提示友好

## 七、开始执行

按照以上计划，从第一阶段开始执行，首先完善医生管理模块。