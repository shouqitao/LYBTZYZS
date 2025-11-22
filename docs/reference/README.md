# Reference (参考) - 技术信息

> **信息导向**: 面向有经验的开发者，提供准确的技术参考
> **适合人群**: 开发者、架构师、技术负责人
> **使用方式**: 精确查询、技术决策、规范遵循

## 🔌 API文档 (API Documentation)

### 核心业务API

#### 🔐 认证模块 (Authentication)
- **[Auth API参考文档](api/auth.md)** - 完整的认证授权API接口文档
  - 涵盖登录、登出、Token刷新、权限验证等8个核心API端点
  - 包含请求/响应格式、状态码、错误处理和配置参数
- **[权限API参考](api/authorization.md)** - 角色权限、资源访问控制
- **[安全API参考](api/security.md)** - 密码策略、安全设置

#### 👥 用户管理 (Users)
- **[Users API参考文档](api/users.md)** - 完整的用户管理API接口文档
  - 涵盖用户CRUD、搜索筛选、状态管理、批量操作等15个核心API端点
  - 包含分页查询、拼音码搜索、权限控制和批量处理功能
- **[用户CRUD API](api/users.md)** - 用户增删改查操作
- **[角色管理API](api/user-roles.md)** - 医生、管理员角色管理
- **[用户设置API](api/user-settings.md)** - 个人偏好设置

#### 🏥 患者管理 (Patients)
- **[患者信息API](api/patients.md)** - 患者档案管理
- **[病史API](api/medical-history.md)** - 病史记录管理
- **[患者搜索API](api/patient-search.md)** - 患者查询和筛选

#### 📋 病历管理 (MedicalCase)
- **[病历API参考](api/medical-case.md)** - 病历创建、更新、查询
- **[病历模板API](api/case-templates.md)** - 病历模板管理
- **[病历分析API](api/case-analytics.md)** - 病历数据分析

#### 🔍 中医诊断 (Consultation)
- **[四诊API](api/consultation.md)** - 望闻问切数据管理
- **[诊断API](api/diagnosis.md)** - 中医诊断结果管理
- **[舌诊API](api/tongue-diagnosis.md)** - 舌诊图像和诊断

#### 💊 处方管理 (Prescriptions)
- **[处方API](api/prescriptions.md)** - 处方开具和管理
- **[草药配伍API](api/herb-combination.md)** - 草药配伍规则
- **[处方审核API](api/prescription-review.md)** - 处方审核流程

#### 🌿 中药管理 (Herbs)
- **[药材API](api/herbs.md)** - 药材信息管理
- **[库存API](api/inventory.md)** - 库存管理和预警
- **[药材分类API](api/herb-classification.md)** - 药材分类体系

#### 📜 方剂管理 (Formula)
- **[方剂API](api/formula.md)** - 方剂创建和管理
- **[经典方剂API](api/classic-formulas.md)** - 经典方剂库
- **[方剂应用API](api/formula-application.md)** - 方剂使用指导

### 系统API

#### 系统管理
- **[系统配置API](api/system-config.md)** - 系统参数配置
- **[日志管理API](api/logs.md)** - 日志查询和管理
- **[监控API](api/monitoring.md)** - 系统监控数据

#### 数据导入导出
- **[数据导入API](api/data-import.md)** - Excel、CSV数据导入
- **[数据导出API](api/data-export.md)** - 报表和数据导出
- **[数据同步API](api/data-sync.md)** - 数据同步接口

## ⚙️ 配置参考 (Configuration)

### 应用配置

#### 系统配置
- **[appsettings.json配置](configuration/appsettings.md)** - 主配置文件详解
- **[环境变量配置](configuration/environment-variables.md)** - 环境变量设置
- **[数据库配置](configuration/database.md)** - 数据库连接配置

#### 业务配置
- **[业务规则配置](configuration/business-rules.md)** - 业务逻辑配置
- **[工作流配置](configuration/workflows.md)** - 业务流程配置
- **[中医诊疗配置](configuration/tcm-settings.md)** - 中医特色配置

