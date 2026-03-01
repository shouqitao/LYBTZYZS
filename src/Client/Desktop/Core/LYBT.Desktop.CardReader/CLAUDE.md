# LYBT.Desktop.CardReader 模块

**功能**: 身份证读卡器集成模块
**位置**: `src/Client/Desktop/Core/LYBT.Desktop.CardReader/`
**架构定位**: Core基础设施层（与Printing模块并列）
**状态**: 开发中
**创建时间**: 2026-01-19

---

## 模块概述

提供身份证读卡器硬件集成，支持多厂商读卡器，用于挂号/就诊工作站快速识别患者身份。

**架构说明**: 本模块属于Core基础设施层，提供硬件抽象能力，不包含业务逻辑。与`LYBT.Desktop.Printing`模块职责定位一致（硬件抽象层）。

## 目录结构

```
LYBT.Desktop.CardReader/
├── Abstractions/          # 抽象接口
│   ├── ICardReader.cs     # 读卡器核心接口
│   └── ICardReaderFactory.cs  # 工厂接口
├── Adapters/              # 硬件适配器
│   ├── HuaDaHD100CardReader.cs  # 华大HD100适配器
│   └── MockCardReader.cs  # 模拟读卡器(开发测试用)
├── Integration/           # 模块集成契约
│   └── IPatientCardReaderIntegration.cs  # 患者集成接口(Patients模块实现)
├── Models/                # 数据模型
│   └── CardReadResult.cs  # 读卡结果DTO
├── Native/                # P/Invoke
│   └── HuaDaNativeMethods.cs  # HDstdapi.dll封装
├── Services/              # 服务层
│   ├── ICardReaderService.cs  # 服务接口
│   ├── CardReaderService.cs   # 服务实现
│   └── CardReaderFactory.cs   # 工厂实现
└── CardReaderModule.cs    # Prism模块注册
```

## 核心架构

### 分层设计

```
┌─────────────────────────────────────────────────────────┐
│  ICardReaderService (业务层)                             │
│  - 生命周期管理、自动读卡模式、事件分发                      │
├─────────────────────────────────────────────────────────┤
│  ICardReaderFactory (工厂层)                             │
│  - 创建读卡器实例、自动检测、DLL可用性检查                   │
├─────────────────────────────────────────────────────────┤
│  ICardReader (抽象层)                                    │
│  - 连接/断开、读卡、检测卡片                               │
├─────────────────────────────────────────────────────────┤
│  HuaDaHD100CardReader / MockCardReader (适配层)          │
│  - 厂商特定实现                                          │
├─────────────────────────────────────────────────────────┤
│  HuaDaNativeMethods (P/Invoke层)                        │
│  - 原生DLL调用封装                                       │
└─────────────────────────────────────────────────────────┘
```

### 策略模式

支持多厂商读卡器扩展，通过实现`ICardReader`接口添加新厂商支持。

## 支持的读卡器

| 类型 | 厂商 | 型号 | 说明 |
|------|------|------|------|
| HuaDaHD100 | 华大电子 | HD100 | USB身份证读卡器 |
| Mock | LYBT | Mock-v1 | 模拟读卡器(开发用) |

## 关键接口

### ICardReader

```csharp
public interface ICardReader : IDisposable
{
    Task<bool> ConnectAsync(string? connectionString = null);
    Task DisconnectAsync();
    Task<CardReadResult> ReadCardAsync(bool savePhoto = false, ...);
    Task<bool> DetectCardAsync();
    event EventHandler<CardReaderConnectionEventArgs>? ConnectionStateChanged;
}
```

### ICardReaderService

```csharp
public interface ICardReaderService : IDisposable
{
    Task<bool> InitializeAsync(CardReaderType readerType = CardReaderType.Auto);
    Task<CardReadResult> ReadCardAsync(bool savePhoto = false, ...);
    void StartAutoRead(int intervalMs = 500);
    void StopAutoRead();
    event EventHandler<CardReadResult>? CardReadCompleted;
}
```

## 使用示例

### 基本使用

```csharp
// 注入服务
private readonly ICardReaderService _cardReaderService;

// 初始化(自动检测)
await _cardReaderService.InitializeAsync();

// 手动读卡
var result = await _cardReaderService.ReadCardAsync();
if (result.IsSuccess)
{
    // 使用 result.Name, result.IdNumber 等
}

// 断开连接
await _cardReaderService.DisconnectAsync();
```

