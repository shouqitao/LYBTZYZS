# STD-01: CQRS 边界规范

## 适用范围

MedicalCase 模块及未来可能引入 CQRS 的模块。

## 规范内容

### 当前 CQRS 模块

MedicalCase 模块因业务复杂度高 (聚合根 + 多子实体 + 状态机 + 打印保护 + 审计)，采用 CQRS 拆分:

| Service | 职责 | 示例方法 |
|---------|------|----------|
| `IMedicalCaseCommandService` | 写操作 (创建/保存/删除) | `SaveAsync`, `DeleteAsync` |
| `IMedicalCaseQueryService` | 读操作 (查询/列表/搜索) | `GetByIdAsync`, `GetListDtoAsync` |
| `IMedicalCaseStateService` | 状态流转 (完成/挂起/取消) | `CompleteAsync`, `SuspendAsync` |

### 其他模块

Patient/User/Herb/Formula 等模块使用单一 `I{Entity}Service`，不拆分 CQRS。

## 边界规则

1. **仅 MedicalCase 因复杂度需 CQRS**: 新增模块默认使用单一 Service，除非业务复杂度明确需要拆分
2. **CQRS 引入门槛**: 满足以下至少 2 项才考虑引入 CQRS:
   - 聚合根包含 2+ 子实体
   - 状态机流转超过 3 个状态
   - 读写比例严重不均 (读 >> 写)
   - Service 方法超过 15 个
3. **Service 方法不超过 50 行**: 超过时提取私有方法或拆分为更细粒度的 Service
4. **禁止跨 Service 类型调用**: CommandService 不调用 QueryService，反之亦然；需要共享逻辑时提取到 Repository 层

## 参考

- 设计模式速查: `docs/05-development/patterns.md` CQRS 分离章节
- MedicalCase PRD: `docs/02-requirements/medical-cases.md`

---

创建日期: 2026-02-26
