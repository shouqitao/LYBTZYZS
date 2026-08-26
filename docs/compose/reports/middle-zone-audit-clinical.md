# 中间区审计 — 临床 4 页

> 日期：2026-08-23 | 基于 _pen_trees/ 元素树 vs 代码 DTO/ViewModel/XAML 核对
> 枚举基线：`MedicalCaseStatus = Suspended(已挂起)/Active(进行中)/Completed(已完成)`；`RegistrationStatus = Waiting(等待中)/InProgress(接诊中)/Completed(已完成)/Cancelled(已取消)`

---

## 1. medical-case.pen（医案工作台）

**框架**：三列（左侧栏 300px / 中间栏 / 右侧栏 240px）+ 工作流步骤条

### ✅ 保留要点
| 元素 | 代码依据 |
|------|----------|
| 右侧栏 挂起按钮（挂起/稍后处理） | `SuspendCommand`（Suspended 状态，MC-D20） |
| 右侧栏 完成按钮（完成接诊并生成处方） | `CompleteCommand`（完成看诊） |
| 右侧栏 打印按钮（打印处方） | `PrintCommand` |
| 右侧栏 保存按钮 | `SaveCommand` |
| 工作流步骤条：接诊→诊断→处方→完成 | `WorkflowStepIndicator` 控件 |
| 处方表头：序号/药材名称/剂量/用法 | `PrescriptionItemDto.HerbName/Dosage/Usage` |
| 剂数选择（共 7 剂） | `PrescriptionDetailDto.DosageCount` |
| 处方脚注：煎服法/合计金额/×7剂 | `Usage/TotalPrice/DosageCount` |
| 中间栏四诊：现病史/舌诊/脉诊/中医诊断 | `ConsultationDetailDto` 四字段 |
| 常用方剂卡（点击引用至处方） | `ImportFormulaCommand` + FormulaImportDialog |
| 医嘱与调护卡片 | `PrescriptionDetailDto.Advice`（医嘱字段） |

### ❌ 编造待删（精确到节点）
| 节点 | 原因 |
|------|------|
| `工具栏 > 新增患者按钮` | 医案不支持新建（BR-000）；代码无 CreateCommand |
| `工具栏 > 导入按钮` | 代码无对应命令 |
| `工具栏 > 导出按钮` | 代码无对应命令 |
| `工具栏 > 批量删除按钮` | MedicalCaseCommandsViewModel 无批量删除命令 |
| `顶部工具栏`（凌隐宝堂·中医医案工作台 + 用户头像 + 时间） | 与 Shell TopBar 重复；框架规定顶部栏属 Shell |
| `患者信息卡 > 过敏行（药物过敏: 青霉素）` | PatientDetailDto 无过敏字段；v1.0 无此数据 |
| `就诊信息卡 > 就诊科室行（脾胃病科 · 门诊2号诊室）` | 代码无 Department/诊室 字段 |
| `就诊信息卡 > 既往病史行（慢性胃炎3年）` | ConsultationDetailDto 无既往史字段（需求列选填但代码未实现） |
| `医嘱提示卡 > 复诊提醒行（复诊提醒已开启 · 2周后）` | 无此功能 US，无代码实现 |
| `患者信息卡 > 病历号（MZ-20260214-0087）` | PatientDetailDto 无病历号；应为 MedicalCase.CaseNumber 或移除 |

### ⚠️ 改标
| 位置 | 现值 | 应为 |
|------|------|------|
| 右侧栏 保存按钮 | 保存草稿 | 暂存医案（MC-D20 Draft→Suspended 已重命名）或 保存医案 |
| 右侧栏 挂起按钮 | 挂起（稍后处理） | ✅保留（语义一致，Suspended=已挂起） |
| 右侧栏 完成按钮 | 完成接诊并生成处方 | 完成看诊（代码按钮 Content="完成看诊"） |
| 右侧栏 打印按钮 | 打印处方 | 打印处方单（代码 Content="打印处方单"） |
| 中间栏 舌诊/脉诊右侧 | — | 无改标，保留（代码有 TongueDiagnosis/PulseDiagnosis） |
| 处方表格 "用法"列 + "备注"列 | 用法 + 备注 | 用法说明(Usage) + 煎法(DecocteMethod)；DTO 无备注字段 |
| 就诊状态徽章（就诊信息卡） | 接诊中 | 进行中（MedicalCaseStatus.Active 说明文字="进行中"） |