### 自动读卡模式

```csharp
// 订阅事件
_cardReaderService.CardReadCompleted += OnCardRead;

// 启动自动读卡(每500ms检测)
_cardReaderService.StartAutoRead(500);

private void OnCardRead(object? sender, CardReadResult e)
{
    // 在UI线程处理读卡结果
    Dispatcher.Invoke(() => ProcessCard(e));
}
```

## P/Invoke说明

### HDstdapi.dll API

| 方法 | 说明 | 返回值 |
|------|------|--------|
| HD_InitComm(port) | 初始化连接 | 1=成功 |
| HD_CloseComm() | 关闭连接 | 1=成功 |
| HD_Authenticate(type) | 卡认证 | 0=成功 |
| HD_Read_BaseMsg(...) | 读取基本信息 | 0=成功 |

### USB端口

华大HD100使用USB端口号`1001`。

## 开发注意事项

1. **DLL依赖**: HDstdapi.dll需放置在应用程序目录或Native子目录
2. **线程安全**: HuaDaHD100CardReader使用lock确保线程安全
3. **异常处理**: 处理AccessViolationException等P/Invoke异常
4. **Mock模式**: DEBUG模式下自动检测失败时回退到MockCardReader

## 扩展新厂商

1. 创建`Adapters/NewVendorCardReader.cs`实现`ICardReader`
2. 在`CardReaderType`枚举添加新类型
3. 在`CardReaderFactory`添加创建逻辑和支持信息
4. 如需P/Invoke，创建对应的Native封装类

## 与其他模块集成

- **Patients模块**: 读卡结果用于快速查找/创建患者
- **MedicalCase模块**: 就诊工作站集成读卡器

---

## 代码文件结构

```
LYBT.Desktop.CardReader/
├── Abstractions/
│   ├── ICardReader.cs                     # 读卡器核心抽象接口 (134行)
│   └── ICardReaderFactory.cs              # 读卡器工厂接口 + 枚举 + 信息记录 (83行)
├── Adapters/
│   ├── HuaDaHD100CardReader.cs            # 华大HD100适配器实现 (293行)
│   └── MockCardReader.cs                  # 模拟读卡器 (125行)
├── Integration/
│   └── IPatientCardReaderIntegration.cs   # 患者集成接口 + 事件参数 (102行)
├── Models/
│   └── CardReadResult.cs                  # 读卡结果DTO + CardType枚举 (200行)
├── Native/
│   └── HuaDaNativeMethods.cs              # P/Invoke封装 (174行)
├── Services/
│   ├── ICardReaderService.cs              # 服务接口 + CardReadErrorEventArgs (88行)
│   ├── CardReaderService.cs               # 服务实现 (356行)
│   └── CardReaderFactory.cs               # 工厂实现 (155行)
└── CardReaderModule.cs                    # Prism模块注册 + IServiceCollection扩展 (52行)
```

### Abstractions/ICardReader.cs

- **接口**: `ICardReader` : `IDisposable`
- **职责**: 读卡器核心抽象，定义连接/断开/读卡/检测操作
- **属性**: Name, Vendor, Model, IsConnected
- **方法**:
  - `ConnectAsync(string? connectionString)` - 初始化连接
  - `DisconnectAsync()` - 断开连接
  - `ReadCardAsync(bool savePhoto, string? photoPath, CancellationToken)` - 读取身份证
  - `DetectCardAsync()` - 检测感应区是否有卡
- **事件**: `ConnectionStateChanged`, `CardDetected`
- **同文件辅助类型**:
  - `CardReaderConnectionEventArgs` - IsConnected, ErrorMessage
  - `CardDetectedEventArgs` - DetectedTime
  - `CardReaderOptions` - ConnectTimeout, ReadTimeout, ReconnectInterval, AutoReconnect, PhotoSaveDirectory, UsbPort, SerialPort

### Abstractions/ICardReaderFactory.cs

