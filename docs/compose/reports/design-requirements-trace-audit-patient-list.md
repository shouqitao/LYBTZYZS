# 患者管理试点设计稿 · 需求追溯审计报告

> 审计对象：`designs/patient-list.pen`（一文件两帧：`患者管理`=展开240 / `患者管理-收拢`=收拢64）
> 需求基准：`docs/02-requirements/`（US-PAT × 14、US-SHELL、US-AUTH、US-REG 等）
> 代码基准：`src/Client/Desktop/`
> 原则：**每个设计元素必须能在需求文档（US 编号）或现有代码中找到依据；找不到即为编造，移除。**
> 日期：2026-08-23 | 结论统计：✅保留 21 · ⚠️改标 12 · ❌移除 17

---

## 一、总体结论

设计稿的**中部患者工作区**（工具栏/列表/详情/分页）追溯性良好，主体可保留；但 **Shell 外壳层是重灾区**——顶部应用栏整条、侧栏 7 项菜单、底部状态栏文字均为编造或与现状冲突。最严重的单项编造：**头像下拉菜单中的「切换角色」**（需求与代码均无此功能）、**通知铃铛**、**会员等级**。

---

## 二、逐元素追溯表

图例：✅=有US+有代码，保留原样 | ⚠️=需求有但设计形态画错，改标 | ❌=无需求无代码，移除

### A. Shell 外壳

| # | 设计元素 | 追溯结论 | US编号 | 代码文件 | 处理建议 |
|---|----------|----------|--------|----------|----------|
| A1 | 顶部应用栏（48px 整条） | ❌ | 无 | `Shell/Views/MainWindow.xaml`——无顶栏结构 | **删除整个顶部应用栏**。品牌/用户信息在侧栏与状态栏承载 |
| A2 | └ 品牌块+诊所名「中医诊所管理系统」 | ⚠️ | — | `MainWindow.xaml:80-90`：Logo(Leaf图标)+「凌隐宝堂」位于**左侧栏顶部**，非顶栏 | 改为侧栏内 Logo + 「凌隐宝堂」（展开帧显示文字，收拢帧只留图标） |
| A3 | └ 页面名「患者管理」 | ⚠️ | US-SHELL-005 | 代码无独立页面名元素；页面上下文由面包屑承载（`PatientMasterDetailControl.xaml:32-40` HeaderContent Breadcrumbs） | 移除顶栏页面名，改为内容区面包屑「首页 / 患者管理」 |
| A4 | └ 通知铃 icon | ❌ | 无（全文档 grep 通知铃/消息中心=0） | Shell 无任何 Bell 图标/通知中心 UI；仅有 Snackbar 弹消息（`MainWindow.xaml:218`）与 `IUserNotificationService`（Toast，非收件箱） | **删除**。如需反馈入口用现有 Snackbar，不设常驻铃铛 |
| A5 | └ 头像圆+陈字 | ⚠️ | — | 无头像控件；当前用户显示为**状态栏**「👤+显示名」(`MainWindow.xaml:204-207`) | 删除顶栏头像；当前用户放底部状态栏（现状即如此）。若坚持要视觉锚点，须先立 US |
| A6 | └ 头像下拉菜单（含个人资料/修改密码/**切换角色**/退出） | ⚠️（拆分） | 见 A6.1-A6.3 | 下拉容器不存在；实际入口为**侧栏底部按钮组** | 菜单容器删除；有效项迁移见下三行 |
| A6.1 | 　　└ 个人资料/修改密码 | ✅ | US-SHELL-004 | `MenuManager.EditProfileCommand`(MenuManager.cs:109) → `AccountSettingsControl`；侧栏底部「个人资料」按钮 MainWindow.xaml:102 | 迁移到侧栏底部按钮组（现状位置），保留 |
| A6.2 | 　　└ **切换角色** | ❌❌ | **无任何 US**（grep 切换角色/SwitchRole=0） | **无任何代码**（全库 grep SwitchRole=0）；角色切换仅存在于重新登录流程 | **删除**。角色由登录账号决定（US-SHELL-003 按角色加载模块），运行时不可切换 |
| A6.3 | 　　└ 退出登录 | ✅ | US-SHELL-003 AC4「登出→清会话返回登录页」 | `LogoutCommand` + `LogoutService.cs`；侧栏底部「退出」按钮 MainWindow.xaml:126-138 | 迁移到侧栏底部按钮组（现状位置），保留 |
| A7 | 左侧导航标题「NAVIGATION」 | ❌ | 无 | 无对应元素 | **删除**该装饰性英文标签 |
| A8 | 侧栏 7 项菜单（首页/患者管理/挂号管理/医案管理/药材验方/报表/系统设置） | ❌（整体形态编造） | US-SHELL-005 只定义导航机制，未定义固定 7 项侧栏 | **`NavigationManager.cs:48-77`（2026-08-17 设计决策）：侧栏仅「主页」1 项**，功能导航由各角色 Home 卡片承载（ClinicalHomeViewModel: NavigateToPatientManagement/HerbLibrary/FormulaLibrary/RegistrationQueue/Reports/AuditLog…） | **删除 6 项功能菜单**，侧栏只留「主页」；功能入口画进各 Home 工作台卡片网格。注意：药材与验方在代码中是两个独立视图(HerbManagementView/FormulaManagementView)，设计中合并为「药材验方」一项也与代码不符 |

