# 2025-09-24 全局框架重构任务（Coder 专用）

- **发布日期**：2025-09-24
- **发布人**：Thinker（ChatGPT）

## 任务定位
结合 2025-09-25 架构深度分析与修改建议，本次重构聚焦于 **Server / Shared 保持稳定** 的前提下，对 `LYBT.All.sln` 中其余层（主要是 Desktop/WPF 与脚本工具）进行架构瘦身：删除冗余层次、统一模式、校准前后端契约，并清理编译障碍，使解决方案回归健康、易维护的基线。业务能力后续迭代再补。

## 架构约束
1. **Server/Shared 暂不改动**：除非遇到编译阻塞。若发现基础层问题，先记录并反馈，再由 Thinker 审批后更新范围。
2. **目标架构**：Client 侧统一为 “ViewModel → Service → ApiClient” 三层；紧耦合的 Module/Business/Query 三层一律合并。
3. **冗余清理优先级**：按照报告建议，先移除纯委托/重复层，再修复契约不一致，最后整理引用与目录。

## 核心目标
1. `dotnet build LYBT.All.sln -c Release --no-restore` 零错误、零 NU1504/NU1603 等包警告。
2. 客户端各模块不再出现 UltraThink 双层（Business/Query）或 Module 纯委托层，统一精简为单一 Service。
3. 前端 DTO/ViewModel 与后端契约一致；多余属性直接删除，如删除导致功能缺失，则暂时移除对应 UI/逻辑并记录待补。
4. `.sln`、项目文件、目录结构整洁：无 `_wpftmp`、无失效项目引用、无重复包引用。

## 工作拆解

### 步骤1：编译基线
- 执行 `dotnet build LYBT.All.sln -c Release --no-restore`，记录全部错误与警告。
- 建立即改即时测流程：每处理一类问题，立即重编验证。

### 步骤2：客户端分层瘦身
- 逐模块删除 Module 层及纯委托 Service，保留一个 Service 实现所有 API 调用与必要业务逻辑。
- 将 QueryService/BusinessService 合并；若逻辑重复，仅保留必要业务方法。
- 更新依赖注入与调用关系，确保 ViewModel 直接依赖统一 Service。

### 步骤3：契约对齐
- 对照 `src/Shared` 的 DTO/枚举，清理 ViewModel/Model/ApiClient 中不存在或命名不一致的字段。以后端为准，多余属性直接删除。
- 同步调整绑定、命令、验证逻辑；若属性删除导致界面功能不可用，则暂时移除相关 UI 或按钮，并记录“功能待补”清单。

### 步骤4：引用与项目清理
- 使用 `dotnet list <Project>.csproj package` 与 `ProjectReference` 检查，移除未使用或重复引用；若编译报缺包再补。
- 删除 IDE 生成的临时工程、无用脚本和空目录，统一项目命名与路径。
- 检查 `.sln` 是否仍引用已删除项目，保持解决方案清洁。

### 步骤5：回归验证
- 重跑 `dotnet build LYBT.All.sln -c Release --no-restore`，确保编译干净。
- 视情况运行关键单元测试项目；若仍存在编译失败的历史测试，记录阻塞原因与后续计划。

## 交付与总结
- 提交遵循 Conventional Commits（如 `refactor(desktop): collapse query/business service`）。
- 在 `docs/tasks/completed/2025-09-24-all-framework-refactor-task-summary.md` 中总结：
  - 架构瘦身改动点
  - 编译/测试结果
  - 删除/暂时下线的功能及建议
  - 后续风险或需 Thinker 决策事项

## 风险与提醒
- 前端属性删除、功能移除需记录，避免后续遗漏。（业务需求后期再补。）
- 若操作触及 Server/Shared 层，先暂停并反馈。
- 清理引用时注意合并多个模块共用的依赖，避免误删导致隐形编译失败。

> **最终目标**：在不破坏后端稳定基线的前提下，让客户端及工具层快速回到 **简洁统一的三层结构**，清除冗余与编译隐患，为后续业务迭代赢得干净战场。
