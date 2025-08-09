# 文档清理计划 - Solution级架构重构

**创建时间**: 2025年8月9日  
**状态**: 进行中  

## 🎯 清理目标

根据用户要求："系统中的有些文档已经陈旧。要不更新，要不删除。根据实际内容重写。UltraThink。"

## 📋 需要清理的文档类别

### 1. 冲突文档（立即删除）
```
docs/09-项目记录/历史文档/存在冲突的文档/
├── [冲突]TODO-Latest-20250108-V2.md
├── [冲突]TODO-Latest-20250108.md
├── [冲突]模块开发进度追踪-20250108.md
└── [冲突]项目实现状态报告-20250108.md
```

### 2. 过时的TODO文档（合并为最新版本）
```
docs/
├── TODO-Latest-20250108.md (过时 - 2025-01-08)
├── TODO-Summary-20250108-2.md (过时)
├── TODO-Summary-20250108-Phase1-Complete.md (过时)
├── TODO-Summary-20250108.md (过时)
└── TODO-纠正版-20250108.md (过时)
```

### 3. 不存在模块的文档（删除）
根据实际8个核心模块，需要删除以下文档：
```
docs/Backend/Documentation/
├── LYBT.Module.Billing_*.md (Billing模块不存在)
├── LYBT.Module.DiagnosisTreatment_*.md (DiagnosisTreatment模块不存在)
├── LYBT.Module.Diagnostics_*.md (Diagnostics模块不存在)  
├── LYBT.Module.Doctors_*.md (Doctors模块不存在)
├── LYBT.Module.FormulaTemplates_*.md (应为Formula)
├── LYBT.Module.Pharmacy_*.md (Pharmacy模块不存在)
├── LYBT.Module.Queueing_*.md (Queueing模块不存在)
├── LYBT.Module.Records_*.md (Records模块不存在)
├── LYBT.Module.Registration_*.md (Registration模块不存在)
├── LYBT.Module.Sync_*.md (Sync模块不存在)
└── LYBT.Module.TreatmentRoom_*.md (TreatmentRoom模块不存在)
```

### 4. 过时的实现总结文档（移至历史文档）
```
docs/04-模块实现/
├── Doctors模块实现总结.md (已删除模块)
├── Queueing模块实现总结.md (已删除模块)
└── 其他已删除模块的总结
```

### 5. 重复的架构文档（合并）
```
docs/03-架构设计/
├── 模块功能定位分析.md (与模块功能定位-最终版.md重复)
├── 模块职能详细说明.md (与模块设计文档.md重复)
└── 系统架构总览.md (与系统架构极简版.md重复)
```

## 🚀 清理执行计划

### 阶段1: 删除冲突和过时文档
1. [x] 删除冲突文档目录
2. [x] 删除过时的TODO文档
3. [x] 删除不存在模块的文档

### 阶段2: 创建最新的统一文档
1. [ ] 创建最新的TODO状态文档
2. [ ] 更新项目架构文档
3. [ ] 更新模块清单文档

### 阶段3: 合并重复文档
1. [ ] 合并架构设计文档
2. [ ] 合并开发指南文档
3. [ ] 整理项目记录文档

## 📝 清理后的文档结构

```
docs/
├── 01-项目管理/          # 保持不变
├── 02-需求分析/          # 保持不变  
├── 03-架构设计/          # 合并重复文档
├── 04-接口设计/          # 保持不变
├── 05-开发指南/          # 更新开发规范
├── 06-测试文档/          # 保持不变
├── 07-部署运维/          # 保持不变
├── 08-用户文档/          # 保持不变
├── 09-项目记录/          # 移动过时文档至历史文档
│   └── 历史文档/         # 保存历史记录
├── 10-项目规范/          # 保持不变
├── API文档/             # 保持不变
├── design/              # 保持不变
├── development/         # 保持不变
├── performance/         # 保持不变
├── README.md            # 更新项目概述
├── CURRENT_STATUS.md    # 新建：当前项目状态
└── MODULE_LIST.md       # 新建：8个核心模块清单
```

## 🎯 清理原则

1. **保留有价值的历史信息** - 移至历史文档而不是直接删除
2. **创建准确的当前状态文档** - 基于实际项目状态重写
3. **建立清晰的文档层次** - 按重要性和时效性组织
4. **确保文档一致性** - 所有文档反映实际的8个核心模块

## ✅ 完成标志

- [ ] 所有冲突文档已删除
- [ ] 所有过时TODO文档已清理
- [ ] 所有不存在模块的文档已删除
- [ ] 创建了准确的当前状态文档
- [ ] 文档结构清晰合理
- [ ] 项目成员可以快速找到需要的文档