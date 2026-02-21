# 身份证读卡器集成 需求规格

## 概述

身份证读卡器模块提供硬件设备集成能力，支持通过二代身份证读卡器快速读取患者身份信息并自动填充到患者表单，提升挂号登记效率。

---

## 用户角色

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| Admin | 使用读卡器读取患者信息 |
| Doctor | 使用读卡器读取患者信息 |
| Receptionist | 使用读卡器读取患者信息 (前台挂号快速登记) |

> 读卡器功能仅在 Desktop 端可用，无服务端 API。

---

## 功能清单

### FR-CARD-001: 身份证读卡器连接与读取

- **描述**: 连接身份证读卡器设备，读取二代身份证芯片信息
- **业务规则**:
  1. 支持多厂商读卡器 (策略模式，ICardReader 接口)
  2. 自动检测读卡器连接状态 (IsConnected)
  3. 支持主动探测卡片 (DetectCardAsync)
  4. 读取信息包含: 姓名、性别、民族、出生日期、身份证号、住址
  5. 可选保存证件照片 (savePhoto 参数)
  6. 连接状态变更通过 ConnectionStateChanged 事件通知
  7. 卡片插入通过 CardDetected 事件通知
- **远程模式**: 不适用 (纯客户端硬件交互)
- **本地模式**: 不适用 (纯客户端硬件交互)
- **验收标准**:
  - [ ] 连接成功 -> ICardReader.IsConnected=true
  - [ ] 读卡成功 -> 返回姓名/身份证号/性别/出生日期/住址
  - [ ] 设备断开 -> ConnectionStateChanged 事件触发

> **[已修订 2026-02-21]** 读卡数据字段映射 "姓名→RealName" 修订为 "姓名→Name"，与代码实现对齐
> 原因: 代码使用 Name 字段更简洁合理，PatientFromCardResult.Name  |  参考: CARD-01

### FR-CARD-002: 读卡数据填充到患者表单

- **描述**: 将读卡器读取的身份信息自动填充到患者管理表单，支持已有患者匹配
- **业务规则**:
  1. 根据身份证号查询已有患者 (FindPatientByIdNumberAsync)
  2. 如患者已存在: 自动加载患者信息，显示就诊历史 (LastVisitTime, VisitCount)
  3. 如患者不存在: 提供快速创建入口 (QuickCreatePatientAsync)
  4. 支持一键匹配或创建 (FindOrCreatePatientAsync)
  5. 读卡数据自动映射: 姓名→RealName, 身份证号→IdNumber, 出生日期→BirthDate, 性别→Gender
  6. 在患者列表页通过 ReadCardCommand 触发
- **远程模式**: 读卡后通过 API 查询/创建患者
- **本地模式**: 读卡后通过 LocalPatientDataSource 查询/创建患者
- **验收标准**:
  - [ ] 身份证号已存在 -> 返回已有患者信息 + LastVisitTime
  - [ ] 身份证号不存在 -> QuickCreatePatient 创建新患者 + 返回 IsNewlyCreated=true
  - [ ] 新创建 -> IsNewlyCreated=true; 已有 -> IsNewlyCreated=false

---

## 数据模型

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

## 接口定义

### ICardReader

**属性**:

| 属性 | 类型 | 说明 |
|------|------|------|
| Name | string | 读卡器名称 |
| Vendor | string | 读卡器厂商 |
| Model | string | 读卡器型号 |
| IsConnected | bool | 是否已连接 |

**方法**:

| 方法 | 返回类型 | 说明 |
|------|----------|------|
| ConnectAsync | Task<bool> | 连接读卡器 (可选 connectionString) |
| DisconnectAsync | Task | 断开连接 |
| ReadCardAsync | Task<CardReadResult> | 读取身份证 (savePhoto, photoPath, cancellationToken) |
| DetectCardAsync | Task<bool> | 探测卡片 |

**事件**: `ConnectionStateChanged`, `CardDetected`

**配置**: `CardReaderOptions` (超时、自动重连、照片目录、USB 端口)

### IPatientCardReaderIntegration

| 方法 | 返回类型 | 说明 |
|------|----------|------|
| FindPatientByIdNumberAsync | Task<PatientFromCardResult?> | 按身份证号查患者 |
| QuickCreatePatientAsync | Task<Guid> | 快速创建患者 |
| FindOrCreatePatientAsync | Task<PatientFromCardResult> | 查找或创建 |
| GetPatientDetailByIdAsync | Task<PatientDetailDto?> | 获取患者详情 |

**事件**: `CardReaderIntegrationEventType` (PatientFound/PatientNotFound/PatientCreated/ReadFailed)

---

## 错误码

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

## 决策记录

| # | 决策 | 结论 | 依据 |
|---|------|------|------|
| 1 | 读卡器支持范围 | 策略模式多厂商，通过 ICardReader 接口抽象 | ICardReader 接口设计 |
| 2 | 读卡器功能可用模式 | 仅 Desktop 端，不区分远程/本地模式 | 纯客户端硬件交互 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-02-11 | v1.1 | 新增错误码章节，含设备错误码 13 个 + 异常处理策略 3 个 |
| 2026-02-11 | v1.2 | 验收标准格式统一为 [场景] -> [预期结果] 格式 |
| 2026-02-17 | v1.3 | PRD审查修复: A2-Receptionist允许使用读卡器(前台挂号快速登记) |
| 2026-02-21 | v1.4 | PRD vs Code 偏差分析修订: 1 项修订 (CARD-01 字段映射姓名→Name) |
