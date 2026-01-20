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

**维护者**: Claude Code
**最后更新**: 2026-01-20 (迁移到Core目录)
