# Phase 4 - 需求明确与完善 执行方案

**Epic**: #1138 文档SSOT整理与需求完善
**Phase**: Phase 4（最终阶段）
**预计时间**: 2天
**状态**: 等待确认
**创建时间**: 2025-10-11

---

## 📋 总体目标

完成Epic #1138的最后一个验收标准：**MVP需求清晰无歧义**

### 核心原则
1. **以实际代码为准** - 代码是唯一真相源，需求文档必须真实反映代码实现
2. **不确定就确认** - 遇到不清楚的实现细节，向用户确认而不是猜测
3. **SSOT原则** - 需求统一在GitHub Issues管理，不创建本地PRD文档
4. **最小充分** - 只记录MVP必要的需求信息，避免过度设计

---

## 📅 Phase 4 执行计划

### Day 1: 代码实现盘点（10-12小时）

#### 目标
1. 生成16个模块的"代码实现清单"，作为需求文档的基础
2. **识别Server-Desktop之间的不一致**（两个团队开发导致）
3. 输出"差异分析清单"，为统一标准提供依据

#### 扫描范围
**Server端8个业务模块**：
1. `LYBT.Module.Auth` - 认证授权
2. `LYBT.Module.Users` - 用户管理
3. `LYBT.Module.Patients` - 患者管理
4. `LYBT.Module.MedicalCase` - 病历管理
5. `LYBT.Module.Consultation` - 诊疗管理
6. `LYBT.Module.Prescriptions` - 处方管理
7. `LYBT.Module.Herbs` - 中药材管理
8. `LYBT.Module.Formula` - 方剂管理

**Desktop端8个对应模块**：
1. `LYBT.Desktop.Auth` - 认证授权UI
2. `LYBT.Desktop.Users` - 用户管理UI
3. `LYBT.Desktop.Patients` - 患者管理UI
4. `LYBT.Desktop.MedicalCase` - 病历管理UI
5. `LYBT.Desktop.Consultation` - 诊疗管理UI
6. `LYBT.Desktop.Prescriptions` - 处方管理UI
7. `LYBT.Desktop.Herbs` - 中药材管理UI
8. `LYBT.Desktop.Formula` - 方剂管理UI

#### 执行步骤

**步骤1: 模块结构扫描**（1小时）
- 使用 `mcp__serena__list_dir` 扫描每个模块的目录结构
- 识别：Controllers/Services/Repositories/DTOs
- 输出：模块结构树

**步骤2: Controller层分析**（2-3小时）
- 使用 `mcp__serena__find_symbol` 查找所有Controller类
- 使用 `mcp__serena__get_symbols_overview` 获取方法列表
- 提取API端点：`[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`
- 记录：方法名、路由、参数类型、返回类型

**步骤3: Service层分析**（2-3小时）
- 使用 `mcp__serena__find_symbol` 查找所有Service类
- 分析业务逻辑方法
- 识别核心业务规则
- 记录：方法签名、业务流程

**步骤4: 实体与DTO分析**（1小时）
- 扫描 `LYBT.Entities` 中的实体类
- 扫描 `LYBT.Shared.Models` 中的DTO
- 记录：核心字段、关系、验证规则

**步骤5: Desktop端ViewModel分析**（2-3小时）
- 使用 `mcp__serena__find_symbol` 查找所有ViewModel类
- 分析数据绑定和命令
- 识别UI交互逻辑
- 记录：Repository调用、数据流转

**步骤6: Server-Desktop差异分析**（2小时）
- 对比相同业务模块的实现差异
- 识别不一致的点：
  - 命名规范差异（如DTO命名、方法命名）
  - 架构模式差异（Repository设计、服务调用）
  - 数据模型差异（字段不对应、类型不一致）
  - 业务流程差异（Server逻辑与Desktop逻辑不匹配）
- 标记差异类型：
  - ⚠️ **严重不一致**：影响功能正确性
  - ⚡ **模式不一致**：架构/规范不统一
  - 💡 **可优化项**：实现方式可改进

**步骤7: 生成清单报告**（1-2小时）
- 创建报告：`docs/reports/2025-10-11-requirements-gap-analysis.md`
- **Part 1: 模块实现清单**
  - 每个模块的：
    - 功能概述（基于代码推断）
    - 核心实体与DTO
    - API端点清单（Server）/ ViewModel清单（Desktop）
    - 业务规则摘要
    - **需要确认的问题**（不确定的实现细节）
