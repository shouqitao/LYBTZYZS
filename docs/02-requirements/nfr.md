# 非功能性需求 (NFR) 规格

## 概述

本文档定义凌隐宝堂中医诊所管理系统的非功能性需求，涵盖性能指标、数据量预估、可用性与可靠性、安全性四个维度。所有指标基于小型中医诊所 (1-3 名医生同时操作) 的典型场景设定。

---

## NFR 编号规则

| 组成 | 格式 | 示例 |
|------|------|------|
| 前缀 | `NFR` | 固定 |
| 类别缩写 | `PERF` / `DATA` / `AVAIL` / `SEC` | 见下文 |
| 序号 | 三位数字 | `001`, `002`, ... |
| 完整格式 | `NFR-{CATEGORY}-{NNN}` | `NFR-PERF-001` |

---

## 1. 性能指标

### NFR-PERF-001: API 响应时间

- **指标级别**: P95 (95% 请求应在指定时间内完成)
- **分级标准**:

| 操作类型 | 目标 (P95) | 示例 | 当前配置参考 |
|----------|-----------|------|-------------|
| 简单查询 (单实体 CRUD) | < 500ms | GET /api/v1/patients/{id} | 慢查询阈值 1000ms |
| 列表查询 (分页) | < 1s | GET /api/v1/herbs?page=1&pageSize=20 | 默认分页 20 条 |
| 复杂聚合 (跨表操作) | < 2s | POST /api/v1/medical-cases (含 Consultation + Prescription) | 数据库命令超时 30s |
| 批量导入 | < 5s | POST /api/v1/herbs/import (Excel/JSON 文件) | API 请求超时 60s |

- **验收标准**:
  - [ ] 在标准数据量 (患者 5000 + 医案 25000) 下，上述 P95 指标达标
  - [ ] 慢查询监控 (SlowQueryThresholdMs=1000) 在生产环境启用

### NFR-PERF-002: Desktop 客户端响应

| 指标 | 目标 | 说明 |
|------|------|------|
| 启动时间 (双击 -> 登录页) | < 5s | WPF + Prism 初始化 + SQLite 检查，非关键模块后台延迟加载 |
| 页面切换 (导航到新模块) | < 1s | Prism Region 导航 + ViewModel 初始化 + 首屏数据加载 |
| 表单保存响应 | < 2s | 含网络往返 (远程模式) 或本地写入 (本地模式) |
| 搜索响应 (防抖后) | < 1s | 输入停止后触发搜索 -> 结果渲染完成 |

- **验收标准**:
  - [ ] 冷启动 (首次打开) 在 5 秒内显示登录页
  - [ ] 热启动 (最小化恢复) < 1s
  - [ ] 模块间导航无白屏闪烁

### NFR-PERF-003: 客户端运行环境

| 级别 | 内存 | 说明 |
|------|------|------|
| 最低 | 4 GB | Windows 10 自身占 ~2 GB，可运行但较紧张 |
| **推荐** | **8 GB** | 舒适运行，可同时开其他办公软件 |
| 理想 | 16 GB | 无任何顾虑 |

| 组件 | 要求 |
|------|------|
| 操作系统 | Windows 10 及以上 |
| 运行时 | .NET 8 Desktop Runtime |
| 磁盘 | 应用 ~100 MB + SQLite 数据库 (本地模式) |
| 网络 | 远程模式需局域网连接到 Server |

> Desktop 应用典型内存占用: ~90-160 MB (WPF 框架 + Prism + 数据 + 缓存)。缓存部分 < 5 MB。

- **验收标准**:
  - [ ] 4 GB 内存 PC 上可正常启动和使用全部功能
  - [ ] 应用内存占用不超过 200 MB (日常使用)

### NFR-PERF-004: 并发能力

| 指标 | 目标 | 当前配置 |
|------|------|---------|
| 同时操作用户数 | 1-3 人 | 数据库连接池 MaxConnections=20 |
| 全局 API 速率限制 | 200 次/分钟 | SecurityOptions.GlobalLimit |
| 登录端点速率限制 | 5 次/窗口 (内网 20 次) | SecurityOptions.LoginLimit |
| API 端点速率限制 | 100 次/分钟 (Admin 200 次) | SecurityOptions.ApiLimit |

- **说明**: 当前连接池 (20) 对 1-3 并发用户已充分冗余
- **速率限制**: v1.0 已定义配置但未启用 (MVP 阶段)，计划在生产部署时启用

