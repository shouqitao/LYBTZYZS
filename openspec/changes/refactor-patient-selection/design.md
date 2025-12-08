# Design: refactor-patient-selection

## Overview

本文档描述患者选择模块优化的技术设计方案，聚焦于性能优化和UI/UX改进。

## Current Architecture Analysis

### 现有组件结构（已良好分层）

```
Client层患者选择组件（生产使用）：
├── LYBT.Desktop.Patients/
│   ├── Views/
│   │   └── PatientSelectionView.xaml        # 主力生产页面（完整功能）
│   ├── ViewModels/
│   │   └── PatientSelectionViewModel.cs     # ~500行，组合多个服务
│   └── Services/
│       ├── PatientSearchManager.cs          # 搜索+分页（Issue #1790已提取）
│       ├── PendingQueueManager.cs           # 待诊队列管理
│       └── UnfinishedCaseHandler.cs         # 未完成医案检查
│   └── ViewModels/Components/
│       ├── MedicalCaseStartCoordinator.cs   # 医案启动协调
│       └── PatientCommandHandler.cs         # 命令处理

未完成的通用控件（暂不在本次改进范围）：
├── LYBT.Desktop.Presentation/
│   └── Components/PatientSelector/
│       ├── PatientSelectorControl.xaml      # 未完成，使用模拟数据
│       └── PatientSelectorViewModel.cs      # 未连接API
```

### 现有架构评估

| 组件 | 评估 | 结论 |
|------|------|------|
| PatientSelectionViewModel | 使用组合模式，职责清晰 | 保持现状 |
| PatientSearchManager | 已从ViewModel提取（Issue #1790） | 保持现状 |
| PendingQueueManager | 职责单一 | 保持现状 |
| MedicalCaseStartCoordinator | 职责单一 | 保持现状 |
| PatientSelectorControl | 未完成，模拟数据 | 暂不改动 |

## Target Architecture

### 新增组件

```
Client层新增：
├── LYBT.Desktop.Patients/
│   └── Services/
│       └── PatientSearchCache.cs            # [新增] LRU搜索缓存
├── LYBT.Desktop.Presentation/
│   └── Helpers/
│       └── HighlightHelper.cs               # [新增] 关键字高亮工具
```

### 改动范围

| 组件 | 变更类型 | 说明 |
|------|----------|------|
| `PatientSelectionViewModel` | 增强 | 集成缓存、调整防抖 |
| `PatientSelectionView.xaml` | 增强 | 键盘导航、状态指示UI |
| `PatientSearchCache` | 新增 | LRU缓存服务 |
| `HighlightHelper` | 新增 | 关键字高亮工具 |

## Detailed Design

### 1. 搜索缓存设计

```csharp
/// <summary>
/// 患者搜索缓存服务
/// 使用LRU策略，缓存最近10次搜索结果
/// </summary>
public class PatientSearchCache : IPatientSearchCache
{
    private readonly int _maxCacheSize = 10;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    private readonly LinkedList<CacheEntry> _cache = new();
    private readonly object _lock = new();

    public PagedResult<PatientDto>? Get(string keyword, int page);
    public void Set(string keyword, int page, PagedResult<PatientDto> result);
    public void Invalidate(string? keyword = null);
}

internal record CacheEntry(
    string Keyword,
    int Page,
    PagedResult<PatientDto> Result,
    DateTime CreatedAt);
```

**缓存策略：**
- 缓存Key：`keyword + page` 组合
- 最大容量：10条缓存
- 过期时间：5分钟
- 失效时机：创建/更新/删除患者时

### 2. 防抖时间调整

**当前**：300ms（`PatientSelectionViewModel:370`）
**目标**：500ms

```csharp
// PatientSelectionViewModel.cs
private void ScheduleSearch()
{
    _searchDebounceTimer?.Dispose();
    _searchDebounceTimer = new System.Threading.Timer(
        _ => System.Windows.Application.Current.Dispatcher.Invoke(async () => await ExecuteSearchAsync()),
        null, 500, System.Threading.Timeout.Infinite);  // 从300改为500
}
```

### 3. 键盘导航设计

