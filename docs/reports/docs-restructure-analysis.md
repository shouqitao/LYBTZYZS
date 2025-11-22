# LYBTZYZS文档重构分析记录

**备份时间**: 2025-11-21 21:24
**备份目录**: docs-backup-20251121-212435
**当前文档总数**: 584个Markdown文件

## 当前目录结构

### 主要目录统计
- **adr/**: 1个文件（架构决策记录）
- **analysis/**: 1个文件
- **architecture/**: 3个文件
- **archive/**: ~150个文件（问题重点）
- **checklists/**: 1个文件
- **data/**: Excel模板文件
- **design/**: 1个文件
- **explanation/**: ~50个文件（核心内容）
- **guides/**: 5个文件
- **how-to/**: ~30个文件
- **reference/**: ~30个文件
- **reports/**: ~40个文件（问题重点）
- **requirements/**: 4个文件
- **support/**: 2个文件
- **tasks/**: 8个文件
- **templates/**: 3个文件
- **testing/**: 7个文件
- **tools/**: 1个文件
- **tutorials/**: 3个文件

### 关键问题识别

#### 1. 过程记录堆积（需要移至Graphiti）
- `archive/reports/2025-10/`: 47个阶段性报告
- `archive/reports-2025-10/`: 大量过程分析
- `reports/`: 包含大量历史分析报告

#### 2. 状态文档（需要保留和清理）
- `explanation/architecture/`: 当前架构说明（含历史过程需要清理）
- `reference/api/`: API参考文档
- `how-to/`: 操作指南
- `guides/`: 开发指南
- `adr/`: 架构决策记录

#### 3. 重复结构（需要合并）
- `design/`, `analysis/`, `reports/` 功能重叠
- 多个目录包含类似的分析文档

## 执行计划

### Phase 1 ✅ 完成项目
- [x] 创建完整备份
- [x] 统计文档数量：584个文件
- [ ] 识别状态vs过程文档

### Phase 2 待执行
- [ ] 清理架构文档历史过程
- [ ] 提取纯状态信息

### Phase 3 待执行
- [ ] 过程信息转移至Graphiti
- [ ] 删除冗余过程文档

### Phase 4 待执行
- [ ] 创建新目录结构
- [ ] 重组文档

### Phase 5 待执行
- [ ] 建立维护机制
- [ ] 验证结果

## 成功指标
- 文档数量从584减少到约100个（减少83%）
- 建立清晰的状态vs过程分离
- 保持所有重要信息可访问性