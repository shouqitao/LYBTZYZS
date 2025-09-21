# LYBT.Module.Prescriptions

> **处方管理核心模块** - 中医智能处方系统
> 处方开具与管理 | 药材项目管理 | 剂量自动计算 | 处方复制功能
> 模块状态: ✅ **生产就绪** | 🎆 **分层架构完成** | **编译通过** | **2025-09-20更新**

## 🚀 快速开始
- 还原依赖：dotnet restore LYBT.Server.sln
- 构建：dotnet build LYBT.Server.sln -c Release --no-restore
- 运行 WebAPI：dotnet run --project src/Server/Services/LYBT.WebAPI
## 🔌 API 接口
- 控制器:   路由前缀: /api/v1/Prescriptions
- 控制器:   路由前缀: /api/v1/prescriptions/operation
## 📚 相关文档
- docs/architecture/overview.md
- docs/api/README.md
- docs/modules/index.md
- src/Shared/LYBT.Shared.Interfaces/Api/IPrescriptionsApi.cs

