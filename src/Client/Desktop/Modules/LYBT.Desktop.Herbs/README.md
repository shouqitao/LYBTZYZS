# LYBT.Desktop.Herbs

> 中药材管理模块 | 药材维护/拼音搜索/批量导入

## 项目定位

- **层级**: Client Modules层
- **职责**: 提供中药材基础数据管理界面，支持药材CRUD、拼音快速搜索、批量导入导出

## 目录结构

```
LYBT.Desktop.Herbs/
├── Interfaces/
│   └── IHerbRepository.cs           # 药材仓储接口
├── Repositories/
│   └── HerbRepository.cs            # 药材仓储实现
├── ViewModels/
│   ├── HerbManagementViewModel.cs   # 药材管理ViewModel
│   ├── HerbDetailViewModel.cs       # 药材详情ViewModel
│   └── HerbItemViewModel.cs         # 药材条目ViewModel
├── Views/
│   ├── HerbManagementView.xaml      # 管理视图
│   ├── HerbDetailView.xaml          # 详情视图
│   └── HerbSelectionDialog.xaml     # 选择对话框
└── HerbsModule.cs                    # Prism模块注册
```

## HerbManagementViewModel

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| Herbs | ObservableCollection | 药材列表 |
| SelectedHerb | HerbDto | 选中的药材 |
| SearchText | string | 搜索关键词(支持拼音) |
| FilterCategory | string | 分类筛选 |
| IsLoading | bool | 加载状态 |
| TotalCount | int | 总数量 |
| PageIndex | int | 当前页码 |

### 命令(19个)

| 命令 | 说明 |
|------|------|
| LoadCommand | 加载药材列表 |
| SearchCommand | 搜索药材(支持拼音首字母) |
| CreateCommand | 新建药材 |
| EditCommand | 编辑药材 |
| DeleteCommand | 删除药材 |
| ImportCommand | 批量导入(Excel) |
| ExportCommand | 导出药材 |
| RefreshCommand | 刷新列表 |
| FilterCommand | 按分类筛选 |

## HerbDetailViewModel

### 属性(16个)

| 属性类别 | 属性 | 说明 |
|----------|------|------|
| 基本信息 | Name | 药材名称 |
| 基本信息 | PinYin | 拼音(用于快速搜索) |
| 基本信息 | Alias | 别名 |
| 基本信息 | Category | 分类(如:补虚药/清热药) |
| 药性 | Nature | 性(寒/热/温/凉/平) |
| 药性 | Flavor | 味(酸/苦/甘/辛/咸) |
| 药性 | Meridian | 归经 |
| 功效 | Functions | 功效 |
| 功效 | Indications | 主治 |
| 用法 | Dosage | 常用剂量 |
| 用法 | Usage | 用法说明 |
| 价格 | UnitPrice | 单价(元/克) |
| 状态 | HasChanges | 变更标记 |
| 状态 | IsActive | 启用状态 |

### 命令(15个)

| 命令 | 说明 |
|------|------|
| SaveCommand | 保存药材 |
| CancelCommand | 取消编辑 |
| ValidateCommand | 验证数据 |
| ToggleActiveCommand | 切换启用状态 |

## IHerbRepository

| 方法 | 说明 |
|------|------|
| GetAllAsync | 获取所有药材 |
| GetByIdAsync | 按ID获取 |
| SearchAsync | 搜索(支持拼音) |
| CreateAsync | 创建药材 |
| UpdateAsync | 更新药材 |
| DeleteAsync | 删除药材 |

## 拼音搜索特性

| 特性 | 说明 |
|------|------|
| 全拼搜索 | 输入"huangqi"匹配"黄芪" |
| 首字母搜索 | 输入"hq"匹配"黄芪" |
| 模糊匹配 | 输入"qi"匹配所有含"芪"的药材 |
| 实时搜索 | 输入即搜，无需回车 |

## 与Prescriptions集成

| 集成点 | 说明 |
|--------|------|
| HerbSelectionDialog | 处方模块调用药材选择对话框 |
| 药材选择 | 返回选中的HerbDto列表 |
| 剂量建议 | 提供常用剂量参考 |

## 依赖关系

### 依赖
- LYBT.Desktop.Models (ViewModelBase)
- LYBT.Desktop.Foundation (BaseApiRepository)
- LYBT.Desktop.Contracts (IHerbApi)
- LYBT.Shared.Models (HerbDto)
- Prism.Core/Prism.DryIoc (8.x)

### 被依赖
- LYBT.Desktop.Shell (模块加载)
- LYBT.Desktop.Prescriptions (药材选择)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |
