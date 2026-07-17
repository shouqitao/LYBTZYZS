# LYBT.Desktop.CardReader

身份证读卡器模块，通过策略模式支持多型号读卡器，自动检测硬件并提供统一的读卡接口。

## 项目定位

桌面客户端的硬件抽象层，负责二代身份证读卡器的连接、检测与数据读取。采用策略模式隔离不同厂商 SDK 差异，上层业务只需依赖 `ICardReader` 接口，无需关心底层硬件细节。

## 目录结构

```
LYBT.Desktop.CardReader/
├── Abstractions/
│   ├── ICardReader.cs              # 读卡器策略接口
│   ├── ICardReaderFactory.cs       # 工厂接口
│   └── CardReadResult.cs           # 读卡结果模型
├── Readers/
│   ├── HuaDaHD100CardReader.cs     # 华大 HD100 P/Invoke 实现
│   ├── MockCardReader.cs           # 测试用模拟读卡器
│   └── ...
├── Services/
│   ├── CardReaderService.cs        # 高层服务（自动读卡/轮询）
│   └── CardReaderFactory.cs        # 工厂实现（自动检测+回退）
├── Enums/
│   └── CardReaderType.cs           # 读卡器类型枚举
└── CardReaderModule.cs             # Prism 模块注册
```

## 核心组件

### ICardReader — 策略接口

**设计依据**：Strategy Pattern，隔离不同厂商 SDK 的 P/Invoke 差异，上层面向接口编程。

| 方法 | 签名 | 说明 |
|------|------|------|
| Connect | `bool Connect()` | 连接读卡器硬件 |
| Disconnect | `void Disconnect()` | 断开连接，释放资源 |
| ReadCard | `CardReadResult? ReadCard()` | 读取当前卡片数据 |
| DetectCard | `bool DetectCard()` | 检测是否有卡片在位 |
| IsConnected | `bool IsConnected { get; }` | 当前连接状态 |

### ICardReaderFactory — 工厂接口

**设计依据**：Abstract Factory，封装读卡器实例的创建与自动检测逻辑。

| 方法 | 签名 | 说明 |
|------|------|------|
| GetSupportedReaders | `IReadOnlyList<CardReaderType> GetSupportedReaders()` | 返回当前环境支持的读卡器列表 |
| CreateReader | `ICardReader CreateReader(CardReaderType type)` | 按类型创建实例 |
| AutoDetectReader | `ICardReader? AutoDetectReader()` | 自动检测并返回第一个可用读卡器 |

### CardReaderType — 读卡器类型枚举

**设计依据**：枚举映射不同厂商型号，`Auto` 用于自动检测模式。

| 值 | 说明 |
|------|------|
| Auto | 自动检测，优先华大 |
| HuaDaHD100 | 华大 HD100 |
| HD200 | 华大 HD200 |
| ShenSi | 深思读卡器 |
| JingLun | 精伦读卡器 |
| XinZhongXin | 新中新读卡器 |
| Mock | 测试用模拟读卡器 |

### HuaDaHD100CardReader — 华大读卡器实现

**设计依据**：通过 P/Invoke 调用 `HDstdapi.dll`，实现 `ICardReader` 接口。华大是国内政务场景最常见的身份证读卡器厂商。

| 成员 | 说明 |
|------|------|
| Connect() | 调用 `HD_InitComm` 初始化串口通信 |
| ReadCard() | 调用 `HD_ReadCard` 读取身份证芯片数据，解析姓名/身份证号/照片等 |
| Disconnect() | 调用 `HD_CloseComm` 关闭通信 |
| P/Invoke | 引用 `HDstdapi.dll`，需确保 DLL 位于输出目录 |

### MockCardReader — 模拟读卡器

**设计依据**：开发/测试环境无硬件时提供固定测试数据，DEBUG 模式下由工厂自动回退。

| 成员 | 说明 |
|------|------|
| ReadCard() | 返回预设的测试身份证数据 |
| DetectCard() | 始终返回 `true` |

### CardReaderService — 高层服务

**设计依据**：Facade 模式，封装自动轮询、初始化、生命周期管理，业务层只需订阅事件。

| 方法 | 签名 | 说明 |
|------|------|------|
| Initialize | `Task<bool> InitializeAsync()` | 自动检测并连接读卡器 |
| ReadCard | `CardReadResult? ReadCard()` | 立即读卡一次 |
| StartAutoRead | `void StartAutoRead(int intervalMs)` | 启动定时轮询，检测到卡片触发事件 |
| StopAutoRead | `void StopAutoRead()` | 停止轮询 |

### CardReaderFactory — 工厂实现

**设计依据**：自动检测优先华大硬件，DEBUG 模式无硬件时回退到 Mock，避免开发环境阻塞。

| 逻辑 | 说明 |
|------|------|
| 自动检测 | 依次尝试 HuaDaHD100 → HD200 → ShenSi → JingLun → XinZhongXin |
| Mock 回退 | `#if DEBUG` 时若全部失败，返回 MockCardReader |

### CardReadResult — 读卡结果

**设计依据**：值对象，承载身份证全字段数据，`Age` 为计算属性。

| 属性 | 类型 | 说明 |
|------|------|------|
| Name | `string` | 姓名 |
| IdNumber | `string` | 身份证号（18位） |
| Gender | `string` | 性别 |
| BirthDate | `DateTime` | 出生日期 |
| CardType | `CardReaderType` | 实际读取的读卡器类型 |
| PhotoData | `byte[]?` | 身份证照片二进制 |
| Age | `int` | 计算属性：当前日期 - 出生日期 |

### CardReaderModule — Prism 模块

**设计依据**：IModule 实现，注册所有读卡器相关服务为 Singleton（硬件资源全局唯一）。

| 注册 | 生命周期 | 说明 |
|------|----------|------|
| ICardReaderFactory → CardReaderFactory | Singleton | 工厂全局唯一 |
| CardReaderService | Singleton | 服务全局唯一，管理硬件连接 |

## 依赖关系

| 依赖 | 用途 |
|------|------|
| Prism.Modularity | IModule 模块注册 |
| Prism.Events | 事件聚合器（读卡事件通知） |
| HDstdapi.dll | 华大读卡器 native DLL（P/Invoke） |
| Microsoft.Extensions.Logging | 日志 |

## 设计决策

1. **策略模式而非继承**：每个读卡器实现独立类，通过工厂切换，避免 God Class
2. **P/Invoke 而非 COM**：华大 SDK 提供的是 C DLL，P/Invoke 更轻量且无需注册 COM 组件
3. **Mock 回退仅限 DEBUG**：防止生产环境误用模拟数据
4. **Singleton 生命周期**：硬件资源全局共享，避免多实例争用串口
5. **自动轮询用 Timer**：`StartAutoRead` 内部使用 `System.Threading.Timer`，间隔可配置

## 已知陷阱

- **HDstdapi.dll 部署**：必须将 native DLL 复制到输出目录（x86/x64 需匹配），否则 P/Invoke 抛 `DllNotFoundException`
- **串口独占**：同一串口不能被多个 `ICardReader` 实例同时连接，工厂应保证单例
- **读卡超时**：华大 SDK 的 `HD_ReadCard` 是阻塞调用，需在后台线程执行避免 UI 卡顿
- **身份证照片解码**：`PhotoData` 是 WLT 格式，需额外转换为 BMP/JPG 才能显示
