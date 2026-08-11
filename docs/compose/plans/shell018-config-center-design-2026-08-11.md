# sysadmin 配置中心设计方案（T6 式需求深化，暂不实现）

> 日期：2026-08-11 | 状态：设计稿（待用户审批后进入实现）
> 依据：需求先行门禁——US-SHELL-018（Must，sysadmin 配置中心）为全新功能，先深化需求再设计
> 关联：11a-shell.md US-SHELL-018/017/019；ADR-0014；11b-configuration.md US-CFG-005/006；13-traceability-matrix v1.4（SHELL-018 🔴 唯一缺失项）

---

## 〇、总览

| 维度 | 现状（2026-08-11 代码扫描） | 本设计 |
|---|---|---|
| 客户端 7 组配置 | Options 类全就绪（ClinicSettings/ClientSession/ApiClient/DefaultPassword/FeatureToggle/CardReader）；**无统一配置面板 UI**（SysadminHomeView 仅运维仪表盘 2 状态卡片） | 配置中心面板（7 组分组编辑 + 保存 + 生效提示） |
| 功能开关热更新 | ✅ FeatureToggleService（T8：IConfiguration 动态读 + GetReloadToken 真热更新，feature-toggles.json） | 直接消费，加开关切换 UI |
| 诊所信息热更新 | ✅ ClinicSettingsService（clinic-settings.json + IOptions reloadOnChange，CFG-006） | 直接消费（保存即时生效的边界修正——见 1.3） |
| 服务端 Configuration API | ⚠️ 基础已就绪：GET / GET{key} / PUT{key} / PUT 批量 / POST validate（CFG-005）+ `ConfigurationWritePolicy` 白名单/黑名单 + `JsonFileConfigurationStore`（runtime-overrides.json 原子写） | 扩展：section 级 GET 脱敏 / POST restart / SysAdminOnly 策略 / 审计 / 限频（ADR-0014 待实施项） |
| 双模式布局 | ADR-0014 已锁范围（远程 2 面板 / 本地 1 面板 + 备份入口） | 按 ADR-0014 实施 |
| 备份恢复入口 | ✅ BackupManagementView（T7-2 已接 Sysadmin 模块） | 本地面板直接嵌入 |

**核心判断**：SHELL-018 的「配置面板 UI」是新代码，但**底层配置基建约 80% 已就绪**（写回白名单、热更新、Options 绑定、备份 UI）。实施以「面板 + API 扩展 + 模式布局」为主，不重复造配置存储轮子。

---

## 一、需求深化（从需求文本到可验收业务规则）

### 1.1 客户端配置面板（7 组 13 项——US-SHELL-018 清单逐项核对）

| # | 分组 | 配置项 | Options 类 / SectionName | 生效方式 | 校验规则（需求） | 面板形态 |
|---|------|--------|--------------------------|---------|------------------|---------|
| 1 | 诊所信息 | Name/Address/Phone/Department/LicenseNumber/Email | `ClinicSettingsOptions` / `ClinicSettings` | 重启（注 1） | 必填 Name；LicenseNumber 长度 ≤30 | 6 个文本框 |
| 2 | 会话设置 | InactivityTimeoutMinutes(1-120) / WarningBeforeTimeoutMinutes(0-10) / ActivityCheckIntervalSeconds(10-120) | `ClientSessionOptions` / `ClientSession` | 重启 | 数值区间（输入校验） | 3 个数字框 |
| 3 | 连接设置 | BaseUrl + TimeoutSeconds(5-300) + RemoteUrl/PreferredMode | `ApiClientOptions` / `ApiClient` | 重启 | TimeoutSeconds 5-300；BaseUrl 合法 URL + 测试连通按钮 | 文本框 + 「测试连通」 |
| 4 | 安全策略 | ForceChangeOnFirstLogin / NewUserPassword | `DefaultPasswordOptions` / `DefaultPasswords` | 重启 | NewUserPassword 满足密码策略（8+ 位/大小写/数字/特殊符） | 开关 + 密码框 |
| 5 | 功能开关 | OverwriteConflicts / DuplicateHerbMergeStrategy | `FeatureToggleOptions` / `FeatureToggles` | **热更新** | OverwriteConflicts 布尔；MergeStrategy ∈ {Skip,Update,Error,Max} | 开关 + 下拉 |
| 6 | 读卡器管理 | 厂家选择/诊断测试/连接状态/手动参数覆盖（UsbPort/ConnectTimeout/ReadTimeout） | `CardReaderOptions` + ICardReader + ICardReaderDiagnostics | 测试模式 UI | 自动检测优先，手动覆盖为兜底 | **US-SHELL-019 单独批次**（本期占位只读状态） |
| 7 | 系统信息 | 版本/DB 状态/连接状态 | DiagnosticsController / SysadminHome Dashboard | 只读 | — | 只读展示（已有） |

