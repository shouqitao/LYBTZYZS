# 管理侧 4 页中间内容区 · 需求追溯审计报告

> 审计对象：`designs/admin-home.pen`、`designs/user-management.pen`、`designs/account-settings.pen`、`designs/system-settings.pen`（展开帧主内容区；三栏框架不在本次范围）
> 需求基准：`docs/02-requirements/03-users.md`（US-USER-001~012）、`02-auth.md`（US-AUTH）、`11a-shell.md`（US-SHELL-003/004/005/007 等）、`11b-configuration.md`（US-CFG-001~006）
> 代码基准：`src/Client/Desktop/`（Shell + Roles/LYBT.Desktop.Admin + Modules/LYBT.Desktop.Users）与 `src/Shared/LYBT.Shared.Models`
> 原则：**每个设计元素必须能在需求文档（US 编号）或现有代码中找到依据；找不到即为编造，移除。**
> 日期：2026-08-23 | 结论统计：✅保留 22 · ⚠️改标 22 · ❌编造待删 41（共 85 个主内容区元素行，含 13 组漏画补画项）

---

## 一、总体结论

四个页面的中间内容区**编造密度是全项目最高的一批**：

1. **admin-home**：设计稿画的是「统计卡片 + 待办审批 + 请假 + 患者反馈」的互联网后台风格工作台。真实 `AdminHomeView.xaml` 是 **8 张纯导航卡片**（用户管理/药材/患者/验方/医案/诊所设置/统计报表/审计日志），无任何统计数字、无待办。设计的快捷入口只对上 2 张（用户管理、系统设置），「数据导出」无代码命令；4 张统计卡全部无数据源；系统状态/最近活动/待办事项整块编造。
2. **user-management**：列表列结构接近真实，但**混入了不存在的角色（药剂师）和不存在状态（待激活）**；详情面板的「角色分配（多选）/权限设置开关」整块违背单角色枚举模型；「所属科室/入职时间」字段 UserDto 没有。真实列表列=用户名/姓名/角色/状态/手机号+行内操作，分页为 UnifiedPaginationBar。
3. **account-settings**：真实页面是左右分栏（左导航+右表单），只有**个人资料（姓名/手机号/邮箱）+ 安全设置（旧密码/新密码/确认密码）**两组内容（US-SHELL-004）。设计稿的「工号/科室/职称」「偏好设置（语言/时区/通知方式/打印格式）」「双重身份验证」及右侧栏「年资历/诊疗人次/好评率/实名认证/收款账户/最近活动」**全部编造**。
4. **system-settings**：真实 `SystemSettingsView` 是**单一长表单**：基本设置(SystemName/HospitalName/ContactPhone)、诊所信息(ClinicSettings 六字段)、数据库配置(自动备份+备份路径)、服务器配置(App:Name 只读 Version/Environment)。设计稿的「营业时间、功能开关(预约/短信/打印/病历)、数据管理(导入/导出/恢复)、安全设置(密码长度/锁定/会话超时/双因素)」与 `FeatureToggleOptions`（仅 2 键）和 `ConfigurationWritePolicy` 白名单不符，多为编造。

另：4 个文件的主内容区顶部都残留一条**患者列表工具栏模板**（搜索姓名/手机号/身份证号 + 筛选 + 新增患者 + 导入 + 导出 + 批量删除），属于 patient-list 样板复制残留，4 页全部应删。

---

## 二、逐元素追溯表

图例：✅=有US+有代码，保留原样 | ⚠️=需求有但设计形态/取值画错，改标 | ❌=无需求无代码，移除
（每页表格中省略与本页无关的三栏外壳元素；外壳层问题已有专项结论，此处仅在涉及主内容区边界时提及）

### A. admin-home（管理员工作台）

代码基准：`Roles/LYBT.Desktop.Admin/Views/AdminHomeView.xaml`（标题「系统管理工作台」+ 3×3 UniformGrid 8 卡片）；`ViewModels/AdminHomeViewModel.cs`（8 个 NavigateTo*Command，注释明确「6个功能卡片导航 + 修改密码」）；`Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs`

