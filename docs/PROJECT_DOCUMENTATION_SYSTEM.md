# 项目文档体系规范

## 📚 文档驱动开发原则

### 核心原则
基于CLAUDE.md中定义的UltraThink文档驱动开发原则：

1. **文档有要求的代码得有** - 所有文档中描述的功能必须在代码中实现
2. **文档没要求的不增加代码** - 避免过度设计和功能蔓延  
3. **需要增加功能的先有文档再有代码** - 任何新功能必须先完善文档设计

## 📁 完整文档目录结构

```
docs/
├── PROJECT_DOCUMENTATION_SYSTEM.md        # 本文档 - 文档体系规范
├── README.md                              # 项目总览文档
│
├── architecture/                          # 架构设计文档
│   ├── system-architecture-overview.md   # 系统整体架构
│   ├── ultrathink-dual-layer-architecture.md  # UltraThink双层架构
│   ├── database-design.md               # 数据库设计
│   └── security-architecture.md         # 安全架构设计
│
├── api/                                  # API接口文档
│   ├── api-standards.md                 # API设计标准
│   ├── authentication-api.md            # 认证接口文档
│   ├── herbs-api.md                     # 药材接口文档
│   ├── formulas-api.md                  # 验方接口文档
│   ├── prescriptions-api.md             # 处方接口文档
│   └── error-codes.md                   # 错误码定义
│
├── modules/                              # 模块功能文档
│   ├── module-overview.md               # 模块总览
│   ├── auth-module.md                   # 认证模块
│   ├── users-module.md                  # 用户模块
│   ├── patients-module.md               # 患者模块
│   ├── herbs-module.md                  # 药材模块
│   ├── formulas-module.md               # 验方模块
│   ├── prescriptions-module.md          # 处方模块
│   ├── consultation-module.md           # 诊疗模块
│   └── medical-case-module.md           # 医案模块
│
├── development/                          # 开发指南
│   ├── getting-started.md               # 快速开始指南
│   ├── coding-standards.md              # 编码规范
│   ├── testing-guide.md                # 测试指南
│   ├── deployment-guide.md             # 部署指南
│   └── troubleshooting.md              # 问题排查
│
├── business/                            # 业务流程文档
│   ├── tcm-workflow.md                 # 中医诊疗流程
│   ├── prescription-workflow.md        # 处方开具流程
│   ├── patient-management.md           # 患者管理流程
│   └── herb-management.md              # 药材管理流程
│
├── design/                             # 设计文档
│   ├── ui-design-system.md            # UI设计系统
│   ├── user-experience.md             # 用户体验设计
│   ├── responsive-design.md           # 响应式设计
│   └── accessibility.md               # 可访问性设计
│
├── ultrathink/                         # UltraThink方法论文档
│   ├── three-modules-analysis/         # 三模块分析目录
│   │   ├── herb-model-optimization-requirements.md
│   │   ├── formula-model-optimization-requirements.md
│   │   ├── prescription-model-optimization-requirements.md
│   │   ├── three-modules-collaboration-design.md
│   │   ├── api-interface-unified-standards.md
│   │   └── data-model-refactoring-plan.md
│   └── completed-analysis/             # 已完成的分析文档
│
├── deployment/                         # 部署文档
│   ├── environment-setup.md           # 环境配置
│   ├── database-setup.md              # 数据库配置
│   ├── server-deployment.md           # 服务器部署
│   └── monitoring-setup.md            # 监控配置
│
└── maintenance/                        # 维护文档
    ├── backup-strategy.md             # 备份策略
    ├── performance-optimization.md    # 性能优化
    ├── security-maintenance.md       # 安全维护
    └── update-procedures.md          # 更新流程
```

## 📋 各类文档标准模板

### 1. 模块文档模板 (modules/)
```markdown
# [模块名称] 模块文档

## 📋 模块概述
- 模块功能描述
- 业务价值说明
- 与其他模块的关系

## 🏗️ 架构设计
- UltraThink双层架构实现
- Service层职责分工
- Repository层设计

## 📊 数据模型
- 实体类定义
- 数据关系图
- 字段说明

## 🚀 API接口
- RESTful接口列表
- 请求/响应示例
- 错误码说明

## 🧪 测试覆盖
- 单元测试覆盖率
- 集成测试用例
- 性能测试指标

## 📈 性能指标
- 响应时间要求
- 并发处理能力
- 资源使用情况

## 🔄 更新日志
- 版本变更记录
- 功能更新说明
```