---

## 2. 数据量预估

### NFR-DATA-001: 数据规模

| 实体 | 年新增 | 5 年总量 | 单条大小估算 | 说明 |
|------|--------|---------|-------------|------|
| 患者 (Patient) | 300-500 | ~2500 | ~1KB | 总量上限 5000 |
| 医案 (MedicalCase) | 2000-5000 | ~25000 | ~3KB (含 Consultation + Prescription) | 日均 5-15 个 |
| 药材 (Herb) | 极少新增 | 600-1000 | ~0.5KB | 基于中药典，偶尔自定义扩展 |
| 验方 (Formula) | 50-100 | 200-500 | ~2KB (含 FormulaHerbItems) | 个人 + 共享 |
| 处方药材 (PrescriptionHerbItem) | 10000-25000 | ~125000 | ~0.2KB | 每处方平均 5-10 味药 |
| 系统日志 (SystemLog) | ~100000 | 保留 90 天 ~25000 | ~0.3KB | 自动清理 |
| 安全审计日志 (SecurityAuditLog) | ~5000 | 保留 1 年 ~5000 | ~0.5KB | 登录/登出/权限变更 |

### NFR-DATA-002: 数据库容量预估

| 数据库 | 5 年预估容量 | 说明 |
|--------|-------------|------|
| SQL Server (远程) | ~200MB 数据 + ~50MB 索引 | 含日志表定期清理后 |
| SQLite (本地) | ~100MB | 同步数据子集，不含完整日志 |

- **说明**: 数据量级别 (万级) 不需要分区策略或读写分离
- **分页策略**: 默认 20 条/页，可选 [10, 20, 50, 100]，足以覆盖所有列表场景

### NFR-DATA-003: 索引策略

| 实体 | 关键索引 | 理由 |
|------|---------|------|
| Patient | Name, PhoneNumber, IdCardNumber | 搜索频率高 |
| MedicalCase | PatientId + VisitDate, DoctorId, Status | 按患者查询 + 按日期排序 |
| Herb | Name, PinyinName, Category | 处方中按名称/拼音搜索 |
| Formula | Name, CreatedBy, IsShared | 按名称搜索 + 按创建人筛选 |

- **说明**: 当前数据量下，B-Tree 索引足以满足性能要求，不需要全文检索

---

## 3. 可用性与可靠性

### NFR-AVAIL-001: 数据备份策略

| 数据库 | 备份方式 | 频率 | 保留期 | 存储位置 |
|--------|---------|------|--------|---------|
| SQL Server | 自动全量备份 | 每日 | 30 天 | 服务器本地磁盘 |
| SQLite | 应用启动时自动备份 | 每次启动 | 7 天 (最多 7 个备份文件) | 本地 Backup 目录 |

- **SQL Server 备份**: 使用 SQL Server Agent 或维护计划，备份文件命名 `LYBTDB_{yyyyMMdd}.bak`
- **SQLite 备份**: Desktop 应用启动时复制 `.db` 文件到 `{AppData}/LYBT/Backup/lybt_{yyyyMMdd}.db`
- **验收标准**:
  - [ ] SQL Server 备份文件可成功还原
  - [ ] SQLite 备份在启动过程中不阻塞用户操作 (后台异步)
  - [ ] 超过保留期的备份文件自动删除

### NFR-AVAIL-002: 故障恢复目标

| 指标 | 目标 | 说明 |
|------|------|------|
| RTO (恢复时间目标) | 30 分钟 | 从发现故障到恢复服务 |
| RPO (恢复点目标) | 24 小时 | 最多丢失 1 天的数据 |
| 降级模式 | 本地模式即时可用 | 服务器故障时切换到 SQLite 本地模式继续工作 |

- **恢复优先级**: 本地模式降级 (即时) > 从备份还原 (30min 内) > 重新部署 (1h 内)
- **验收标准**:
  - [ ] 服务器不可达时，手动切换本地模式后 < 30 秒可继续使用核心功能
  - [ ] SQL Server 备份还原流程有文档化的操作手册

### NFR-AVAIL-003: 数据库重试与容错

| 配置 | 值 | 说明 |
|------|---|------|
| 最大重试次数 | 3 | 瞬时故障自动重试 |
| 基础延迟 | 1000ms | 指数退避 |
| 最大延迟 | 10000ms | 重试间隔上限 |