- **Part 2: Server-Desktop差异分析**
  - 按模块列出差异项
  - 差异类型分类（严重/模式/优化）
  - 统一建议
- **Part 3: 统一标准建议**
  - 命名规范统一方案
  - 架构模式统一方案
  - 优先级排序

#### 产出物
```
docs/reports/2025-10-11-requirements-gap-analysis.md
├─ Part 1: 模块实现清单
│  ├─ Server.Auth
│  │  ├─ 功能概述
│  │  ├─ API端点 (10个)
│  │  ├─ 核心实体 (User, Role, Permission)
│  │  └─ 需确认项 (3个)
│  ├─ Desktop.Auth
│  │  ├─ 功能概述
│  │  ├─ ViewModels (5个)
│  │  ├─ Repository调用
│  │  └─ 需确认项 (2个)
│  ├─ ... (其他14个模块)
├─ Part 2: Server-Desktop差异分析
│  ├─ Auth模块差异
│  │  ├─ ⚠️ 严重不一致 (2项)
│  │  ├─ ⚡ 模式不一致 (5项)
│  │  └─ 💡 可优化项 (3项)
│  ├─ ... (其他7个模块对)
├─ Part 3: 统一标准建议
│  ├─ 命名规范统一方案
│  ├─ 架构模式统一方案
│  └─ 实施优先级
```

#### 并行策略
- Server模块与Desktop模块可以并行扫描
- 每对模块（如Auth Server + Auth Desktop）可以2个一组并行
- 差异分析在扫描完成后串行执行
- 预计可节省40-50%时间

---

### Day 2: 需求文档补充与统一标准（6-8小时）

#### 目标
1. 基于Day 1的清单，补充/创建GitHub Issues，确保MVP需求清晰
2. **制定统一标准规范**，解决Server-Desktop不一致问题
3. 为统一标准实施创建Issue（后续执行）

#### 执行步骤

**步骤1: 现有需求Issue审查**（1-2小时）
- 使用 `gh issue list` 查询现有feature Issues
- 按模块分类（`module:server` 标签）
- 对比Day 1的代码清单
- 标记三类情况：
  - ✅ 代码与需求一致
  - ⚠️ 代码已实现但需求缺失
  - ❓ 需求存在但代码未实现（MVP外）

**步骤2: 补充缺失的需求Issue**（2-3小时）
- 为"需求缺失"的模块创建Feature Issue
- Issue模板：
  ```markdown
  ## 功能概述
  [基于代码实现总结]

  ## 核心功能
  1. 功能A（基于Controller.MethodA）
  2. 功能B（基于Controller.MethodB）

  ## API端点
  | 方法 | 路径 | 功能 |
  |------|------|------|

  ## 验收标准
  - [ ] API端点可访问
  - [ ] 业务逻辑符合规则

  ## MVP范围
  ✅ MVP必须 / ⏳ MVP外
  ```
- 标签：`type:feature`, `module:server`, `priority:p2`

**步骤3: 澄清模糊需求**（1小时）
- 对不清晰的需求Issue进行补充
- 添加缺失的验收标准
- 明确MVP边界

**步骤4: 制定统一标准规范**（2-3小时）
- 基于Day 1的差异分析，制定统一方案
- **命名规范统一**：
  - DTO命名规范（Server ↔ Desktop）
  - 方法命名规范
  - 变量命名规范
- **架构模式统一**：
  - Repository设计模式
  - Service调用方式
  - 异常处理模式
  - 数据验证模式
- **数据模型统一**：
  - 字段映射规则
  - 类型转换标准
  - 验证规则对齐
- 更新到现有标准文档：
  - `docs/architecture/client/unified-design-standard.md`
  - `docs/architecture/server-module-design-standard.md`

**步骤5: 创建统一标准实施Issue**（30分钟-1小时）
- 为每个差异类别创建Issue
- 按优先级排序：
  - P0: 严重不一致（影响功能）
  - P1: 模式不一致（架构规范）
  - P2: 可优化项（改进建议）
- 标签：`type:refactor`, `epic:code-unification`

