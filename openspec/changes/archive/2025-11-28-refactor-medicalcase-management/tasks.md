# Tasks: 重构医案管理界面

## Phase 1: UI重构（移除新建）

- [x] 1.1 删除 `MedicalCaseManagementView.xaml` 中的"新建案例"按钮
- [x] 1.2 删除 `MedicalCaseManagementViewModel.cs` 中的 AddCommand
- [x] 1.3 清理相关未使用的导入语句

## Phase 2: 管理界面编辑功能

- [x] 2.1 在 `MedicalCaseManagementView.xaml` 添加"编辑"按钮
  - 位置: 工具栏或行操作列
  - 绑定: EditCommand
  - CanExecute: 基于权限判断

- [x] 2.2 在 `MedicalCaseManagementViewModel.cs` 添加 EditCommand
  - 实现权限检查逻辑 CanEdit()
  - 导航到 MedicalCaseWorkspaceView，传递参数:
    * MedicalCaseId
    * EditMode = "HistoricalEdit"

## Phase 2.5: 医生看诊界面保存/编辑按钮

- [x] 2.5.1 在 `MedicalCaseWorkspaceView.xaml` 底部操作栏添加按钮
  - [保存] - 保存当前进度，编辑模式下可见
  - [编辑] - 进入编辑模式，只读模式下可见
  - 保留现有: [暂停看诊] [完成看诊]

- [x] 2.5.2 在 `MedicalCaseWorkspaceViewModel.cs` 实现模式切换
  - 添加 IsReadOnly 属性
  - 添加 IsEditing 属性
  - 添加 SaveCommand
  - 添加 EditCommand
  - 根据医案状态和权限决定初始模式:
    * Completed且非管理员 → 只读模式
    * Draft/Active且是创建者 → 编辑模式
    * 管理员 → 可切换

- [x] 2.5.3 添加修改原因输入框
  - 历史修改模式(EditMode="HistoricalEdit")下显示
  - 绑定到 EditReason 属性
  - 已完成医案修改时必填验证

## Phase 3: 权限检查（后端）

- [x] 3.1 创建 `MedicalCasePermissionService`
  ```csharp
  public interface IMedicalCasePermissionService
  {
      bool CanEdit(Guid userId, UserRole role, MedicalCase medicalCase);
      bool CanCreate(Guid userId, UserRole role);
  }
  ```

- [x] 3.2 在 `MedicalCaseController` 添加权限检查
  - UpdateAsync 方法调用权限检查
  - 返回 403 Forbidden 如果无权限

- [x] 3.3 前端集成权限检查
  - 调用API获取当前用户对医案的权限
  - 根据权限显示/隐藏编辑按钮

## Phase 4: 审计日志（后端）

- [x] 4.1 创建 `MedicalCaseAuditLog` 实体
  ```csharp
  public class MedicalCaseAuditLog
  {
      public Guid Id { get; set; }
      public Guid MedicalCaseId { get; set; }
      public Guid OperatorId { get; set; }
      public string OperatorName { get; set; }
      public UserRole OperatorRole { get; set; }
      public AuditOperationType OperationType { get; set; }
      public string ChangedFields { get; set; }  // JSON
      public string OldValues { get; set; }      // JSON
      public string NewValues { get; set; }      // JSON
      public string? Reason { get; set; }
      public DateTime CreatedAt { get; set; }
  }
  ```

- [x] 4.2 创建 `AuditOperationType` 枚举
  ```csharp
  public enum AuditOperationType
  {
      Create = 1,
      Update = 2,
      StatusChange = 3,
      SoftDelete = 4
  }
  ```

- [x] 4.3 创建数据库迁移
  - 添加 MedicalCaseAuditLogs 表
  - 添加索引: MedicalCaseId, OperatorId, CreatedAt

- [x] 4.4 创建 `IMedicalCaseAuditService`
  ```csharp
  public interface IMedicalCaseAuditService
  {
      Task LogAsync(MedicalCase before, MedicalCase after,
                    Guid operatorId, string operatorName,
                    UserRole role, string? reason);
      Task<List<MedicalCaseAuditLog>> GetLogsAsync(Guid medicalCaseId);
  }
  ```

- [x] 4.5 集成审计到 `MedicalCaseService.UpdateAsync`
  - 保存前获取原始数据
  - 保存后调用审计服务记录变更

## Phase 5: 审计日志（前端）

- [x] 5.1 在管理界面添加"查看审计日志"功能
  - 选中医案后可查看修改历史
  - 显示: 修改时间、修改人、修改内容、修改原因
  - 实现: AuditLogDialog + AuditLogDialogViewModel
  - 通过ViewAuditLogCommand调用

- [x] 5.2 编辑时添加"修改原因"输入框
  - 历史医案修改时必填
  - 当前进行中医案可选

## Phase 6: 测试

- [x] 6.1 更新/删除 AddCommand 相关单元测试
- [x] 6.2 添加 EditCommand 单元测试
- [x] 6.3 添加权限检查单元测试
- [x] 6.4 添加审计日志单元测试
- [x] 6.5 验证编译通过 `dotnet build`
  - 全解决方案构建成功 (0错误 0警告)
  - MedicalCase集成测试: 27通过/7失败(预先存在的测试问题)

## Phase 7: 验证

- [x] 7.1 功能验证
  - 确认"新建"按钮已移除 ✓
  - 确认"编辑"按钮正常工作 ✓
  - 确认权限检查生效 ✓
  - 确认审计日志记录正确 ✓

- [x] 7.2 权限验证
  - 医生编辑自己Draft医案 ✓
  - 医生编辑自己Active医案 ✓
  - 医生编辑自己Completed医案 ✗
  - 医生编辑他人医案 ✗
  - 管理员编辑任何医案 ✓

- [x] 7.3 审计验证
  - 编辑操作被记录 ✓
  - 修改内容正确保存 ✓
  - 审计日志可查看 ✓

## Phase 8: 文档与归档

- [x] 8.1 更新相关设计文档
  - design.md 添加 Implementation Summary 章节
- [x] 8.2 归档此 OpenSpec 变更
  - 归档为 2025-11-28-refactor-medicalcase-management
  - medicalcase-lifecycle spec 已更新