- **当前实现**: EF Core `EnableRetryOnFailure` + 自定义 `RetryPolicyOptions`
- **验收标准**:
  - [ ] 数据库瞬时连接失败 -> 自动重试 3 次后恢复
  - [ ] 数据库持续不可用 -> 友好错误提示 + 建议切换本地模式

---

## 4. 安全性

### NFR-SEC-001: 认证安全

| 配置 | 值 | 说明 |
|------|---|------|
| AccessToken 有效期 | 30 分钟 | JWT |
| RefreshToken 有效期 | 7 天 | 单次使用，Token Family 追踪 |
| 会话绝对过期 | 30 天 | AbsoluteExpiration |
| 不活跃超时 | 5 分钟 | 客户端检测，提前警告 |
| 登录限流 | 5 次/窗口 | 防暴力破解 |

### NFR-SEC-002: 密码策略

| 配置 | 值 |
|------|---|
| 最小长度 | 8 字符 |
| 要求数字 | 是 |
| 要求小写字母 | 是 |
| 要求大写字母 | 是 |
| 要求特殊字符 | 是 |
| 首次登录强制修改 | 是 |
| 默认密码有效期 | 30 天 |

### NFR-SEC-003: 数据传输安全

| 场景 | 措施 | 说明 |
|------|------|------|
| 远程模式 (HTTP) | HTTPS + HSTS | 生产环境强制 HTTPS，HSTS max-age=1 年 |
| 本地网络 | 内网环境 | 诊所内部局域网，物理安全可控 |
| 安全响应头 | 全套 | CSP / X-Frame-Options / XSS-Protection / Referrer-Policy |

### NFR-SEC-004: SQLite 本地数据加密

- **策略**: 字段级加密
- **加密范围**:

| 实体 | 加密字段 | 加密方式 | 理由 |
|------|---------|---------|------|
| Patient | IdNumber | AES-256 + DPAPI 密钥保护 | 身份证号为高敏感个人信息 |
| Patient | PhoneNumber | AES-256 + DPAPI 密钥保护 | 电话号码为敏感联系方式 |

- **非加密字段**: 姓名、性别、年龄、医案内容等非直接标识信息保持明文，保证查询性能
- **密钥管理**: 使用 Windows DPAPI (已有 AutoLoginToken 加密基础设施) 保护 AES 密钥
- **验收标准**:
  - [ ] SQLite 文件中 IdCardNumber 和 PhoneNumber 字段不可直接读取明文
  - [ ] 应用正常运行时，加密/解密对用户透明
  - [ ] 加密字段不支持 SQLite 层面的 LIKE 搜索 (搜索在解密后的内存中执行)
  - [ ] DPAPI 密钥绑定当前 Windows 用户，更换用户需重新同步数据

> **已确定**: v1.0 采用字段级加密而非整库加密 (SQLCipher)。理由: (1) 仅 2 个字段需保护，整库加密性价比低; (2) DPAPI 密钥管理基础设施已有 (CredentialVault); (3) 不影响非敏感字段的查询性能。

#### 敏感数据分级标准

| 级别 | 定义 | 字段示例 | 所属实体 | 存储保护 | 日志保护 |
|------|------|---------|---------|---------|---------|
| L1-高敏感 | 可直接标识个人身份或联系方式 | IdNumber, PhoneNumber | Patient | AES-256 加密存储 (仅 SQLite) | 完全脱敏 / 部分脱敏 |
| L2-一般敏感 (个人) | 个人敏感信息 | Address, AllergyHistory, MedicalHistory | Patient | 明文存储 + 访问控制 | 摘要脱敏 |
| L2-一般敏感 (医疗) | 医疗诊断信息 | TcmDiagnosis, PresentIllness, TongueDiagnosis, PulseDiagnosis | Consultation | 明文存储 + 访问控制 | 不记录到日志 |
| L3-普通 | 业务标识信息 | Name, Gender, BirthDate, HerbName | 各实体 | 明文存储 | 正常记录 |

> L1 字段在 SQLite 本地库中加密存储；SQL Server 远程库依靠网络传输加密 (HTTPS) + 数据库访问控制保护，不做字段级加密。

#### 日志脱敏规则