| # | 设计元素 | 类型 | 现状值 | 结论 | US编号 | 代码文件 | 建议动作 |
|---|----------|------|--------|------|--------|----------|----------|
| A1 | 主内容区·患者工具栏残留（搜索姓名/手机号/身份证号+筛选+新增患者+导入+导出+批量删除） | 工具栏 | 整条 | ❌ | 无 | AdminHomeView 无工具栏；属 patient-list 样板复制残留 | **整条删除** |
| A2 | 应用栏（Logo+凌隐宝堂中医诊所+分隔线+管理员工作台标签） | 页头 | 64px 条 | ⚠️ | US-SHELL-003 AC1「Admin 登录→导航到管理工作台」 | AdminHomeView.xaml:19-31 居中标题「系统管理工作台」+副题「凌隐宝堂中医诊所」，无 Logo 行、无右侧管理员信息条 | 改标为居中大标题形态；右侧「管理员信息+设置入口」删除（用户身份在全局状态栏） |
| A3 | └ 管理员信息（头像+姓名「管理员」+角色「系统管理员」） | 信息组 | — | ❌ | 无此 UI | AdminHomeView 无头像/姓名元素；VM 有 CurrentUserName 但 XAML 未绑定展示 | 删除；当前用户由底部全局状态栏 CurrentUserDisplayName 承载 |
| A4 | └ 设置入口 icon 按钮 | 图标按钮 | 36x36 | ❌ | 无独立入口 | 无对应命令（设置导航在卡片6「诊所设置」） | 删除（与卡片重复且无代码） |
| A5 | 统计卡片行（今日挂号数/今日收入/活跃医生数/待处理事项） | 卡片组 | 4 张 | ❌ | 无（PRD 无仪表盘 US） | AdminHomeViewModel 无任何统计属性/命令；全库无 dashboard 数据源 API | **整行删除**。「今日挂号/收入」类数字无数据源依据；如需保留须先立 US + 统计 API |
| A6 | 快捷操作面板·标题（快捷操作 / 常用管理入口） | 面板头 | — | ✅（容器成立） | US-SHELL-003（按角色加载功能模块）+ US-SHELL-005 | AdminHomeView 功能卡片网格即「常用管理入口」 | 保留容器，改为 8 卡片网格形态（见 A7-A13） |
| A7 | └ 用户管理入口 | 导航卡 | 描述「管理医生、前台与管理员账号」 | ✅ | US-USER-001~012 | `NavigateToUserManagementCommand` → ViewNames.UserManagement | 保留；描述可保留 |
| A8 | └ 系统设置入口 | 导航卡 | 描述「配置诊所信息与系统参数」 | ✅ | US-CFG-006（诊所信息热更新 UI） | `NavigateToSystemSettingsCommand` → SystemSettingsView | 保留；建议文案对齐代码页名「诊所设置」 |
| A9 | └ 数据导出入口 | 导航卡 | 描述「导出挂号、收入与库存数据」 | ❌ | 无（AdminHome 无导出命令；患者域导出≠此处） | AdminHomeViewModel 8 命令中无 Export | **删除**。报表域导出属 Reports 模块，如需要走「统计报表」卡片 |
| A10 | └ 底部链接「查看全部功能 ›」 | 链接 | — | ❌ | 无 | 无对应命令/视图 | 删除 |
| A11 | 缺失卡片：药材管理/患者管理/验方管理/医案管理 | （漏画） | 设计只有 3 入口 | ❌→补 | US-SHELL-003 菜单可见性矩阵（药材/用户管理=Admin+） | NavigateToHerbManagement/PatientManagement/FormulaManagement/MedicalCaseManagement 四个命令均存在 | **补画 4 张卡片**，与代码 8 卡片对齐 |
| A12 | 缺失卡片：统计报表 / 审计日志 | （漏画） | — | ❌→补 | 报表：10-reports.md US-REPORT-001~004；审计日志：US-SHELL-014 | NavigateToReportsCommand→ReportsHomeView；NavigateToAuditLogCommand→AuditLogView | 补画 |
| A13 | 缺失卡片：诊所设置第 8 卡与网格布局 | 布局 | 左右两栏面板式 | ⚠️ | — | AdminHomeView 用 3×3 UniformGrid 居中卡片（Button+矢量图标+标题） | 形态改为居中卡片网格（图标+文字按钮），非左右面板 |
| A14 | 系统状态卡片（服务器状态·已连续运行12天/数据库连接 SQL Server·延迟8ms/全部正常徽章） | 卡片组 | — | ❌ | 运维健康属 sysadmin 域（US-SHELL-018 SysadminHomeView），AdminHome 无 | SysadminHomeViewModel/Sysadmin\Services 有 AuthHealthService，但那是 SysadminHome 的能力 | **整卡删除**。若产品要 Admin 可见健康状态须先立 US；「连续运行12天/延迟8ms」均为编造示例数据 |
| A15 | 最近活动流（王医生更新排班/新增2笔挂号/自动备份 03:00 + 「全部」链接） | 列表 | 3 条 | ❌ | 审计日志查看=US-SHELL-014，但那是独立 AuditLogView，非 Home 内嵌活动流 | 无内嵌活动组件 | **整卡删除**。审计诉求由「审计日志」卡片跳转满足 |
| A16 | 待办事项列表（审批王医生的请假申请/核对7月中药库存盘点数据/处理患者反馈 + 紧急/重要/普通优先级 + 4项待处理徽章） | 任务列表 | 3 项 | ❌❌ | 无（grep 请假/审批/盘点/反馈=0；系统无待办域模型） | 全库无 Todo/Approval 实体与 API | **整卡删除**——本报告最严重编造之一 |
| A17 | 底部状态栏（●API已连接/远程模式/时间） | 状态栏 | 页面级 32px | ✅（归属⚠️） | US-SHELL-007 AC5「切换成功→状态栏显示模式标识」 | MainWindow.xaml 状态栏：ApiStatusIcon+ConnectionModeDisplay+CurrentUserDisplayName+CurrentTimeDisplay | 保留内容；归属为主窗口全局状态栏（跨内容区、不含侧栏），时间应含时分秒 yyyy-MM-dd HH:mm:ss |

