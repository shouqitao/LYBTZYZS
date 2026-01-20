# integrate-cardreader-module

## Why

将已完整实现的`LYBT.Desktop.CardReader`模块集成到Desktop解决方案中，为挂号/就诊工作台提供身份证读卡功能，实现患者快速识别和信息录入。

### 现状分析

| 组件 | 状态 | 说明 |
|------|------|------|
| CardReader模块 | **已完成** | Adapters/Services/Models/Native/Integration已实现 |
| 华大HD100适配器 | **已完成** | P/Invoke封装，支持USB读卡器 |
| MockCardReader | **已完成** | 开发测试用模拟读卡器 |
| IPatientCardReaderIntegration | **已定义** | 接口已在CardReader模块，待Patients模块实现 |
| Shell集成 | **待实现** | 未添加到解决方案/模块注册 |
| UI集成 | **待实现** | 工作台未添加读卡器控件 |

### 用户需求（头脑风暴确认）

1. **集成范围**: 挂号工作台 + 就诊工作台（两者都要）
2. **快速创建逻辑**: 预填表单模式（因电话等必填字段身份证无法提供）
3. **自动读卡模式**: 需要支持刷卡即触发

## What Changes

### Phase 1: 基础集成（解决方案+Shell）

1. 将CardReader项目添加到LYBT.Desktop.sln
2. 在Shell.csproj添加项目引用
3. 在App.xaml.cs注册CardReaderModule
4. 编译验证

### Phase 2: Patients模块实现IPatientCardReaderIntegration

1. 在Patients模块创建`PatientCardReaderIntegration`服务
2. 实现`FindPatientByIdNumberAsync` - 通过SearchAsync扩展支持
3. 实现`QuickCreatePatientAsync` - 需要用户UI确认流程
4. 实现`FindOrCreatePatientAsync` - 组合查找+创建
5. 在PatientsModule注册服务

### Phase 3: 就诊工作台UI集成

1. 在MedicalCaseWorkspaceView左侧面板添加读卡器状态控件
2. 在MedicalCaseWorkspaceViewModel添加CardReader交互逻辑
3. 支持自动读卡模式（可开关）
4. 读卡成功后自动选择/创建患者并加入待诊队列

### Phase 4: 挂号工作台UI集成（待定）

> **注意**: 当前无独立挂号工作台，待诊队列在就诊工作台实现。
> 如需独立挂号工作台，需另开提案规划。

本Phase暂定为：在Patients模块的主详情页支持读卡快速录入。

## Architecture

### 集成架构图

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Shell (App.xaml.cs)                          │
│                    注册 CardReaderModule                             │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
        ┌──────────────────────┼──────────────────────┐
        ▼                      ▼                      ▼
┌───────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ CardReader    │    │   Patients      │    │   Clinical      │
│ Module        │    │   Module        │    │   Module        │
├───────────────┤    ├─────────────────┤    ├─────────────────┤
│ ICardReader   │◄───│ IPatientCard-   │◄───│ Workspace-      │
│ Service       │    │ ReaderIntegra-  │    │ ViewModel       │
│               │    │ tion            │    │                 │
│ HuaDaHD100    │    │                 │    │ 自动读卡模式    │
│ Adapter       │    │ PatientCard-    │    │ 患者选择/创建   │
│               │    │ ReaderIntegra-  │    │                 │
│ MockReader    │    │ tion (实现)     │    │                 │
└───────────────┘    └─────────────────┘    └─────────────────┘
```

### 数据流

```
读卡器检测到卡片
    │
    ▼
ICardReaderService.ReadCardAsync()
    │
    ▼
CardReadResult (姓名、身份证号、性别、出生日期、地址、照片)
    │
    ▼
IPatientCardReaderIntegration.FindOrCreatePatientAsync()
    │
    ├──► 找到患者 → PatientFromCardResult (IsNewlyCreated=false)
    │
    └──► 未找到 → 显示预填表单 → 用户补充电话等必填字段 → 创建患者
                                                        │
                                                        ▼
                                           PatientFromCardResult (IsNewlyCreated=true)
    │
    ▼
加入待诊队列 / 导航到患者医案
```

## Impact

- **文件变更**: 约15-20个文件
- **风险等级**: Low（新增功能，不影响现有流程）
- **测试要求**:
  - MockCardReader模式功能测试
  - 实机读卡器测试（需HD100设备）

## Risks

| 风险 | 缓解措施 |
|------|----------|
| HDstdapi.dll运行时找不到 | 添加DLL到输出目录，README说明部署要求 |
| 读卡器硬件不可用 | 支持MockCardReader作为Fallback |
| 患者快速创建缺少必填字段 | 预填表单模式，用户必须确认补全 |
| 自动读卡模式性能影响 | 可配置开关，默认关闭 |

## References

- CardReader模块文档: `src/Client/Desktop/Modules/LYBT.Desktop.CardReader/CLAUDE.md`
- IPatientCardReaderIntegration接口: `CardReader/Integration/IPatientCardReaderIntegration.cs`
- 就诊工作台: `Clinical/Views/MedicalCaseWorkspaceView.xaml`

---

**提案日期**: 2026-01-20
**提案人**: Claude Code
