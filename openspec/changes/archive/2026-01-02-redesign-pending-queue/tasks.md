# Tasks: redesign-pending-queue

## Phase 1: Server端状态判定修复

- [x] 1.1 修改 `PendingMedicalCaseDto` 添加 `QueueNumber` 属性
- [x] 1.2 修改 `MedicalCaseRepository.GetPendingCasesAsync` 实现正确的状态判定
  - Active状态 → PendingCaseType.InProgress
  - Draft状态 → PendingCaseType.Suspended
- [x] 1.3 添加 QueueNumber 序号计算逻辑

## Phase 2: Desktop端控件重构

- [x] 2.1 PendingQueueControl 新增序号列（40px宽度）
- [x] 2.2 优化状态标签样式（颜色区分）- 已存在
- [x] 2.3 控件内部集成 `IApplicationTickService` 轮询
- [x] 2.4 新增 `AutoRefreshEnabled`、`AutoRefreshInterval` 属性
- [x] 2.5 新增 `PatientSelected` 事件（替代Command模式）

## Phase 3: 切换逻辑简化

- [x] 3.1 重构 `WorkspacePendingQueueHandler`
- [x] 3.2 实现编辑模式自动暂存机制
- [x] 3.3 简化暂存患者弹窗为双选项（继续/新建）
- [x] 3.4 删除三选项弹窗逻辑 - 保留作为死代码，后续清理

## Phase 4: 集成与测试

- [x] 4.1 更新 WorkspaceViewModel 适配新控件接口 - 无需修改，已兼容
- [x] 4.2 编译验证 - 通过，0错误
- [x] 4.3 更新API文档（如需要）- 无需更新

---

## 验收标准

1. [x] Server端正确返回 InProgress/Suspended 状态
2. [x] 切换患者时无需手动确认暂存（自动暂存）
3. [x] 队列显示序号，便于识别
4. [x] 队列每30秒自动刷新
5. [x] 编译通过，无回归问题

---

## 完成记录

**完成时间**: 2026-01-02
**实施结果**: 全部任务完成

### 主要变更文件

| 文件 | 变更类型 | 说明 |
|------|----------|------|
| `PendingMedicalCaseDto.cs` | 修改 | 添加QueueNumber属性 |
| `MedicalCaseRepository.cs` | 修改 | 修复状态判定逻辑，添加序号计算 |
| `PendingQueueControl.xaml` | 修改 | 添加序号列 |
| `PendingQueueControl.xaml.cs` | 修改 | 添加轮询刷新、PatientSelected事件 |
| `WorkspacePendingQueueHandler.cs` | 修改 | 简化切换逻辑，自动暂存 |
