# 文档重组说明

## 概述

本文档说明了LYBTZYZS项目文档的重组计划，将原有散落的文档按照新的文档规范进行整理。

## 新文档规范

根据docs/README.md的要求，项目文档按以下结构组织：

### 核心文档（已完成）
- ✅ **架构文档**: `docs/architecture/ARCHITECTURE.md`
- ✅ **模块规范**: `docs/architecture/MODULES.md`
- ✅ **API契约**: `docs/architecture/API_CONTRACTS.md`
- ✅ **设置指南**: `docs/development/SETUP.md`
- ✅ **编码标准**: `docs/development/CODING_STANDARDS.md`
- ✅ **开发路线**: `docs/development/ROADMAP.md`

## 文档整理方案（已实施）

### 1. 架构相关文档 (docs/architecture/)

**核心文档（双语版本）：**
- ARCHITECTURE.md / ARCHITECTURE_EN.md - 系统架构文档
- MODULES.md / MODULES_EN.md - 模块规范文档
- API_CONTRACTS_CN.md / API_CONTRACTS.md - API契约文档

**附件文档位置：**
- `docs/architecture/ARCHITECTURE/`
  - DATA_FLOW_AND_DTO_DESIGN.md - 数据流和DTO设计
  - THREE_LAYER_MODEL_DESIGN.md - 三层模型设计

### 2. 开发相关文档 (docs/development/)

**核心文档（双语版本）：**
- SETUP.md / SETUP_EN.md - 开发环境设置文档
- CODING_STANDARDS.md / CODING_STANDARDS_EN.md - 编码标准文档
- ROADMAP.md / ROADMAP_EN.md - 开发路线图文档

**附件文档位置：**
- `docs/development/SETUP/`
  - LOCAL_DEVELOPMENT_SETUP.md - 本地开发设置
  - getting-started.md - 快速开始指南
  - 进程管理说明.md - 进程管理说明

- `docs/development/CODING_STANDARDS/`
  - LYBT_CODING_STANDARDS.md - LYBT编码标准
  - MODULE_CODING_STANDARDS_CHECK.md - 模块编码标准检查
  - MODEL_LAYER_STANDARDS.md - 模型层标准
  - DTO_USAGE_STANDARDS.md - DTO使用标准

- `docs/development/references/` - 开发参考文档
  - WebAPI-Documentation.md
  - TECHNICAL_DEBT.md
  - 各种迁移报告（*MIGRATION*.md）
  - 各种分析文档（*_ANALYSIS.md）

### 3. API相关文档 (docs/api/)

**保留的文档：**
- 各模块的README和FUNCTIONALITY文档
- postman-collections/ - Postman测试集合
- api-test-report.md
- 系统完整技术文档.md

### 4. 测试相关文档 (docs/testing/)

**保留的文档：**
- API_测试指南.md
- 各种测试报告
- LYBT-Postman-Testing-Guide.md

### 5. 部署相关文档 (docs/deployment/)

**保留的文档：**
- auto-deploy-guide.md
- quick-deploy.md
- scripts-usage.md

### 6. 前端相关文档 (docs/frontend/)

**保留的文档：**
- 控件相关文档
- DTO控件对应报告

### 7. 开发模板 (docs/dev-templates/)

**保留所有模板文档**

### 8. 其他文档

**移到根目录docs/下的文档：**
- PROJECT-COMPLETION-REPORT.md - 项目完成报告
- technical-debt.md - 技术债务总览
- 系统管理模块第四阶段总结.md - 阶段性总结

**需要删除或归档的文档：**
- 重复的文档（如components/下与api/下重复的文档）
- 过时的文档

## 实施步骤

1. **第一阶段**：整理核心文档内容
   - 将相关内容整合到对应的核心文档中
   - 确保核心文档包含所有必要信息

2. **第二阶段**：重组目录结构
   - 移动文档到正确的位置
   - 删除重复文档

3. **第三阶段**：更新索引
   - 更新docs/README.md
   - 确保所有文档都有正确的链接

## 文档归类清单

### 需要整合的文档

