# 中间内容区审计目录：patient-list / herb-management / formula-management

> 审计对象：`designs/patient-list.pen`、`designs/herb-management.pen`、`designs/formula-management.pen`（各含展开/收拢两帧；本审计覆盖「主内容区」节点 = 工具栏 + 内容区，不含三栏框架）
> 方法：Python 解析 .pen 展开帧 → 递归列出主内容区元素树（305/305/263 节点）→ 逐元素对照 `docs/02-requirements/04-patients.md、05-herbs.md、06-formulas.md` 与 Desktop 代码 View。
> 图例：✅=有 US+有代码，保留 | ⚠️=需求有但画错形态，改标 | ❌=无需求无代码，编造待删
> 日期：2026-08-23 | 结论统计：✅ 保留 34 · ⚠️ 改标 21 · ❌ 编造待删 41

---

## 〇、总体结论（先读这个）

1. **patient-list.pen 中间区质量最好**：新模板（外层 pad16/gap16、工具栏56 r12、卡片 r12 描边 #E9DFD7）已套用，结构可保留；遗留问题集中在**编造数据字段**（病历号 MZ- 前缀、会员等级、过敏史明文、最近就诊列、状态取值 复诊中/在治/新患者/已治愈）与**编造面板**（就诊历史卡、处方记录卡）。前次审计报告（`design-requirements-trace-audit-patient-list.md`）指出的 B11/B12/B14/B18/B19/B20/B22 等问题**在 .pen 中均未修复**。
2. **herb-management.pen 与 formula-management.pen 的中间区整体是旧版自生成稿**：新模板只有一层空壳——「工具栏(56 r12)」内是患者页的按钮组，「内容区」里嵌着一整套旧布局（各自带顶部工具栏+侧边栏+独立详情面板），与三栏框架重复。这两页需要按 patient-list 定稿模板**重做中间区**，而非局部修补。

---

## 一、patient-list.pen（患者管理）

### P-A 工具栏（h56 r12）

| 页面 | 元素 | 类型 | 现状值 | 追溯结论 | US编号 | 代码文件 | 建议动作 |
|---|---|---|---|---|---|---|---|
| patient-list | 主内容区容器 | frame | layout=vertical, pad=[16,16,16,16], gap=16, fill=$bg-warm | ✅ | US-SHELL（框架规范） | designs/patient-list.pen 定稿母版 | 保留 |
| patient-list | 工具栏容器 | frame | h=56, r=12, stroke=#E9DFD7 1px, fill=$surface | ✅ | 同上 | 同上 | 保留 |
| patient-list | 搜索框 | frame+icon+text | w340 h40 r20；占位文字「搜索姓名 / 手机号 / 身份证号」 | ⚠️ | US-PAT-001/014 | PatientMasterDetailControl.xaml SearchBox Placeholder="搜索患者姓名/手机号..."；US-PAT-001 还有拼音首字母搜索 | 保留节点；占位 content 改为「搜索 姓名/手机号/拼音码」（身份证走刷卡录入，不必入占位） |
| patient-list | 筛选按钮 | frame+icon(tune)+text「筛选」 | — | ❌ | 无（US-PAT-001 仅关键字搜索，无筛选器） | 无筛选 UI/命令 | 删除整个「筛选按钮」frame |
| patient-list | 新增患者按钮 | frame+icon(add)+text「新增患者」 | fill=$primary | ✅ | US-PAT-003 | DataGridToolbar CreateCommand→CreateNewCommand (PatientMasterDetailControl.xaml:65) | 保留 |
| patient-list | 导入按钮 | frame+icon(upload_file)+text「导入」 | — | ✅ | US-PAT-011 | ImportPatientsCommand (PatientMasterDetailViewModel.cs:309) | 保留 |
| patient-list | 导出按钮 | frame+icon(download)+text「导出」 | — | ✅ | US-PAT-012 | ExportPatientsAsync (:359) | 保留 |
| patient-list | 弹性占位 | frame | fill_container | ✅ | 布局惯例 | DataGridToolbar | 保留 |
| patient-list | 批量删除按钮 | frame+icon(delete)+text「批量删除」 | w96 h36 fill=#B00020 | ⚠️（形态） | US-PAT-008 | DataGridToolbar BatchDeleteCommand→DeleteCommand(:62)；另有批量启用/禁用 BatchEnable/BatchDisable（US-PAT-006） | 保留；样式对齐工具栏体系（r12 系），并在其旁**补画「批量启用」「批量禁用」「刷新」「更多(恢复/刷卡录入)」**——代码有、设计漏 |

### P-B 患者列表卡片（w880）

