# LYBT.Desktop.CardReader

> 身份证读卡器硬件集成，策略模式支持多厂商适配

## 项目定位

- **层级**: Desktop Core (基础设施层)
- **职责**: 提供身份证读卡器硬件抽象，通过策略模式支持多厂商读卡器，当前支持华大 HD100 和 Mock 模式
- **状态**: Active

## 目录结构

```
LYBT.Desktop.CardReader/
├── CardReaderModule.cs    # Prism 模块注册
├── Abstractions/          # ICardReader, ICardReaderFactory 抽象接口
├── Adapters/              # 硬件适配器 (HuaDaHD100, Mock)
├── Integration/           # IPatientCardReaderIntegration 患者集成接口
├── Models/                # CardReadResult 读卡结果 DTO
├── Native/                # HuaDaNativeMethods (P/Invoke 封装)
└── Services/              # CardReaderFactory, CardReaderService
```

## 核心组件

| 名称 | 说明 |
|------|------|
| ICardReader | 读卡器核心抽象，定义连接/断开/读卡/检测卡片操作 |
| ICardReaderFactory | 工厂接口，创建读卡器实例，支持自动检测 |
| ICardReaderService | 业务层服务，管理生命周期、自动读卡模式、事件分发 |
| HuaDaHD100CardReader | 华大 HD100 USB 读卡器适配器，线程安全实现 |
| MockCardReader | 模拟读卡器，DEBUG 模式下自动回退使用 |
| HuaDaNativeMethods | HDstdapi.dll P/Invoke 封装 (InitComm/CloseComm/Authenticate/ReadBaseMsg) |
| CardReadResult | 读卡结果模型，包含姓名、身份证号、照片等信息 |
| IPatientCardReaderIntegration | 读卡结果与患者模块的集成接口 |

## 设计依据

采用三层策略模式架构：Service (生命周期管理) -> Factory (实例创建) -> Adapter (厂商实现)。新增厂商只需实现 ICardReader 接口并在 Factory 注册，无需修改上层代码。

P/Invoke 层封装了华大 HD100 的 HDstdapi.dll 原生调用，需要 AllowUnsafeBlocks 编译选项。USB 默认端口号 1001。

## 依赖关系

### 依赖
- Prism.Core / Prism.DryIoc / Prism.Wpf - 模块化框架
- LYBT.Desktop.Infrastructure - 基础设施支持
- LYBT.Shared.Models - 共享 DTO 模型

### 被依赖
- LYBT.Desktop.Patients - 患者模块集成读卡器快速识别
- LYBT.Desktop.Clinical - 临床工作站集成读卡器
- LYBT.Desktop.Shell - 主程序模块加载

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 初始 README 创建 |