---

## 2. medical-case-management.pen（医案管理）

**框架**：列表 + 右侧详情面板（360px）

### ✅ 保留要点
| 元素 | 代码依据 |
|------|----------|
| 搜索框（搜索患者姓名/病历号） | `SearchBox.Placeholder`（代码为"搜索患者姓名/诊断..."） |
| 新建医案按钮（内层顶部栏） | ⚠️ → 见下方 ❌ |
| 表格列：患者姓名 | `MedicalCaseListDto.PatientName` |
| 表格列：性别/年龄 | `PatientGender/PatientAge` |
| 表格列：诊断 | `Diagnosis` |
| 表格列：就诊日期 | `CreatedAt` |
| 表格列：医师 | `DoctorName` |
| 表格列：状态 | `CaseStatus`（MedicalCaseStatus） |
| 分页栏（共 N 条/每页 N 条） | `UnifiedPaginationBar` |
| 详情面板：姓名/性别/年龄/联系电话 | PatientDetailDto 字段 |
| 详情面板：辨证（TcmDiagnosis） | `ConsultationDetailDto.TcmDiagnosis` |
| 详情面板：方剂名/药材列表 | `PrescriptionDetailDto.ReferencedFormulas/Items` |

### ❌ 编造待删
| 节点 | 原因 |
|------|------|
| `工具栏 > 新增患者按钮` | 医案模块无此功能；属于 Patients 模块 |
| `工具栏 > 导入按钮` | 代码无对应命令 |
| `工具栏 > 导出按钮` | 代码无对应命令 |
| `工具栏 > 批量删除按钮` | MedicalCaseMasterDetailControl Toolbar 有 BatchDeleteCommand ✓，但外层模板工具栏非本页 UI；应由内层 DataGridToolbar 承载 |
| `顶部栏 > 新建医案按钮` | XAML 注释："医案不支持新建，新建通过挂号入口创建"（BR-000） |
| `表格列 处方`（值如"柴胡疏肝散加减"） | `MedicalCaseListDto` 仅有 `HasPrescription` bool，无方剂名称字段；列内容系编造 |
| `详情面板 > 辨证信息 > 治法行` | `ConsultationDetailDto` 无治法字段 |
| `表格行 > 操作列`（查看/编辑/更多图标） | 代码为右键菜单 EditCommand/DeleteCommand，无行内图标；更多操作无命令 |

### ⚠️ 改标
| 位置 | 现值 | 应为 |
|------|------|------|
| 表格列 头 病历号 | 病历号 | 案例编号（`CaseNumber` DisplayName="案例编号"） |
| 表格列 头 就诊日期 | 就诊日期 | 日期（代码 Header="日期"，绑定 CreatedAt） |
| 状态值 待复诊 | 待复诊 | 已挂起（MedicalCaseStatus.Suspended；无"待复诊"枚举值） |
| 搜索提示文字 | 搜索患者姓名、病历号... | 搜索患者姓名/诊断...（代码 SearchBox Placeholder） |
| 详情面板 > 主诉行 | 主诉 | ⚠️ ConsultationDetailDto 无主诉字段；需求 D8 列为必填但代码未实现——记录为缺口 |
| 表格头 操作（列） | 操作 | ⚠️ 建议移除，操作改为右键菜单（与代码一致） |

---

## 3. clinical-workspace.pen（临床工作台）

**框架**：三列（待诊队列 248px / 诊疗区 / 常用验方 260px）