### 2. API文档模板 (api/)
```markdown
# [模块名称] API接口文档

## 🎯 接口概览
| 接口 | 方法 | 功能描述 | 状态 |
|------|------|----------|------|
| `/api/v1/resource` | GET | 获取资源列表 | ✅ |
| `/api/v1/resource/{id}` | GET | 获取单个资源 | ✅ |

## 📊 接口详情

### GET /api/v1/resource
**功能**: 获取资源分页列表

**请求参数**:
```json
{
  "page": 1,
  "pageSize": 20,
  "keyword": "搜索关键词"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [...],
    "totalCount": 100,
    "pageIndex": 1,
    "pageSize": 20
  }
}
```

**错误码**:
- 400: 参数错误
- 401: 未授权
- 500: 服务器错误
```

### 3. 架构文档模板 (architecture/)
```markdown
# [架构名称] 架构文档

## 🎯 架构目标
- 设计目标说明
- 解决的问题
- 架构优势

## 🏗️ 架构设计
- 整体架构图
- 组件关系图
- 数据流图

## 📊 技术栈
- 后端技术栈
- 前端技术栈
- 数据库选型
- 第三方依赖

## 🔄 部署架构
- 部署环境说明
- 服务器配置
- 网络拓扑

## 📈 性能考虑
- 可扩展性设计
- 性能优化点
- 监控策略
```

## 📝 文档维护规范

### 文档更新流程
1. **代码变更前** - 先更新相关设计文档
2. **代码实现中** - 同步更新API文档和模块文档  
3. **代码完成后** - 验证文档与代码一致性
4. **版本发布前** - 完整审查所有文档更新

### 文档质量要求
- **准确性**: 文档内容与实际代码100%一致
- **完整性**: 覆盖所有重要功能和接口
- **时效性**: 代码变更后24小时内完成文档更新
- **可读性**: 使用统一的模板和标准格式

### 文档Review检查项
- [ ] 文档内容与代码实现一致
- [ ] API示例可以正常执行
- [ ] 架构图反映实际设计
- [ ] 业务流程描述准确
- [ ] 错误码和异常处理完整

## 🎯 当前文档体系建设任务

### Phase 1: 核心模块文档完善
- [ ] 更新Herbs模块文档 (基于现有代码)
- [ ] 更新Formulas模块文档 (基于现有代码)  
- [ ] 更新Prescriptions模块文档 (基于现有代码)
- [ ] 创建三模块协作文档

### Phase 2: API文档完善
- [ ] 创建统一的API标准文档
- [ ] 完善各模块API接口文档
- [ ] 添加完整的错误码文档
- [ ] 创建API测试用例文档

### Phase 3: 架构文档完善  
- [ ] 创建系统整体架构文档
- [ ] 完善UltraThink双层架构文档
- [ ] 创建数据库设计文档
- [ ] 添加安全架构文档

### Phase 4: 业务文档完善
- [ ] 创建中医诊疗业务流程文档
- [ ] 完善用户使用指南
- [ ] 添加系统管理员指南
- [ ] 创建培训材料

## 🔧 文档工具和规范

### 文档格式规范
- **Markdown**: 所有文档使用Markdown格式
- **图片**: 使用相对路径，存放在docs/images/目录
- **代码**: 使用语法高亮，标明语言类型
- **链接**: 使用相对链接，确保文档间关联

### 文档命名规范
- 使用小写字母和连字符: `module-name.md`
- 包含版本信息: `api-v1.md`
- 按日期归档: `analysis-20250901.md`
- 功能描述清晰: `herb-optimization-requirements.md`

### 文档版本管理
- 重要变更创建新版本文档
- 保持版本历史记录
- 在文档末尾标明版本和更新时间
- 使用Git跟踪文档变更历史

---

**文档版本**: v1.0  
**创建时间**: 2025-09-01  
**维护者**: UltraThink项目组  
**更新状态**: 文档体系框架建立完成