#### 安全配置
- **[身份认证配置](configuration/authentication.md)** - 认证系统配置
- **[数据加密配置](configuration/encryption.md)** - 数据加密设置
- **[访问控制配置](configuration/access-control.md)** - 访问权限配置

### 开发配置

#### 开发环境
- **[开发环境配置](configuration/development.md)** - 本地开发环境
- **[测试环境配置](configuration/testing.md)** - 自动化测试环境
- **[调试配置](configuration/debugging.md)** - 调试工具和配置

#### 构建配置
- **[项目构建配置](configuration/build.md)** - MSBuild配置
- **[依赖包配置](configuration/dependencies.md)** - NuGet包管理
- **[发布配置](configuration/publishing.md)** - 应用发布配置

## 📋 业务规则 (Business Rules)

### 医疗业务规则

#### 患者管理规则
- **[患者注册规则](business-rules/patient-registration.md)** - 患者注册条件和流程
- **[数据隐私规则](business-rules/data-privacy.md)** - 患者隐私保护规则
- **[医疗数据规范](business-rules/medical-data.md)** - 医疗数据标准化

#### 诊疗业务规则
- **[四诊规范](business-rules/four-diagnostics.md)** - 望闻问切执行规范
- **[诊断标准](business-rules/diagnosis-standards.md)** - 中医诊断标准
- **[病历书写规范](business-rules/medical-record-standards.md)** - 病历书写要求

#### 处方业务规则
- **[处方开具规则](business-rules/prescription-rules.md)** - 处方开具规范
- **[草药配伍规则](business-rules/herb-compatibility.md)** - 草药配伍禁忌
- **[剂量控制规则](business-rules/dosage-control.md)** - 药物剂量管理

### 技术业务规则

#### 数据验证规则
- **[输入验证规则](business-rules/input-validation.md)** - 用户输入验证
- **[数据完整性规则](business-rules/data-integrity.md)** - 数据完整性约束
- **[业务逻辑验证](business-rules/business-logic-validation.md)** - 业务逻辑校验

#### 系统运行规则
- **[并发控制规则](business-rules/concurrency-control.md)** - 并发访问控制
- **[事务处理规则](business-rules/transaction-handling.md)** - 事务管理规则
- **[错误处理规则](business-rules/error-handling.md)** - 异常处理规范

## 📐 技术规范 (Technical Specifications)

### 架构规范

#### 系统架构
- **[整体架构设计](technical-specs/system-architecture.md)** - 系统架构总览
- **[微服务架构](technical-specs/microservices.md)** - 服务拆分规范
- **[数据库架构](technical-specs/database-architecture.md)** - 数据库设计规范