### ✅ 保留要点
| 元素 | 代码依据 |
|------|----------|
| 左侧待诊队列（今日待诊 + 患者列表 + 接诊按钮） | `PendingQueueView` / `PatientSelectionView` 待诊队列 |
| 队列患者项：编号/姓名/性别年龄 | `QueueNumber/PatientName/Gender/Age` |
| 操作按钮：开始看诊 | `ClinicalWorkspaceViewModel.StartConsultationCommand` |
| 操作按钮：新建患者 | `NewPatientCommand` |
| 诊断录入区：舌诊/脉诊/中医诊断 + 添加 | `ConsultationDetailDto.TongueDiagnosis/PulseDiagnosis/TcmDiagnosis` |
| 处方编辑区：药材名称/剂量/用法/操作 | `PrescriptionItemDto` |
| 右侧常用验方列表 | `ImportFormulaCommand`（验方引用，US-MC-016） |
| 右侧历史医案列表 | `US-MC-008/009` 历史医案查询 |

### ❌ 编造待删
| 节点 | 原因 |
|------|------|
| `工具栏 > 新增患者按钮` | 不属于临床工作台直接功能 |
| `工具栏 > 导入按钮` | 代码无对应命令 |
| `工具栏 > 导出按钮` | 代码无对应命令 |
| `工具栏 > 批量删除按钮` | 代码无对应命令 |
| `Header Bar > Workflow Steps > 收费` | v1.0 无收费模块（REG-BR-006 叫号 v2.0；收费模块不存在） |
| `Header Bar > Logo/系统名/用户区` | 与 Shell TopBar 48px 完全重复；框架规定顶栏属 Shell |
| `队列状态 Badge > 优先` | RegistrationStatus 无"优先"枚举值；仅 Waiting/InProgress/Completed/Cancelled |
| `患者信息 > 病历号(P2024-00156)` | PatientDetailDto 无病历号字段 |
| `患者信息 > 初诊/内科/城镇医保` | 代码无 VisitType/Department/Insurance 字段 |
| `过敏警示（重复出现两次）` | UI 元素重复（同一节点两次渲染）；且 PatientDetailDto 无过敏字段 |
| `过敏史列表内容（青霉素·磺胺类·花粉）` | 无过敏数据字段 |
| `Action Bar > 保存草稿按钮` | MC-D20 重命名；代码为暂存医案（SuspendCommand） |
| `右侧历史医案 > 疗效:好转/痊愈` | DTO 无疗效/转归字段（Outcome field 不存在） |

### ⚠️ 改标
| 位置 | 现值 | 应为 |
|------|------|------|
| 队列状态Badge 就诊中 | 就诊中 | 接诊中（RegistrationStatus.InProgress 说明="接诊中"） |
| 队列状态Badge 候诊 | 候诊 | 等待中（RegistrationStatus.Waiting 说明="等待中"） |
| 已等候 X 分 | 已等候15分 | ⚠️ 可由 CreatedAt 推导，保留但标注 mock 属性 |
| Action Bar 保存草稿 | 保存草稿 | 暂存医案（SuspendCommand）或 保存医案 |
| Action Bar 打印处方 | 打印处方 | 打印处方单（代码 Content="打印处方单"） |
| Action Bar 完成问诊 | 完成问诊 | 完成看诊（代码 Content="完成看诊"） |
| Header Bar 步骤 问诊/诊断/处方 | 问诊→诊断→处方 | ⚠️ 建议对齐 WorkflowStepIndicator 步骤（接诊→诊断→处方→完成） |

---

## 4. registration.pen（挂号管理）

**框架**：列表 + 右侧详情面板（380px）

### ✅ 保留要点
| 元素 | 代码依据 |
|------|----------|
| 搜索框（搜索患者姓名/手机号） | `RegistrationListViewModel.SearchText` |
| 新建挂号按钮 | `CreateRegistrationCommand`（打开弹窗） |
| 表格列：患者姓名 | `RegistrationListDto.PatientName` |
| 表格列：指定医生 | `DoctorName`（DisplayName="医生姓名"） |
| 表格列：状态 | `Status`（RegistrationStatus） |
| 表格列：挂号时间 | `CreatedAt` |
| 表格列：费用 | `RegistrationFee` |
| 分页栏 | 代码有分页模式 |
| 详情面板：手机号（脱敏） | `PatientDetailDto.PhoneNumber` |
| 详情面板：挂号费用 ¥80.00 | `RegistrationFee`（REG-BR-009） |
| 详情面板：排队号 A-009 | `QueueNumber`（int） |
| 详情面板：备注卡片 | `RegistrationDetailDto.Remark`（备注字段存在） |
| 列表状态过滤器 全部状态 | Status 字段可用（筛选逻辑代码待补，但字段存在） |

