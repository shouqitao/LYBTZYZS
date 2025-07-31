# LYBT项目文档结构规范

## 📁 目录结构

```
D:\source\repos\LYBTZYZS\
├── docs/                           # 📚 所有文档
│   ├── api/                        # 🔌 API文档
│   │   ├── modules/               # 各模块API文档
│   │   ├── swagger/               # Swagger相关
│   │   └── README.md              # API文档索引
│   ├── architecture/              # 🏗️ 架构文档
│   │   ├── system-design.md       # 系统设计
│   │   ├── database-schema.md     # 数据库架构
│   │   └── module-structure.md    # 模块结构
│   ├── development/               # 👨‍💻 开发文档
│   │   ├── CLAUDE.md              # Claude开发指南
│   │   ├── setup-guide.md         # 环境搭建
│   │   ├── coding-standards.md    # 编码规范
│   │   └── troubleshooting.md     # 故障排除
│   ├── testing/                   # 🧪 测试文档
│   │   ├── api-testing-guide.md   # API测试指南
│   │   ├── test-reports/          # 测试报告
│   │   └── test-cases/            # 测试用例
│   ├── user-guide/                # 📖 用户指南
│   │   ├── quick-start.md         # 快速开始
│   │   ├── user-manual.md         # 用户手册
│   │   └── faq.md                 # 常见问题
│   └── scripts/                   # 📜 脚本文档
│       └── README.md              # 脚本说明
├── testing/                       # 🔬 测试资源
│   ├── api-tests/                 # API测试文件
│   │   ├── postman/               # Postman集合
│   │   └── newman/                # Newman配置
│   ├── tools/                     # 测试工具
│   └── reports/                   # 测试报告输出
├── scripts/                       # 🛠️ 实用脚本
└── tools/                         # 🔧 开发工具
```

## 📋 文档分类规则

### 1. API文档 (`docs/api/`)
- 模块功能说明
- API端点文档  
- Swagger配置
- 请求/响应示例

### 2. 架构文档 (`docs/architecture/`)
- 系统整体设计
- 数据库架构
- 模块间关系
- 技术选型说明

### 3. 开发文档 (`docs/development/`)
- 环境搭建指南
- 编码规范
- Git工作流
- 调试指南

### 4. 测试文档 (`docs/testing/`)
- 测试策略
- 测试用例
- 自动化测试指南
- 测试报告模板

### 5. 用户文档 (`docs/user-guide/`)
- 快速开始指南
- 功能使用说明
- 故障排除
- 常见问题

### 6. 脚本文档 (`docs/scripts/`)
- 脚本功能说明
- 使用方法
- 参数说明

## 🎯 文档命名规范

### 文件命名
- 使用小写字母和连字符：`api-testing-guide.md`
- 避免中文文件名，使用英文描述
- 包含版本号：`api-v1.2-reference.md`

### 目录命名
- 使用小写字母和连字符
- 功能导向：`user-guide`, `api-reference`
- 避免缩写：使用`development`而不是`dev`

### 内容组织
- 每个目录包含`README.md`作为索引
- 使用清晰的标题层级
- 包含目录导航
- 提供交叉引用

## 📝 文档模板

### README模板
```markdown
# [模块/功能名称]

## 🎯 概述
简短描述模块功能和用途。

## 📚 文档索引
- [文档1](./doc1.md) - 描述
- [文档2](./doc2.md) - 描述

## 🚀 快速开始
核心使用步骤。

## 📞 相关链接
- [相关文档链接]
- [代码仓库链接]
```

### API文档模板
```markdown
# [API名称] API文档

## 基本信息
- 版本：v1.0
- 基础URL：http://localhost:5297
- 认证方式：JWT Bearer Token

## 端点列表
### GET /api/v1/endpoint
**描述：** 端点功能描述
**参数：**
- param1 (string, required): 参数描述
**响应：**
```json
{
  "success": true,
  "data": {}
}
```
```

## 🔄 迁移计划

### 即将移动的文件
1. `测试报告.md` → `docs/testing/reports/`
2. `认证问题修复状态报告.md` → `docs/development/`
3. API测试相关文件 → `testing/api-tests/`
4. 各模块文档 → `docs/api/modules/`

### 清理计划
1. 删除重复的文档文件
2. 合并类似内容的文档
3. 更新文档间的交叉引用
4. 创建统一的文档索引

---

**创建时间：** 2025-07-30  
**维护者：** 开发团队  
**版本：** v1.0