### B. user-management（用户管理）

代码基准：`Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml`（SearchBox「搜索用户名/姓名...」+ 角色/状态筛选 ComboBox + DataGridToolbar + DataGrid 列：用户名/姓名/角色/状态/手机号 + UnifiedPaginationBar + 右侧详情 Transitioner）；`Shared/LYBT.Shared.Models/Contracts/Users/UserListDto.cs`；`Enums/AuthEnums.cs`（UserRole：Receptionist=0 前台接待, Doctor=1 医生, **Admin=10 管理员**, SuperAdmin=100 超级管理员）；`Enums/SystemEnums.cs`（CommonStatus 仅 Enabled 启用/Disabled 禁用）

| # | 设计元素 | 类型 | 现状值 | 结论 | US编号 | 代码文件 | 建议动作 |
|---|----------|------|--------|------|--------|----------|----------|
| B1 | 主内容区·外层患者工具栏残留 | 工具栏 | 同 A1 | ❌ | 无 | 复制残留 | **删除** |
| B2 | 页头：用户管理 + 副题「共 10 位用户 · 4 种角色」 | 标题区 | — | ⚠️ | US-USER-001（返回 TotalCount） | 页面上下文由面包屑承载；总数文案=UnifiedPaginationBar「共 {TotalCount} 条记录」 | 标题可留作面包屑；「4 种角色」错——系统 4 角色但用户可见角色枚举为 4 值没错，然「位用户」计数应来自查询 TotalCount，建议并入分页栏 |
| B3 | 搜索框占位「搜索姓名 / 用户名 / 邮箱」 | 输入框 | 280x40 | ⚠️ | US-USER-001（keyword 筛选） | SearchBox Placeholder=“搜索用户名/姓名...” | 占位文案改「搜索用户名 / 姓名」对齐实现 |
| B4 | 缺失：角色筛选下拉（全部角色） | 下拉 | 未画 | ❌→补 | US-USER-001（role 筛选参数） | ComboBox ItemsSource=RoleOptions（UserRole 四值）SelectedRoleFilter | 补画 |
| B5 | 缺失：状态筛选下拉（全部状态） | 下拉 | 未画 | ❌→补 | US-USER-001（status 筛选参数） | ComboBox StatusOptions（启用/禁用）SelectedStatusFilter | 补画 |
| B6 | 「批量操作 ▾」下拉按钮 | 按钮 | 含下拉箭头 | ⚠️ | US-USER-012（批量删除/启用/禁用） | DataGridToolbar 固定按钮组：新增/刷新/**导出**/批量启用/批量禁用/批量删除（无聚合下拉） | 形态改为一排显式按钮：新增/刷新/批量启用/批量禁用/批量删除（+编辑）；「导出」按钮代码有但用户域无导出 US——以工具栏现状为准保留与否需产品裁决，倾向删 |
| B7 | 新建用户按钮 | 按钮 | — | ✅ | US-USER-004 | DataGridToolbar CreateCommand→CreateNewCommand；Ctrl+N | 保留 |
| B8 | 表格列：勾选框（含表头全选） | 列 | 44px | ✅ | US-USER-012（多选批量） | DataGrid 多选 SelectedItems；DataGridCheckboxColumn | 保留 |
| B9 | 表格列：姓名（带头像字） | 列 | 165px | ✅（头像⚠️装饰） | US-USER-001 | DataGrid 姓名 Column（RealName） | 保留；头像圆为装饰可去 |
| B10 | 表格列：用户名 | 列 | 140px | ✅ | US-USER-001/005 | DataGrid 用户名 Column（UserName） | 保留 |
| B11 | 表格列：角色（标签：医生/前台/系统管理员/**药剂师**） | 列 | 徽标 | ⚠️（取值半错） | US-SHELL-003 业务规则3（四角色体系） | UserRole 枚举仅 4 值：前台接待/医生/管理员/超级管理员；**「药剂师」不存在** | 保留徽标；取值改为枚举四值；所有「药剂师」行删除 |
| B12 | 表格列：邮箱 | 列 | 230px | ❌（列表级） | — | **UserListDto 无 Email 字段**（Email 在 UserDetailDto）；DataGrid 也无邮箱列 | **删除该列**。邮箱放详情面板 |
| B13 | 表格列：状态（正常/禁用/**待激活**） | 列 | 徽标 | ⚠️（取值半错） | US-USER-010 | StatusBadge Status=CommonStatus → 仅「启用/禁用」两值；**「待激活」「正常」不是合法值**（正确文本=启用） | 保留徽标；取值改「启用/禁用」；「待激活」行删除或改禁用 |
| B14 | 表格列：最后登录时间 | 列 | 163px | ⚠️ | — | UserListDto.LastLoginTime 存在但 DataGrid 未显示该列 | 二选一：删列对齐现状，或作为增强提案先加列再画（DTO 已支持，成本低）。倾向：保留列并在代码补绑定 |
| B15 | 缺失列：手机号 | （漏画） | — | ❌→补 | US-USER-001（DTO 有 PhoneNumber） | DataGrid 手机号 Column 存在 | 补画 |
| B16 | 缺失：行内操作列（编辑/重置密码/切换状态/恢复/删除） | （漏画） | — | ❌→补 | US-USER-005/006/007/010/011 | DataGrid 操作列 5 按钮 + 右键菜单同项 | 补画操作列（或注明右键菜单承载） |
| B17 | 分页栏「共 10 条记录 · 第 1 / 2 页」+ 上一页/1/2/下一页 | 分页器 | 52px | ⚠️ | US-USER-001（分页+TotalCount） | UnifiedPaginationBar：首页/上一页/「x / y」/下一页/末页 + 每页N条 ComboBox + 「共 N 条记录」 | 形态对齐：补首页/末页按钮与每页条数选择器；文案改「共 N 条记录」 |
| B18 | 详情面板·用户头部（大头像60+姓名+状态徽标+@用户名） | 卡片 | — | ✅（形态微调） | US-USER-002 | UserViewControl：UserName 标题+基本信息组 | 保留；@前缀可留作视觉处理 |
| B19 | └ 操作行：编辑资料 / 重置密码 | 按钮 | — | ✅ | 编辑=US-USER-005；重置密码=US-USER-007（SuperAdmin，返回临时密码） | DetailToolbar EditCommand/ResetPasswordCommand | 保留；重置密码建议补「层级约束」提示文案 |
| B20 | └ 缺失按钮：切换状态 / 恢复用户 / 删除 | （漏画） | — | ❌→补 | US-USER-010/011/006 | ToggleUserStatusCommand/RestoreCommand/DeleteCommand（右键菜单+操作列均有） | 补画三入口（sysadmin 不可禁用/删除等约束可用 tooltip 表达） |
| B21 | 基本信息卡片：邮箱 / 手机号 / **所属科室（中医内科）** / **入职时间 2021-03-15** | 信息组 | — | ⚠️（半错） | 邮箱/手机号=US-USER-002 | UserViewControl 显示：真实姓名/拼音码/角色/手机号码/邮箱地址/最后登录/创建时间/更新时间；**无科室、无入职时间**（UserDetailDto 无此字段） | 保留邮箱/手机号；「所属科室」「入职时间」**删除**；建议补画拼音码/角色/创建时间/最后登录 |
| B22 | 角色分配卡片「可同时分配多个角色」（医生✔/系统管理员/药剂师 多选） | 多选组 | — | ❌❌ | 违背 US-USER-004 单角色模型（UserRole 单值枚举） | 编辑角色=UserEditControl 单选 ComboBox RoleOptions；无多角色存储 | **整卡删除**——严重编造：系统为单角色模型，「同时分配多个角色」不可能 |
| B23 | 权限设置卡片（门诊挂号/处方管理/病历管理/库存管理/系统设置 开关×5） | 开关组 | — | ❌❌ | 无（权限=角色静态映射，US-SHELL-003 菜单可见性矩阵；无按用户细粒度授权 US/存储） | 全库无 per-user permission toggle | **整卡删除**——严重编造 |
| B24 | 新建用户对话框·标题+副题「添加新用户并为其分配角色与权限」 | 对话框头 | 560px | ⚠️ | US-USER-004 | 实际为详情区内联 UserEditControl（Transitioner 切换），非浮动 Dialog；副题「与权限」措辞错（无权限分配） | 容器形态改内嵌编辑视图；副题删「与权限」 |
| B25 | └ 字段：用户名* | 输入框 | 必填 | ✅ | US-USER-004（唯一+保留名校验）；US-USER-005（不可变） | UserEditControl UserName TextBox + IsUserNameReadOnly | 保留 |
| B26 | └ 字段：姓名*（真实姓名） | 输入框 | 必填 | ✅ | US-USER-004 | RealName TextBox | 保留；标签统一「真实姓名」 |
| B27 | └ 字段：邮箱* | 输入框 | 标了必填 | ⚠️ | — | UserInputDto.Email 可选（EmailAddress 校验，无 Required） | 保留字段；去掉必填星号 |
| B28 | └ 缺失字段：拼音码（可修正）/ 手机号码 | （漏画） | — | ❌→补 | PinYinCode=可手动修正多音字（控件 ToolTip 明示）；PhoneNumber=UserInputDto 字段 | UserEditControl 拼音码/手机号码 TextBox | 补画两个字段 |
| B29 | └ 字段：角色*（请选择角色 下拉） | 下拉 | 必填 | ✅ | US-USER-004（指定角色） | Role ComboBox，选项=UserRole 四值（前台接待/医生/管理员/超级管理员） | 保留；下拉选项必须用枚举四值（不得出现药剂师） |
| B30 | └ 字段：密码* + 确认密码*（提示「至少 8 位，需包含字母和数字」） | 密码框 | 必填 | ⚠️ | Issue #1262：**密码可选**，不填用 Server 默认密码（appsettings:DefaultPasswords，US-USER-004） | UserInputDto Password/ConfirmPassword 可选 | 星号去掉；提示文案改「留空则使用默认密码；填写时≥6位」（DTO 最小 6 位；改密场景才是 8 位策略） |
| B31 | └ 缺失字段（编辑态）：账户状态（启用/禁用 下拉）+ 挂号费(元) + 备注 | （漏画） | — | ❌→补 | 状态=US-USER-010；挂号费=REG-BR-009（ToolTip 明示）；备注=UserInputDto.Remark | UserEditControl ShowStatus 状态下拉/RegistrationFee/备注卡片 | 编辑对话框补画三个字段 |
| B32 | └ 底部：取消 / 创建用户 | 按钮 | — | ✅ | US-USER-004 | SaveCommand/CancelCommand（DetailToolbar） | 保留；按钮文案随内嵌形态对齐 DetailToolbar（保存/取消） |

### C. account-settings（个人资料/账户设置）

代码基准：`Shell/Controls/AccountSettingsControl.xaml`（左260 导航栏：返回/头像+RealName+角色/RadioButton 个人资料·安全设置；右侧表单）；`Shell/ViewModels/AccountSettingsViewModel.cs`（SaveProfileAsync→ChangeProfileDto{RealName,PhoneNumber,Email}；ChangePasswordAsync 校验旧密码/≥8位/两次一致/新旧不同）；需求：US-SHELL-004（个人资料+密码合并页）、US-USER-003/008/009

| # | 设计元素 | 类型 | 现状值 | 结论 | US编号 | 代码文件 | 建议动作 |
|---|----------|------|--------|------|--------|----------|----------|
| C1 | 主内容区·患者工具栏残留 | 工具栏 | 同 A1 | ❌ | 无 | 复制残留 | **删除** |
| C2 | Page Header「账户设置 / 管理您的个人信息、安全设置和偏好」 | 页头 | — | ⚠️ | US-SHELL-004 | 控件无页头；左侧导航即入口 | 标题可保留但副题删「偏好」（无此 Tab） |
| C3 | Tabs Bar：个人信息 / 安全设置 / **偏好设置** | 页签 | 3 个 | ⚠️ | US-SHELL-004（两个分区） | RadioButton ×2：个人资料/安全设置（左侧竖排导航，非顶 Tabs） | 删除「偏好设置」Tab；形态改左栏 RadioButton 双项 |
| C4 | Personal Info Section·姓名* | 输入框 | 张明远 | ✅ | US-SHELL-004 BR1 / US-USER-008 | EditRealName（必填，空则警告） | 保留 |
| C5 | └ **工号（自动生成）** | 输入框 | 只读示意 | ❌ | 无（全库 grep 工号=0；UserDetailDto 无） | 无字段 | **删除** |
| C6 | └ 手机号码* | 输入框 | 标必填 | ⚠️ | US-USER-008（PhoneNumber 可空） | EditPhoneNumber（空则存 null，非必填） | 保留字段；去必填星号 |
| C7 | └ 电子邮箱 | 输入框 | zhang.my@lybtzys.com | ✅ | US-USER-008 | EditEmail | 保留（示例域名注意品牌规范应为 lybt 相关而非 lybtzys.com——品牌名规范禁止 LYBTZYS 字样，建议 @lybt.cn 与用户列表一致） |
| C8 | └ **所属科室***（中医内科）/ **职称***（副主任医师 下拉） | 下拉 | 必填 | ❌❌ | 无（用户域无科室/职称字段；科室概念仅存在于就诊信息文本） | UserDetailDto/UserInputDto 无 Department/Title | **两项删除** |
| C9 | Security Section：当前密码 / 新密码 / 确认新密码 | 密码框×3 | — | ✅ | US-SHELL-004 AC2 / US-USER-009（需旧密码；8位+复杂度策略） | OldPassword/NewPassword/ConfirmPassword + ChangePasswordCommand | 保留；新密码提示「至少 8 位」（VM 校验 ≥8 且 ≠旧密码） |
| C10 | **2FA Toggle Row「双重身份验证」（登录时输入手机验证码）** | 开关 | — | ❌❌ | 无（grep 两步验证/双因素(客户端)/TwoFactor UI=0；认证=账号密码+JWT） | 无任何 2FA 代码/配置 | **删除**——任务点名核查项，确认编造 |
| C11 | Preferences Section：界面语言/界面主题（跟随系统）/通知方式（站内+短信）/时区（UTC+8）/处方打印格式（A4） | 表单组 | 5 字段 | ❌（主题除外） | 主题切换=US-SHELL-005 BR3（浅色/深色一键切换，全局侧栏 ToggleButton，非本页表单） | 无语言/通知/时区/个人打印格式设置；ClinicSettings.Timezone 是**系统级**日界配置非用户偏好 | **整节删除**；如要表达主题切换，归全局侧栏深色模式开关（框架层） |
| C12 | Button Row：取消 / 保存更改 | 按钮 | — | ✅ | US-SHELL-004 AC3（保存→API，IDOR 仅本人） | SaveProfileCommand/GoBackCommand（取消实为返回） | 保留；「取消」语义=GoBack 返回 |
| C13 | Right Sidebar·Profile Card（大头像80+张明远+副主任医师·中医内科） | 侧卡 | — | ⚠️ | US-SHELL-004 | 左栏确有头像64+RealName+角色 EnumDesc | 形态迁移到左栏（无职称/科室后缀，仅角色名） |
| C14 | └ Profile Stats：年资历 12 / 诊疗人次 3.2K / 好评率 98% | 统计组 | — | ❌❌ | 无（无绩效/评价域模型） | 无字段无接口 | **删除** |
| C15 | Quick Actions Card：实名认证（已认证）/处方权限（已开通）/**收款账户（未绑定）** | 动作列表 | 3 项 | ❌❌ | 无（grep 实名认证/收款账户=0；处方权限=角色决定不可开通） | 无 | **整卡删除** |
| C16 | Activity Card 最近活动（登录系统 09:32/修改处方模板/更新个人信息 + 时间线） | 时间线 | 3 条 | ❌ | 最后登录时间=US-SHELL-004 BR3「登录信息只读」——但只是**一个字段**非活动流 | AccountSettingsControl 只有 LastLoginTime 文本行 | **删除活动流**；在资料区补只读「最后登录」行即可 |
| C17 | Version Info「v2.1.0 · .NET 8」 | 文本 | — | ⚠️ | — | 无版本展示控件；系统版本口径 AppInfoOptions.Version（当前 1.0.0） | 删除或改真实版本来源；「凌隐宝堂中医诊所管理系统」命名注意品牌规范（简称 lybt） |
| C18 | 缺失：只读信息组（用户名/角色/注册时间/最后登录） | （漏画） | — | ❌→补 | US-SHELL-004 BR3 | AccountSettingsControl 显示 UserName/Role/CreatedAt/LastLoginTime 只读 | 补画 4 行只读信息 |

### D. system-settings（诊所设置/系统设置）

代码基准：`Roles/LYBT.Desktop.Admin/Views/SystemSettingsView.xaml` + `SystemSettingsViewModel.cs`；服务端键：`ConfigurationWritePolicy.cs`（白名单节 App/Cors/Database/DesktopUpdate/Jwt/Logging/MemoryCache/Security/Session/Swagger/SystemAdmin；禁止 ConnectionStrings/Jwt:SecretKey/DefaultPasswords）；`AppInfoOptions`（App:Name/Version/Environment）；`ClinicSettingsOptions`（Name/Address/Phone/Department/LicenseNumber/Email/Timezone）；`ISystemSettingsService`（%LOCALAPPDATA%\LYBT\Desktop\system-settings.json：SystemName/HospitalName/ContactPhone/AutoBackupEnabled/BackupPath）

| # | 设计元素 | 类型 | 现状值 | 结论 | US编号 | 代码文件 | 建议动作 |
|---|----------|------|--------|------|--------|----------|----------|
| D1 | 主内容区·患者工具栏残留 | 工具栏 | 同 A1 | ❌ | 无 | 复制残留 | **删除** |
| D2 | Page Header「系统设置 / 首页 / 系统设置」 | 页头 | — | ✅ | — | VM PageTitle=「诊所设置」 | 保留面包屑；页名建议随代码叫「诊所设置」 |
| D3 | Clinic Info Card：诊所名称/详细地址/联系电话 | 输入框×3 | — | ✅ | US-CFG-006（clinic-settings.json 热更新） | ClinicName/ClinicAddress/ClinicPhone ← ClinicSettingsOptions.Name/Address/Phone | 保留 |
| D4 | └ **营业时间**（周一至周六 08:30-17:30） | 输入框 | — | ❌ | 无（grep 营业时间/BusinessHours=0） | ClinicSettingsOptions 无该字段 | **删除** |
| D5 | └ 缺失字段：科室/邮箱/许可证号 | （漏画） | — | ❌→补 | US-CFG-006（打印标题区驱动） | ClinicSettingsOptions.Department/LicenseNumber/Email + SystemSettingsView 已有 | 补画 3 字段（科室/许可证号/邮箱） |
| D6 | └ Save Button「保存更改」 | 按钮 | — | ✅ | US-CFG-006 | SaveAsync→SaveSettingsAsync（热更新） | 保留 |
| D7 | Feature Toggles Card：预约管理/短信通知/打印处方/电子病历 开关×4 | 开关组 | — | ❌❌ | 违背 US-CFG-004 BR1/BR2：v1.0 仅 OverwriteConflicts+DuplicateHerbMergeStrategy 两键；18 布尔开关已废弃 | FeatureToggleOptions.cs 注释明示「不回到已废弃的 18 布尔开关反模式」 | **整卡删除**——画的正是已被清理掉的反模式；预约/短信/电子病历概念亦无实体 |
| D8 | Data Management Card：导入数据(Excel/CSV)/导出数据/**自动备份**/**恢复数据** | 动作列表×4 | — | ⚠️（拆分） | 自动备份✅=US-SHELL-013（BackupManagementView，Sysadmin 域）+ 本页 AutoBackupEnabled 开关；导入=US-PAT-011 患者域（非系统设置）；**导出数据/从备份恢复**在本页无入口（恢复在 BackupManagementView「恢复所选备份」） | LocalDbBackupService/BackupManagementView | 重构：①「自动备份」以开关+路径形式并入数据库配置卡（对齐 AutoBackupEnabled/BackupPath）；②导入/导出/手动备份/恢复移除——备份恢复指向 BackupManagementView 入口即可 |
| D9 | Security Settings Card：密码最小长度 8 位 | 输入框 | — | ⚠️ | 密码策略=US-USER-009（8位+大小写+数字+特殊字符）——**写死在策略里，不是可配置项** | PasswordPolicyValidator；SecurityOptions 无 MinLength 键 | 删除输入框（如表达策略，做只读说明文字） |
| D10 | └ 登录失败锁定（5次后锁定30分钟） | 输入框 | — | ⚠️ | 机制=US-AUTH-002；阈值=SecurityOptions.AccountLockout.MaxFailedCount/LockoutMinutes——Server 配置节，且 DefaultPasswords 类敏感节禁运行时改 | ConfigurationWritePolicy（Security 节白名单内但此项语义为运维改 appsettings，非本页表单） | 从本页删除；如需展示归 Sysadmin 配置中心（ConfigurationCenterViewModel） |
| D11 | └ 会话超时 30 分钟 | 输入框 | — | ⚠️ | Session:TimeoutMinutes 真实存在（SessionOptions） | SessionOptions.TimeoutMinutes；同样属 Server 配置节 | 同 D10：移出本页 |
| D12 | └ **双因素认证开关** | 开关 | — | ❌ | 无（同 C10） | 无 | **删除** |
| D13 | └ Save「保存安全设置」 | 按钮 | — | ❌ | 随整卡 | — | 随 D9-D12 删除 |
| D14 | 缺失 Section：基本设置（系统名称/医院名称/联系电话） | （漏画） | — | ❌→补 | Epic #1832 Phase 2（本地系统设置持久化） | SystemSettingsService.SystemName/HospitalName/ContactPhone + View「基本设置」卡已实现 | 补画基本设置卡 |
| D15 | 缺失 Section：数据库配置（自动备份开关+备份路径+浏览…） | （漏画） | — | ❌→补 | US-SHELL-013 关联 | View 已有「数据库配置」卡（AutoBackupEnabled Toggle+BackupPath+浏览按钮） | 补画 |
| D16 | 缺失 Section：服务器配置（应用名称 ServerAppName 可改/版本号/运行环境 只读+保存/验证/刷新） | （漏画） | — | ❌→补 | US-CFG-001/002（GET）、US-CFG-005（PUT/validate） | LoadServerConfigAsync 读 App:Name/App:Version/App:Environment；SaveServerConfigAsync PUT App:Name；ValidateConfigAsync POST validate | **补画服务器配置卡**：应用名称（可编辑，PUT App:Name 白名单内）、版本号/运行环境（OneWay 只读）、保存服务器配置/验证配置/刷新三按钮——这是本页与 SystemConfigurationService 真实配置键的核心对应区 |
| D17 | Status Bar（页面级）「系统版本 v2.1.0 / 管理员: admin@clinic.com」 | 状态条 | — | ❌ | — | 全局状态栏无版本/邮箱；内容=API状态/连接模式/用户名/时间（MainWindow.xaml） | 删除该页面级状态条（与全局状态栏重复且内容编造；admin@clinic.com 无此账号约定） |
| D18 | 底部状态栏（●API已连接/远程模式/2026-08-23 10:00） | 状态栏 | — | ✅（归属⚠️） | US-SHELL-007 AC5 | MainWindow.xaml 状态栏 | 同 A17：保留内容，归全局状态栏，时间含时分秒 |