| 页面 | 元素 | 类型 | 现状值 | 追溯结论 | US编号 | 代码文件 | 建议动作 |
|---|---|---|---|---|---|---|---|
| patient-list | 患者列表卡片 | frame | w880, r12, stroke=#E9DFD7 | ✅ | US-PAT-001/002 | MasterDetailLayout MasterContent | 保留 |
| patient-list | 顶部装饰条 | frame | h3 fill=$accent | ⚠️ | — | DataGrid 无此装饰；属纯视觉元素 | 可保留为装饰，但需在设计文档标注「非数据元素」；最小忠实方案=删除 |
| patient-list | 卡片标题行「患者列表」 | text | fontSize16 600 | ⚠️ | — | Master 区无卡片标题（DataGridToolbar 即标题行）；页面上下文由面包屑承载（Breadcrumbs） | 删除标题行或改面包屑「首页 / 患者管理」（对应代码 HeaderContent） |
| patient-list | 总数徽标「共 1,286 人」 | frame+text | — | ⚠️ | US-PAT-001（返回总数） | UnifiedPaginationBar 文案="共 {TotalCount} 条记录" | 徽标删除；总数并入分页栏文案「共 X 条记录」 |
| patient-list | 今日新增提示「今日新增 3 人」 | frame+icon(favorite)+text | — | ❌ | 无（grep 今日新增/TodayNewCount=0） | 无字段无命令 | 删除整个「今日新增提示」frame |
| patient-list | 表头行 | frame | h44 fill=#F7F1EB | ✅ | US-PAT-001 | DataGrid HeadersVisibility=Column | 保留 |
| patient-list | 表头列「患者」 | text | w230 | ✅ | US-PAT-001 | DataGrid 姓名 Column（Name） | 保留列；但单元格内的病历号见下 |
| patient-list | 行内「姓名+头像+病历号」 | frame 组 | 头像34 r17+姓氏字；病历号 MZ-2026001…008 | ⚠️+❌ | US-PAT-001 | DataGrid 无头像列；PatientListDto **无病历号字段**，MZ- 前缀全库=0（医案编号前缀为 MC） | 头像列删或标注装饰（倾向删）；每行「病历号」text 节点全部删除（8 行×1） |
| patient-list | 表头列「性别」 | text | w90 | ✅ | US-PAT-001 | Gender Column（EnumDesc） | 保留 |
| patient-list | 表头列「年龄」 | text | w90 | ✅ | US-PAT-001 | Age Column StringFormat={}{0}岁 | 保留；示例值「68 岁」格式 OK |
| patient-list | 表头列「联系电话」 | text | w162 | ⚠️ | US-PAT-001/013 | 代码列名「手机号」（PhoneNumber）；DTO 为脱敏返回 | 保留列；表头文字「联系电话」改「手机号」，示例值改脱敏形态（如 138****4477） |
| patient-list | 表头列「最近就诊」+行日期值 | text | 值如 2026-01-12 | ❌（列表列） | 无（患者**列表** DTO 无此字段；「最近就诊记录」仅存在于临床工作台 ClinicalWorkspaceViewModel.PatientHistory，非患者管理页） | PatientListDto(Id/Name/Gender/Age/PhoneNumber/PinYinCode/Status/CreatedAt) 无最近就诊；DataGrid 无此列 | 删除表头单元格 + 8 个行的「就诊日期单元格」 |
| patient-list | 表头列「状态」 | text | w128 | ✅ | US-PAT-006 | DataGridTemplateColumn Status | 保留列 |
| patient-list | 行状态徽标取值「复诊中/在治/新患者/已治愈」 | ellipse+text | 4 种取值 5 色 | ❌（取值编造） | US-PAT-006 | CommonStatus 枚举仅 Disabled=0「禁用」/Enabled=1「启用」（SystemEnums.cs） | 8 个状态徽标的 status 文字全部改为「启用/禁用」二值（配色同步归一：启用=成功色、禁用=中性色） |
| patient-list | 数据行 ×8 | frame | h52 分隔线 #F0E7E0 | ✅ | US-PAT-001 | DataGrid 行 | 保留结构 |
| patient-list | 分页栏「显示 1-8 / 共 1,286 位患者」 | text | — | ⚠️ | US-PAT-001 | UnifiedPaginationBar（首页/上一页/x ÷ y/下一页/末页+每页条数 ComboBox） | 文案改「共 X 条记录」；补画首页/末页按钮与「每页 N 条」选择器；页码用 x/y 而非 1 2 3 … 43 按钮串 |
| patient-list | 页码按钮串 1/2/3/…/43 | frame×5 | — | ⚠️ | 同上 | 同上 | 形态改标（见上） |

### P-C 患者详情面板

