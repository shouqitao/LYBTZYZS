# implement-formula-copy-flow

## Summary
实现验方"复制为我的验方"功能，将复制按钮从列表操作移至详情页，采用类似"另存为"的用户体验流程。同时基于用户权限控制验方的编辑和复制能力。

## Motivation
当前验方复制功能在列表操作中以简单按钮形式存在，用户体验不佳。用户期望的流程是：
1. 在详情页查看验方内容
2. 点击"复制为我的验方"按钮
3. 打开编辑界面，预填充当前验方数据（名称加"(副本)"后缀）
4. 用户可修改内容后保存为自己的新验方

此外，需要根据验方所有权控制功能：
- **自己的验方**：可编辑 + 可复制（衍生新验方）
- **共享/他人验方**：仅查看 + 可复制

## Scope
- **Client层**: FormulaDetailViewModel, FormulaDetailView
- **Shared层**: 无变更（现有FormulaDto已有CreatedBy字段支持）
- **Server层**: 无变更（现有API授权规则已满足需求）

## Dependencies
- `api-authorization` spec - 已定义验方资源级权限控制规则
- `desktop-detail-views` spec - BaseDetailContainer容器模式

## Impact Assessment
- **风险等级**: 低
- **影响范围**: 仅客户端Formula模块
- **向后兼容**: 完全兼容，新增功能不影响现有行为

## Alternatives Considered
1. **在列表直接复制** - 用户无法预览和修改内容，体验差
2. **弹窗确认后复制** - 仍无法修改内容，不符合"另存为"预期

## Related Issues
- optimize-module-list-ui OpenSpec中移除了列表复制按钮，预留给此提案实现