---

## 三、反向覆盖检查（需求 → 设计漏画汇总）

| 来源 | 内容 | 状态 |
|------|------|------|
| AdminHomeView 8 卡片 | 药材/患者/验方/医案/统计报表/审计日志 6 卡未画（A11/A12） | ❌漏画 |
| US-USER-001 role/status 筛选 | 两个下拉未画（B4/B5） | ❌漏画 |
| DataGridToolbar 批量启用/禁用 | 未画（B6 合并成了「批量操作」下拉） | ⚠️形态错 |
| 行内/右键操作列 5 项 | 未画（B16/B20） | ❌漏画 |
| UserEditControl 拼音码/手机号/状态/挂号费/备注 | 新建对话框缺 5 字段（B28/B31） | ❌漏画 |
| AccountSettingsControl 只读组 4 行 | 未画（C18） | ❌漏画 |
| SystemSettingsView 基本设置/数据库配置/服务器配置三卡 | 未画（D14/D15/D16） | ❌漏画 |

---

## 四、修改点清单（属性级执行摘要）

### ❌ 删除（41 项，按页分组）
- **admin-home（12）**：患者工具栏残留整条(A1)；管理员信息组(A3)；设置入口icon(A4)；统计卡4张整行(A5)；数据导出入口(A9)；查看全部链接(A10)；系统状态卡整卡(A14)；最近活动整卡(A15)；待办事项整卡(A16，最严重编造)；「陈医生/医生」顶栏用户组（各页通用，见下）
- **user-management（8）**：患者工具栏残留(B1)；邮箱列(B12)；「药剂师」角色全部出现处(B11/B22)；「待激活」状态值(B13)；所属科室行(B21)；入职时间行(B21)；角色分配多选卡(B22)；权限设置开关卡(B23)
- **account-settings（17）**：患者工具栏残留(C1)；偏好设置Tab(C3)；工号(C5)；所属科室(C8)；职称(C8)；**双重身份验证开关(C10)**；偏好设置整节5字段(C11)；Profile Stats 三统计(C14)；快捷操作卡3项（实名认证/处方权限/收款账户）(C15)；最近活动卡(C16)；版本信息(C17 或改标)
- **system-settings（4）**：患者工具栏残留(D1)；营业时间(D4)；功能开关整卡4开关(D7)；安全设置整卡4项+保存(D9-D13)；页面级Status Bar(D17)
- **4 页通用（外壳残留，交框架轮处理）**：顶部应用栏48px 整条（含「陈医生/医生」——管理员场景角色文案错误）；左侧导航「用户管理/药材验方」菜单项（侧栏仅主页）；「导航分组标题」