| 页面 | 元素 | 类型 | 现状值 | 追溯结论 | US编号 | 代码文件 | 建议动作 |
|---|---|---|---|---|---|---|---|
| patient-list | 患者详情面板容器 | frame | r12 stroke=#E9DFD7 pad16 gap12 | ✅ | US-PAT-002 | MasterDetailLayout DetailContent（HasSelection→ShowDetailPanel） | 保留 |
| patient-list | 大头像48+姓氏字 | frame+text | — | ⚠️ | — | 详情区实为 DetailToolbar（标题+取消/删除/编辑/保存）+ Transitioner | 删除大头像；头部改为 DetailToolbar 形态 |
| patient-list | 「病历号 MZ-2026001 · 建档于 2024-03-18」 | text | — | ⚠️（拆分） | CreatedAt 部分=US-PAT-002 | PatientDetailDto 无病历号；CreatedAt 有（PatientViewControl 创建时间） | 「病历号 MZ-2026001 · 」删除；保留「建档于 YYYY-MM-DD」 |
| patient-list | 详情状态徽标「复诊中」 | frame+ellipse+text | — | ❌（取值） | US-PAT-006 | CurrentDetail.Status=CommonStatus | 文字改「启用/禁用」 |
| patient-list | 编辑 icon | icon(edit) | — | ⚠️ | US-PAT-004 | DetailToolbar EditCommand/SaveCommand/CancelCommand/DeleteCommand | 编辑入口改为 DetailToolbar 按钮组形态（编辑/保存/取消/删除） |
| patient-list | 信息行1：性别/年龄/联系电话 | 信息项×3 | — | ✅（电话标签⚠️） | US-PAT-002/013 | PatientViewControl 性别/年龄/手机号码 | 保留；「联系电话」标签改「手机号码」 |
| patient-list | 身份证号 3301**********2246 | 信息项 | Partial 脱敏 前4后4 | ✅ | US-PAT-013 | PatientViewControl IdNumber + [SensitiveData] 序列化掩码 | 保留（脱敏示例正确） |
| patient-list | 「会员等级 金卡会员」 | 信息项 | — | ❌ | 无（全库 grep 会员/MemberLevel/VipLevel=0） | 无字段 | 删除该信息项（标签 text + 值 text） |
| patient-list | 「过敏史 青霉素过敏」明文 | 信息项 | — | ❌ | 违背 US-PAT-013（AllergyHistory 为 Hash 脱敏仅可比对不可还原） | PatientDetailDto/PatientViewControl 均无过敏史展示 | 删除该信息项 |
| patient-list | 就诊历史卡片（标题+查看全部+3 条记录：日期块/主诉/医师·方名·剂数） | frame 组 | 「查看全部」链接×1 + 就诊记录×3 | ❌ | 无此嵌入面板；查看医案是跳转动作（右键菜单 ViewMedicalRecordsCommand/NewConsultationCommand，后者为 FUTURE stub US-MC-010） | 无内嵌历史 UI | 删除整张「就诊历史卡片」。如要表达医案关联，改为一个「查看医案」入口按钮（对应既有命令） |
| patient-list | 处方记录卡片（标题+查看全部+3 条：方名/用法·开具日/「已取药」徽标） | frame 组 | 「已取药」徽标×3 | ❌ | 无（处方明细属医案/处方域；「取药/Dispensary」概念全库=0） | 无 | 删除整张「处方记录卡片」 |
| patient-list | GridSplitter（分割线+拖拽柄） | frame | w1/#E9DFD7 + 拖拽柄 6×48 #D9A05B | ✅ | US-PAT-002 | MasterDetailLayout 左右分割 GridSplitter | 保留 |

---

## 二、herb-management.pen（药材管理）

### H-0 结构性问题（最高优先级）

| 页面 | 元素 | 类型 | 现状值 | 追溯结论 | US编号 | 代码文件 | 建议动作 |
|---|---|---|---|---|---|---|---|
| herb | 工具栏(56 r12) 内按钮组 | frame | 内容是患者页的：搜索(姓名/手机号/身份证号)+筛选+新增患者+导入+导出+批量删除 | ❌（整组错页） | — | HerbMasterDetailControl 工具栏应为：新增/刷新/导出/批量启用/批量禁用/批量删除/模板/导入 + 搜索占位「搜索药材名称/拼音码...」 | 整组重做（见修改点清单 H-R1） |
| herb | 内容区内嵌「顶部工具栏」（Logo+系统标题+/药材管理+新增药材/导入/导出） | frame | h56 fill=$bg-card | ❌（与三栏框架重复） | — | 页面级无顶栏；品牌在侧栏 | 删除整个「顶部工具栏」frame |
| herb | 内容区内嵌「侧边栏」（导航菜单：药材管理/患者管理/预约排班/处方管理/库存盘点/收费结算/统计报表/系统设置/帮助中心） | frame | w240 | ❌（与三栏框架重复且菜单项编造——侧栏实际仅「主页」1 项，NavigationManager.cs:48-77） | US-SHELL-005 | NavigationManager.cs | 删除整个「侧边栏」frame |
| herb | 内容区缺 GridSplitter/主从分割 | — | 详情面板直接拼接（strokeWidth={'left':1}） | ⚠️ | US-HERB-002 | HerbMasterDetailControl 用 MasterDetailLayout（同 patient-list 结构） | 按 patient-list 定稿结构重排：列表卡(w880) + GridSplitter + 详情卡 |

