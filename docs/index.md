# 📚 LYBTZYZS 文档中心

> 凌隐宝堂中医诊所诊疗系统 - 技术文档导航
> 更新时间：2025-09-21

## 🗺️ 快速导航

### 📋 项目概述
- [系统介绍](../README.md)
- [项目状态](../README.md#项目状态)
- [核心特性](../README.md#核心特性)
- [技术栈](architecture/tech-stack.md)

### 🏗️ 架构文档
- [系统架构设计](architecture/)
- [UltraThink架构](ultrathink/)
- [数据库设计](database/)
- [API设计规范](api/)

### 📦 Shared层规范
- **[类型清单](shared-inventory/shared-types.md)** - 268+类型完整清单
- **[依赖关系](shared-inventory/shared-deps.md)** - 模块依赖关系图
- **[枚举规范](shared-inventory/shared-enums-spec.md)** - 枚举标准与i18n
- **[结构优化](shared-inventory/shared-structure-proposal.md)** - 目录重构方案
- **[架构门禁](shared-inventory/shared-arch-gates.md)** - 依赖边界规范

### 💻 开发指南
- [环境配置](development/setup.md)
- [编码规范](development/coding-standards.md)
- [Git工作流](development/git-workflow.md)
- [贡献指南](development/CONTRIBUTING.md)

### 🧪 测试文档
- [测试策略](testing/test-strategy.md)
- [测试指南](testing/test-guidelines.md)
- [覆盖率报告](reports/)

### 📦 部署运维
- [部署指南](deployment/)
- [配置说明](deployment/configuration.md)
- [运维手册](deployment/operations.md)
- [故障排查](deployment/troubleshooting.md)

### 📖 业务文档
- [需求文档](requirements/)
- [用户手册](user-guide/)
- [培训材料](training/)

## 🔍 按模块查看

### 后端模块 (Server)
1. [Auth模块](modules/auth/) - 认证授权
2. [Users模块](modules/users/) - 用户管理
3. [Patients模块](modules/patients/) - 患者管理
4. [MedicalCase模块](modules/medical-case/) - 病历管理
5. [Consultation模块](modules/consultation/) - 问诊管理
6. [Prescriptions模块](modules/prescriptions/) - 处方管理
7. [Herbs模块](modules/herbs/) - 药材管理
8. [Formula模块](modules/formula/) - 方剂管理

### 前端模块 (Client)
- [Shell主程序](client/shell/)
- [各业务模块](client/modules/)
- [共享组件](client/shared/)

## 📈 最新更新

### 2025-09-21
- ✅ 添加Shared层规范文档（5个新文档）
- ✅ 类型清单梳理完成（268+类型）
- ✅ 依赖关系图绘制完成
- ✅ 枚举规范制定完成
- ✅ 架构门禁规范发布

### 2025-09-20
- ✅ DTO优化三阶段完成
- ✅ 接口统一化完成
- ✅ 达到零编译错误

## 🔗 相关资源

- [GitHub仓库](https://github.com/shouqitao/LYBTZYZS)
- [Issues追踪](https://github.com/shouqitao/LYBTZYZS/issues)
- [Wiki文档](https://github.com/shouqitao/LYBTZYZS/wiki)
- [更新日志](CHANGELOG.md)

## 📝 文档规范

所有文档遵循以下规范：
- 使用Markdown格式
- 包含更新时间戳
- 提供清晰的目录结构
- 包含代码示例
- 保持与代码同步

---

*凌隐宝堂中医诊所诊疗系统 - 让中医诊疗更智能、更高效、更专业*