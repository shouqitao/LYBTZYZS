# LYBT.Desktop.Receptionist

前台角色模块 -- 提供前台工作台主页，整合患者登记、挂号管理和身份证读卡功能。

## 项目定位

Receptionist 模块是前台接待角色的工作空间，提供今日统计概览、挂号队列快捷入口、患者搜索、身份证读卡和快捷操作入口。搜索患者后根据结果数量智能分发：0 个提示创建、1 个直接预填充挂号、多个跳转患者列表。

## 目录结构

```
LYBT.Desktop.Receptionist/
├── ReceptionistModule.cs            # Prism IModule 入口
├── ViewModels/
│   └── ReceptionistHomeViewModel.cs
└── Views/
    └── ReceptionistHomeView.xaml(.cs)
```

## 核心组件

### ReceptionistModule

**设计依据**: Prism IModule 标准入口，依赖 Patients、Registration、CardReader 三个业务模块。

| 注册项 | 类型 | 说明 |
|--------|------|------|
| `ReceptionistHomeViewModel` | ViewModel | 前台主页 VM |
| `ReceptionistHomeView` | Navigation | 前台主页视图 |

**模块依赖**: `PatientsModule`, `RegistrationModule`, `CardReaderModule`

### ReceptionistHomeViewModel

**设计依据**: NavigableViewModelBase 子类，前台工作台的核心交互枢纽。

| 属性 | 说明 |
|------|------|
| `CurrentUserName` | 当前用户姓名 |
| `SearchKeyword` | 患者搜索关键词 |
| `TodayRegistrationCount` | 今日挂号数 |
| `WaitingCount` | 等待中数量 |
| `CompletedCount` | 已完成数量 |
| `RegistrationQueue` | 挂号队列集合 |

| 命令 | 说明 |
|------|------|
| `NavigateToPatientManagement` | 导航到患者管理 |
| `NavigateToRegistrationQueue` | 导航到挂号队列 |
| `NavigateToCardReaderAsync` | 打开身份证读卡器 |
| `CreateNewPatient` | 创建新患者（导航到患者管理 + Create 参数） |
| `CreateNewRegistration` | 创建新挂号（导航到挂号队列 + Create 参数） |
| `SearchPatientAsync` | 搜索患者（智能分发） |

**SearchPatientAsync 智能分发逻辑**:

| 搜索结果 | 行为 |
|----------|------|
| 0 个结果 | 弹窗提示「是否创建新患者？」 |
| 1 个结果 | 直接导航到挂号创建，预填充 `PatientId` + `PatientName` |
| 多个结果 | 导航到患者管理列表，带 `SearchKeyword` 参数 |

**读卡器集成**:

- 读卡前自动检查连接状态，未连接则尝试 `InitializeAsync()`
- 读卡成功后调用 `ProcessCardReaderResultAsync()` 查找/创建患者
- 找到已有患者：提示「是否前往挂号？」后预填充导航
- 未找到患者：提示「是否创建新患者？」后调用 `FindOrCreatePatientAsync()`
- 身份证号脱敏: `MaskIdNumber()` 保留前 6 后 4 位

## 依赖关系

```
ReceptionistModule
├── LYBT.Desktop.Contracts    # INavigationCoordinator, IRegistrationService, IPatientService, ISessionManager
├── LYBT.Desktop.Infrastructure  # NavigableViewModelBase, ViewNames
├── LYBT.Desktop.CardReader   # ICardReaderService, IPatientCardReaderIntegration
├── LYBT.Desktop.Registration # IRegistrationService (挂号队列)
└── LYBT.Desktop.Patients     # IPatientService (患者搜索)
```

## 设计决策

1. **单 ViewModel 架构**: 前台角色仅有一个 `ReceptionistHomeViewModel`，所有功能通过导航命令分发到业务模块视图
2. **智能搜索分发**: `SearchPatientAsync` 根据结果数量（0/1/多）选择不同交互路径，减少前台操作步骤
3. **读卡器延迟初始化**: 不在构造时初始化读卡器，而是在用户点击时按需连接，避免启动阻塞
4. **导航参数预填充**: 通过 `Dictionary<string, object>` 传递 `PatientId`/`PatientName`/`Action` 等参数

## 已知陷阱

- **模块依赖加载顺序**: ReceptionistModule 依赖 PatientsModule、RegistrationModule、CardReaderModule，若这些模块未加载会 DI 失败
- **读卡器初始化失败**: `NavigateToCardReaderAsync` 中读卡器初始化失败仅显示警告，不会抛出异常
- **MaskIdNumber 重复实现**: CardReaderViewModel 和 ReceptionistHomeViewModel 各自实现了 `MaskIdNumber()`，修改时需同步
- **LoadStatisticsAsync 数据来源**: 当前通过 `GetPagedAsync(pageSize:100)` 获取统计数据，非专用统计 API，大数据量时性能堪忧
- **RegistrationQueue 模型映射**: 手动将 `RegistrationListDto` 映射为 `RegistrationQueueItem`，DTO 变更时需同步更新