### H-A 列表区（内嵌「内容区>搜索栏/统计标签栏/表格容器/分页栏」）

| 页面 | 元素 | 类型 | 现状值 | 追溯结论 | US编号 | 代码文件 | 建议动作 |
|---|---|---|---|---|---|---|---|
| herb | 搜索栏「搜索药材名称、拼音或功效…」+筛选按钮 | frame | — | ⚠️（占位措辞）+❌（筛选） | US-HERB-001（名称/拼音首字母/Category 分类筛选——分类筛选是后端参数，UI 现无独立筛选按钮） | SearchBox Placeholder="搜索药材名称/拼音码..." | 占位改「搜索药材名称/拼音码...」；「筛选」按钮删除（如要表达 Category 筛选须先立 UI 需求） |
| herb | 统计标签栏 全部128/在库96/缺货8/停用24 | frame×4 | — | ❌（在库/缺货=库存概念） | 记录模式明确**不涉及库存**（05-herbs.md 模块级规则「Record-Only：无库存量」）；Status 只有启用/禁用 | CommonStatus 枚举 | 删除整条「统计标签栏」（若要状态过滤须先立 US） |
| herb | 表格容器 r8 | frame | — | ⚠️ | — | 代码为 DataGrid（无卡片式圆角容器） | 重排进列表卡（r12 描边卡） |
| herb | 表头列「药材名称」 | text | — | ✅ | US-HERB-001 | DataGrid 名称 Name | 保留 |
| herb | 表头列「拼音」 | text | — | ⚠️ | US-HERB-001（PinyinAbbreviation） | DataGrid 拼音码 PinYinCode（缩写码，如 dq，非全拼 huang qi） | 保留列；表头改「拼音码」，示例值改缩写码形态（如 HQ/DQ） |
| herb | 表头列「性味」 | text | — | ❌ | US-HERB-002 详情字段性味(Properties)，**列表 DTO 无** | HerbMasterDetailControl DataGrid 列=名称/拼音码/规格/单位/零售价/状态 | 删除表头+10 行的性味单元格（列表不显示；移到详情面板） |
| herb | 表头列「归经」 | text | — | ❌ | 同上（列表 DTO 无） | 同上 | 删除表头+10 行的归经单元格 |
| herb | 表头列「功效」 | text | — | ❌ | 同上（Effect 属详情字段） | 同上 | 删除表头+10 行的功效单元格 |
| herb | 表头列「状态」 | text | 取值「在库/缺货/停用」 | ⚠️（取值错） | US-HERB-010 | DataGridTemplateColumn Status=CommonStatus 启用/禁用 | 保留列；取值全部改「启用/禁用」 |
| herb | 表头列「库存」 | text | 值如 256 kg/0 kg | ❌ | Record-Only 无库存概念（05-herbs.md 横切规则） | HerbDto 无库存字段 | 删除表头+10 行库存单元格 |
| herb | 表头列「操作」（visibility/edit/delete 三图标） | frame×10 | — | ❌（形态错） | US-HERB-004/005/010 | 操作入口=DataGrid 行右键菜单（编辑/复制/切换状态/恢复/删除），无行内图标列 | 删除操作列；操作以右键菜单/详情工具栏承载（可在设计稿以注释说明） |
| herb | 缺失列：规格/单位/零售价 | — | 设计未画 | ⚠️（漏画） | US-HERB-001/002 | DataGrid 规格Spec/单位Unit/零售价Price 三列 | 补画三列 |
| herb | 缺失控件：复选框列/行选中 | — | 设计未画 | ⚠️（漏画） | US-HERB-012 | ShowCheckBoxColumn=True（批量操作依赖勾选） | 补画行首复选框 |
| herb | 分页栏「共 128 条记录，第 1/13 页」 | text+分页控件 | 页码方块 1-5 | ⚠️ | US-HERB-001 | UnifiedPaginationBar | 文案/形态对齐：「共 X 条记录」+首页/上一页/x÷y/下一页/末页+每页 N 条 |

### H-B 详情面板（w340 内嵌版）

