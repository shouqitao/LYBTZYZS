# PatientImportWizardViewModel 组件化分析报告

**Issue**: #1153 - Desktop端組件化架构标准化  
**Phase**: 4.1 - PatientImportWizard 模块结构分析  
**日期**: 2025-01-11  
**当前代码行数**: 1079 行

---

## 1. 现状分析

### 1.1 基本信息
- **文件路径**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientImportWizardViewModel.cs`
- **继承关系**: `BindableBase, IDisposable`（未使用 UnifiedViewModelBase）
- **核心依赖**: IPatientRepository, ILogger
- **特殊机制**: BackgroundWorker（长时间导入任务）

### 1.2 功能模块识别

#### 模块 1: 文件操作 (~150行)
**职责**：
- 下载 Excel 导入模板
- 打开文件选择对话框
- 读取 Excel 文件内容
- 生成模板文件

**关键方法**：
- `ExecuteDownloadTemplate()` - 保存模板
- `ExecuteSelectFile()` - 选择文件
- `LoadDataPreviewAsync()` - 加载预览数据

**使用的帮助类**：
- `ExcelHelper.ImportFromExcel()`
- `SaveFileDialog` / `OpenFileDialog`

---

#### 模块 2: 数据验证 (~250行)
**职责**：
- 验证 Excel 列结构
- 验证必需字段
- 检查数据格式和约束
- 检测重复数据（姓名、电话、证件号）
- 生成验证报告（错误和警告）

**关键方法**：
- `ValidateImportData(DataTable)` - 主验证逻辑
- `ValidateFileSelection()` - 文件有效性验证

**验证规则**：
- 必需列：姓名、性别
- 可选列：年龄、电话、证件号、地址、过敏史
- 姓名：不能为空，≤50字符
- 性别：必须是"男"或"女"
- 年龄：0-150 范围
- 电话：11位数字
- 证件号：18位
- 数据重复检查

**输出**：
- `ImportValidationResult` 对象
  - `IsValid: bool`
  - `Errors: List<string>`
  - `Warnings: List<string>`
  - `ValidRows: int`
  - `InvalidRows: int`

---

#### 模块 3: 导入执行 (~200行)
**职责**：
- 使用 BackgroundWorker 执行异步导入
- 逐行处理患者数据
- 调用 Repository 保存数据
- 处理导入过程中的异常
- 支持取消操作

**关键方法**：
- `InitializeImportWorker()` - 初始化 BackgroundWorker
- `ImportWorker_DoWork()` - 导入主逻辑
- `ImportWorker_ProgressChanged()` - 进度更新
- `ImportWorker_RunWorkerCompleted()` - 完成回调
- `ExecuteStartImport()` - 启动导入
- `ExecuteCancelImport()` - 取消导入

**BackgroundWorker 配置**：
- `WorkerReportsProgress = true`
- `WorkerSupportsCancellation = true`

**数据转换**：
- `DataRow` → `PatientCreateDto`
- 字段映射和类型转换

---

#### 模块 4: 进度监控 (~100行)
**职责**：
- 跟踪导入进度
- 统计成功/失败数量
- 更新进度百分比
- 记录错误消息

**数据结构**：
```csharp
public class ImportProgressInfo
{
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int ProgressPercentage { get; set; }
    public string CurrentMessage { get; set; }
    public List<string> ErrorMessages { get; set; }
}
```

---

#### 模块 5: UI 状态管理 (~200行)
**职责**：
- 向导步骤切换（4个步骤）
- 步骤样式更新（Active/Completed/Pending）
- 按钮状态管理（下一步/上一步/取消）
- 步骤内容视图切换
- 命令 CanExecute 状态

**步骤枚举**：
```csharp
public enum ImportWizardStep
{
    TemplateDownload,    // 步骤1：下载模板
    FileSelection,       // 步骤2：选择文件
    DataPreview,         // 步骤3：预览验证
    ImportExecution      // 步骤4：执行导入
}
```

**UI 状态属性**：
- `Step1Style` / `Step2Style` / `Step3Style` / `Step4Style`
- `CurrentStepContent`
- `StepDescription`
- `NextButtonText`
- `CanGoNext` / `CanGoPrevious`

**关键方法**：
- `UpdateStepStyles()` - 更新步骤样式
- `UpdateButtonStates()` - 更新按钮状态
- `UpdateStepContent()` - 更新内容视图

---

#### 模块 6: 命令处理 (~100行)
**职责**：
- 8个命令的实现
- CanExecute 逻辑

**命令列表**：
1. `NextCommand` - 下一步
2. `PreviousCommand` - 上一步
3. `CancelCommand` - 取消向导
4. `DownloadTemplateCommand` - 下载模板
5. `SelectFileCommand` - 选择文件
6. `StartImportCommand` - 开始导入
7. `CancelImportCommand` - 取消导入

---

### 1.3 复杂度评估

| 指标 | 数值 | 评估 |
|------|------|------|
| 总代码行数 | 1079 行 | ⚠️ 严重超标（阈值 800 行） |
| 独立职责数量 | 6 个模块 | ⚠️ 超标（阈值 4 个） |
| 命令数量 | 8 个 | ✅ 正常 |
| 外部依赖 | 2 个 (Repository, Logger) | ✅ 正常 |
| 异步操作复杂度 | 高（BackgroundWorker） | ⚠️ 需要特殊处理 |
| 状态管理复杂度 | 高（4步骤 + 进度 + 验证） | ⚠️ 需要简化 |

**触发组件化的原因**：
1. ✅ ViewModel > 800 行（1079 行）
2. ✅ 独立职责 ≥ 4 类型（6 个模块）
3. ✅ 复杂的异步操作和状态管理

---

## 2. 组件化拆分方案

### 2.1 推荐架构

```
PatientImportWizardViewModel (协调器, ~280行)
├── ImportFileReader (文件读取, ~150行)
├── ImportDataValidator (数据验证, ~250行)
├── ImportExecutor (导入执行, ~200行)
└── ImportProgressReporter (进度监控, ~100行)
```

**总行数**: 280 + 150 + 250 + 200 + 100 = 980 行（含组件）  
**ViewModel 行数**: ~280 行（减少 74%）

---

### 2.2 组件设计

#### 组件 1: ImportFileReader

**职责**：
- Excel 文件读写操作
- 模板生成
- 文件选择对话框

**公共方法**：
```csharp
public class ImportFileReader
{
    public async Task<(bool success, string? filePath, string? errorMessage)> 
        SelectFileAsync();
    