- **接口**: `ICardReaderFactory`
- **方法**:
  - `GetSupportedReaders()` -> `IReadOnlyList<CardReaderInfo>`
  - `CreateReader(CardReaderType, CardReaderOptions?)` -> `ICardReader`
  - `AutoDetectReaderAsync(CardReaderOptions?)` -> `Task<ICardReader?>`
- **同文件辅助类型**:
  - `CardReaderType` (enum) - Auto=0, HuaDaHD100=1, HuaDaHD200=2(预留), ShenSiSS628=10(预留), JingLunIDR=20(预留), XinZhongXinDKQ=30(预留), Mock=99
  - `CardReaderInfo` (record) - Type, DisplayName, Vendor, Model, Description, RequiredDlls, IsAvailable

### Adapters/HuaDaHD100CardReader.cs

- **类型**: `HuaDaHD100CardReader` (sealed class) : `ICardReader`
- **职责**: 华大HD100 USB身份证读卡器的具体实现
- **构造参数**: `CardReaderOptions?`, `ILogger<HuaDaHD100CardReader>?`
- **线程安全**: 所有操作使用 `lock(_lockObj)` 保护
- **ConnectAsync**: 调用 `HD_InitComm(port)`，默认 USB 端口 1001
- **ReadCardAsync**: 调用 `HD_Authenticate(1)` + `HD_Read_BaseMsg(...)` + `GetCardType()`
- **DetectCardAsync**: 通过 `HD_Authenticate(1)` 返回值判断是否有卡
- **异常处理**: 捕获 `AccessViolationException` (P/Invoke 内存访问异常)
- **日志脱敏**: `MaskIdNumber()` 对身份证号中间10位脱敏

### Adapters/MockCardReader.cs

- **类型**: `MockCardReader` (sealed class) : `ICardReader`
- **职责**: 开发测试用模拟读卡器，不需要真实硬件
- **模拟数据**: 返回固定测试数据 (张三, 110101199001011234)
- **模拟延迟**: ConnectAsync 500ms, ReadCardAsync 800ms
- **DetectCardAsync**: 已连接时始终返回 true

### Integration/IPatientCardReaderIntegration.cs

- **接口**: `IPatientCardReaderIntegration`
- **职责**: 定义读卡结果与患者模块的集成契约
- **方法**:
  - `FindPatientByIdNumberAsync(string)` - 按身份证号查找患者
  - `QuickCreatePatientAsync(CardReadResult)` - 根据读卡结果快速创建患者
  - `FindOrCreatePatientAsync(CardReadResult)` - 查找或创建患者
  - `GetPatientDetailByIdAsync(Guid)` - 获取患者详情 (OpenSpec: integrate-cardreader-module)
- **同文件辅助类型**:
  - `PatientFromCardResult` - PatientId, Name, IdNumber, IsNewlyCreated, LastVisitTime, VisitCount
  - `CardReaderIntegrationEventType` (enum) - PatientFound, PatientNotFound, PatientCreated, ReadFailed
  - `CardReaderIntegrationEventArgs` - EventType, CardResult, Patient, ErrorMessage
- **实现方**: `PatientCardReaderIntegration` (在 Patients 模块)

### Models/CardReadResult.cs

- **类型**: `CardReadResult` (class)
- **职责**: 身份证读取结果 DTO，与 PatientInputDto 字段对齐
- **属性**: Name, IdNumber, Gender, Nation, BirthDate, Address, IssuingAuthority, ValidFrom, ValidTo, CardType, PhotoData, PhotoFilePath, ReadTime, IsSuccess, ErrorMessage, ErrorCode
- **计算属性**: `Age` - 根据 BirthDate 计算年龄
- **工厂方法**:
  - `Success(name, idNumber, sex, nation, birth, address, department, effectDate, expireDate)` - 创建成功结果
  - `Failure(int errorCode, string? errorMessage)` - 创建失败结果
- **私有辅助**: `ParseGender()`, `ParseDate()` (YYYYMMDD), `ParseExpireDate()` ("长期"处理), `GetErrorMessage()` (错误码映射)
- **同文件枚举**: `CardType` - IdCard=0, ForeignerResidencePermit=1, HongKongMacaoTaiwanResidencePermit=2

### Native/HuaDaNativeMethods.cs