### ❌ 编造待删
| 节点 | 原因 |
|------|------|
| `工具栏 > 新增患者按钮` | 属于 Patients 模块，不属于 Registration 模块 |
| `工具栏 > 导入按钮` | 代码无对应命令 |
| `工具栏 > 导出按钮` | 代码无对应命令 |
| `工具栏 > 批量删除按钮` | 代码无批量删除命令 |
| `内层工具栏 > 批量操作按钮` | `RegistrationListViewModel` 无批量命令（仅 Refresh/Create/StartVisit/Cancel） |
| `列表头部筛选 > 全部类型` | RegistrationSource 无"挂号类型"概念（仅有来源=前台挂号/医生看诊）；"全部类型" ❌ 应为"全部来源" |
| `详情面板 > 挂号单号 GH20260815009` | `RegistrationDetailDto` 仅存 Guid Id；无人类可读挂号单号格式 |
| `详情面板 > 病历号 P20240815001` | `PatientDetailDto` 无病历号字段 |
| `详情面板 > 过敏史 青霉素过敏` | `PatientDetailDto` 无过敏字段 |
| `详情面板 > 就诊第 12 次` | DTO 无就诊次数字段 |
| `表头 > 操作列` | 代码操作为工具栏级（接诊/取消挂号），非行内操作图标 |

### ⚠️ 改标
| 位置 | 现值 | 应为 |
|------|------|------|
| 表格列头 挂号类型 | 挂号类型 | 挂号来源（`Source` DisplayName="挂号来源"；值为前台挂号/医生看诊） |
| 详情面板 挂号类型行（值"专家门诊"） | 挂号类型:专家门诊 | 挂号来源:前台挂号 或 医生看诊（RegistrationSource 枚举） |
| 详情面板 就诊状态 | 就诊状态（待就诊） | 挂号状态（代码 DisplayName="挂号状态"） |
| 状态值 待就诊 | 待就诊 | 等待中（RegistrationStatus.Waiting 说明="等待中"） |
| 列表头部筛选 全部类型 | 全部类型 | 全部来源（映射 Source 字段） |
| 表格列 排队号 | 缺失（仅在详情显示） | ⚠️ 建议补充排队号列（QueueNumber，代码注册列表首列） |
| 详情面板 详情头部 编辑/更多图标 | 编辑图标 + 更多功能图标 | ⚠️ 代码无编辑命令（仅 CancelRegistration）；建议移除或改为取消按钮 |
| 表格列 操作列头 | 操作 | ⚠️ 建议移除，操作通过工具栏按钮接诊/取消挂号触发 |

---

## 汇总修改点清单（属性级）

### 🔴 编造删除（❌ 共 23 项）