### ⚠️ 改标（22 项）
1. admin-home 页头→居中「系统管理工作台」标题形态（A2）；快捷面板→3×3 居中卡片网格（A13）；「系统设置」卡名→「诊所设置」（A8）；状态栏时间含时分秒（A17）
2. users 副题计数并入分页栏（B2）；搜索占位→「搜索用户名 / 姓名」（B3）；批量操作下拉→显式按钮排（B6）；角色徽标值→枚举四值（B11）；状态徽标值→启用/禁用（B13）；最后登录列保留需补绑定（B14）；分页器→UnifiedPaginationBar 形态（B17）；新建对话框→内嵌编辑视图+副题删「与权限」（B24）；邮箱去必填星（B27）；密码去必填星+提示改默认密码规则（B30）
3. account 页头副题删「偏好」（C2）；Tabs→左栏双 RadioButton（C3）；手机去必填星（C6）；示例邮箱域名对齐 @lybt.cn（C7）；侧栏头像形态迁左栏（C13）
4. system 页名→「诊所设置」（D2）；数据管理卡拆解（D8）；密码策略/锁定/会话超时→移出本页（D9-D11）

### ✅ 保留（22 项）
admin-home：快捷面板容器(A6)、用户管理卡(A7)、诊所设置卡(A8)、状态栏内容(A17)；
users：勾选列(B8)、姓名列(B9)、用户名列(B10)、角色列容器(B11)、状态列容器(B13)、详情头(B18)、编辑/重置密码(B19)、分页容器(B17)、新建对话框用户名/姓名/角色/取消创建(B25/B26/B29/B32)；
account：姓名(C4)、邮箱(C7)、密码三框(C9)、取消/保存(C12)；
system：页头(D2)、诊所名称/地址/电话(D3)、保存更改(D6)、状态栏内容(D18)

