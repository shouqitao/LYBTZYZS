# 架构文档归档

本目录存档已被新版ARCHITECTURE.md取代的旧架构文档。

## 归档原则

根据文档SSOT原则，这些文档已不再作为权威来源，仅供历史参考。

**当前权威文档**: `docs/ARCHITECTURE.md`

---

## 归档文档列表

### Server端架构文档

**server-module-design-standard-v1.4.md** (34KB)
- **原路径**: `docs/architecture/server-module-design-standard.md`
- **归档日期**: 2025-10-12
- **最终版本**: v1.4
- **归档原因**: 已整合到 `docs/ARCHITECTURE.md` Part II
- **核心内容**:
  - 三层架构标准（Controller → Service → Repository）
  - 禁止CQRS模式
  - Service接口统一设计（6-12方法、ISP/SRP/YAGNI）
  - Repository层设计
  - DTO设计规范
  - 服务注册模式（AutoMapper/Validator集中注册）
  - 迁移指南与FAQ

### Desktop端架构文档

**unified-design-standard-v2.4.md** (45KB)
- **原路径**: `docs/architecture/client/unified-design-standard.md`
- **归档日期**: 2025-10-12
- **最终版本**: v2.4
- **归档原因**: 已整合到 `docs/ARCHITECTURE.md` Part III
- **核心内容**:
  - 模块化架构v2.0（ViewModel → Repository → WebAPI）
  - 移除Service层（ADR-002）
  - Repository接口位置标准v2.2（对齐Server端）
  - ViewModel设计标准（构造函数依赖、命令命名、属性命名）
  - Repository返回裸类型v2.1
  - 组件化架构v2.4（复杂度阈值、共享组件基类）
  - View层设计（XAML模板、Code-behind规则）
  - 迁移指南（Service层→Repository层）

---

## 版本整合映射

| 旧文档 | 新文档位置 | Part | 说明 |
|-------|----------|------|------|
| `server-module-design-standard.md` v1.4 | `ARCHITECTURE.md` Part II | Server端架构 | 三层架构、Service接口、Repository层 |
| `unified-design-standard.md` v2.4 | `ARCHITECTURE.md` Part III | Desktop端架构 | MVVM、模块化、Repository层、组件化 |
| 两份文档的ADR内容 | `ARCHITECTURE.md` Part V | 架构决策记录 | ADR-001~004 |
| 两份文档的Changelog | `ARCHITECTURE.md` Part VI | 架构演进 | v1.0→v2.4完整历史 |

---

## 历史参考价值

这些归档文档保留用于：
- **历史追溯**: 了解架构演进过程
- **决策背景**: 查阅某个架构决策的原始上下文
- **迁移参考**: 类似项目的迁移指南示例

---

## 变更历史

| 日期 | 操作 | 说明 |
|------|------|------|
| 2025-10-12 | 归档 | Server v1.4 和 Desktop v2.4 文档归档，创建统一ARCHITECTURE.md |

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
