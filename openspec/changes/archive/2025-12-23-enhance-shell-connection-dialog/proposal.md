# OpenSpec Proposal: enhance-shell-connection-dialog

## Summary

完善Shell启动时API连接失败后的对话机制，提供用户友好的重试交互，并为v2.0本地模式预留统一入口。

## Background

当前系统在API健康检查失败时的处理存在以下问题：

1. **无法重试连接**: `ApiHealthCheckStartupStep`失败后直接显示MessageBox并退出应用，用户无法重试
2. **用户体验差**: 使用系统原生MessageBox，信息冗长，缺乏友好引导
3. **无恢复机制**: 一旦失败必须重启应用程序才能重试
4. **缺乏扩展性**: 未考虑v2.0本地模式的入口设计

### Current Behavior

```
App.OnInitialized()
  └─ InitializeApplicationAsync()
      └─ StartupPipeline.ExecuteAsync()
          └─ ApiHealthCheckStartupStep (Order=40)
              └─ 失败时返回 StartupStepResult.Failed()
                  └─ HandleInitializationFailureAsync()
                      └─ MessageBox.Show() + Shutdown(1)
```

用户只能选择查看日志或关闭应用，无法重试连接。

## Scope

### In Scope

1. 创建专用的API连接失败对话框(`ApiConnectionFailedDialog`)
2. 实现交互式重试机制(用户手动触发)
3. 设计简洁友好的错误信息展示(可展开详情)
4. 预留v2.0本地模式入口(当前禁用)
5. 更新启动管道的错误处理流程

### Out of Scope

- v2.0本地模式的完整实现
- 自动重试机制(Exponential Backoff)
- 网络监控和自动恢复
- 其他启动步骤的对话框处理

## Design Highlights

### 对话框设计

```
┌─────────────────────────────────────────────────┐
│  ⚠ 无法连接到服务器                              │
├─────────────────────────────────────────────────┤
│                                                 │
│  无法连接到凌隐宝堂服务，请检查：                  │
│                                                 │
│  • WebAPI服务是否已启动                          │
│  • 网络连接是否正常                              │
│  • 防火墙是否阻止连接                            │
│                                                 │
│  ▶ 展开详情                                     │
│  ┌─────────────────────────────────────────┐   │
│  │ 服务地址: http://localhost:5001         │   │
│  │ 错误类型: HttpRequestException          │   │
│  │ 详细信息: Connection refused            │   │
│  └─────────────────────────────────────────┘   │
│                                                 │
├─────────────────────────────────────────────────┤
│  [离线模式(v2.0)]  [查看日志]  [重试]  [退出]     │
│   (禁用)                                        │
└─────────────────────────────────────────────────┘
```

### 状态流转

```
ApiHealthCheckStartupStep失败
         │
         ▼
  显示ApiConnectionFailedDialog
         │
    ┌────┴────┬────────┬────────┐
    ▼         ▼        ▼        ▼
  [重试]   [离线]   [日志]   [退出]
    │         │        │        │
    ▼         ▼        ▼        ▼
 重新执行  (v2.0)   打开日志   Shutdown
 健康检查  预留入口   文件夹
```

## Success Criteria

1. API连接失败时显示专用对话框而非系统MessageBox
2. 用户可通过[重试]按钮触发重新连接
3. 错误信息简洁友好，技术详情可展开查看
4. [离线模式]按钮已预留，v1.0禁用状态
5. 不影响正常启动流程的性能

## Related Specs

- `shell-layout` - Shell布局规范
- `dialog-patterns` - 对话框模式规范
- `error-handling` - 错误处理规范
- `login-state-machine` - 登录状态机

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| 对话框阻塞主线程 | 界面卡顿 | 使用异步显示，保持UI响应 |
| 重试循环无限制 | 资源占用 | 记录重试次数，可选添加提示 |
| v2.0入口误触 | 用户困惑 | 按钮禁用+ToolTip说明 |

## Dependencies

- Prism IDialogService
- 现有ConfirmationDialog模式
- StartupPipeline基础设施