| 页面 | 元素 | 类型 | 现状值 | 追溯结论 | US编号 | 代码文件 | 建议动作 |
|---|---|---|---|---|---|---|---|
| herb | 详情头部「药材详情」+close icon | frame | — | ⚠️ | US-HERB-002 | DetailToolbar Title={Binding DetailTitle}（取消/删除/编辑/保存），无 close 收起交互 | 改 DetailToolbar 形态 |
| herb | 药材图片区（local_florist 图+黄芪+Astragalus membranaceus 拉丁名） | frame | — | ❌（拉丁学名）+⚠️（图标块） | 无拉丁名字段（grep=0）；HerbDetailModel 无图片字段 | HerbViewControl | 删除拉丁名 text；图片区可保留为装饰性图标块（标注非数据），最小忠实=删 |
| herb | 字段-拼音 huang qi | 信息项 | — | ⚠️ | US-HERB-002 | PinYinCode 为拼音**缩写码** | 保留项；label「拼音码」，值改缩写码形态 |
| herb | 字段-性味 甘，微温 | 信息项 | — | ✅ | US-HERB-002（性味归经功效是详情核心字段） | HerbViewControl Properties（性味） | 保留 |
| herb | 字段-归经 脾、肺经 | 信息项 | — | ✅ | US-HERB-002 | HerbViewControl（归经并入 Properties 展示） | 保留 |
| herb | 字段-产地 内蒙古、山西、甘肃 | 信息项 | — | ✅ | US-HERB-002（Origin） | HerbViewControl Origin「产地」 | 保留 |
| herb | 字段-采集季节 春、秋二季 | 信息项 | — | ❌ | HerbDetailModel/HerbViewControl 无采集季节字段（grep 采集=0） | 无 | 删除该信息项 |
| herb | 字段-炮制方法 切厚片，生用或蜜炙 | 信息项 | — | ⚠️ | 代码有 Usage（用法）与 Remark（备注），无独立「炮制方法」字段 | HerbViewControl labels：用法/备注 | label 改「用法」（或并入备注）；值形态保留 |
| herb | 功效区域（长文本） | text | — | ✅ | US-HERB-002 | HerbViewControl HerbEffect「功效」 | 保留 |
| herb | 库存卡片 当前库存 256 kg + 预警值: 50 kg | 卡片 | — | ❌ | Record-Only 无库存/预警概念 | 无字段 | 删除整张库存卡片 |
| herb | 价格卡片 采购单价 ¥68/kg + 零售价 ¥85/kg | 卡片 | — | ⚠️ | Price（零售价）✅有；CostPrice（成本价）✅有但语义≠「采购单价」且是否对外展示存疑；kg 单位=Unit 自由文本字段 | HerbViewControl 价格信息：成本价 CostPrice/零售价 Price | 保留价格展示但拆两字段：零售价（¥xx / 单位）、成本价；「/kg」「预警值」等编造单位后缀去掉 |
| herb | 操作按钮区 编辑/删除 | frame×2 | — | ⚠️ | US-HERB-004/005 | DetailToolbar（编辑/保存/取消/删除） | 改 DetailToolbar 形态；另补「切换状态」（ToggleStatusCommand，US-HERB-010） |

---

## 三、formula-management.pen（验方管理）

### F-0 结构性问题（最高优先级）

| 页面 | 元素 | 类型 | 现状值 | 追溯结论 | US编号 | 代码文件 | 建议动作 |
|---|---|---|---|---|---|---|---|
| formula | 工具栏(56 r12) 内按钮组 | frame | 同患者页按钮组（搜索姓名/手机号/身份证号+筛选+新增患者+导入+导出+批量删除） | ❌（整组错页） | — | FormulaMasterDetailControl 工具栏：新增/刷新/导出/批量启用/批量禁用/批量删除/模板/导入/编辑/更多(复制/切换状态/恢复) + 搜索占位「搜索验方名称...」 | 整组重做（见修改点清单 F-R1） |
| formula | 内容区内嵌「顶部工具栏」（验方管理+共 24 个验方+搜索框+新增验方） | frame | h56 | ❌（与三栏框架重复） | — | 页面级无顶栏 | 删除；有效信息迁移：搜索框并入标准工具栏，「共 N」并入分页栏 |
| formula | 内容区缺主从分割 | — | 列表区+竖线+详情面板横排（无 GridSplitter、无卡片描边） | ⚠️ | US-FORM-002 | MasterDetailLayout | 按 patient-list 定稿结构重排 |

### F-A 列表区