### B. 中部主体（患者工作区）

| # | 设计元素 | 追溯结论 | US编号 | 代码文件 | 处理建议 |
|---|----------|----------|--------|----------|----------|
| B1 | 主从布局：列表卡(880)+分隔条(12)+详情面板 | ✅ | US-PAT-002 | `MasterDetailLayout.xaml`（左右分割+GridSplitter）、`PatientMasterDetailControl.xaml` MasterContent/DetailContent | 保留 |
| B2 | 展开态列表宽 / 收拢后隐藏详情面板 | ✅ | US-PAT-002 | `MasterDetailLayout HasSelection="{Binding ShowDetailPanel}"`（选中才显详情） | 保留（收拢帧语义=未选中患者时仅列表，成立） |
| B3 | 搜索框（占位：姓名/手机号/身份证号） | ✅（措辞⚠️微调） | US-PAT-001（姓名/电话/**拼音首字母**）、US-PAT-014（按身份证查询） | `SearchBox` 控件；`PatientMasterDetailControl.xaml:137-142` Placeholder="搜索患者姓名/手机号..." | 保留；占位文案补「拼音码」对齐 US-PAT-001：「搜索 姓名/手机号/拼音码」 |
| B4 | 筛选按钮 | ❌ | 无（US-PAT-001 仅关键字搜索，无筛选器） | 无筛选 UI/命令 | **删除**。如未来需要按状态筛选须先立 US |
| B5 | 新增患者按钮 | ✅ | US-PAT-003 | `DataGridToolbar` CreateCommand→CreateNewCommand（PatientMasterDetailControl.xaml:65）；Ctrl+N 全局快捷键（US-SHELL-005） | 保留 |
| B6 | 导入按钮 | ✅ | US-PAT-011 | ImportPatientsAsync（PatientMasterDetailViewModel.cs:309，JSON 导入） | 保留 |
| B7 | 导出按钮 | ✅ | US-PAT-012 | ExportPatientsAsync（同上 :359，JSON 导出） | 保留 |
| B8 | 批量删除按钮 | ✅ | US-PAT-008 | DataGridToolbar BatchDeleteCommand→DeleteCommand(:62)；批量启用/禁用也在工具栏(BatchEnable/BatchDisable, US-PAT-006) | 保留；建议补画「批量启用/批量禁用」按钮（代码有、设计漏） |
| B9 | 表格列：患者（姓名） | ✅ | US-PAT-001 | DataGrid 姓名 Column(:165) | 保留；但设计把病历号 MZ-2026001 放姓名列下——患者无病历号字段，见 B14 |
| B10 | 表格列：性别/年龄/联系电话 | ✅ | US-PAT-001 | DataGrid 性别/年龄/手机号 Columns(:171-190)；PatientListDto(Name/Gender/Age/PhoneNumber) | 保留 |
| B11 | 表格列：最近就诊 | ❌ | 无 | PatientListDto **无最近就诊字段**（Id/Name/Gender/Age/PhoneNumber/PinYinCode/Status/CreatedAt） | **删除该列**（或降级为 v2 提案，需先立 US+DTO 扩展） |
| B12 | 表格列：状态徽标（复诊中/在治/新患者/已治愈） | ❌（取值编造） | 部分：状态概念存在 | PatientListDto.Status=`CommonStatus` 枚举只有 **启用/禁用** 两值（SystemEnums.cs:8-16）；诊疗阶段属医案域非患者属性 | **改标**：徽标保留但值改为「启用/禁用」（对应 US-PAT-006 启用/禁用）；「复诊中/在治/新患者/已治愈」删除 |
| B13 | 行内头像圆+姓氏字 | ⚠️ | — | DataGrid 无头像列 | 可作为纯视觉增强保留（不绑定数据），标注为装饰元素；或删。倾向：删（保持最小忠实） |
| B14 | 详情头部「病历号 MZ-2026001」 | ❌ | 无（MZ- 前缀全库=0；编号规则 MC=医案 MedicalCaseNumberOptions.Prefix="MC"） | 患者 DTO 无病历号字段 | **删除病历号**。「建档于 2024-03-18」✅保留（CreatedAt，US-PAT-002 详情含 CreatedAt，PatientViewControl 显示创建时间） |
| B15 | 详情大头像+编辑icon | ⚠️ | 编辑✅ | 详情区实为 `DetailToolbar`（标题+取消/删除/编辑/保存命令 :264-271）+ Transitioner 查看/编辑切换 | 头像删除；编辑入口改为 DetailToolbar 形态 |
| B16 | 详情信息行1：性别/年龄/联系电话 | ✅ | US-PAT-002/013 | PatientViewControl 性别/年龄/手机号码(:58-101) | 保留 |
| B17 | 身份证号脱敏显示 3301**********2246 | ✅ | US-PAT-013（Partial 脱敏：身份证保留前4后4） | PatientViewControl IdNumber(:93-95)；服务端 `[SensitiveData]` 序列化掩码 | 保留（脱敏示例数据符合前4后4规则，画得对） |
| B18 | **会员等级：金卡会员** | ❌ | 无（全库 grep 会员/MemberLevel/VipLevel=0） | 无字段 | **删除** |
| B19 | 过敏史：青霉素过敏 | ❌（形态错） | US-PAT-013 相关但**不可显示**：AllergyHistory 为 Hash 脱敏（仅可比对不可还原） | PatientDetailDto/PatientViewControl **均无过敏史展示** | **删除**。Hash 脱敏字段无法明文回显，画出来即违背 US-PAT-013 规则 |
| B20 | 就诊历史卡片（3条：主诉·证型/医师·方名） | ❌ | 无此嵌入面板（查看医案是跳转动作非内嵌历史） | 无 ViewMedicalRecordsCommand 尚为 FUTURE stub（PatientMasterDetailViewModel.cs:248 注释 FUTURE US-MC-010）；右键菜单有「查看医案」「新建医案」入口 | **删除内嵌卡片**。若要表达医案关联，改为详情页一个「查看医案」入口按钮（对应右键菜单既有命令） |
| B21 | 就诊历史/处方记录「查看全部」链接 | ❌ | 无 | 无 | **删除**（随 B20/B22 一并消失） |
| B22 | 处方记录卡片（归脾汤加减等+「已取药」徽标） | ❌ | 无（处方明细属医案/处方域；「取药/Dispensary」概念全库=0） | 无 | **删除**。患者详情不含处方列表 |
| B23 | 分页栏：上一页/页码/下一页 | ✅ | US-PAT-001（分页参数 pageIndex/pageSize+总数） | `UnifiedPaginationBar`（首页/上一页/页码 x/y/下一页/末页+每页条数 ComboBox） | 保留；形态对齐 UnifiedPaginationBar：补「首页/末页」按钮与「每页 N 条」选择器，页码用 "x / y" 文本而非 1 2 3 … 43 按钮串 |
| B24 | 分页栏文字「显示 1-8 / 共 1,286 位患者」 | ⚠️ | US-PAT-001（返回总数） | UnifiedPaginationBar 文案="共 {TotalCount} 条记录" | 改为「共 X 条记录」对齐现组件文案 |
| B25 | 底部状态栏「共 128 条记录」 | ❌（重复+口径错） | — | 总数已在分页栏显示；全局底部状态栏内容=API状态icon+连接模式+当前用户+时间（MainWindow.xaml:188-211），**无记录数** | **删除**该文字（与 B24 重复且全局状态栏不放业务计数） |
| B26 | 底部状态栏右侧时间「2026-08-23」 | ⚠️ | —（无 US 明文要求，但代码有） | `StatusBarManager.cs:119` CurrentTimeDisplay=yyyy-MM-dd HH:mm:ss 每秒刷新，状态栏右侧 | **改标保留**：时间显示存在，格式应为 `2026-08-23 14:30:05` 含时分秒；位置在全局窗口底部状态栏（非页面内）。同时状态栏应补画：API连接状态icon、连接模式（本地/远程，US-SHELL-007 AC「状态栏显示模式标识」）、当前用户显示名 |
| B27 | 底部状态栏整体（页面级 32px 条） | ⚠️ | — | 状态栏是**主窗口级**（Grid Row=1, Height=32，跨内容区、不含侧栏列） | 保留 32px 高度设定，但归属为主窗口全局状态栏，内容按 B26 修正 |

### C. 反向检查：需求 → 设计覆盖度（防漏画）

| US | 名称 | 优先级 | 设计是否体现 | 说明/处理建议 |
|----|------|--------|--------------|----------------|
| US-PAT-001 | 分页查询患者列表（关键字+拼音搜索） | Must | ✅ 已体现 | 搜索框+表格+分页齐全；占位文案需补拼音码（B3） |
| US-PAT-002 | 查看患者详情 | Must | ✅ 已体现 | 右侧详情面板成立 |
| US-PAT-003 | 创建患者 | Must | ✅ 已体现 | 新增患者按钮；编辑弹层/抽屉未画（可接受，试点聚焦列表页） |
| US-PAT-004 | 更新患者 | Must | ⚠️ 弱体现 | 仅详情头一个编辑icon；应明确 DetailToolbar 编辑/保存/取消形态（B15） |
| US-PAT-005 | 删除患者（软删+引用检查） | Must | ❌ 未画 | 工具栏无删除按钮（单删在右键菜单/DetailToolbar）。建议工具栏或详情补删除入口+引用检查确认弹窗示意 |
| US-PAT-006 | 启用/禁用患者 | Must | ❌ 未画 | 设计漏。工具栏有批量启用/禁用命令、状态列应为启用/禁用徽标（B12 关联） |
| US-PAT-007 | 恢复软删除患者 | Should | ❌ 未画 | 代码有 RestoreCommand（更多菜单+右键菜单）。建议「更多」溢出菜单中画「恢复」项 |
| US-PAT-008 | 批量删除患者 | Should | ✅ 已体现 | 批量删除按钮（B8）；顺带补批量启用/禁用 |
| US-PAT-009/010 | 单个/批量引用检查 | Must/Should | ➖ 无独立UI | 后端预检能力，UI 通过删除确认弹窗间接表达即可，不强求单独画面 |
| US-PAT-011 | 下载导入模板 | Should | ⚠️ 未画模板按钮 | 代码有「模板」按钮（DownloadImportTemplateCommand）。工具栏补「模板」按钮 |
| US-PAT-012 | 导出患者数据(JSON) | Should | ✅ 已体现 | 导出按钮 |
| US-PAT-013 | 敏感数据脱敏 | Must | ✅ 已体现 | 身份证掩码示例正确（B17）；但 B19 过敏史明文违背本 US，删除 |
| US-PAT-014 | 按身份证号查询 | Must | ✅ 已体现 | 搜索框占位含身份证号；另有读卡链路（更多菜单「刷卡录入」，可在「更多」中体现） |

---

## 三、移除/修正清单（执行摘要）

### ❌ 从设计中移除（17 项）
1. **顶部应用栏整条**（A1）及其上的：诊所名顶栏形态(A2)、页面名(A3)、**通知铃(A4)**、头像(A5)、头像下拉菜单容器(A6)
2. **「切换角色」菜单项**（A6.2）——本次审计发现的最严重编造，需求与代码零依据
3. **侧栏 NAVIGATION 标题**(A7) 与 **6 项功能菜单**(A8)——侧栏按现状只留「主页」
4. 筛选按钮(B4)
5. 表格「最近就诊」列(B11)
6. 状态徽标编造取值「复诊中/在治/新患者/已治愈」(B12)
7. 「病历号 MZ-2026001」(B14)
8. **「会员等级 金卡会员」**(B18)
9. 「过敏史 青霉素过敏」明文展示(B19)——违背 Hash 脱敏规则
10. 就诊历史卡片+查看全部(B20/B21)、处方记录卡片+「已取药」徽标(B22)
11. 底部「共 128 条记录」(B25)
12. （可选）行内头像列(B13)

### ⚠️ 改标（12 项）
1. 品牌→侧栏内 Logo+「凌隐宝堂」（A2）
2. 页面名→内容区面包屑（A3）
3. 用户身份→底部状态栏显示名（A5/A6）
4. 个人资料/退出→侧栏底部按钮组（A6.1/A6.3）
5. 搜索占位文案→「姓名/手机号/拼音码」（B3）
6. 状态徽标→启用/禁用两值（B12）
7. 详情编辑入口→DetailToolbar 形态（B15）
8. 分页形态→UnifiedPaginationBar（首页/末页/每页N条/x÷y）（B23）
9. 分页文案→「共 X 条记录」（B24）
10. 时间→含时分秒、置于全局状态栏（B26）
11. 状态栏补 API 状态+连接模式+用户名（B26/B27）
12. 医案关联→「查看医案」入口按钮替代内嵌卡片（B20）

### ✅ 保留原样（21 项）
主从布局+GridSplitter(B1)、选中展开/未选中收拢(B2)、搜索框(B3)、新增患者(B5)、导入(B6)、导出(B7)、批量删除(B8)、姓名/性别/年龄/电话四列(B9/B10)、建档于日期(B14部分)、性别/年龄/电话信息行(B16)、身份证脱敏示例(B17)、分页器(B23)、32px状态栏高度(B27) 及上述已列各项。

---

## 四、给下一轮设计稿的硬约束（Design Token 之外）

1. Shell 结构照抄 `MainWindow.xaml`：左栏(可折叠 240↔64, Ctrl+M) + 内容区 + 底部 32px 全局状态栏；**没有顶部应用栏**。
2. 侧栏 = Hamburger + Logo「凌隐宝堂」 + 「主页」菜单 + 底部（个人资料/主题开关/退出）。
3. 功能导航入口在各角色 Home 卡片网格（医生：患者/医案/药材/验方/挂号队列/报表/审计日志；前台：患者+读卡+挂号）。
4. 患者可用字段白名单：Name/Gender/Age/PhoneNumber/PinYinCode/CommonStatus(启用禁用)/CreatedAt/UpdatedAt/IdNumber(脱敏)。**不存在**：病历号、会员等级、过敏史明文、最近就诊、复诊状态。
5. 工具栏按钮集 = 新增/刷新/导出/批量启用/批量禁用/批量删除 + 模板/导入/编辑/更多(恢复/刷卡录入)。
6. 分页 = UnifiedPaginationBar 形态（每页N条 + 共X条记录 + 首页/上一页/x/y/下一页/末页）。