**步骤6: 生成完成报告**（30分钟）
- 创建：`docs/reports/2025-10-11-requirements-completion.md`
- 包含：
  - 需求覆盖度统计
  - 新增Issue清单
  - MVP需求总结
  - **统一标准方案摘要**
  - 遗留问题（需用户确认的）

#### 产出物
- 新增/更新的GitHub Issues（预计10-15个）
  - 需求类：5-10个（`type:feature`）
  - 统一标准类：5-8个（`type:refactor`）
- 更新的标准文档：
  - `docs/architecture/client/unified-design-standard.md`
  - `docs/architecture/server-module-design-standard.md`
- **新建需求文档体系**：
  - `docs/requirements/README.md` - 需求导航索引
  - `docs/requirements/mvp-scope.md` - MVP范围定义
  - `docs/requirements/modules/auth.md` - Auth模块需求（链接到Issues）
  - `docs/requirements/modules/users.md` - Users模块需求（链接到Issues）
  - ... (其他6个模块需求文档)
- 完成报告：`docs/reports/2025-10-11-requirements-completion.md`

---

## 🛠 使用工具

### MCP工具
- **serena.list_dir** - 扫描目录结构
- **serena.find_symbol** - 查找类和方法
- **serena.get_symbols_overview** - 获取类概览
- **serena.search_for_pattern** - 搜索特定模式（如`[HttpGet]`）

### GitHub CLI
- **gh issue list** - 查询Issues
- **gh issue create** - 创建Issues
- **gh issue edit** - 更新Issues

### 文档工具
- **Write** - 创建报告文档
- **Edit** - 更新索引

---

## ✅ 验收标准

### Phase 4整体验收
- [ ] 16个模块（8 Server + 8 Desktop）的代码实现清单完成
- [ ] Server-Desktop差异分析完成，差异项分类清晰
- [ ] 统一标准规范制定完成，更新到标准文档
- [ ] 每个模块的核心功能有对应的GitHub Issue
- [ ] 所有Issue包含清晰的验收标准
- [ ] MVP需求明确标记
- [ ] 不确定项已向用户确认
- [ ] 统一标准实施Issue已创建（后续执行）

### Epic #1138最终验收
- [ ] 文档数量减少35%（Phase 1完成）
- [ ] 每类信息只有1个权威文档（Phase 2完成）
- [ ] 无重复/矛盾的内容（Phase 2完成）
- [ ] 新人能在5分钟内从docs/index.md找到所需文档（Phase 3完成）
- [ ] **MVP需求清晰无歧义**（Phase 4目标）

---

## ⚠️ 关键决策点（需确认）

### 决策1: 扫描范围
- **建议**: 先扫描8个Server模块，Desktop端模块暂不扫描
- **理由**: Server端是业务逻辑核心，Desktop端是UI层，需求主要在Server端体现
- **问题**: 是否需要同时扫描Desktop端？

### 决策2: 需求文档位置 ✅ 已确认
- **方案**: **双层架构 - GitHub Issues + docs/需求文档**
- **理由**: 兼顾GitHub全程跟踪和完整文档体系可查阅，防止需求偏离

**GitHub Issues层**（单一真相源，全程跟踪）：
- 每个功能的详细需求、验收标准、讨论、进度
- 标签体系管理（module/type/priority）
- 跟踪：需求 → 实现 → 验收 → 关闭

**docs/需求文档层**（完整体系，可查阅参考）：
- 创建 `docs/requirements/README.md` - 需求导航索引
- 创建 `docs/requirements/mvp-scope.md` - MVP范围定义
- 创建 `docs/requirements/modules/` - 每个模块一个概述文件
- 每个文档链接到对应的GitHub Issues（如"详见 #1234"）

**双向同步机制**（防止需求偏离）：
- 文档引用Issue编号
- Issue关联文档位置（如"需求文档: docs/requirements/modules/auth.md"）
- 定期对照文档与Issue，确保一致性

### 决策3: MVP范围界定 ✅ 已确认
- **方案**: **识别并标记超出MVP的功能，供用户取舍**
- **理由**: 当前代码确实有超出MVP的功能，需要明确边界

