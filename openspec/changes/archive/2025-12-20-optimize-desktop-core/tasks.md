# Tasks: optimize-desktop-core

## Status: Draft

## Phase 1: P0级消除重复 (0/12)

### 1.1 异常处理统一 (0/4)

- [ ] 1.1.1 在Shared.ExceptionHandling创建IDesktopExceptionHandler接口
- [ ] 1.1.2 创建DesktopExceptionHandler实现（适配WPF环境）
- [ ] 1.1.3 删除Infrastructure/Services/ErrorHandling/目录
- [ ] 1.1.4 删除Presentation/Notifications/UnifiedErrorHandlingService.cs

### 1.2 Token管理统一 (0/4)

- [ ] 1.2.1 删除Infrastructure/Interfaces/ITokenManager.cs
- [ ] 1.2.2 更新所有ITokenManager引用改用ITokenLifecycleService
- [ ] 1.2.3 简化ISessionManager移除Token相关属性
- [ ] 1.2.4 更新SessionManager实现

### 1.3 映射器统一 (0/4)

- [ ] 1.3.1 删除Models/Mapping/MappingService.cs
- [ ] 1.3.2 删除Models/Mapping/IMappingService.cs
- [ ] 1.3.3 更新所有MappingService引用改用SimpleMapper
- [ ] 1.3.4 验证编译通过

---

## Phase 2: P1级澄清职责 (0/12)

### 2.1 会话管理职责划分 (0/4)

- [ ] 2.1.1 删除Infrastructure/Interfaces/IUserSessionManager.cs（合并到ISessionManager）
- [ ] 2.1.2 更新ISessionManager定义（仅保留会话状态）
- [ ] 2.1.3 更新SessionManager实现
- [ ] 2.1.4 更新所有引用点

### 2.2 ViewModel基类简化 (0/4)

- [ ] 2.2.1 创建新的简化版ViewModelBase（~150行）
- [ ] 2.2.2 创建ListViewModelBase<T>
- [ ] 2.2.3 创建DetailViewModelBase
- [ ] 2.2.4 迁移HTTP状态码处理到ApiExceptionHandler

### 2.3 接口位置调整 (0/4)

- [ ] 2.3.1 将IUserNotificationService移至Presentation
- [ ] 2.3.2 将ILoginCoordinator移至Foundation
- [ ] 2.3.3 更新模块注册
- [ ] 2.3.4 验证编译通过

---

## Phase 3: P2级组织优化 (0/8)

### 3.1 控件分离 (0/4)

- [ ] 3.1.1 创建Infrastructure/Controls/Common/目录
- [ ] 3.1.2 移动通用控件到Common目录
- [ ] 3.1.3 识别并迁移业务控件到对应模块
- [ ] 3.1.4 更新XAML命名空间引用

### 3.2 Item模型命名规范 (0/4)

- [ ] 3.2.1 审计现有Item模型命名
- [ ] 3.2.2 创建Item模型命名规范文档
- [ ] 3.2.3 重命名不符合规范的模型
- [ ] 3.2.4 更新所有引用

---

## Phase 4: 验证与文档 (0/4)

- [ ] 4.1 全解决方案编译验证
- [ ] 4.2 运行单元测试
- [ ] 4.3 更新DESKTOP_ARCHITECTURE_STANDARD.md
- [ ] 4.4 更新client-layer-architecture spec

---

## Summary

| Phase | 任务数 | 完成数 | 状态 |
|-------|--------|--------|------|
| Phase 1: P0级消除重复 | 12 | 0 | Pending |
| Phase 2: P1级澄清职责 | 12 | 0 | Pending |
| Phase 3: P2级组织优化 | 8 | 0 | Pending |
| Phase 4: 验证与文档 | 4 | 0 | Pending |
| **Total** | **36** | **0** | **0%** |

---

## Dependencies

- Phase 2依赖Phase 1完成
- Phase 3可与Phase 2并行
- Phase 4依赖所有Phase完成

## Notes

- 每个子Phase完成后验证编译
- 保持git提交粒度适中（每个子Phase一个提交）
- 如遇阻塞问题及时记录