- **类型**: `HuaDaNativeMethods` (internal static class)
- **职责**: HDstdapi.dll P/Invoke 封装
- **DLL**: `HDstdapi.dll` (CallingConvention.StdCall)
- **设备控制**: `HD_InitComm(int port)`, `HD_CloseComm()`, `HD_Authenticate(int type)`
- **读卡操作**: `HD_ReadCard()`, `HD_Read_BaseMsg(10个StringBuilder参数)`
- **字段获取** (需先调用 HD_ReadCard): `GetName()`, `GetCertNo()`, `GetSex()`, `GetNation()`, `GetBirth()`, `GetAddress()`, `GetDepartemt()`, `GetEffectDate()`, `GetExpireDate()`, `GetCardType()`
- **照片操作**: `GetBmpFileData()`, `GetBmpFile(string path)`
- **辅助方法**: `PtrToString(IntPtr)` - IntPtr 转字符串, `IsDllAvailable()` - 检查 DLL 是否存在

### Services/ICardReaderService.cs

- **接口**: `ICardReaderService` : `IDisposable`
- **属性**: CurrentReader, IsConnected, IsAutoReadEnabled
- **方法**: `InitializeAsync(CardReaderType)`, `DisconnectAsync()`, `ReadCardAsync(bool, CancellationToken)`, `StartAutoRead(int)`, `StopAutoRead()`
- **事件**: `ConnectionStateChanged`, `CardReadCompleted`, `CardReadError`
- **同文件类型**: `CardReadErrorEventArgs` - ErrorCode, Message, Exception

### Services/CardReaderService.cs

- **类型**: `CardReaderService` : `ICardReaderService`
- **职责**: 高层业务封装，管理读卡器生命周期和自动读卡模式
- **构造参数**: `ICardReaderFactory factory`, `ILogger<CardReaderService>? logger`
- **自动读卡**: 使用 `Timer` 定时轮询 `DetectCardAsync()`，通过 `_lastReadIdNumber` 防止同一张卡重复触发
- **线程安全**: 使用 `lock(_lockObj)` 保护共享状态

### Services/CardReaderFactory.cs

- **类型**: `CardReaderFactory` : `ICardReaderFactory`
- **构造参数**: `ILoggerFactory? loggerFactory`
- **自动检测顺序**: HuaDaHD100 -> (预留其他) -> DEBUG 模式回退 Mock
- **DLL检查**: 检查应用目录、Native子目录、系统目录

### CardReaderModule.cs

- **类型**: `CardReaderModule` : `IModule` (Prism)
- **DI注册**: `ICardReaderFactory` (单例), `ICardReaderService` (单例)
- **扩展方法**: `CardReaderServiceCollectionExtensions.AddCardReaderServices()` - 非 Prism 环境使用

---

## 死代码与废弃标记

| 文件 | 状态 | 说明 |
|------|------|------|
| `ICardReaderFactory.cs` 中预留的枚举值 | 预留代码 | HuaDaHD200, ShenSiSS628, JingLunIDR, XinZhongXinDKQ 仅定义枚举值，无对应实现 |

当前无死代码。所有接口和类均有明确引用: ICardReaderService 被 Clinical/Patients 模块使用，IPatientCardReaderIntegration 由 Patients 模块实现。

---

## 已知陷阱

1. **Dispose 中的同步等待**: `HuaDaHD100CardReader.Dispose()` 调用 `DisconnectAsync().Wait(1000)`，同步阻塞最多1秒。在 UI 线程上可能导致短暂卡顿
2. **AutoReadCallback 中的 async void**: `CardReaderService.AutoReadCallback` 是 `async void` 方法 (Timer 回调要求)，异常仅被日志记录，不会向上传播
3. **HD_Authenticate 用于卡检测**: `DetectCardAsync()` 复用 `HD_Authenticate(1)` 判断是否有卡，这会消耗一次认证操作
4. **GetDepartemt 拼写错误**: 原生 DLL 方法名 `GetDepartemt` (缺少 'n')，属于 HDstdapi.dll 的原始 API 拼写，不可修改
5. **DEBUG 条件编译**: `CardReaderFactory.AutoDetectReaderAsync()` 在 DEBUG 模式下无真实读卡器时返回 MockCardReader，Release 模式返回 null，注意测试环境差异

---

**维护者**: Claude Code
**最后更新**: 2026-03-01