**执行策略**：
1. **功能分类**：
   - 🔴 **核心MVP功能**：业务必须，无此功能系统无法运行
   - 🟡 **扩展功能**：增强体验，但非必须
   - 🟢 **高级功能**：锦上添花，可选

2. **识别标准**：
   - 核心业务流程（如登录、患者管理、开处方）→ MVP必须
   - 辅助功能（如批量导入、高级搜索、统计报表）→ 扩展功能
   - 优化功能（如快捷键、主题切换、导出PDF）→ 高级功能

3. **标记方式**：
   - 在Day 1的清单报告中，每个功能标记MVP等级
   - 在Day 2创建的Issue中，明确标注MVP范围
   - 生成"超出MVP功能清单"供用户决策

4. **取舍流程**：
   - Day 1：识别并标记所有功能的MVP等级
   - Day 2：生成"超出MVP功能清单"
   - 用户决策：保留/移除/降级
   - 后续Issue：实施用户决策（如需要）

### 决策4: 不确定项处理 ✅ 已确认
- **方案**: 标记为"需确认"，等待用户反馈，不做假设
- **理由**: 避免错误理解导致需求文档不准确

**处理流程**：
1. 扫描时遇到不确定的实现细节，标记"❓需确认"
2. 在Day 1报告中汇总所有不确定项
3. 按模块分类，按重要性排序
4. 在Day 2向用户逐项确认
5. 根据确认结果更新需求文档和Issue

**大量不确定项应对**（如>20个）：
- 分批确认：先确认影响MVP范围的
- 优先级排序：P0（影响功能）> P1（影响设计）> P2（细节问题）
- 文档化：创建"待确认清单"文档，持续跟踪

### 决策5: 扫描优先级 ✅ 已确认
- **方案**: **Desktop优先，因为Desktop存在问题相对更多**
- **理由**: 先解决问题多的部分，为统一标准提供更全面的依据

**扫描顺序**：
1. **Desktop端8个模块**（优先，问题更多）
   - ViewModel/Repository/数据绑定
   - 识别架构不一致、命名混乱等问题
2. **Server端8个模块**（随后）
   - Controller/Service/Repository
   - 作为标准参照
3. **差异分析**（最后）
   - 对比Desktop与Server的差异
   - 以Server架构标准为基准，识别Desktop需要调整的地方

**策略调整**：
- Day 1执行步骤调整为：Desktop → Server → 差异分析
- 并行处理：Desktop和Server可以部分重叠并行
- 重点关注Desktop的架构问题和不规范实现

---

## 📊 预期工作量

| 任务 | 预计时间 | 复杂度 |
|------|---------|--------|
| Day 1: 代码实现盘点（16模块+差异分析） | 10-12小时 | 中-高 |
| Day 2: 需求文档补充+统一标准+docs创建 | 6-8小时 | 中 |
| **总计** | **16-20小时** | **2-3个工作日** |

**说明**：
- Day 1增加Desktop端扫描+差异分析，时间增加60-70%
- Day 2增加docs/requirements/文档体系创建，时间增加50%
- 通过并行处理可节省40-50%时间

---

## 🎯 成功标准

Phase 4完成后，应达到：
1. ✅ 每个Server模块有清晰的功能清单
2. ✅ 每个核心功能有对应的GitHub Issue
3. ✅ 所有Issue包含验收标准
4. ✅ MVP范围明确无歧义
5. ✅ 开发者可以基于Issues理解系统功能

---

## ✅ 方案确认完成

所有5个关键决策已确认：

1. ✅ **扫描范围**: 完整扫描16个模块（8 Server + 8 Desktop）
2. ✅ **需求文档位置**: 双层架构（GitHub Issues + docs/requirements/）
3. ✅ **MVP范围界定**: 识别并标记超出MVP的功能，供用户取舍
4. ✅ **不确定项处理**: 标记"需确认"，等待用户反馈
5. ✅ **扫描优先级**: Desktop优先（问题更多）

---

## 🚀 下一步

**立即可以开始执行**：
1. 创建 Issue #1149 - Phase 4 Day 1: 代码实现盘点与差异分析
2. 启动Day 1扫描任务
3. 按顺序执行：Desktop → Server → 差异分析

---

🤖 方案编制：Phase 4 - 需求明确与完善
📅 方案已确认，准备开始执行