| 页面 | 元素 | 类型 | 现状值 | 追溯结论 | US编号 | 代码文件 | 建议动作 |
|---|---|---|---|---|---|---|---|
| formula | 筛选行 全部24/启用18/停用6 + 筛选/排序 | frame | — | ⚠️（计数标签无依据）/❌（筛选排序按钮） | US-FORM-011（Status 启用/禁用概念存在）；但 UI 无筛选标签栏/排序控件 | 无 FilterCommand/SortCommand | 标签栏删除（或降级 v2 提案先立 US）；筛选/排序两个 icon+文字删除 |
| formula | 名称行前彩色圆点（$amber） | ellipse×8 | — | ❌ | 无对应数据/装饰定义 | DataGrid 无 | 删除（8 行） |
| formula | 表头列「序号」01-08 | text | — | ❌ | 列表无序号列 | DataGrid 无序号列 | 删除表头+8 行序号 |
| formula | 表头列「验方名称」 | text | — | ✅ | US-FORM-001 | DataGrid 名称 Name | 保留 |
| formula | 表头列「组成药材数」 | text | 值「6味/4味…」 | ⚠️ | US-FORM-001（HerbCount） | DataGrid 列名「药味」（{Binding HerbCount}） | 保留列；表头文字改「药味」，值形态改数字（代码列直显 int） |
| formula | 表头列「适应症」 | text | 长文本 | ❌（列表列） | Indication 是**详情**字段；US-FORM-001 列表项只要求 HerbCount 不显价格 | DataGrid 列=名称/分类/药味/共享/状态 | 删除表头+8 行适应症单元格（移详情面板展示） |
| formula | 表头列「状态」启用/停用 | text | $green-light/$red-light 徽标 | ⚠️（取值措辞） | US-FORM-011 | Status=CommonStatus「启用/禁用」 | 保留徽标；「停用」改「禁用」 |
| formula | 表头列「创建人」张仲景/钱乙… | text | — | ❌ | 列表 DTO 无创建人显示字段（FormulaListDto.CreatedAt 有、CreatedBy 无；示例数据张仲景等为杜撰） | DataGrid 无创建人列 | 删除表头+8 行创建人单元格 |
| formula | 缺失列：分类/共享 | — | 设计未画 | ⚠️（漏画） | US-FORM-001（category 筛选参数）；FormulaListDto.Category/IsShared | DataGrid 分类 Category 列 + 共享 IsShared TemplateColumn | 补画「分类」「共享」两列 |
| formula | 缺失控件：复选框列 | — | 未画 | ⚠️（漏画） | US-FORM-013（批量操作） | ShowCheckBoxColumn=True | 补画行首复选框 |
| formula | 分页栏「显示 1-8 条，共 24 条」+页码 1-3 | frame | — | ⚠️ | US-FORM-001 | UnifiedPaginationBar | 对齐组件形态：共 X 条记录 + 首页/上一页/x÷y/下一页/末页 + 每页 N 条 |

### F-B 验方详情面板（w380）

| 页面 | 元素 | 类型 | 现状值 | 追溯结论 | US编号 | 代码文件 | 建议动作 |
|---|---|---|---|---|---|---|---|
| formula | 详情头部「验方详情」+close | frame | — | ⚠️ | US-FORM-002 | DetailToolbar（取消/删除/编辑/保存）+ PopupBox（复制/切换状态/恢复） | 改 DetailToolbar 形态 |
| formula | 「麻黄汤」+「经典方剂 · 出自《伤寒论》」 | text×2 | — | ⚠️+❌ | 名称✅；「出自《伤寒论》」出处文案编造（FormulaDetailModel 无 Source 字段；FormulaViewControl 的 Formula.Source 绑定目标不存在） | FormulaViewControl 验方名称 Name | 保留名称；出处副标题删除（或改 Effect 功效摘要） |
| formula | 状态行「启用中」+更新于 2024-01-15 | frame+text | — | ⚠️ | US-FORM-011/002 | Status 枚举描述=「启用」；UpdatedAt 有（系统信息 更新时间） | 「启用中」改「启用」；「更新于 …」保留（对齐 UpdatedAt） |
| formula | 药材组成（4味）：麻黄9g[君药]/桂枝6g[臣药]/杏仁9g[佐药]/炙甘草3g[使药] | frame×4 | 君臣佐使徽标 | ⚠️（错域） | 药材组成✅ US-FORM-002（Herbs 列表：HerbName/Dosage/Unit）；君臣佐使=`HerbRole` 枚举（Sovereign/Minister/Assistant/Guide）**真实存在但属处方域**（PrescriptionItemDto/PrescriptionItemModel 消费），FormulaHerbItem(Dto/Model) 均无此字段——画在验方详情即跨域错置 | FormulaViewControl Formula.Herbs（HerbName/Dosage/Unit）；FormulaHerbItemModel(HerbId/HerbName/Dosage/Unit/ProcessingMethod/DecocteMethod/Remark) | 保留「药材组成」列表（名称+剂量+单位）；删除 4 个君臣佐使徽标（该属性随处方开立时录入，不属验方模板）；药材图标块内叠 1-4 个 local_florist 的画法删除（统一 1 个或删）；如需体现验证工作流可加 IsValidated 绑定标识（Draft/Validated，US-FORM-002 AC） |
| formula | 适应症区（长文本） | text | — | ✅ | US-FORM-002（Indication 详情字段） | FormulaViewControl（主治/适应症展示） | 保留 |
| formula | 用法用量区（水煎服…） | text | — | ✅ | US-FORM-002（Usage） | FormulaViewControl 用法 Usage | 保留 |
| formula | 注意事项区（#FFF3E0 提示块） | frame+icon(info) | — | ❌ | FormulaDetailModel/FormulaViewControl 无注意事项字段（grep 注意事项=0；Remark 备注≠注意事项语义） | 无 | 删除整块（或改绑 Remark「备注」并改标签） |
| formula | 操作按钮区 编辑/停用 | frame×2 | — | ⚠️ | US-FORM-004/011 | DetailToolbar 编辑/保存/取消/删除 + PopupBox 复制/切换状态/恢复 | 改为 DetailToolbar+PopupBox 形态；「停用」按钮改「切换状态」（ToggleStatusCommand）；补「复制」（CopyFormulaCommand，US-FORM-014） |

