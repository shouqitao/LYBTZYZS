---
type: concept
title: 同步冲突处理
tags: [sync, data-consistency, distributed-systems]
related: [sync-module, sync-dependency-chain]
created: 2026-06-10
updated: 2026-06-10
sources: ["docs/04-api-reference/sync.md"]
---
# 同步冲突处理

**同步冲突处理 (Sync Conflict Resolution)** 是指在凌隐宝堂系统的双向同步过程中，当同一实体在本地（SQL Server LocalDB）和服务端（SQL Server）同时发生变更且内容不一致时，系统识别、报告并解决这些冲突的机制。

## 冲突检测

冲突检测主要通过 `POST /sync/compare` 端点实现。客户端发送本地实体的元数据列表（包含 `EntityId`, `Checksum`, `UpdatedAt`），服务端将其与自己的元数据进行比对。

**比对逻辑**:
1.  **仅本地存在**: 标记为 `toUpload`。
2.  **仅服务端存在**: 标记为 `toDownload`。
3.  **两端都存在且 Checksum 不同**:
    *   若 `UpdatedAt` 差异显著或业务规则判定为并发修改，标记为 `conflicted`。
    *   当前实现主要依赖 Checksum 不一致来识别潜在冲突。

**响应示例**:
```json
{
  "data": {
    "conflicted": ["guid-12345"],
    "summary": {
      "conflicted": 1
    }
  }
}
```

## 冲突类型

1.  **数据内容冲突**: 同一字段在两端被修改为不同值（例如：患者手机号在本地修改，同时在服务端被管理员修改）。
2.  **状态冲突**:
    *   **ERR-70303 (SyncActiveCaseConflict)**: 本地试图上传一个医案，但服务端该患者已有一个“活跃”状态的医案。这违反了业务规则（通常一个患者同一时间只能有一个活跃医案）。
    *   **ERR-70304 (SyncCaseLocked)**: 本地试图更新一个已在服务端被锁定（如已打印或归档）的医案。

## 处理策略

目前系统采用**“检测-报告-人工/策略干预”**的模式：

1.  **自动跳过/保留**: 对于非关键冲突，系统可能默认保留服务端版本（Last Write Wins 的变体），或在 `compare` 阶段仅报告而不自动合并。
2.  **客户端干预**:
    *   当 `compare` 返回 `conflicted` 列表时，Desktop 客户端应暂停自动同步流程。
    *   UI 应展示冲突详情，允许用户选择“保留本地”、“保留服务端”或“手动合并”。
    *   对于 **ERR-70303** 等业务规则冲突，必须阻止上传，并要求用户先在本地完成或取消活跃医案。
3.  **错误码反馈**:
    *   **ERR-70103 (SyncDataConflict)**: 在上传阶段，若服务端检测到不可接受的覆盖（如 `OverwriteConflicts=false`），则返回此错误。

## 局限性与发展

*   **黑盒现状**: 当前 API 文档指出 `compare` 返回冲突列表，但未详细规定默认的自动合并算法。这意味着冲突处理逻辑主要集中在客户端 `SyncViewModel` 中。
*   **未来优化**: 可能需要引入更细粒度的字段级比对（Field-level Diff）而非整行 Checksum 比对，以支持自动合并非冲突字段。

## 相关概念

*   [[sync-module]]: 同步模块概览。
*   [[medical-case-sync-strategy]]: 医案特有的状态冲突规则。
*   [[checksum-comparison-strategy]]: 基于校验和的差异检测机制。