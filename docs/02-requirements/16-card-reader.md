# 身份证读卡器 产品需求文档

---

## 1. Problem Statement

### 1.1 问题描述

中医诊所日常接诊需要登记患者身份信息 (姓名、性别、出生日期、身份证号、住址)。手动输入身份证号码 18 位数字容易出错，且逐项填写耗时长，尤其在高峰时段严重影响挂号效率。通过集成二代身份证读卡器，一次刷卡即可自动填充全部身份字段，同时支持已有患者自动匹配，消除重复建档。

### 1.2 用户痛点

| 角色 | 痛点 | 影响 |
|------|------|------|
| 前台 | 手动输入身份证号 18 位数字，易错且慢 | 单次登记耗时 1-2 分钟，高峰期排队 |
| 医生 | 初诊患者需逐项录入基本信息，打断诊疗节奏 | 每日接诊 15-30 人，累计浪费 15-30 分钟 |
| 前台/医生 | 手动输入身份证号时无法自动匹配已有患者 | 重复建档导致就诊历史分散 |

### 1.3 证据

- 临床工作流观察: 手动录入身份证号平均耗时 30-60 秒，出错率约 5%
- 行业实践: 大多数医疗机构已采用读卡器自动采集患者身份信息
- 产品需求分析: 患者模块已具备 IdNumber 字段和查重逻辑，读卡器仅需对接数据入口

---

## 2. Target Users

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| Admin | 使用读卡器读取患者信息 |
| Doctor | 使用读卡器读取患者信息 |
| Receptionist | 使用读卡器读取患者信息 (前台挂号快速登记) |

> 读卡器功能仅在 Desktop 端可用，无服务端 API。纯客户端硬件交互，不区分远程/本地模式。

---

## 3. Strategic Context

### 3.1 业务目标

| 目标 | 对应 |
|------|------|
| 挂号效率提升 | 刷卡自动填充替代手动输入，单次登记从 1-2 分钟降至 3-5 秒 |
| 数据准确性 | 身份证芯片直读消除手动输入错误，准确率从 ~95% 提升至 100% |
| 患者去重 | 读卡后自动匹配已有患者，避免重复建档 |
| 硬件扩展性 | 策略模式抽象读卡器接口，支持多厂商设备无缝切换 |

### 3.2 Why Now

诊所已完成患者管理电子化，具备完整的患者 CRUD 和查重能力。读卡器集成是数据入口的"最后一公里"，将手动录入升级为硬件自动采集，与现有患者管理流程无缝衔接。

---

## 4. Solution Overview

身份证读卡器模块提供硬件设备集成能力，通过策略模式抽象多厂商读卡器接口，实现一次刷卡自动填充患者信息并匹配/创建患者记录。

**核心能力:**
- **设备抽象**: ICardReader 策略接口，支持多厂商读卡器 (当前: 华大 HD100 + Mock)
- **自动检测**: 工厂模式 (ICardReaderFactory) 自动检测可用设备，DEBUG 模式回退 MockCardReader
- **一键填充**: 刷卡读取身份证芯片信息 (姓名/性别/民族/出生日期/身份证号/住址)
- **患者匹配**: 按身份证号查询已有患者，未找到则快速创建 (FindOrCreatePatientAsync)
- **事件驱动**: ConnectionStateChanged / CardDetected / CardReadError 事件通知 UI

**操作流程:**
```
用户点击"读卡" -> 检测读卡器连接 -> [已连接] 读取身份证
                                   -> [未连接] 提示连接设备
读取成功 -> MatchPatientAsync(CardReadResult) 降级链:
    1. IdNumber 精确匹配 -> ExactMatch: 加载已有患者 + 就诊历史
    2. Name+BirthDate 模糊匹配 -> FuzzyMatch: 加载已有患者
    3. 多条命中 -> MultipleCandidates: UI 显示候选列表供用户选择
    4. 未命中 -> NoMatch: 快速创建新患者
读取失败 -> CardReadError 事件通知 -> 显示错误信息
```

---

## 5. Success Metrics

| 指标 | 当前 (手动输入) | v1.0 目标 | 衡量方式 |
|------|----------------|----------|---------|
| 单次登记耗时 | 1-2 分钟 | < 5 秒 (刷卡+匹配) | 操作日志时间差 |
| 身份信息录入准确率 | ~95% (手动) | 100% (芯片直读) | 数据校验 |
| 重复建档率 | 存在 (手动查重易遗漏) | 0% (自动匹配) | 重复 IdNumber 统计 |
| 读卡成功率 | N/A | > 95% | CardReadResult.IsSuccess 统计 |