> 注 1：需求原文「诊所信息需重启生效」与 CFG-006 现状（ClinicSettingsService 热更新已实现）存在张力——**待确认项 A**：诊所信息保存后即时生效（现状能力）还是严格按需求重启生效（回滚热更新）？推荐：保留热更新（已实现且更优），文档同步修改生效方式为「热更新」。

### 1.2 双模式面板（ADR-0014 深化）

| 模式 | 面板布局 | 数据源 | 保存目标 |
|------|---------|--------|---------|
| **远程** | Tab ① 客户端配置（本机 7 组）→ Tab ② 服务端配置（Configuration API） | ① 本机 appsettings（IConfiguration）② `GET /configuration` 全节脱敏 | ① 本机 appsettings.json（原子写 + 备份）② `PUT /configuration/{section}`（白名单）→ 提示重启 + `POST /configuration/restart` |
| **本地** | 单面板：客户端 7 组 + LocalWebAPI 特有（OfflineMode/LocalApiBaseUrl/本地 Jwt 只读）+ 备份恢复入口（嵌入 BackupManagementView） | 本机 appsettings（全栈一层） | 本机 appsettings.json（同远程①） |

**服务端配置面板（仅远程）交互**：
1. 加载：`GET /configuration` → 按节分组渲染；敏感字段（Jwt.SecretKey/ConnectionStrings/DefaultPasswords）掩码 `***` + 🔒 图标
2. 编辑：业务参数行可编辑 → 保存调 `PUT /configuration/{section}`（白名单校验服务端兜底）→ 响应 `{ applied, restartRequired, effectiveMode }`
3. 重启：`POST /configuration/restart` 前**二次确认**（「所有在线用户将短暂断连，确认重启？」）→ 延迟 30 秒 StopApplication → 进程管理器自动拉起
4. 审计：每次 GET/PUT/Restart 写 SecurityAuditLog（操作人/时间/IP/变更 diff）——D1 审计链复用

### 1.3 保存与生效语义（客户端）

- **保存机制**：IConfiguration 是只读快照，不可直接写。方案：**仿服务端 `JsonFileConfigurationStore` 先例**——客户端新增 `ClientConfigurationStore`：读 appsettings.json → 内存修改 → 原子写（临时文件 + rename + `.bak.{timestamp}` 备份）→ 触发 `IConfiguration.Reload()`。
- **生效语义**（按需求 AC）：
  - 功能开关（FeatureToggles）→ 保存后**即时生效**（FeatureToggleService 热更新已支持）
  - 其他 5 组 → 保存成功提示「需重启生效」；本地模式提供「立即重启应用」按钮（Process.Restart 或提示手动重启——**待确认项 B**）
- **连接设置特殊性**：BaseUrl 保存后若当前模式为 Remote 且 URL 变化——需联动 `IConnectionModeService`（探测 + 提示重连）——**待确认项 C**：是否本期做联动（推荐：提示「重启生效」即可，联动归后续）。

### 1.4 边界与风险

| 风险 | 缓解 |
|------|------|
| appsettings.json 写坏致应用无法启动 | 原子写 + 启动前校验（现有 ProductionConfigurationValidator）+ `.bak` 备份可恢复 |
| 服务端 API 公网暴露（ADR-0014 D3 B+） | HTTPS 强制 + SysAdminOnly 策略 + 审计 + 脱敏 + 重启限频（每小时 ≤3） |
| 重启断连影响在线用户 | 30 秒延迟 + 二次确认 + SignalR 通知（ADR-0013 已建） |
| 密码/密钥误改 | DefaultPasswords 整节禁止（ConfigurationWritePolicy 已有）+ UI 隐藏编辑（只读展示掩码） |
| 双模式数据源混淆 | 面板按模式切换数据源绑定（远程 Tab ② 走 API client；本地不显示服务端 Tab） |

---

## 二、服务端 Configuration API 扩展（ADR-0014 待实施项落地）

### 2.1 端点设计（现有 ConfigurationController 扩展）

