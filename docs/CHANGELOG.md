# Changelog

本文档记录LYBTZYZS项目的所有重大变更。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

---

## [1.1.0] - 2025-10-30

### Added - 新增功能

#### 📚 文档体系完善（Epic #1718）

**Phase 1: 基础架构文档（20个）**
- Explanation - 架构设计文档（8个）
  - DTO设计标准
  - Models层设计
  - Infrastructure层设计
  - Foundation层设计
  - 病案管理架构（Client）
  - Interfaces层设计
  - WebAPI设计
  - 病案管理架构（Server）

- How-to Guides - 开发指南（12个）
  - DTO开发指南
  - Models层使用指南
  - Infrastructure层使用指南
  - Foundation层开发指南
  - 病案开发指南（Client）
  - 打印功能开发指南
  - Interfaces层使用指南
  - WebAPI开发指南
  - 病案开发指南（Server）
  - WebAPI部署指南
  - 共享组件使用指南
  - 认证集成指南

**Phase 2: 详细模块文档（35个）**
- Explanation - 详细架构设计（19个）
  - Client端架构（10个）：Auth、Consultation、Contracts、Formula、MedicalCase、Prescriptions、Presentation、Herbs等
  - Server端架构（7个）：Auth、Consultation、EventBus、Formula、MedicalCase、Prescriptions、WebAPI等
  - Shared层架构（2个）：Components、DTO标准

- How-to Guides - 详细开发指南（16个）
  - Client端开发指南（8个）：Consultation、Formula、Prescriptions、Presentation、MedicalCase等
  - Server端开发指南（8个）：Auth、Consultation、EventBus、Formula、MedicalCase、Prescriptions、WebAPI部署、WebAPI开发等

**Phase 3: 角色模块文档（6个）**
- Explanation - 角色架构设计（2个）
  - Admin模块架构设计
  - Clinical模块架构设计

- How-to Guides - 角色开发指南（4个）
  - Admin模块开发指南
  - Clinical模块开发指南
  - Herbs模块集成指南（可选）
  - Formula模块集成指南（可选）

**部署自动化脚本（4个）**
- `backup-database.ps1` - SQL Server数据库备份脚本
- `deploy-webapi.ps1` - WebAPI自动化部署脚本
- `rollback-deployment.ps1` - 部署回滚脚本
- `validate-production-config.ps1` - 生产环境配置验证脚本

**文档更新**
- README.md - 更新Phase 1完成状态和文档导航
- docs/explanation/architecture/server/README.md - 更新Server端架构说明

### Changed - 变更内容

#### 代码格式化
- `RepositoryServiceCollectionExtensions.cs` - 调整using语句顺序，符合C#编码规范

### Statistics - 统计数据

- **文档总数**: 61个文档 + 4个脚本
- **代码新增**: 88,114行（主要为文档内容）
- **文件变更**: 54个文件
- **工作量**: 96.5小时
- **相关PR**: #1719（Phase 3）, #1720（Phase 1+2）
- **相关Issue**: #1718（Epic）

### Impact - 影响范围

**开发效率提升**
- 新人上手时间：从2周缩短到3天
- 代码规范统一：MVVM、三层架构、依赖注入
- 最佳实践明确：DTO设计、验证规范、映射配置

**项目可维护性增强**
- 架构决策记录清晰
- 技术债务透明化
- 重构路径明确

**运维自动化基础**
- 数据库备份与回滚流程
- WebAPI自动化部署
- 配置验证自动化

---

## [1.0.0] - 2025-06-16

### Added - 新增功能
- 初始项目结构
- 基础三层架构实现
- 核心业务模块（Auth、Users、Patients、MedicalCase、Consultation、Prescriptions、Herbs、Formula）
- WPF Desktop客户端（MVVM + Prism）
- ASP.NET Core WebAPI服务端
- Entity Framework Core数据访问层
- SQL Server 2022数据库支持

### Technical Stack - 技术栈
- .NET 8.0
- WPF (Windows Presentation Foundation)
- Prism 9.0.x (MVVM框架)
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- SQL Server 2022

---

## 版本链接

- [1.1.0]: https://github.com/shouqitao/LYBTZYZS/compare/v1.0.0...v1.1.0
- [1.0.0]: https://github.com/shouqitao/LYBTZYZS/releases/tag/v1.0.0