---

## 6. Epic Hypothesis

We believe that 集成二代身份证读卡器实现一键刷卡填充 + 患者自动匹配 for 前台和医生 will achieve 挂号登记效率大幅提升和零重复建档。We'll know we're right when 单次登记耗时从 1-2 分钟降至 5 秒以内、身份信息准确率达到 100%、且重复建档率降为零。

---

## 7. User Stories

### 优先级汇总

| US 编号 | 名称 | Priority |
|---------|------|----------|
| US-CARD-001 | 身份证读卡器连接与读取 | Should |
| US-CARD-002 | 读卡数据填充到患者表单 | Should |

---

### US-CARD-001: 身份证读卡器连接与读取

> As a 诊所工作人员 (前台/医生), I want to 通过身份证读卡器一键读取患者身份信息,
> so that 我不必手动输入 18 位身份证号和其他身份字段，提升登记效率和准确性。

**Acceptance Criteria:**
- [ ] 连接成功 -> ICardReader.IsConnected=true
- [ ] 读卡成功 -> 返回姓名/身份证号/性别/出生日期/住址
- [ ] 设备断开 -> ConnectionStateChanged 事件触发
- [ ] 卡片插入 -> CardDetected 事件触发
- [ ] 读卡失败 -> CardReadError 事件通知，包含 ErrorCode 和 ErrorMessage

**Business Rules:**
1. 支持多厂商读卡器 (策略模式，ICardReader 接口)
2. 自动检测读卡器连接状态 (IsConnected)
3. 支持主动探测卡片 (DetectCardAsync)
4. 读取信息包含: 姓名、性别、民族、出生日期、身份证号、住址
5. 可选保存证件照片 (savePhoto 参数)
6. 连接状态变更通过 ConnectionStateChanged 事件通知
7. 卡片插入通过 CardDetected 事件通知
8. 设备参数 (端口/超时等) 从 appsettings.json `["CardReader"]` 节读取 (CardReaderOptions)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 不适用 (纯客户端硬件交互) |
| 本地 | 不适用 (纯客户端硬件交互) |

### US-CARD-002: 读卡数据填充到患者表单

> As a 诊所工作人员 (前台/医生), I want to 读卡后系统自动匹配已有患者或快速创建新患者,
> so that 我不必手动查重和填写表单，一次刷卡完成患者信息的录入和匹配。

**Acceptance Criteria:**
- [ ] 身份证号已存在 -> 返回已有患者信息 + LastVisitTime
- [ ] 身份证号不存在 -> QuickCreatePatient 创建新患者 + 返回 IsNewlyCreated=true
- [ ] 新创建 -> IsNewlyCreated=true; 已有 -> IsNewlyCreated=false

**Business Rules:**
1. 根据身份证号查询已有患者 (FindPatientByIdNumberAsync)
2. 如患者已存在: 自动加载患者信息，显示就诊历史 (LastVisitTime, VisitCount)
3. 如患者不存在: 提供快速创建入口 (QuickCreatePatientAsync)
4. 支持一键匹配或创建 (FindOrCreatePatientAsync)
5. 读卡数据自动映射: 姓名->Name, 身份证号->IdNumber, 出生日期->BirthDate, 性别->Gender
6. 在患者列表页通过 ReadCardCommand 触发

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 读卡后通过 API 查询/创建患者 |
| 本地 | 读卡后通过 LocalPatientDataSource 查询/创建患者 |

---

## 8. Out of Scope

| 排除项 | 原因 |
|--------|------|
| ~~照片 DPAPI 加密存储 (IPhotoStorageService)~~ | **已实现 (Sprint 6)**。DpapiPhotoStorageService + 11 tests; 集成到读卡流程 |
| 服务端读卡 API | 读卡器为纯客户端硬件交互，无服务端组件 |
| 非身份证类证件支持 | v1.0 仅支持二代身份证，港澳台居住证等待后续扩展 |
| 自动轮询读卡 | v1.0 用户手动触发读卡，自动轮询检测待后续考虑 |

> 注: 患者去重降级链 (模糊匹配) 已于 Sprint 4 实现 (CARD-D03)，不再列为 Out of Scope。

---