    public async Task<(bool success, DataTable? data, string? errorMessage)> 
        ReadExcelAsync(string filePath);
    
    public async Task<(bool success, string? errorMessage)> 
        GenerateTemplateAsync(string savePath);
}
```

**依赖**：
- ILogger
- ExcelHelper (静态工具类)

---

#### 组件 2: ImportDataValidator

**职责**：
- 验证 Excel 数据结构
- 验证业务规则
- 生成验证报告

**公共方法**：
```csharp
public class ImportDataValidator
{
    public ImportValidationResult ValidateData(DataTable data);
    
    public bool ValidateRow(DataRow row, out List<string> errors, out List<string> warnings);
    
    public bool CheckDuplicates(DataTable data, out Dictionary<string, int> duplicates);
}
```

**输出对象**：
```csharp
public class ImportValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
}
```

**验证规则封装**：
- 必需列配置
- 字段验证规则（长度、格式、范围）
- 重复检测逻辑

---

#### 组件 3: ImportExecutor

**职责**：
- BackgroundWorker 管理
- 逐行数据导入
- Repository 调用
- 异常处理和重试

**公共方法**：
```csharp
public class ImportExecutor : IDisposable
{
    public event EventHandler<ImportProgressEventArgs>? ProgressChanged;
    public event EventHandler<ImportCompletedEventArgs>? ImportCompleted;
    
    public void StartImport(DataTable data);
    
    public void CancelImport();
    
    public bool IsImporting { get; }
    
