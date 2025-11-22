# Issues分析报告

**生成时间**：2025-10-20
**分析范围**：当前所有Open状态的Issues
**总数**：47个开放Issues

## 📊 Epic分类统计

| Epic | Issue数量 | 优先级 | 状态 |
|------|----------|--------|------|
| **Epic #1494** - 医案流程UI实现 | 6个 | P0-Critical | 🔥 **进行中** |
| **Epic #1513** - Workstation架构重构 | 4个 | P2-Medium | ✅ **Phase 1完成** |
| **Epic #1483** - 就诊流程UI/UX实现 | 10个 | High | 📋 **待启动** |
| **Epic #1343** - MVP功能完整实现 | 24个 | High/Medium | 📋 **待启动** |
| **Epic #1456** - 临床工作台完整实现 | 1个 | P0-Critical | 📋 **待启动** |
| **其他独立任务** | 2个 | - | 📋 **待处理** |

---

## 🔥 Epic #1494 - 医案流程UI实现（当前最高优先级）

**目标**：实现4步流程的医案录入界面（患者选择 → 诊断录入 → 处方开具 → 完成医案）

### 任务清单

| Issue | 标题 | 优先级 | 状态 | 工作区文件 |
|-------|------|--------|------|------------|
| #1512 | Shell启动后无法显示HomeView | P0-Critical | ✅ **已修复** | PR #1517已创建 |
| #1496 | MedicalCaseFlowView核心框架 | High | ✅ **已完成** | MedicalCaseFlowViewModel.cs已实现 |
| #1497 | Step 1 - PatientSelectionView实现 | High | 🔄 **进行中** | PatientSelectionView文件存在工作区 |
| #1498 | Step 2 - ConsultationForm实现 | High | 🔄 **进行中** | ConsultationFormView文件存在工作区 |
| #1499 | Step 3 - PrescriptionEditor实现 | High | ❌ **未开始** | - |
| #1500 | Step 4 - CompletionView实现 | High | ❌ **未开始** | - |
| #1502 | 自动保存草稿功能 | Medium | ❌ **未开始** | - |
| #1503 | 小屏幕兼容性测试 | Medium | ❌ **未开始** | - |

### 当前工作区文件分析

**已实现但未提交的文件**：
```
src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/
├── ViewModels/
│   ├── ConsultationFormViewModel.cs          # Task #1498 (未提交)
│   ├── PatientSelectionViewModel.cs          # Task #1497 (未提交)
│   └── MedicalCaseFlowViewModel.cs           # 已修改 (未提交)
└── Views/
    ├── ConsultationFormView.xaml             # Task #1498 (未提交)
    ├── ConsultationFormView.xaml.cs
    ├── PatientSelectionView.xaml             # Task #1497 (未提交)
    └── PatientSelectionView.xaml.cs
```

**修改的项目文件**：
- `LYBT.Desktop.MedicalCase.csproj` - 添加了Patients模块依赖

### 🎯 下一步建议（Epic #1494）

**优先级排序**：
1. **立即完成Task #1497** - PatientSelectionView实现并提交PR
2. **立即完成Task #1498** - ConsultationFormView实现并提交PR
3. **开始Task #1499** - PrescriptionEditor实现（8列DataGrid）
4. **开始Task #1500** - CompletionView实现
5. **可选Task #1502** - 自动保存草稿
6. **最后Task #1503** - 小屏幕兼容性测试

---

## ✅ Epic #1513 - Workstation架构重构（Phase 1已完成）

### 任务状态

| Issue | 标题 | 优先级 | 状态 |
|-------|------|--------|------|
| #1513 | Epic: Workstation架构重构 | P2-Medium | 🔄 **进行中** |
| #1514 | Phase 1: 重命名HomeView为ClinicalHomeView | P2-Medium | ✅ **已完成** (PR #1517) |
| #1515 | Phase 2: 创建Reception模块 | P3-Low | ❌ **未开始** |
| #1516 | Phase 3: 移除Workstation容器模块 | P3-Low | ❌ **未开始** |

### 成果

- ✅ **ADR-003架构决策**：docs/architecture/decisions/adr-003-workstation-refactoring.md
- ✅ **Shell层设计文档**：docs/architecture/client/shell-layer-design.md
- ✅ **PR #1517**：ClinicalHomeView迁移到MedicalCase模块
- ✅ **架构修复**：单一职责原则（LoginViewModel认证，MainWindowViewModel导航）

### 下一步建议（Epic #1513）

- **Phase 2-3优先级较低**，暂缓执行
- **Phase 1已完成**，等待PR #1517合并

---

## 📋 Epic #1483 - 就诊流程UI/UX实现（未启动）