## 9. Dependencies & Risks

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| 读卡器驱动兼容性 | DLL 依赖特定厂商驱动，跨厂商需适配 | ICardReader 策略模式 + ICardReaderFactory 工厂模式，新厂商仅需实现接口 |
| P/Invoke 内存异常 | AccessViolationException 导致应用崩溃 | 捕获并转换为错误码 -100，不传播异常 |
| 设备被占用 | 其他进程占用 USB 读卡器 | 错误码 -7 提示用户关闭占用进程 |
| 华大 DLL 仅 Windows | 限制跨平台能力 | 当前仅需 Windows Desktop，跨平台为远期目标 |
| 读卡器初始化失败 | 启动时设备不可用 | InitializeAsync 失败不阻塞应用启动 (CARD-D01)，DEBUG 回退 MockCardReader |

---

## 10. Open Questions

| ID | 问题 | 状态 |
|----|------|------|
| OQ-CARD-01 | 是否支持连续刷卡模式 (自动轮询检测卡片)? | 延期。v1.0 用户手动触发，后续根据使用反馈决定 |
| OQ-CARD-02 | 证件照片功能何时实现 DPAPI 加密存储? | **已实现** (Sprint 6)。DpapiPhotoStorageService + 11 tests; 集成到读卡流程 |
| OQ-CARD-03 | 患者去重降级链 (模糊匹配) 何时实现? | **已实现** (Sprint 4)。MatchPatientAsync 实现完整降级链 |
| OQ-CARD-04 | 是否需要支持港澳台居住证等非身份证类型? | 待定。CardType 枚举已预留，需确认业务需求 |

---

## Data Model

### CardReadResult

| 字段 | 类型 | 说明 |
|------|------|------|
| Name | string | 姓名 |
| IdNumber | string | 身份证号码 |
| Gender | Gender | 性别 (Male/Female/Unknown) |
| Nation | string | 民族 |
| BirthDate | DateTime? | 出生日期 |
| Address | string | 住址 |
| IssuingAuthority | string | 签发机关 |
| ValidFrom | DateTime? | 有效期开始日期 |
| ValidTo | DateTime? | 有效期截止日期 (长期为 null) |
| CardType | CardType | 证件类型 (IdCard/ForeignerResidencePermit/HongKongMacaoTaiwanResidencePermit) |
| PhotoData | byte[]? | 照片数据 (BMP 格式) |
| PhotoFilePath | string? | 照片文件路径 |
| ReadTime | DateTime | 读取时间 |
| IsSuccess | bool | 是否读取成功 |
| ErrorMessage | string? | 错误信息 |
| ErrorCode | int | 原始错误代码 |
| Age | int? | 计算属性，根据 BirthDate 自动计算 |

**工厂方法**: `CardReadResult.Success(...)` / `CardReadResult.Failure(errorCode, errorMessage)`

### PatientMatchResult (Sprint 4 新增)

MatchPatientAsync 返回结果:

| 字段 | 类型 | 说明 |
|------|------|------|
| MatchType | PatientMatchType | 匹配类型 (见下) |
| Patient | PatientDto? | 匹配到的患者 (单条结果时) |
| Candidates | IReadOnlyList\<PatientDto\> | 候选列表 (MultipleCandidates 时) |

**PatientMatchType 枚举:**

| 值 | 含义 |
|----|------|
| ExactMatch | IdNumber 精确匹配成功 |
| FuzzyMatch | Name+BirthDate 模糊匹配成功 |
| MultipleCandidates | 多条候选，需用户确认 |
| NoMatch | 未匹配，需新建患者 |

### PatientFromCardResult

| 字段 | 类型 | 说明 |
|------|------|------|
| PatientId | Guid | 患者 ID |
| Name | string | 姓名 |
| IdNumber | string | 身份证号 |
| IsNewlyCreated | bool | 是否新创建 |
| LastVisitTime | DateTime? | 最近就诊时间 |
| VisitCount | int | 就诊次数 |

---

## Error Codes

> 读卡器模块为纯客户端硬件交互，使用 Result 模式 + 事件驱动。错误通过 CardReadResult.ErrorCode 返回。

### 设备错误码

| 错误码 | 错误消息 | 触发条件 |
|--------|----------|----------|
| 0 | (成功) | 读卡成功 |
| -1 | 打开设备失败 / 读卡器未连接 | 设备不存在、驱动问题或服务未初始化 |
| -2 | 设备未初始化 / 读卡器未连接 | 设备初始化失败或 HuaDaHD100 未连接 |
| -3 | 卡认证失败 / 操作被取消 | 身份证认证不通过或 CancellationToken 触发 |
| -4 | 读卡失败 | 读卡硬件错误 |
| -5 | 无卡或卡未放好 | 卡片位置不对 |
| -6 | 通讯超时 | USB 通讯超时 |
| -7 | 设备被占用 | 其他进程正在使用读卡器 |
| -8 | 内存分配失败 | 系统内存不足 |
| -9 | 参数错误 | P/Invoke 调用参数错误 |
| -10 | 设备不支持 | 设备不支持此操作 |
| -99 | (异常原始消息) | 通用异常捕获 |
| -100 | 读卡器访问异常，请重新连接设备 | AccessViolationException (P/Invoke 异常) |

