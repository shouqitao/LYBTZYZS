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

### 核心功能

- 属性: Herbs列表、拼音搜索、分类筛选、分页
- 命令(19个): CRUD、批量导入导出(Excel)、分类筛选

## HerbDetailViewModel

### 核心功能

- 属性(16个): 基本信息(名称/拼音/别名/分类)、药性(性/味/归经)、功效/主治、用法/剂量/价格、状态
- 命令(15个): 保存/取消、验证、启用状态切换

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

## 设计依据

- 药材作为独立基础数据模块，是验方和处方的底层依赖，独立管理避免与业务流程耦合
- 拼音搜索(全拼+首字母)适配中医师快速检索习惯，输入"hq"即可定位"黄芪"
- HerbListControl/HerbItemControl采用可复用控件设计，同时服务于药材管理、验方编辑和处方开具三个场景
- 批量导入(Excel)功能支持药材基础数据的初始化录入，降低系统启用门槛

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