**键盘快捷键：**
| 按键 | 功能 | 作用域 |
|------|------|--------|
| `↓` | 从搜索框移动焦点到列表第一项 | 搜索框 |
| `↑/↓` | 在列表项间移动 | 结果列表 |
| `Enter` | 选择当前项/开始看诊 | 结果列表 |
| `Escape` | 清空搜索/取消选择 | 全局 |
| `Ctrl+N` | 快速新建患者 | 全局 |

**实现方式：**
```xaml
<!-- PatientSelectionView.xaml -->
<UserControl PreviewKeyDown="OnPreviewKeyDown">
    <Grid>
        <TextBox x:Name="SearchBox" KeyDown="SearchBox_KeyDown"/>
        <DataGrid x:Name="PatientList" KeyDown="PatientList_KeyDown"/>
    </Grid>
</UserControl>
```

### 4. 搜索状态机

```
[Idle] --输入关键字--> [Debouncing]
[Debouncing] --500ms超时--> [Searching]
[Debouncing] --继续输入--> [Debouncing] (重置计时)
[Searching] --API返回--> [ResultsReady]
[Searching] --API错误--> [Error]
[ResultsReady] --清空输入--> [Idle]
[Error] --重试--> [Searching]
```

**状态定义：**
```csharp
public enum SearchState
{
    Idle,           // 空闲，无搜索
    Debouncing,     // 等待输入稳定
    Searching,      // 正在搜索（显示加载指示器）
    ResultsReady,   // 结果就绪
    Error           // 搜索失败
}
```

### 5. 关键字高亮设计

```csharp
/// <summary>
/// 关键字高亮工具
/// </summary>
public static class HighlightHelper
{
    /// <summary>
    /// 创建带高亮的TextBlock Inlines
    /// </summary>
    public static IEnumerable<Inline> CreateHighlightedText(
        string text,
        string keyword,
        Brush highlightBrush)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            yield return new Run(text);
            yield break;
        }

        int index = 0;
        int keywordLength = keyword.Length;
        int foundIndex;

        while ((foundIndex = text.IndexOf(keyword, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            // 非高亮部分
            if (foundIndex > index)
            {
                yield return new Run(text.Substring(index, foundIndex - index));
            }

            // 高亮部分
            yield return new Run(text.Substring(foundIndex, keywordLength))
            {
                Background = highlightBrush,
                FontWeight = FontWeights.Bold
            };

            index = foundIndex + keywordLength;
        }

        // 剩余部分
        if (index < text.Length)
        {
            yield return new Run(text.Substring(index));
        }
    }
}
```

### 6. UI状态指示

```xaml
<!-- PatientSelectionView.xaml 状态区域 -->
<Grid>
    <!-- 搜索状态指示器 -->
    <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
        <!-- 加载中 -->
        <ProgressBar IsIndeterminate="True"
                     Width="16" Height="16"
                     Visibility="{Binding IsSearching, Converter={StaticResource BoolToVisibility}}"/>

        <!-- 结果计数 -->
        <TextBlock Text="{Binding ResultCountText}"
                   Foreground="#666"
                   Margin="8,0,0,0"
                   Visibility="{Binding HasResults, Converter={StaticResource BoolToVisibility}}"/>

        <!-- 错误状态 -->
        <StackPanel Orientation="Horizontal"
                    Visibility="{Binding HasError, Converter={StaticResource BoolToVisibility}}">
            <TextBlock Text="{Binding ErrorMessage}" Foreground="Red"/>
            <Button Content="重试" Command="{Binding RetrySearchCommand}" Margin="8,0,0,0"/>
        </StackPanel>
    </StackPanel>
</Grid>
```

## Migration Strategy

### Phase 1: 性能优化（无UI变更）
1. 实现 `PatientSearchCache` 服务
2. 调整防抖时间为500ms
3. 集成缓存到 `PatientSelectionViewModel`
4. （可选）添加Server端轻量级搜索DTO

### Phase 2: UI/UX增强
1. 添加键盘导航支持
2. 添加搜索状态指示
3. 添加关键字高亮

## Testing Strategy

### 单元测试
- `PatientSearchCache` 缓存逻辑测试
- `HighlightHelper` 高亮逻辑测试

### 集成测试
- 缓存命中/未命中场景
- 键盘导航完整流程

### 性能测试
- 搜索响应时间基准测试
- 缓存命中率统计

## Rollback Plan

如果改进出现问题：
1. 缓存服务：可在DI中禁用
2. 防抖时间：一行代码恢复
3. UI变更：可独立回滚
