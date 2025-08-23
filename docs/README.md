# 凌隐宝堂中医诊所系统 - 文档中心

> **最新状态**: ✅ UltraThink v2.0 全项目重构完成 | ✅ 0错误 0警告 | ✅ 生产就绪

## 📖 核心文档

### 主要指导文档
- **[CLAUDE.md](../CLAUDE.md)** - 🎯 **主要项目指导文档** (开发规范、架构说明、模块详情)
- **[功能清单](ultrathink/comprehensive-project-functionality-catalog-20250823.md)** - 完整技术架构与功能清单

### 架构设计
- **[系统架构分析](architecture/system-architecture-analysis-20250818.md)** - UltraThink架构分析
- **[API响应标准](architecture/ultrathink-api-response-standards-20250817.md)** - API设计规范
- **[控制器设计模式](architecture/ultrathink-controller-design-patterns-20250817.md)** - 控制器架构

### 开发指南
- **[开发标准v2](development/DEVELOPMENT_STANDARDS_V2.md)** - 开发规范和最佳实践
- **[文件组织规范](development/FILE_ORGANIZATION.md)** - 文件命名和目录结构
- **[快速开始](guides/quick-start.md)** - 项目快速启动指南

### 最新重构报告
- **[UltraThink精细化优化完成](ultrathink/whole-project-architecture-refactoring-complete-20250823.md)** - 最新重构总结
- **[前后端统一架构](ultrathink/frontend-backend-unified-architecture-20250821.md)** - 架构统一完成

## 🧱 8个核心业务模块

| 模块 | 功能描述 | 状态 |
|-----|---------|------|
| **Auth** | JWT认证 + RBAC权限 | ✅ 完成 |
| **Users** | 用户管理 (医生/管理员) | ✅ 完成 |
| **Patients** | 患者档案管理 | ✅ 完成 |
| **MedicalCase** | 诊疗流程管理容器 | ✅ 完成 |
| **Consultation** | 中医四诊记录 | ✅ 完成 |
| **Prescriptions** | 智能处方管理 | ✅ 完成 |
| **Herbs** | 中药材信息管理 | ✅ 完成 |
| **Formula** | 验方模板管理 | ✅ 完成 |

## 🏗️ 技术架构概览

- **后端**: .NET 8 + ASP.NET Core Web API + EF Core 8.0.17
- **前端**: WPF + Prism.DryIoc 9.0.537 + Refit
- **数据库**: SQL Server + 统一AppDbContext
- **缓存**: IMemoryCache智能缓存系统
- **认证**: JWT Bearer Token + RBAC
- **监控**: 8个健康检查端点

## 📊 项目质量状态

**编译状态**: ✅ 0错误 0警告  
**测试覆盖**: 🔄 2.76% → 60%目标 (Repository层97个测试完成)  
**架构标准**: ✅ UltraThink三层模块化完全实施  
**生产就绪**: ✅ Ready for Deployment  

## 🎯 核心诊疗流程

```
患者档案 → 创建医案 → 中医四诊 → [可选]处方开具 → 完成诊疗
Patients → MedicalCase → Consultation → Prescriptions → Complete
```

- **1:1关系**: 一个医案对应一次诊断 (MedicalCase ↔ Consultation)
- **无复诊概念**: 每次就诊创建新医案，通过PatientId关联历史
- **v1.0范围**: 诊断+处方核心功能，挂号收费模块v2.0计划

## 📁 文档组织结构

```
docs/
├── architecture/          # 架构设计文档
├── development/           # 开发指南和规范
├── guides/               # 用户和开发指南
├── ultrathink/           # UltraThink方法论文档
├── testing/              # 测试相关文档
├── reports/              # 项目报告 (仅保留最新)
└── README.md            # 本文档
```

## 🚀 快速开始

1. **开发环境**: 参考 [CLAUDE.md](../CLAUDE.md) 环境配置部分
2. **启动系统**: `scripts\start-dev.bat` 
3. **API文档**: https://localhost:7001/swagger
4. **默认登录**: sysadmin / Admin@123456

---

> 📌 **重要提醒**: 所有新功能开发请严格遵循 [CLAUDE.md](../CLAUDE.md) 中的开发规范和架构约定