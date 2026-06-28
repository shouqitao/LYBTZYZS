# Card Reader (身份证读卡器)

> 版本: v1.0 | 日期: 2026-06-28 | 状态: ✅ 已完成
> Split from 11-platform.md (2026-06-28)

## 模块概述

身份证读卡器模块通过策略模式抽象多厂商读卡器接口（当前：华大 HD100 + Mock），实现一次刷卡自动填充患者信息并按 PRD-15 降级链匹配/创建患者。纯客户端硬件交互，不区分远程/本地模式。

> 原 2 US，保留 **2 US**：US-CARD-001~002。

---

### US-CARD-001: 身份证读卡（初始化+读取+自动读）

**角色**: 前台 / 医生 / 管理员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 诊所工作人员，**我想要** 通过身份证读卡器一键读取患者身份信息，**以便** 不必手动输入 18 位身份证号与其他身份字段，提升登记效率与准确性。

**验收标准**:
- [ ] 连接成功 → `IsConnected=true`
- [ ] 读卡成功 → 返回姓名/身份证号/性别/出生日期/住址
- [ ] 设备断开 → `ConnectionStateChanged` 事件触发
- [ ] 卡片插入 → `CardDetected` 事件触发
- [ ] 读卡失败 → `CardReadError` 事件，含 ErrorCode 与 ErrorMessage
- [ ] 支持自动轮询读卡（`StartAutoRead(intervalMs)` + 重复去重）

**业务规则**:
1. 策略模式 `ICardReader`，支持多厂商；`ICardReaderFactory.AutoDetectReaderAsync` 自动检测。
2. 读取信息含：姓名/性别/民族/出生日期/身份证号/住址；可选保存证件照片（DPAPI 加密）。
3. 设备参数从 `CardReaderOptions`（appsettings `["CardReader"]`）读取。
4. DEBUG 模式回退 `MockCardReader`；`InitializeAsync` 失败不阻塞应用启动。
5. P/Invoke `AccessViolationException` 捕获转错误码 -100，不传播崩溃。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 不适用（纯客户端硬件交互） |
| 本地 | 不适用（纯客户端硬件交互） |

**实现参考**: `src/Client/Desktop/Core/LYBT.Desktop.CardReader/Services/ICardReaderService.cs:10`、`HuaDaHD100CardReader`、`MockCardReader`

---

### US-CARD-002: 患者去重查找或创建（PRD-15）

**角色**: 前台 / 医生
**优先级**: Should
**状态**: ✅ 已实现

**作为** 诊所工作人员，**我想要** 读卡后系统自动匹配已有患者或快速创建新患者（PRD-15 降级链），**以便** 一次刷卡完成录入与匹配，消除重复建档。

**验收标准**:
- [ ] IdNumber 精确匹配 → `ExactMatch`，加载已有患者 + 就诊历史
- [ ] Name+BirthDate 模糊匹配 → `FuzzyMatch`
- [ ] 多条命中 → `MultipleCandidates`，UI 显示候选列表
- [ ] 未命中 → `NoMatch`，`QuickCreatePatient` 创建新患者
- [ ] 新创建 → `IsNewlyCreated=true`；已有 → `false`

**业务规则**:
1. `MatchPatientAsync` 实现完整降级链：ExactMatch → FuzzyMatch → MultipleCandidates → NoMatch。
2. `PatientMatchType` 枚举驱动 UI 分支。
3. 读卡数据自动映射：姓名→Name、身份证号→IdNumber、出生日期→BirthDate、性别→Gender。
4. 照片通过 `DpapiPhotoStorageService`（DPAPI LocalMachine 加密）存储于 `{AppDataLocal}/LYBT/photos/`。
5. 在患者列表页通过 `ReadCardCommand` 触发。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 读卡后通过 API 查询/创建患者 |
| 本地 | 读卡后通过本地数据源查询/创建患者 |

**实现参考**: `IPatientCardReaderIntegration`、`MatchPatientAsync`、`PatientMatchType`、`DpapiPhotoStorageService`

---

## 变更记录

| 版本 | 日期 | 变更 | 原因 |
|------|------|------|------|
| v1.0 | 2026-06-28 | Split from 11-platform.md into focused module | 文档结构优化 S4 批次 3 |