| # | 页面 | 精确节点路径 | 说明 |
|---|------|-------------|------|
| 1 | 全部4页 | `工具栏 > 新增患者按钮` | 模板复制，不属于本页功能 |
| 2 | 全部4页 | `工具栏 > 导入按钮` | 代码无对应命令 |
| 3 | 全部4页 | `工具栏 > 导出按钮` | 代码无对应命令 |
| 4 | 全部4页 | `工具栏 > 批量删除按钮` | 代码无对应命令（模板复制） |
| 5 | medical-case | `患者信息卡 > 过敏行（药物过敏: 青霉素）` | 无过敏字段 |
| 6 | medical-case | `就诊信息卡 > 就诊科室行（脾胃病科）` | 无 Department 字段 |
| 7 | medical-case | `就诊信息卡 > 既往病史行` | ConsultationDetailDto 无此字段 |
| 8 | medical-case | `医嘱提示卡 > 复诊提醒行` | 无此功能 |
| 9 | medical-case | `患者信息卡 > 病历号（MZ-...）` | 无病历号字段 |
| 10 | medical-case | `顶部工具栏`（品牌+用户区） | 与 Shell TopBar 重复 |
| 11 | medical-case-management | `顶部栏 > 新建医案按钮` | BR-000 医案不支持新建 |
| 12 | medical-case-management | `表格列 处方`（值"柴胡疏肝散..."） | ListDto 仅有 HasPrescription bool |
| 13 | medical-case-management | `详情面板 > 治法行` | ConsultationDetailDto 无此字段 |
| 14 | clinical-workspace | `Header Bar > 步骤 > 收费` | v1.0 无收费模块 |
| 15 | clinical-workspace | `Header Bar > Logo/系统名/用户区` | 与 Shell TopBar 重复 |
| 16 | clinical-workspace | `队列 Badge > 优先` | RegistrationStatus 无此枚举值 |
| 17 | clinical-workspace | `患者信息 > 病历号` | PatientDetailDto 无此字段 |
| 18 | clinical-workspace | `患者信息 > 初诊/内科/城镇医保` | 无 VisitType/Department/Insurance |
| 19 | clinical-workspace | `过敏警示（重复节点）` | UI 重复渲染 + 无过敏字段 |
| 20 | clinical-workspace | `历史医案 > 疗效:好转/痊愈` | 无疗效/转归字段 |
| 21 | registration | `内层工具栏 > 批量操作按钮` | RegistrationListViewModel 无批量命令 |
| 22 | registration | `详情面板 > 挂号单号 GH...` | 无此字段（仅 Guid Id） |
| 23 | registration | `详情面板 > 病历号/过敏史/就诊第N次` | PatientDetailDto 无对应字段 |

### 🟡 改标（⚠️ 共 19 项）

| # | 页面 | 节点 | 现值 → 应为 |
|---|------|------|------------|
| 1 | medical-case | 右侧栏 保存按钮 | 保存草稿 → 暂存医案（MC-D20） |
| 2 | medical-case | 右侧栏 完成按钮 | 完成接诊并生成处方 → 完成看诊 |
| 3 | medical-case | 右侧栏 打印按钮 | 打印处方 → 打印处方单 |
| 4 | medical-case | 就诊状态徽章 | 接诊中 → 进行中（Active） |
| 5 | medical-case | 处方表格用法/备注列 | 用法 + 备注 → 用法说明(Usage) + 煎法(DecocteMethod) |
| 6 | medical-case-management | 表格头 病历号 | 病历号 → 案例编号（CaseNumber） |
| 7 | medical-case-management | 表格头 就诊日期 | 就诊日期 → 日期（代码 Header="日期"） |
| 8 | medical-case-management | 状态值 | 待复诊 → 已挂起（Suspended） |
| 9 | clinical-workspace | 队列Badge 就诊中 | 就诊中 → 接诊中（InProgress） |
| 10 | clinical-workspace | 队列Badge 候诊 | 候诊 → 等待中（Waiting） |
| 11 | clinical-workspace | 按钮 保存草稿 | 保存草稿 → 暂存医案 |
| 12 | clinical-workspace | 按钮 完成问诊 | 完成问诊 → 完成看诊 |
| 13 | registration | 表格头 挂号类型 | 挂号类型 → 挂号来源（Source；值=前台挂号/医生看诊） |
| 14 | registration | 详情面板 挂号类型行 | 专家门诊 → 前台挂号/医生看诊 |
| 15 | registration | 详情面板 就诊状态 | 就诊状态 → 挂号状态 |
| 16 | registration | 状态值 | 待就诊 → 等待中（Waiting） |
| 17 | registration | 筛选器 全部类型 | 全部类型 → 全部来源 |
| 18 | registration | 表格缺失列 | ⚠️ 建议补充排队号(QueueNumber)列 |
| 19 | medical-case-management | 详情面板 主诉行 | ⚠️ 需求 D8 列必填但代码 ConsultationDetailDto 无主诉字段——缺口 |