#### 代码规范
- **[C#编码规范](technical-specs/csharp-coding.md)** - C#代码编写标准
- **[命名规范](technical-specs/naming-conventions.md)** - 命名约定
- **[注释规范](technical-specs/commenting.md)** - 代码注释标准

#### 接口规范
- **[REST API规范](technical-specs/rest-api.md)** - REST接口设计规范
- **[数据传输规范](technical-specs/data-transfer.md)** - DTO设计规范
- **[版本控制规范](technical-specs/versioning.md)** - API版本管理

### 数据规范

#### 数据模型
- **[实体模型设计](technical-specs/entity-models.md)** - 实体设计规范
- **[数据关系设计](technical-specs/data-relationships.md)** - 关系设计规范
- **[数据迁移规范](technical-specs/data-migration.md)** - 数据库迁移规范

#### 数据标准
- **[数据类型标准](technical-specs/data-types.md)** - 数据类型使用规范
- **[数据格式标准](technical-specs/data-formats.md)** - 数据交换格式
- **[数据质量标准](technical-specs/data-quality.md)** - 数据质量要求

### 安全规范

#### 应用安全
- **[认证授权规范](technical-specs/auth-authorization.md)** - 身份认证和授权
- **[数据加密规范](technical-specs/encryption.md)** - 数据加密标准
- **[安全审计规范](technical-specs/security-audit.md)** - 安全审计要求

#### 医疗安全
- **[HIPAA合规规范](technical-specs/hipaa-compliance.md)** - 医疗数据保护
- **[医疗信息标准](technical-specs/medical-standards.md)** - 医疗信息标准
- **[隐私保护规范](technical-specs/privacy-protection.md)** - 隐私保护要求

## 🔍 快速查找

### 按功能模块查找
- **认证授权** → [认证API](api/auth.md) + [权限API](api/authorization.md)
- **用户管理** → [用户API](api/users.md) + [角色API](api/user-roles.md)
- **患者管理** → [患者API](api/patients.md) + [病史API](api/medical-history.md)
- **病历管理** → [病历API](api/medical-case.md) + [模板API](api/case-templates.md)
- **中医诊断** → [四诊API](api/consultation.md) + [诊断API](api/diagnosis.md)
- **处方管理** → [处方API](api/prescriptions.md) + [草药API](api/herb-combination.md)
- **中药管理** → [药材API](api/herbs.md) + [库存API](api/inventory.md)
- **方剂管理** → [方剂API](api/formula.md) + [经典方剂API](api/classic-formulas.md)

### 按技术类型查找
- **API接口** → [API文档](api/)
- **配置参数** → [配置参考](configuration/)
- **业务规则** → [业务规则](business-rules/)
- **技术标准** → [技术规范](technical-specs/)
- **数据模型** → [数据模型](technical-specs/entity-models.md)
- **安全规范** → [安全规范](technical-specs/auth-authorization.md)

### 按使用场景查找
- **开发新功能** → [API文档](api/) + [业务规则](business-rules/)
- **系统配置** → [配置参考](configuration/)
- **故障排查** → [错误代码参考](troubleshooting/error-codes.md)
- **性能优化** → [性能分析指南](performance/analysis.md)
- **安全加固** → [安全规范](technical-specs/security-audit.md)
- **合规检查** → [合规规范](technical-specs/hipaa-compliance.md)

## 📊 版本信息

### 当前版本
- **API版本**: v1.0
- **数据库版本**: v1.0
- **配置版本**: v1.0

### 版本兼容性
| 版本 | 发布日期 | 兼容性 | 主要变更 |
|------|----------|--------|----------|
| v1.0 | 2025-11-22 | - | 初始版本 |
| v1.1 | 计划中 | 向后兼容 | [计划变更] |

## 🔗 相关资源

### 内部文档
- 🎓 **[Tutorials](../tutorials/)** - 学习教程
- 🛠️ **[How-to Guides](../how-to-guides/)** - 操作指南
- 🧠 **[Explanation](../explanation/)** - 原理说明

### 外部资源
- 📚 **[.NET官方文档](https://docs.microsoft.com/dotnet/)** - .NET技术文档
- 🗄️ **[SQL Server文档](https://docs.microsoft.com/sql/sql-server/)** - 数据库文档
- 🏥 **[医疗信息标准](https://www.hl7.org/)** - 医疗信息标准

### 工具资源
- 🔧 **[Postman集合](tools/postman-collection.md)** - API测试工具
- 📊 **[数据库工具](tools/database-tools.md)** - 数据库管理工具
- 🛠️ **[开发工具](tools/development-tools.md)** - 开发环境工具

## 📞 获取帮助

### 文档问题
- 📋 **[文档反馈表单](https://forms.office.com/...)** - 文档问题和改进建议
- 🐛 **[文档Bug报告](https://github.com/shouqitao/LYBTZYZS/issues/new?labels=documentation)** - 文档错误报告

### 技术支持
- 💬 **[技术社区](https://github.com/shouqitao/LYBTZYZS/discussions)** - 技术讨论社区
- 📧 **[架构团队](mailto:architecture@example.com)** - 架构设计咨询
- 🎯 **[API支持](mailto:api-support@example.com)** - API使用支持

---

**文档类型**: Reference Index
**更新时间**: 2025-11-22
**维护团队**: 架构组 + 开发团队
**文档标准**: 遵循Microsoft API文档规范