| 原文档 | 目标文档 | 整合方式 |
|-------|---------|----------|
| LOCAL_DEVELOPMENT_SETUP.md | SETUP.md | 合并内容 |
| getting-started.md | SETUP.md | 合并内容 |
| LYBT_CODING_STANDARDS.md | CODING_STANDARDS.md | 合并内容 |
| MODULE_CODING_STANDARDS_CHECK.md | CODING_STANDARDS.md | 作为检查清单 |
| MODEL_LAYER_STANDARDS.md | CODING_STANDARDS.md | 作为专项标准 |
| DTO_USAGE_STANDARDS.md | CODING_STANDARDS.md | 作为专项标准 |

### 作为附件保留的文档

| 文档类别 | 文档列表 |
|---------|---------|
| 架构附件 | DATA_FLOW_AND_DTO_DESIGN.md, THREE_LAYER_MODEL_DESIGN.md |
| 开发参考 | WebAPI-Documentation.md, TECHNICAL_DEBT.md, 各种迁移报告 |
| 测试文档 | 保持testing/目录不变 |
| API文档 | 保持api/目录不变 |
| 部署文档 | 保持deployment/目录不变 |
| 前端文档 | 保持frontend/目录不变 |
| 开发模板 | 保持dev-templates/目录不变 |

### 需要删除的重复文档

- components/目录下的所有文档（与api/目录重复）
- 其他明显重复的文档

## 新的文档结构

```
docs/
├── README.md                               # 文档索引（包含双语链接）
├── DOCUMENT_REORGANIZATION.md              # 本文档
├── architecture/                           # 架构相关
│   ├── ARCHITECTURE.md                    # 系统架构（中文）
│   ├── ARCHITECTURE_EN.md                 # 系统架构（英文）
│   ├── ARCHITECTURE/                      # 架构附件
│   │   ├── DATA_FLOW_AND_DTO_DESIGN.md
│   │   └── THREE_LAYER_MODEL_DESIGN.md
│   ├── MODULES.md                         # 模块规范（中文）
│   ├── MODULES_EN.md                      # 模块规范（英文）
│   ├── API_CONTRACTS_CN.md                # API契约（中文）
│   └── API_CONTRACTS.md                   # API契约（英文）
├── development/                            # 开发相关
│   ├── SETUP.md                          # 开发设置（中文）
│   ├── SETUP_EN.md                       # 开发设置（英文）
│   ├── SETUP/                            # 设置附件
│   │   ├── LOCAL_DEVELOPMENT_SETUP.md
│   │   ├── getting-started.md
│   │   └── 进程管理说明.md
│   ├── CODING_STANDARDS.md               # 编码标准（中文）
│   ├── CODING_STANDARDS_EN.md            # 编码标准（英文）
│   ├── CODING_STANDARDS/                 # 编码标准附件
│   │   ├── LYBT_CODING_STANDARDS.md
│   │   ├── MODULE_CODING_STANDARDS_CHECK.md
│   │   ├── MODEL_LAYER_STANDARDS.md
│   │   └── DTO_USAGE_STANDARDS.md
│   ├── ROADMAP.md                        # 路线图（中文）
│   ├── ROADMAP_EN.md                     # 路线图（英文）
│   └── references/                       # 开发参考文档
│       ├── WebAPI-Documentation.md
│       ├── TECHNICAL_DEBT.md
│       └── 各种迁移和分析报告
├── api/                                  # API文档（保持不变）
├── testing/                              # 测试文档（保持不变）
├── deployment/                           # 部署文档（保持不变）
├── frontend/                             # 前端文档（保持不变）
└── dev-templates/                        # 开发模板（保持不变）
```

## 预期结果

文档重组后，项目文档将：
1. **清晰的层级结构**：核心文档与附件分离
2. **双语支持**：所有核心文档都有中英文版本
3. **附件组织**：相关附件存放在对应的子文件夹中
4. **没有重复内容**：删除了components目录的重复文档
5. **易于查找和维护**：通过文件夹结构快速定位相关文档

## 注意事项

- 在整合文档时，保留有价值的内容
- 确保不丢失重要信息
- 保持文档的可读性和完整性
- 更新所有相关链接