    public void Dispose();
}
```

**事件参数**：
```csharp
public class ImportProgressEventArgs : EventArgs
{
    public int ProcessedRows { get; set; }
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public string CurrentMessage { get; set; }
}

public class ImportCompletedEventArgs : EventArgs
{
    public bool Success { get; set; }
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> ErrorMessages { get; set; }
}
```

**依赖**：
- IPatientRepository
- ILogger
- BackgroundWorker（内部管理）

**关键特性**：
- 支持取消操作
- 进度报告（每处理 10 行报告一次）
- 错误收集和日志记录

---

#### 组件 4: ImportProgressReporter

**职责**：
- 进度信息聚合
- 统计数据计算
- 进度百分比计算

**公共方法**：
```csharp
public class ImportProgressReporter
{
    public ImportProgressInfo CurrentProgress { get; }
    
    public void UpdateProgress(int processed, int total, int success, int failure);
    
    public void AddError(string errorMessage);
    
    public void Reset();
    
    public string GetSummaryMessage();
}
```

**数据模型**：
```csharp
public class ImportProgressInfo : BindableBase
{
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int ProgressPercentage { get; set; }
    public string CurrentMessage { get; set; }
    public ObservableCollection<string> ErrorMessages { get; set; }
}
```

---

### 2.3 重构后的 ViewModel 结构

```csharp
public class PatientImportWizardViewModel : BindableBase, IDisposable
{
    #region 组件
    private readonly ImportFileReader _fileReader;
    private readonly ImportDataValidator _validator;
    private readonly ImportExecutor _executor;
    private readonly ImportProgressReporter _progressReporter;
    #endregion

    #region 属性 (~80行)
    // 向导状态
    public ImportWizardStep CurrentStep { get; set; }
    public string SelectedFilePath { get; set; }
    public DataTable? PreviewData { get; set; }
    public ImportValidationResult? ValidationResult { get; set; }
    
    // UI 状态
    public bool IsImporting => _executor.IsImporting;
    public ImportProgressInfo ProgressInfo => _progressReporter.CurrentProgress;
    public Style Step1Style/Step2Style/Step3Style/Step4Style { get; }
    public string StepDescription { get; }
    public bool CanGoNext/CanGoPrevious { get; }
    #endregion

    #region 命令 (~20行)
    public ICommand NextCommand { get; }
    public ICommand PreviousCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand DownloadTemplateCommand { get; }
    public ICommand SelectFileCommand { get; }
    public ICommand StartImportCommand { get; }
    public ICommand CancelImportCommand { get; }
    #endregion

    #region 命令实现 (~80行)
    // 委托给组件执行，只保留协调逻辑
    private async void ExecuteDownloadTemplate()
    {
        var (success, errorMessage) = await _fileReader.GenerateTemplateAsync(...);
        // 处理结果
    }

    private async void ExecuteSelectFile()
    {
        var (success, filePath, errorMessage) = await _fileReader.SelectFileAsync();
        SelectedFilePath = filePath;
        UpdateButtonStates();
    }

    private async void ExecuteNext()
    {
        if (CurrentStep == ImportWizardStep.FileSelection)
        {
            var (success, data, errorMessage) = await _fileReader.ReadExcelAsync(SelectedFilePath);
            PreviewData = data;
            ValidationResult = _validator.ValidateData(data);
            CurrentStep = ImportWizardStep.DataPreview;
        }
        // ... 其他步骤
    }

    private void ExecuteStartImport()
    {
        _executor.StartImport(PreviewData);
    }
    #endregion

    #region UI 更新方法 (~80行)
    private void UpdateStepStyles() { }
    private void UpdateButtonStates() { }
    private void UpdateStepContent() { }
    #endregion

    #region 事件处理 (~20行)
    private void OnImportProgressChanged(object? sender, ImportProgressEventArgs e)
    {
        _progressReporter.UpdateProgress(e.ProcessedRows, e.TotalRows, e.SuccessCount, e.FailureCount);
    }

    private void OnImportCompleted(object? sender, ImportCompletedEventArgs e)
    {
        // 处理完成逻辑
    }
    #endregion