---

## 四、修改点清单（属性级）

### R1. herb-management.pen —— 中间区重建（结构性）

1. **删除** `内容区 > 顶部工具栏`（Logo图标+凌隐宝堂中医诊所+/药材管理+新增药材/导入/导出 整个 frame）。
2. **删除** `内容区 > 主体区域 > 侧边栏`（导航菜单整个 frame，w240）。
3. **替换** `工具栏`(h56 r12) 子节点为药材页真实按钮组：
   - 搜索框 content：「搜索姓名 / 手机号 / 身份证号」→「搜索药材名称/拼音码...」
   - 删除「筛选按钮」「新增患者按钮」「导入按钮(primary-soft 版)」「导出按钮(text-secondary 版)」「批量删除按钮(#B00020 版)」
   - 新增（按 DataGridToolbar 顺序）：新增（CreateNewCommand）、刷新（RefreshCommand）、导出（ExportCommand）、批量启用（BatchEnableCommand）、批量禁用（BatchDisableCommand）、批量删除（BatchDeleteCommand）、模板（DownloadImportTemplateCommand）、导入（ImportHerbsCommand）
4. **重排** `内容区` 为定稿三段式：`药材列表卡片(w880 r12 描边#E9DFD7)` + `GridSplitter(w1 #E9DFD7+拖拽柄)` + `药材详情卡片(r12 描边#E9DFD7 pad16)`。
5. 列表卡内：
   - 删除 `统计标签栏`（全部128/在库96/缺货8/停用24）
   - 表头行改为：`☐ | 名称 | 拼音码 | 规格 | 单位 | 零售价 | 状态`（删除 性味/归经/功效/库存/操作 五列表头）
   - 10 个数据行同步：删除各行的 性味/归经/功效/库存/操作 单元格与名称前圆点；新增 规格/单位/零售价 单元格（示例值自拟合理值）；状态值 在库→启用、缺货→启用、停用→禁用；拼音值 huang qi→HQ 等
   - 分页栏文案：「共 128 条记录，第 1/13 页」→「共 X 条记录」；页码方块 1-5 → 首页/上一页/x÷y/下一页/末页+每页 N 条
6. 详情卡内：
   - 头部「药材详情+close」→ DetailToolbar 形态（标题+编辑/保存/取消/删除）
   - 图片区：删除「Astragalus membranaceus」text
   - 字段区：label「拼音」→「拼音码」（值改缩写码）；**删除** `字段-采集季节`；label「炮制方法」→「用法」
   - **删除** `库存卡片`（当前库存/预警值）
   - 价格卡片拆为 零售价（Price）与 成本价（CostPrice）两项，去「/kg」后缀
   - 操作按钮区：编辑/删除 → DetailToolbar 承载；新增「切换状态」按钮（ToggleStatusCommand）

### R2. formula-management.pen —— 中间区重建（结构性）

1. **删除** `内容区域 > 顶部工具栏`（验方管理/共 24 个验方/搜索框/新增验方 整个 frame）及其下 `工具栏分割线`。
2. **替换** 外层 `工具栏`(h56 r12) 子节点为验方页真实按钮组：
   - 搜索框 content →「搜索验方名称...」
   - 删除患者页五按钮（同 R1.3）
   - 新增：新增（CreateNewCommand）、刷新（RefreshCommand）、导出（ExportFormulasCommand）、批量启用、批量禁用、批量删除、模板（DownloadImportTemplateCommand）、导入（ImportFormulasCommand）、编辑（EditCommand）、更多 PopupBox（复制 CopyFormulaCommand/切换状态 ToggleStatusCommand/恢复 RestoreCommand）
3. **重排** `内容区` 为：`验方列表卡片(w880 r12 描边)` + `GridSplitter` + `验方详情卡片(r12 描边)`。
4. 列表卡内：
   - 删除 `筛选行`（全部/启用/停用计数标签 + 筛选/排序 icons）
   - 表头改为：`☐ | 名称 | 分类 | 药味 | 共享 | 状态`（删除 序号/适应症/创建人；新增 分类/共享）
   - 8 个数据行同步：删除 序号/名称前圆点/适应症/创建人 单元格；新增 分类/共享（✓）单元格；状态徽标「停用」→「禁用」；「N味」→ 数字
   - 分页栏对齐 UnifiedPaginationBar 形态
5. 详情卡内：
   - 头部「验方详情+close」→ DetailToolbar（+PopupBox：复制/切换状态/恢复）
   - 删除副标题「经典方剂 · 出自《伤寒论》」text
   - 状态行：「启用中」→「启用」；保留「更新于 …」
   - 药材组成区：删除 4 个「君药/臣药/佐药/使药」徽标 frame；每个 `药材图标` 内多余的 local_florist icon 刪至 1 个（或删除图标块）
   - **删除** `注意事项区` 整块（或改标签「备注」绑 Remark）
   - 操作按钮区：编辑/停用 → DetailToolbar/切换状态；新增「复制」

### R3. patient-list.pen —— 定向修补

1. 工具栏：**删除** `筛选按钮` frame；搜索占位 content →「搜索 姓名/手机号/拼音码」；批量删除按钮旁**新增**「批量启用」「批量禁用」「刷新」「更多(恢复/刷卡录入)」按钮节点。
2. 列表卡：
   - 删除 `今日新增提示` frame
   - 删除 `总数徽标`（总数并入分页栏）
   - 卡片标题行处理：删除「患者列表」标题或改面包屑「首页 / 患者管理」
   - **删除** 表头 `最近就诊` 单元格 + 8 行的 `就诊日期单元格`
   - 表头「联系电话」→「手机号」；8 行电话示例值改脱敏形态（138****4477）
   - 8 行的 `病历号` text 节点全部删除（MZ-2026001~008）
   - 8 个状态徽标 status 文字：复诊中→启用、在治→启用、新患者→启用、已治愈→启用（并留 1 行「禁用」示例；四色归一为启用/禁用双色）
   - （可选）删除 8 行 `头像` 装饰列
   - 分页栏：「显示 1-8 / 共 1,286 位患者」→「共 X 条记录」；页码串 1/2/3/…/43 → 首页/上一页/x÷y/下一页/末页+每页 N 条
3. 详情面板：
   - 删除 `大头像` frame；编辑 icon → DetailToolbar 按钮组（编辑/保存/取消/删除）
   - 「病历号 MZ-2026001 · 建档于 2024-03-18」→「建档于 2024-03-18」
   - 详情状态徽标「复诊中」→「启用」
   - 信息项「联系电话」label →「手机号码」
   - **删除** 信息项「会员等级 金卡会员」（标签+值两个 text）
   - **删除** 信息项「过敏史 青霉素过敏」（违背 US-PAT-013 Hash 脱敏不可回显）
   - **删除** `就诊历史卡片` 整个 frame（含 3 条就诊记录与「查看全部」）；如需表达医案关联，改为单个「查看医案」按钮
   - **删除** `处方记录卡片` 整个 frame（含 3 条处方记录、「已取药」徽标、「查看全部」）

---

## 附：证据索引

| 结论 | 证据 |
|---|---|
| 患者 DTO 字段 | `PatientListDto(Id/Name/Gender/Age/PhoneNumber/PinYinCode/CommonStatus/CreatedAt)`；`PatientDetailDto(+BirthDate/IdNumber/UpdatedAt/CreatedBy)` —— 无病历号/会员等级/过敏史/最近就诊 |
| 患者状态枚举 | `SystemEnums.cs` CommonStatus：Disabled=0 禁用、Enabled=1 启用 |
| 脱敏规则 | 04-patients.md US-PAT-013：AllergyHistory/MedicalHistory Hash 脱敏（仅可比对不可还原）→ 明文展示即违规 |
| 患者工具栏 | PatientMasterDetailControl.xaml：DataGridToolbar(Create/Refresh/Export/BatchEnable/BatchDisable/BatchDelete) + AdditionalContent(模板/导入/编辑/更多[恢复/刷卡录入]) + SearchBox(姓名/手机号) + 右键菜单(编辑/查看医案/新建医案/恢复/删除) |
| 药材列表列 | HerbMasterDetailControl.xaml DataGrid：名称/拼音码/规格/单位/零售价/状态(Template)+操作(Template) |
| 药材详情字段 | HerbDetailModel(Name/PinYinCode/Category/Properties/Origin/Spec/Unit/Price/CostPrice/Effect/Usage/Remark/Status/CreatedAt/UpdatedAt)；HerbViewControl labels：产地/分类/性味/规格/单位/功效/用法/备注/成本价/零售价/拼音码 —— 无采集季节/库存/拉丁名 |
| 验方列表列 | FormulaMasterDetailControl.xaml DataGrid：名称/分类/药味(HerbCount)/共享(IsShared Tpl)/状态(Tpl)+操作(Tpl) |
| 验方详情字段 | FormulaDetailModel(Name/Effect/Usage/Property/Remark/IsShared/Category/Status/Herbs…)；FormulaHerbItemModel(HerbId/HerbName/Dosage/Unit/ProcessingMethod/DecocteMethod/Remark) —— 无创建人显示列/注意事项/Source 出处；君臣佐使=`HerbRole` 枚举存在但仅处方域（PrescriptionItemDto）消费，验方药材项无此字段 |
| 药材记录模式 | 05-herbs.md 模块级规则：Record-Only，不管理库存，无库存量/入库/出库概念 |
| 侧栏现状 | NavigationManager.cs `BuildNavigationItems`：「侧边栏仅保留主页入口，功能导航由 Home View 卡片承载」（herb/formula 内嵌侧边栏的 7-9 项菜单均为编造） |
