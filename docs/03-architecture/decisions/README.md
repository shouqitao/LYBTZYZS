# 架构决策记录 (ADR)

> 所有架构决策按编号排列。ADR-0001 ~ ADR-0027。

- ADR-0026: 授权矩阵 SSOT 收敛（产品层 04-permissions.md 为唯一权威，架构层 12-permissions-matrix.md 仅视图）
- ADR-0027: 删除语义与批量归一（四态：SoftDelete/Restore/HardDelete/BatchSoftDelete + SetStatus/BatchSetStatus）
- ADR-SharedHost: 统一宿主抽象（SharedHost 使 Server/LocalWebAPI 为薄包装，消除双轨漂移，方案 D）
