# Project Context

## Purpose
凌隐宝堂中医诊所管理系统（LYBTZYZS）- 为中医诊所提供完整的患者管理、诊疗记录、处方开具和药材管理功能。

## Tech Stack
- **后端**: .NET 8, ASP.NET Core Web API, Entity Framework Core
- **前端**: WPF (Windows Presentation Foundation), Prism Framework (MVVM)
- **数据库**: SQL Server
- **测试**: xUnit, NSubstitute
- **构建**: MSBuild, dotnet CLI

## Project Conventions

### Code Style
- C# 编码规范遵循 Microsoft 官方指南
- 命名规范: PascalCase (类/方法), camelCase (局部变量), _camelCase (私有字段)
- 所有代码注释使用**中文**
- 文件头部包含功能说明和关联 Issue/Epic 引用

### Architecture Patterns
- **后端**: 三层架构 (API/Service/Repository)
- **前端**: MVVM 模式 (Prism Framework)
- **DDD**: 聚合根设计，MedicalCase 为核心聚合根
- **依赖注入**: 通过 Prism IContainerRegistry 和 Microsoft.Extensions.DependencyInjection

### Testing Strategy
- 单元测试: xUnit + NSubstitute (Mock)
- 测试命名: `方法名_场景_期望结果`
- AAA 模式: Arrange-Act-Assert
- 测试覆盖: Repository/Service/ViewModel 层

### Git Workflow
- 主分支: master
- 提交格式: `type(scope): description #issue-number`
- 类型: feat/fix/docs/refactor/test/chore
- 提交尾部包含 Claude Code 标记

## Domain Context
- **医案(MedicalCase)**: 核心业务实体，包含患者诊疗的完整记录
- **诊断(Consultation)**: 仅指中医诊断部分（望闻问切、辨证）
- **处方(Prescription)**: 药材配伍和剂量
- **经验方(Formula)**: 可复用的处方模板
- **药材(Herb)**: 中药材库

## Important Constraints
- MVP 优先原则: 最小可行产品，避免过度设计
- 三层对齐: View/ViewModel/Service/Repository 命名保持一致
- 聚合根边界: MedicalCase 是唯一聚合根，其他实体通过它访问
- 中文界面: 所有用户界面使用简体中文

## External Dependencies
- SQL Server 数据库
- Windows 操作系统 (WPF 仅支持 Windows)