| 端点 | 现状 | 扩展 |
|------|------|------|
| `GET /configuration` | ✅ 返回 Dictionary<string,string>（**未脱敏**） | 脱敏：命中 `[SensitiveData]` 键（SecretKey/连接串/密码）→ `***`；按节分组返回 |
| `GET /configuration/{section}` | 🔴 无 | 新增：按节返回（脱敏）；节不存在 404 |
| `PUT /configuration/{section}` | ⚠️ 仅 PUT{key} 单键 | 新增：body 为该节字段字典 → `ConfigurationWritePolicy.IsAllowed` 逐键校验（白名单外 403）→ JsonFileConfigurationStore 原子写 → ReloadConfiguration → 返回 `{ applied, restartRequired, effectiveMode }` |
| `POST /configuration/restart` | 🔴 无 | 新增：SysAdminOnly + 限频（每小时 ≤3）→ 二次确认由客户端承担 → `IHostApplicationLifetime.StopApplication()` 延迟 30s |
| `POST /configuration/validate` | ✅ | 保留（生产配置校验） |

### 2.2 权限与安全

- **新策略 `SysAdminOnly`**：仅 `IsSysAdmin=true` 放行（与 AdminOrSuperAdmin 并存）——PUT/restart 双端策略（BaseCrudController 体系外——独立装饰）。
- **写回工具**：复用 `JsonFileConfigurationStore`（runtime-overrides.json——**不改源 appsettings**，覆盖文件 + baseline 比较，天然可回滚）。**待确认项 D**：服务端写回用 runtime-overrides.json（推荐——已实现，可回滚）还是 ADR-0014 原文的「备份 + 重写 appsettings.json」？
- **审计**：`SecurityAuditService`（T5-1 已建）记录 GET/PUT/Restart（操作人/时间/IP/diff）。

### 2.3 双端同步（架构约束）

ConfigurationController 在 **WebAPI + LocalWebAPI 双控制器树**——本地模式不暴露服务端 Tab，但 LocalWebAPI 端 Controller 仍按相同签名同步（本地「重启」语义 = 重启内嵌 LocalWebAPI——**待确认项 B 联动**）。

---

## 三、客户端配置面板（Desktop UI）

### 3.1 视图结构（SysadminHomeView 扩展）

```
SysadminHomeView（运维控制台 → 配置中心标题）
├── 模式感知布局（IConnectionModeService.CurrentMode）
│   ├── 远程：TabControl
│   │   ├── Tab「客户端配置」——7 组卡片（分组展开）
│   │   └── Tab「服务端配置」——Configuration API 面板（节列表 + 编辑 + 重启）
│   └── 本地：TabControl
│       ├── Tab「本地配置」——7 组 + LocalWebAPI 特有（OfflineMode/LocalApiBaseUrl）
│       └── Tab「备份恢复」——嵌入 BackupManagementView（T7-2 已有）
└── 全局：保存状态条（「已保存 · 需重启生效」/「已热更新生效」）+ 系统信息只读卡片（保留现有 Dashboard）
```

### 3.2 ViewModel 设计

| VM | 职责 | 依赖 |
|----|------|------|
| `ConfigurationCenterViewModel`（新增） | 模式感知布局 + Tab 协调 + 保存编排 | IConnectionModeService / IOptionsMonitor 各 Options / IClientConfigurationStore（新增） |
| `ClientConfigSectionViewModel`（新增，7 组复用） | 单组配置编辑（加载/校验/保存） | Options 绑定 + 校验规则映射 |
| `ServerConfigSectionViewModel`（新增，仅远程） | 服务端节列表 + 编辑 + 重启 | IApiClient（GET/PUT/restart 端点）+ ISecurityAuditService |
| `SysadminHomeViewModel`（扩展） | 保留仪表盘 + 嵌入配置中心区域 | — |

### 3.3 校验规则映射

| 组 | 校验（客户端镜像服务端） |
|----|------------------------|
| 会话 | InactivityTimeoutMinutes ∈ [1,120]；Warning ∈ [0,10]；Interval ∈ [10,120]；Warning ≤ Inactivity |
| 连接 | TimeoutSeconds ∈ [5,300]；BaseUrl Uri.TryCreate + http/https |
| 安全 | NewUserPassword 匹配 `PasswordPolicyValidator.Policy`（8 位/大小写/数字/特殊符） |
| 功能开关 | MergeStrategy ∈ {Skip, Update, Error, Max}（与 DuplicateHerbMergeStrategy 消费端一致） |

---

## 四、待确认项（审批前用户决策）

