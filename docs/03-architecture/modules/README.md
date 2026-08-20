# 模块设计文档

> 本目录包含各业务模块的详细设计文档。每个模块文档描述其职责、实体关系、状态流转和 API 端点。

## 模块索引

| 模块 | 文档 | 复杂度 | 一句话说明 |
|------|------|--------|-----------|
| [认证授权](auth.md) | Auth | 4/10 | JWT 认证、Token 管理、角色授权 |
| [用户管理](users.md) | Users | 5/10 | 用户 CRUD、角色分配、密码管理 |
| [患者管理](patients.md) | Patients | 6/10 | 患者档案、就诊记录、数据导入导出 |
| [医案管理](medical-case.md) | MedicalCase | 8/10 | 核心聚合根：诊疗全流程（挂号→诊断→开方） |
| [挂号管理](registration.md) | Registration | 6/10 | 挂号、接诊、状态流转 |
| [药材管理](herbs.md) | Herbs | 中低 | 药材档案、分类、导入导出 |
| [验方管理](formulas.md) | Formulas | 3/5 | 验方模板、延迟绑定、验证工作流 |
| [打印模块](printing.md) | Printing | 6/10 | 处方模板、PDF 生成、打印队列 |
| [报表模块](reports.md) | Reports | 低 | 统计报表、数据汇总 |
| [平台壳程序](platform.md) | Platform | 高 | WPF Shell、Prism 模块加载、导航框架 |

## 与架构文档的关系

- **模块设计**（本目录）：单模块内部视角——实体、状态、端点、依赖
- **架构总览**（`../00-architecture-summary.md`）：全景技术栈与项目结构
- **数据模型**（`../04-data-model.md`）：跨模块实体关系与字段定义
- **业务流程**（`../11-business-flows.md`）：跨模块端到端流程（挂号→就诊→开方→打印）