### ➕ 补画（13 组）
admin-home 补 6 卡片（A11/A12）；users 补角色/状态筛选（B4/B5）、手机号列（B15）、操作列（B16）、切换状态/恢复/删除按钮（B20）、编辑态状态/挂号费/备注字段（B31）、拼音码/手机号字段（B28）；account 补只读信息组（C18）；system 补基本设置/数据库配置/服务器配置三卡（D14/D15/D16，其中服务器配置卡承载 App:Name 可写 + App:Version/App:Environment 只读 + 验证/刷新，对应 US-CFG-001/002/005）

---

## 五、给下一轮设计稿的硬约束

1. **AdminHome = 纯导航页**：居中标题 + 3×3 八卡片（用户管理/药材管理/患者管理/验方管理/医案管理/诊所设置/统计报表/审计日志），每卡=图标+文字 Button；**不画统计数字、待办、活动流、健康状态**（无 US 无 API）。
2. **用户模型铁律**：UserRole 单选四值（前台接待=0/医生=1/管理员=10/超级管理员=100）；CommonStatus 两值（启用/禁用）；**没有**药剂师、待激活、多角色、按用户权限开关、工号、科室、入职时间。
3. **AccountSettings 只有两块**：个人资料（姓名/手机号/邮箱 + 只读用户名/角色/注册时间/最后登录）+ 安全设置（旧/新/确认密码）；无 2FA、无偏好设置。
4. **诊所设置三卡照抄 SystemSettingsView**：基本设置（system-settings.json）/ 诊所信息（clinic-settings.json 六字段）/ 数据库配置（自动备份+路径）/ 服务器配置（App:Name 可写、Version/Environment 只读、保存/验证/刷新）；功能开关只有 FeatureToggles 两键且无 UI。
5. 各页不要复用患者列表工具栏模板；主内容区顶部工具栏必须来自目标页自己的 ViewModel 命令。

> 元素树快照：`docs/compose/reports/_pen_mainzone_trees_admin.txt`（66KB，本次解析产物，供复查）