    #region IDisposable
    public void Dispose()
    {
        _executor?.Dispose();
    }
    #endregion
}
```

**预估行数**: ~280 行

---

## 3. 特殊考虑

### 3.1 BackgroundWorker 封装
- 将 BackgroundWorker 完全封装在 ImportExecutor 内部
- 通过事件向外暴露进度和完成通知
- ViewModel 不直接操作 BackgroundWorker

### 3.2 IDisposable 实现
- ImportExecutor 实现 IDisposable
- ViewModel 的 Dispose 方法委托给 ImportExecutor
- 确保 BackgroundWorker 正确释放

### 3.3 UI 线程同步
- ImportExecutor 的事件在工作线程触发
- ViewModel 负责使用 Dispatcher 切换到 UI 线程
- 或在 ImportExecutor 内部处理线程同步

### 3.4 是否迁移到 UnifiedViewModelBase
**当前问题**：
- PatientImportWizardViewModel 继承 BindableBase + IDisposable
- UnifiedViewModelBase 未实现 IDisposable

**方案 A**（推荐）：保持当前继承关系
- 继续使用 BindableBase + IDisposable
- 组件化后代码量已大幅减少
- 避免引入 UnifiedViewModelBase 的 IDisposable 冲突

**方案 B**：扩展 UnifiedViewModelBase
- 在 UnifiedViewModelBase 添加 IDisposable 支持
- 影响所有继承类，需要全局测试
- 风险较高，不建议

---

## 4. 实施建议

### 4.1 Phase 4.2: 创建组件
1. ✅ 创建 `Components` 目录：`LYBT.Desktop.Patients/ViewModels/Components/`
2. ✅ 实现 ImportFileReader（~150行）
3. ✅ 实现 ImportDataValidator（~250行）
4. ✅ 实现 ImportExecutor（~200行）
5. ✅ 实现 ImportProgressReporter（~100行）

### 4.2 Phase 4.3: 重构 ViewModel
1. ✅ 注入组件依赖
2. ✅ 委托文件操作给 ImportFileReader
3. ✅ 委托验证逻辑给 ImportDataValidator
4. ✅ 委托导入执行给 ImportExecutor
5. ✅ 委托进度管理给 ImportProgressReporter
6. ✅ 保留 UI 状态管理和命令协调
7. ✅ 测试完整导入流程

### 4.3 预期成果
- **代码行数**: 1079 → 280 行（减少 74%）
- **独立职责**: 6 个模块 → 2 个（UI 协调 + 命令处理）
- **可测试性**: 大幅提升（组件可单独单元测试）
- **可维护性**: 显著改善（关注点分离）

---

## 5. 风险评估

| 风险项 | 等级 | 缓解措施 |
|--------|------|----------|
| BackgroundWorker 封装复杂度 | 中 | 参考现有实现，保持接口简单 |
| UI 线程同步问题 | 中 | 明确线程同步责任边界 |
| 重构引入 Bug | 中 | 完整的导入流程测试 |
| IDisposable 资源泄漏 | 低 | 严格遵循 Dispose 模式 |
| 进度报告性能 | 低 | 批量更新（每 10 行） |

---

## 6. 结论

PatientImportWizardViewModel（1079行）是 Issue #1153 识别的三个目标模块中最复杂的一个。通过拆分为 4 个专用组件，可以将其简化到约 280 行，达到统一设计标准的要求。

**关键收益**：
1. ✅ 符合复杂度阈值规则（< 800 行）
2. ✅ 职责清晰分离（< 4 个独立职责）
3. ✅ 可测试性大幅提升
4. ✅ 可维护性显著改善
5. ✅ 为未来类似功能提供参考模板

**下一步行动**：
- [ ] 决定是否实施 Phase 4.2-4.3（实际组件创建和重构）
- [ ] 或者基于 Prescription/Formula 的成功经验，直接更新文档并总结规范
- [ ] 提交最终 PR

---

**生成时间**: 2025-01-11  
**分析工具**: Claude Code + Serena MCP