### 异常处理策略

| 异常类型 | 处理方式 | 对应错误码 |
|----------|----------|-----------|
| OperationCanceledException | 捕获后返回失败 | -3 |
| AccessViolationException | 捕获后返回失败 | -100 |
| Exception (通用) | 捕获后返回失败 | -99 |

### 错误事件

错误通过 `CardReadError` 事件通知，包含 `CardReadErrorEventArgs`:
- `ErrorCode`: 错误码
- `ErrorMessage`: 错误消息
- `Exception`: 异常对象 (可选)

---

## Decision Log

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| CARD-D01 | 多厂商驱动加载策略 | US-CARD-001 | 已确定: 工厂模式选择 (ICardReaderFactory + CardReaderType 枚举)，支持自动检测 (AutoDetectReaderAsync)。DLL 放应用目录或 Native/ 子目录。启动时 InitializeAsync() 失败不阻塞。DEBUG 模式下自动回退到 MockCardReader。当前支持 HuaDaHD100 和 Mock 两种类型 |
| CARD-D02 | 照片存储与保护 | US-CARD-001 | **已实现 (Sprint 6)**: DpapiPhotoStorageService -- DPAPI LocalMachine 加密存 {AppDataLocal}/LYBT/photos/{patientId}.dat。接口 IPhotoStorageService (Save/Load/Delete/Exists)。读取解密到内存不写临时文件。11 单元测试。集成到读卡流程 |
| CARD-D03 | 患者去重降级 | US-CARD-001 | **已实现 (Sprint 4)**: MatchPatientAsync 实现完整降级链: (1) IdNumber 精确匹配->ExactMatch (2) Name+BirthDate 模糊匹配->FuzzyMatch (3) 多条命中->MultipleCandidates (4) 未命中->NoMatch。PatientMatchType 枚举 + PatientMatchResult 类型 |

### 修订历史

| 日期 | 修订项 | 原因 | 参考 |
|------|--------|------|------|
| 2026-02-21 | 读卡数据字段映射 "姓名->RealName" 修订为 "姓名->Name" | 代码使用 Name 字段更简洁合理，PatientFromCardResult.Name | CARD-01 |
| 2026-02-28 | CARD-D01 对齐工厂模式实现 | PRD 偏差修复 | PRD-13 |
| 2026-02-28 | ~~CARD-D02 标注 DPAPI 未实现~~ | ~~PRD 偏差修复~~ **Sprint 6 (2026-03-09) 已实现** | PRD-14 |
| 2026-02-28 | CARD-D03 对齐仅精确匹配实现 | PRD 偏差修复 | PRD-15 |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-02-11 | v1.1 | 新增错误码章节，含设备错误码 13 个 + 异常处理策略 3 个 |
| 2026-02-11 | v1.2 | 验收标准格式统一为 [场景] -> [预期结果] 格式 |
| 2026-02-17 | v1.3 | PRD审查修复: A2-Receptionist允许使用读卡器(前台挂号快速登记) |
| 2026-02-21 | v1.4 | PRD vs Code 偏差分析修订: 1 项修订 (CARD-01 字段映射姓名->Name) |
| 2026-02-22 | v1.5 | Phase 2 模块功能细化: 新增 CARD-D01/D02/D03 |
| 2026-02-28 | v1.6 | PRD 偏差修复: CARD-D01/D02/D03 对齐实际实现 |
| 2026-03-06 | v2.0 | PRD 全面重写: FR->US 格式迁移，新增 Problem Statement/Strategic Context/Success Metrics/Epic Hypothesis/Out of Scope/Dependencies & Risks/Open Questions 7 个章节，移除接口定义章节 (属架构层) |
| 2026-03-09 | v2.1 | Sprint 4 完成: CARD-D03 降级链已实现 (MatchPatientAsync + PatientMatchType + PatientMatchResult); 操作流程更新; BR-8 CardReaderOptions 配置化; 移出 Out of Scope; OQ-CARD-03 标记已实现; 新增 PatientMatchResult 数据模型 |
| 2026-03-09 | v2.2 | Sprint 6 完成状态同步: CARD-D02 照片 DPAPI 加密已实现 (DpapiPhotoStorageService + 11 tests); Out of Scope/OQ-CARD-02/Decision Log 更新 |