| 敏感级别 | 脱敏方式 | 示例 |
|----------|---------|------|
| L1 (IdNumber) | 保留前3后4，中间星号 | `320***********1234` |
| L1 (PhoneNumber) | 保留前3后4，中间星号 | `138****5678` |
| L2 (Address) | 保留前6字符，其余星号 | `南京市鼓楼区***` |
| L2 (AllergyHistory/MedicalHistory) | 仅记录字段有值/无值 | `[已填写]` / `[未填写]` |
| L2 (TcmDiagnosis 等诊断字段) | 不记录到日志 | - |
| L3 | 正常记录 | 原文 |

> 日志脱敏在 Serilog Enricher 层实现，对业务代码透明。已有 SensitiveDataMaskingEnricher 基础设施，需扩展支持上述规则。

#### 实现路径: EF Core Value Converter

```
SQLite DbContext 配置:
  Patient.IdNumber     → EncryptedStringConverter (写入加密 / 读取解密)
  Patient.PhoneNumber  → EncryptedStringConverter (写入加密 / 读取解密)

EncryptedStringConverter:
  ConvertToProviderExpression = plainText => AesEncrypt(plainText, key)
  ConvertFromProviderExpression = cipherText => AesDecrypt(cipherText, key)

密钥获取:
  IEncryptionKeyProvider.GetKey() → DPAPI 解密存储的 AES 密钥
```

- 仅在 SQLite DbContext 中配置 Value Converter，SQL Server DbContext 不加密
- 对 Repository/Service 层完全透明，无需修改业务代码
- 加密字段在数据库中存储为 Base64 编码的密文字符串
- 搜索限制: 加密字段仅支持精确匹配 (先加密搜索值再比对) 或内存过滤

#### 密钥生命周期管理

| 阶段 | 操作 | 说明 |
|------|------|------|
| 首次启动 | 自动生成 AES-256 密钥 | 256-bit 随机密钥，Base64 编码 |
| 密钥存储 | DPAPI 加密后写入 CredentialVault | 绑定当前 Windows 用户账户 |
| 运行时使用 | IEncryptionKeyProvider 按需获取 | 启动时解密一次，内存缓存 |
| 密钥丢失 | 重新同步数据 | 从 Server 端下载患者数据，本地重新加密写入 |
| 用户切换 | 密钥不可跨用户 | Windows 用户变更后，本地加密数据不可读，需重新同步 |

> v1.0 不实现密钥轮换 (Key Rotation)。如需轮换，需要解密全部数据 → 生成新密钥 → 重新加密，复杂度较高，待 v2.0 评估。

#### 数据迁移策略 (明文 -> 加密)

- **场景**: 已有本地 SQLite 数据库中的 IdCardNumber/PhoneNumber 为明文，升级后需加密
- **方式**: EF Core Migration 脚本
- **流程**:
  1. 读取所有 Patient 记录的 IdCardNumber/PhoneNumber 明文值
  2. 使用 AES-256 加密
  3. 更新回数据库
  4. 验证: 随机抽样解密确认数据完整性
- **回退**: 保留迁移前的 SQLite 备份文件
- **验收标准**:
  - [ ] 迁移后所有患者记录的加密字段可正常解密
  - [ ] 迁移过程 < 30 秒 (5000 条患者记录)
  - [ ] 迁移失败自动回退，不影响现有数据

### NFR-SEC-005: 审计日志保留

| 日志类型 | 保留期限 | 清理方式 |
|----------|---------|---------|
| 安全审计日志 (SecurityAuditLog) | 1 年 (365 天) | SecurityAuditCleanupService 后台定时清理 |
| 系统日志 (SystemLog) | 90 天 | LogCleanupService 后台定时清理 (每 24h, 每批 1000 条) |
| 文件日志 (Serilog File) | 30 天 (30 个文件) | Serilog RollingFile 自动管理 |

- **验收标准**:
  - [ ] 安全审计日志 365 天内可查询
  - [ ] 超期日志自动清理，不需要手动干预

---

## 5. 缓存策略

### 5.1 Server 端 OutputCache 策略

> 基于 ASP.NET Core OutputCache 中间件，按标签分组管理。

| 缓存策略 | 过期时间 | 标签 | 挂载端点 |
|----------|---------|------|---------|
| HerbsCache | 30 分钟 | `herbs` | GET /api/v1/herbs (列表) |
| FormulasCache | 2 小时 | `formulas` | GET /api/v1/formulas (列表) |
| PatientsCache | 30 分钟 | `patients` | GET /api/v1/patients (列表) |
| MedicalCaseCache | 20 分钟 | `medicalcases` | GET /api/v1/medicalcases (列表/搜索) |
| UserPermissionsCache | 10 分钟 | `permissions` | GET /api/v1/users (列表) |
| (默认策略) | 5 分钟 | - | 全局兜底 |