**目标**：实现完整的就诊流程UI/UX

### 任务清单（10个任务）

| Issue | 模块 | 标题 | 优先级 |
|-------|------|------|--------|
| #1484 | Shared | Task 1: 全局样式系统实现 | High |
| #1485 | Shared | Task 2: 导航与Shell框架实现 | High |
| #1486 | Consultation | Task 3: HomeView Dashboard实现 | High |
| #1487 | Patients | Task 4: PatientSelectionDialog优化 | High |
| #1488 | Consultation | Task 5: ConsultationView框架实现 | High |
| #1489 | Consultation | Task 6: 诊断表单实现 | High |
| #1490 | Prescriptions | Task 7: 处方手工录入实现 | High |
| #1491 | Prescriptions | Task 8: 验方导入功能实现 | High |
| #1492 | Prescriptions | Task 9: 历史复制功能实现 | High |
| #1493 | Consultation | Task 10: 流程控制与集成实现 | High |

### 分析

**与Epic #1494重叠度高**：
- Epic #1483的Task 5-10与Epic #1494的Task 1-4功能重叠
- 建议：**优先完成Epic #1494**（更聚焦），Epic #1483可以作为补充优化

---

## 📋 Epic #1343 - MVP功能完整实现（24个任务）

**目标**：实现MVP阶段的所有核心功能

### 任务分类

#### 1. 处方编号功能（3个任务）
- #1390: 实现处方自动编号服务
- #1391: 处方详情页显示编号
- #1392: 测试处方编号功能

#### 2. 业务规则约束（6个任务）
- #1393: RULE-1 - "一案一诊一方"业务规则
- #1394: RULE-2 - 处方当日可编辑规则
- #1395: RULE-3 - 处方必填项验证
- #1396: RULE-4 - 药材库存警告（可选）
- #1397: RULE-5 - 测试所有业务规则
- #1423: 实现医案业务规则约束总览

#### 3. 处方状态管理（3个任务）
- #1398: STATUS-1 - 添加处方状态枚举
- #1399: STATUS-2 - 实现状态自动管理逻辑
- #1400: STATUS-3 - UI显示处方状态并测试

#### 4. 数据导入功能（4个任务）
- #1383: IMPORT-1 - 实现患者导入功能
- #1384: IMPORT-2 - 实现病案导入功能
- #1385: IMPORT-3 - 集成导入到患者管理和病案页
- #1386: IMPORT-4 - 测试导入功能

#### 5. 就诊查询功能（3个任务）
- #1387: SEARCH-1 - 实现就诊记录查询服务
- #1388: SEARCH-2 - 实现就诊查询UI
- #1389: SEARCH-3 - 测试就诊查询功能

#### 6. 验方管理功能（2个任务）
- #1470: FORMULA-5 - 实现手动映射药材
- #1471: FORMULA-6 - 实现待验证验方查询

#### 7. 历史处方功能（1个任务）
- #1476: ENTRY-17 - 创建历史处方搜索对话框

#### 8. 打印功能（1个任务）
- #1382: PRINT-5 - 测试打印功能

### 下一步建议（Epic #1343）

**这些任务大多数属于MVP后期的功能细化**，建议：
1. **暂缓执行**，优先完成Epic #1494核心流程
2. **重新评估优先级**，很多功能可能不是MVP必需的
3. **合并相似任务**，减少任务数量

---

## 🔥 Epic #1456 - 临床工作台看诊流程完整实现

**目标**：完整实现临床工作台的看诊流程

### 分析

- **只有1个Epic Issue**，没有具体的子任务
- **优先级P0-Critical**，但内容模糊
- **与Epic #1494、#1483高度重叠**

### 建议

- **合并到Epic #1494**，避免重复
- 或者将Epic #1456作为总控Epic，#1494和#1483作为子Epic

---

## 📋 其他独立任务

| Issue | 标题 | 分析 |
|-------|------|------|
| #1480 | 【Phase 3】文档更新（包含Statistics过度开发说明） | 文档任务，优先级低 |
| #1477 | 【架构纠正v2】MedicalCase聚合根强势修正 | 架构任务，需评估是否还需要 |

---

## 🎯 总体建议与优先级

### 立即执行（本周内）

**Epic #1494 - 医案流程UI实现**（当前最高优先级）

1. **Task #1497** - PatientSelectionView实现 ✅ **工作区已有文件，需验证和提交**
   - 检查PatientSelectionViewModel.cs实现完整性
   - 检查PatientSelectionView.xaml UI设计
   - 编译测试 → 创建PR

2. **Task #1498** - ConsultationFormView实现 ✅ **工作区已有文件，需验证和提交**
   - 检查ConsultationFormViewModel.cs实现完整性
   - 检查ConsultationFormView.xaml UI设计
   - 编译测试 → 创建PR

