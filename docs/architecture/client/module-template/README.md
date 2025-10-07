# Client 端业务模块模板

> 本目录包含标准化的模块模板，用于快速创建符合统一设计标准的新模块。

---

## 模板文件清单

### 1. ViewModel 模板
- `{Entity}ManagementViewModel.template.cs` - 列表管理ViewModel
- `{Entity}DetailViewModel.template.cs` - 详情查看ViewModel

### 2. View 模板
- `{Entity}ManagementView.template.xaml` - 列表管理View
- `{Entity}DetailView.template.xaml` - 详情查看View

### 3. Service 模板
- `{Entity}Service.template.cs` - 业务服务实现
- `{Entity}MappingProfile.template.cs` - AutoMapper配置

### 4. 检查清单
- `module-checklist.md` - 模块开发检查清单

---

## 使用方法

### 步骤1：创建模块项目

```powershell
# 在 src/Client/Desktop/Modules/ 目录下创建新模块
mkdir LYBT.Desktop.{ModuleName}
cd LYBT.Desktop.{ModuleName}

# 创建标准目录结构
mkdir Models, ViewModels, Views

# 创建项目文件（参考其他模块的 .csproj）
```

### 步骤2：复制模板文件

1. 复制对应的 `.template.cs` 或 `.template.xaml` 文件
2. 重命名，将 `{Entity}` 替换为实际实体名（如 `Patient`, `User`）
3. 将 `{Module}` 替换为模块名（如 `Patients`, `Users`）
4. 将 `{entity}` 替换为小写实体名（如 `patient`, `user`）

### 步骤3：填充业务逻辑

1. 根据实际需求修改模板中的占位符
2. 实现特定业务逻辑
3. 添加自定义命令和属性

### 步骤4：验证

使用 `module-checklist.md` 检查清单验证模块是否符合标准

---

## 快速替换脚本（PowerShell）

```powershell
# 使用参数替换模板
param(
    [Parameter(Mandatory=$true)]
    [string]$EntityName,      # 如 "Patient"

    [Parameter(Mandatory=$true)]
    [string]$ModuleName       # 如 "Patients"
)

$entity = $EntityName.ToLower()

# 复制并替换 ViewModel
$vmTemplate = Get-Content ".\{Entity}ManagementViewModel.template.cs" -Raw
$vmContent = $vmTemplate -replace '\{Entity\}', $EntityName `
                         -replace '\{Module\}', $ModuleName `
                         -replace '\{entity\}', $entity

Set-Content -Path "..\LYBT.Desktop.$ModuleName\ViewModels\${EntityName}ManagementViewModel.cs" -Value $vmContent

Write-Host "✅ ViewModel 已生成"
```

---

## 参考

- [统一设计标准](../unified-design-standard.md)
- [模块开发检查清单](./module-checklist.md)