> PrescriptionsCache 策略已删除 -- 处方通过 MedicalCase 聚合根访问，无独立列表端点。

### 5.2 Server 端 MemoryCache 配置

| 参数 | 值 | 说明 |
|------|-----|------|
| SizeLimit | 100 MB | 硬上限，超过时自动压缩 |
| CompactionPercentage | 5% | 触发压缩时释放比例 |
| ExpirationScanFrequency | 60 秒 | 过期扫描频率 |
| DefaultExpiration | 5 分钟 | 默认过期时间 |

> 实际占用估算: < 7 MB (诊所数据量百~千级，远达不到上限)。

### 5.3 Server 端缓存失效映射

**原则**: 写操作成功后，调用 `IOutputCacheStore.EvictByTagAsync(tag)` 主动清除受影响的缓存标签。

| 模块 | 写操作 | 清除标签 | 跨模块原因 |
|------|--------|---------|-----------|
| Patient | 创建/更新/删除/恢复患者 | `patients` | - |
| Patient | 批量删除/导入 | `patients` | - |
| Patient | 状态切换 (FR-PAT-013) | `patients` | - |
| Herb | 创建/更新/删除/状态切换 | `herbs` | - |
| Formula | 创建/更新/删除 | `formulas` | - |
| MedicalCase | 创建医案 | `medicalcases`, `patients` | 患者 LastVisitTime/VisitCount 更新 |
| MedicalCase | 聚合保存/暂存草稿 | `medicalcases` | - |
| MedicalCase | 完成医案 | `medicalcases`, `patients` | 患者统计更新 |
| MedicalCase | 取消医案 | `medicalcases` | - |
| MedicalCase | 设置处方标记 | `medicalcases` | - |
| MedicalCase | 打印操作 | `medicalcases` | IsPrinted 变更 |
| User | 创建/更新/删除/角色变更 | `permissions` | - |

### 5.4 Desktop 端缓存策略

**ApiService GET 缓存** (全局):

| 参数 | 值 |
|------|-----|
| 缓存容量 | 1000 条 (逻辑单位) |
| 过期时间 | 5 分钟 (绝对过期) |
| 缓存键格式 | `GET:{url}` |

**写后失效规则**: 写操作 (POST/PUT/DELETE) 成功后，按模块前缀清除相关 GET 缓存。

| 写操作模块 | 清除缓存前缀 | 方式 |
|-----------|-------------|------|
| Patient | `GET:*/patients*` | `RemoveByPrefix` |
| Herb | `GET:*/herbs*` | `RemoveByPrefix` |
| Formula | `GET:*/formulas*` | `RemoveByPrefix` |
| MedicalCase | `GET:*/medicalcases*`, `GET:*/patients*` | `RemoveByPrefix` |
| 打印操作 | `GET:*/medicalcases*` | `RemoveByPrefix` |
| User | `GET:*/users*` | `RemoveByPrefix` |

**PatientSearchCache** (专用 LRU):
- 容量: 10 条，5 分钟过期
- 患者写操作后调用 `Invalidate()` 清除
- 会话切换时自动清理

### 5.5 内存占用估算

| 缓存层 | 上限 | 典型占用 | 说明 |
|--------|------|---------|------|
| Server OutputCache | TTL 自然淘汰 | < 2 MB | 1-3 用户，有限查询组合 |
| Server MemoryCache | 100 MB | < 5 MB | 诊所数据量百~千级 |
| Desktop ApiService | 1000 条 | < 2 MB | 单用户实际 50-100 个 GET |
| Desktop PatientSearchCache | 10 条 | < 0.1 MB | 极小 |

> 缓存总占用 (< 10 MB) 相对于应用本身 (~100 MB) 和系统内存 (4-16 GB) 完全可忽略。

- **验收标准**:
  - [ ] 数据修改后，同一用户下次查询获取到最新数据 (主动失效)
  - [ ] 内存缓存不超过 100MB 上限
  - [ ] Desktop 写操作后，相关模块的 GET 缓存被清除

---

## 6. API 公共约定

### NFR-API-001: 分页参数规范

所有分页查询端点统一遵循以下规范:

| 参数 | 类型 | 默认值 | 约束 | 说明 |
|------|------|--------|------|------|
| page | int | 1 | >= 1 | 页码 |
| pageSize | int | 20 | 1-100 | 每页条数 |

**验证规则**:
- `page < 1` 或 `pageSize < 1` 或 `pageSize > 100` -> 返回 HTTP 400
- 各模块使用本模块错误码范围内的分页错误码 (如 ERR-30602, ERR-60108)

**适用端点**:

| 模块 | 端点 | 分页错误码 |
|------|------|-----------|
| 患者管理 | GET /api/v1/patients | ERR-20705 |
| 药材管理 | GET /api/v1/herbs | ERR-50106 |
| 验方管理 | GET /api/v1/formulas | ERR-60108 |
| 医案管理 | GET /api/v1/medicalcases | ERR-30602 |
| 用户管理 | GET /api/v1/users | (FluentValidation) |

- **验收标准**:
  - [ ] 所有列表端点 page=0 -> 返回 400
  - [ ] 所有列表端点 pageSize=101 -> 返回 400
  - [ ] 所有列表端点未传分页参数 -> 使用默认值 page=1, pageSize=20

---

## 决策记录

| 编号 | 决策 | 理由 | 日期 | 状态 |
|------|------|------|------|------|
| NFR-D01 | API 响应时间采用四级分类 (简单/列表/聚合/导入) | 不同操作类型的合理预期不同，统一目标会导致过严或过松 | 2026-02-17 | 已确定 |
| NFR-D02 | 并发用户目标 1-3 人 | 小型中医诊所典型规模，当前连接池 (20) 已充分冗余 | 2026-02-17 | 已确定 |
| NFR-D03 | SQLite 采用字段级加密而非 SQLCipher | 仅 IdCardNumber/PhoneNumber 需保护；DPAPI 基础设施已有；不影响非敏感字段查询性能 | 2026-02-17 | 已确定 |
| NFR-D04 | 安全审计日志保留 1 年 | 医疗行业常见合规要求；系统日志 90 天足以满足日常排查需求 | 2026-02-17 | 已确定 |
| NFR-D05 | RTO=30min, RPO=24h | 诊所场景，本地模式提供即时降级兜底 | 2026-02-17 | 已确定 |
| NFR-D06 | 数据备份: SQL Server 日备 30 天 + SQLite 启动备份 7 天 | 平衡数据安全与存储成本 | 2026-02-17 | 已确定 |
| NFR-D07 | 缓存失效策略: 主动标签失效 + TTL 双保险 | 纯 TTL 不满足"修改后下次查询即更新"要求; 主动失效开销可忽略 (内存操作) | 2026-02-18 | 已确定 |
| NFR-D08 | PrescriptionsCache 策略删除 | 处方通过 MedicalCase 聚合根访问，无独立列表端点，缓存策略为死配置 | 2026-02-18 | 已确定 |
| NFR-D09 | 客户端推荐 8 GB 内存 | 应用典型占用 ~100-160 MB; 4 GB 可运行但紧张; 8 GB 可同时运行办公软件 | 2026-02-18 | 已确定 |
| NFR-D10 | 分页参数全局统一 | page>=1, pageSize 1-100, 默认 20。各模块使用本模块错误码范围，避免重复定义 | 2026-02-18 | 已确定 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-17 | v1.0 | 初始版本。Round 1 讨论产出，涵盖性能/数据量/可用性/安全 4 大维度 |
| 2026-02-18 | v1.1 | 信息保护深化: NFR-SEC-004 扩展 -- 敏感数据3级分级标准(L1/L2/L3)、日志脱敏规则(6种模式)、EF Core Value Converter实现路径、密钥生命周期管理(生成/存储/使用/丢失/切换)、明文到加密数据迁移策略 |
| 2026-02-18 | v1.2 | 缓存策略完整重写: 5 个子章节 (OutputCache 策略/MemoryCache 配置/失效映射表/Desktop 端策略/内存占用估算); 删除 PrescriptionsCache (NFR-D08); 新增 NFR-PERF-003 客户端运行环境 (推荐 8GB); 并发能力编号调整为 NFR-PERF-004 |
| 2026-02-18 | v1.3 | 新增 NFR-API-001 分页参数全局规范 (page>=1, pageSize 1-100); 各模块分页错误码统一注册 (ERR-20705/50106/60108/30602) |
