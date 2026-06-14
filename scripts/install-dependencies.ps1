# PowerShell脚本 - 批量安装依赖包

Write-Host "=== 开始安装项目依赖 ===" -ForegroundColor Green

$modules = @(
    "src/Server/Modules/LYBT.Module.Users/LYBT.Module.Users.csproj",
    "src/Server/Modules/LYBT.Module.Herbs/LYBT.Module.Herbs.csproj",
    "src/Server/Modules/LYBT.Module.Patients/LYBT.Module.Patients.csproj"
)

# 为所有模块安装AutoMapper和FluentValidation
foreach ($module in $modules) {
    Write-Host "`n正在为 $module 安装包..." -ForegroundColor Yellow

    # 安装AutoMapper.Extensions.Microsoft.DependencyInjection
    Write-Host "  - 安装 AutoMapper.Extensions.Microsoft.DependencyInjection" -ForegroundColor Cyan
    dotnet add $module package AutoMapper.Extensions.Microsoft.DependencyInjection

    # 安装FluentValidation.DependencyInjectionExtensions
    Write-Host "  - 安装 FluentValidation.DependencyInjectionExtensions" -ForegroundColor Cyan
    dotnet add $module package FluentValidation.DependencyInjectionExtensions
}

Write-Host "`n=== 恢复所有包 ===" -ForegroundColor Green
dotnet restore LYBTZYZS.sln

Write-Host "`n=== 尝试编译 ===" -ForegroundColor Green
dotnet build LYBTZYZS.sln --no-restore

Write-Host "`n=== 安装完成 ===" -ForegroundColor Green