| # | 议题 | 选项 | 推荐 |
|---|------|------|------|
| A | 诊所信息生效方式 | ① 保留热更新（现状 ClinicSettingsService 已实现，即时生效）② 严格按需求重启生效（回滚热更新） | **①**——已实现且更优，文档同步改「热更新」 |
| B | 「重启生效」的重启语义（本地模式） | ① 仅提示手动重启 ② 提供「立即重启应用」按钮（Process.Restart）③ 本地模式重启 = 重启内嵌 LocalWebAPI 而非整个 Desktop | **③**——LocalWebAPI 重启即可让服务端配置生效，Desktop 会话不丢（配置读取时机差异需核） |
| C | 连接设置 BaseUrl 变更联动 | ① 仅提示重启生效 ② 联动 IConnectionModeService 探测 + 提示重连 | **①**——联动归后续批次，避免本期范围膨胀 |
| D | 服务端写回目标 | ① runtime-overrides.json 覆盖文件（JsonFileConfigurationStore 已实现，可回滚）② ADR-0014 原文「备份 + 重写 appsettings.json」 | **①**——已实现 + 天然可回滚；若选 ② 需新写 JSON 原子写工具 |
| E | 读卡器组本期范围 | ① 占位只读状态卡片（完整诊断归 US-SHELL-019 批次）② 本期一并实现 SHELL-019 | **①**——SHELL-019 是独立 Should US，分批实施 |

---

## 五、实施计划（审批后）

### Phase 1：服务端 Configuration API 扩展（远程面板前置依赖）
1. `SysAdminOnly` 策略（PolicyConstants + 认证管线扩展）
2. `GET /configuration/{section}` + `GET /configuration` 脱敏（SensitiveData 键掩码）
3. `PUT /configuration/{section}`（白名单逐键 → JsonFileConfigurationStore → Reload → 返回生效语义）
4. `POST /configuration/restart`（限频 + 延迟 30s StopApplication）
5. 审计接线（GET/PUT/Restart → SecurityAuditService）
6. **双端同步**：LocalWebAPI ConfigurationController 同签名扩展
7. 门禁：构建 0/0 + 架构测试 + Server Configuration 相关单测

### Phase 2：客户端配置面板（7 组）
8. `ClientConfigurationStore`（appsettings.json 原子写 + 备份 + Reload）
9. `ClientConfigSectionViewModel` × 7 组（加载/校验/保存）+ 功能开关热更新即时生效
10. SysadminHomeView 配置中心布局（分组卡片 + 保存状态条 + 系统信息保留）
11. 门禁：构建 0/0 + Desktop VM 单测

### Phase 3：双模式布局整合
12. 模式感知 Tab（远程 2 面板 / 本地 1 面板 + 备份恢复嵌入）
13. `ServerConfigSectionViewModel`（API 面板：节列表/编辑/二次确认重启）
14. 本地模式 LocalWebAPI 重启语义（待确认项 B 结论）
15. 门禁：构建 0/0 + 架构测试 + 双端手动验收（远程/本地切换）

### 文档同步（实施时）
- 11b-configuration.md：Configuration API 端点补全（section GET/PUT/restart）
- 11a-shell.md US-SHELL-018：状态 🧲→✅；生效方式按待确认项 A/B 结论修正
- 06-operations/02-configuration.md：sysadmin 远程配置管理段（ADR-0014 即时影响项）
- 13-traceability-matrix.md：SHELL-018 ✅（v1.5）

---

## 附：需求追溯

| 验收标准（US-SHELL-018） | 本设计覆盖 |
|--------------------------|-----------|
| SysadminHomeView 展示配置中心面板，分组显示 | §三.1 视图结构 |
| 诊所信息 6 项可编辑保存 | §三.2 组 1（生效方式待确认 A） |
| 会话设置 3 项可编辑 | §三.2 组 2 |
| 连接设置 BaseUrl + 测试连通 + Timeout | §三.2 组 3 |
| 安全策略 ForceChangeOnFirstLogin / NewUserPassword | §三.2 组 4 |
| 功能开关 2 项切换，热更新即时生效 | §三.2 组 5（FeatureToggleService 已就绪） |
| 读卡器参数可编辑（自动检测优先） | §三.2 组 6（SHELL-019 批次，待确认 E） |
| 系统信息只读展示 | §三.1 现有 Dashboard 保留 |
| 保存生效语义（重启 vs 热更新） | §1.3 + 待确认 A/B |

**依赖前置**：ADR-0014 待实施项（SysAdminOnly/写回/审计/限频）为 Phase 1 主体；T5-1 已建 SecurityAuditService、T8 已建 FeatureToggleService、T7-2 已建 BackupManagementView——均为本设计的现成积木。