3. **Task #1499** - PrescriptionEditor实现 ❌ **未开始，紧急**
   - 实现8列DataGrid处方录入界面
   - 集成现有PrescriptionViewModel组件化架构
   - 支持手工录入、验方导入、历史复制三种模式

4. **Task #1500** - CompletionView实现 ❌ **未开始**
   - 医案完成确认界面
   - 显示患者信息、诊断摘要、处方汇总

---

### 短期执行（1-2周内）

**Epic #1494 - 医案流程UI实现（补充功能）**

5. **Task #1502** - 自动保存草稿功能（可选）
6. **Task #1503** - 小屏幕兼容性测试

---

### 中期执行（2-4周内）

**Epic #1343 - MVP功能完整实现（核心功能子集）**

重点任务：
- 处方编号功能（#1390-#1392）
- "一案一诊一方"业务规则（#1393）
- 处方必填项验证（#1395）
- 就诊查询功能（#1387-#1389）

---

### 暂缓执行

**Epic #1483 - 就诊流程UI/UX实现**
- 与Epic #1494高度重叠，暂缓

**Epic #1513 - Workstation架构重构（Phase 2-3）**
- Phase 1已完成，Phase 2-3优先级低

**Epic #1343 - MVP功能完整实现（非核心功能）**
- 数据导入功能（#1383-#1386）- MVP后期
- 药材库存警告（#1396）- 可选功能
- 打印功能（#1382）- MVP后期

---

## 🚀 下一步行动计划

### 第1步：清理工作区（今天）

```bash
# 1. 验证工作区文件的实现完整性
# 2. 编译测试
dotnet build LYBT.All.sln -c Release --no-restore

# 3. 创建Task #1497的PR（PatientSelectionView）
git checkout -b feature/1497-patient-selection-view
git add src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PatientSelectionViewModel.cs
git add src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/PatientSelectionView.*
git commit -m "feat(medicalcase): 实现Step 1患者选择界面"
gh pr create --title "feat: Task #1497 - PatientSelectionView实现"

# 4. 创建Task #1498的PR（ConsultationFormView）
git checkout -b feature/1498-consultation-form-view
git add src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/ConsultationFormViewModel.cs
git add src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/ConsultationFormView.*
git commit -m "feat(consultation): 实现Step 2诊断录入界面"
gh pr create --title "feat: Task #1498 - ConsultationFormView实现"
```

---

### 第2步：开始Task #1499（明天）

**PrescriptionEditor实现**（8列DataGrid）
- 基于现有PrescriptionViewModel组件化架构
- 复用PrescriptionDataManager、PrescriptionCommandHandler
- 集成验方导入、历史复制功能

---

### 第3步：完成Task #1500（2天后）

**CompletionView实现**
- 医案完成确认界面
- 汇总显示功能

---

## 📊 风险评估

### 高风险

1. **工作区文件未验证**
   - PatientSelectionView和ConsultationFormView的实现完整性未知
   - 需要立即验证编译和功能

2. **任务过多且分散**
   - 47个开放Issues，管理难度大
   - 建议：关闭已完成或不需要的Issues

3. **Epic重叠**
   - Epic #1494、#1483、#1456功能重叠
   - 建议：重新梳理Epic边界

---

### 中风险

1. **PrescriptionEditor复杂度高**
   - 8列DataGrid设计复杂
   - 需要3-5天开发时间

2. **小屏幕兼容性测试缺失**
   - 1366x768和1280x720分辨率未测试
   - 可能需要UI调整

---

## 📋 建议关闭的Issues

**已完成或重复的Issues**：
- #1512: Shell启动后无法显示HomeView（PR #1517已解决）
- #1514: Phase 1迁移ClinicalHomeView（PR #1517已解决）

**优先级过低或不需要的Issues**：
- #1480: 文档更新（可合并到其他文档任务）
- #1477: 架构纠正（需评估是否已通过其他方式解决）

---

## 🎯 成功指标

**本周目标**：
- ✅ PR #1517（Shell层架构修复）合并
- ✅ Task #1497（PatientSelectionView）完成并提交PR
- ✅ Task #1498（ConsultationFormView）完成并提交PR
- 🎯 Task #1499（PrescriptionEditor）启动开发

**2周目标**：
- ✅ Epic #1494的4个核心任务（#1497-#1500）全部完成
- ✅ 医案流程UI基本可用

**1个月目标**：
- ✅ Epic #1343核心功能完成（处方编号、业务规则、查询功能）
- ✅ MVP功能基本完整

---

**报告生成**：Claude Code | **审核人**：项目负责人
**下次更新**：2025-10-27
