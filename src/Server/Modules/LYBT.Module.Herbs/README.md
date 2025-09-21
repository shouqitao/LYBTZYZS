# LYBT.Module.Herbs

> **药材管理核心模块** - 中药材信息管理中心
> 药材档案管理 | 价格维护 | 拼音检索 | 批量导入导出
> 模块状态: ✅ **生产就绪** | 🎆 **分层架构完成** | **编译通过** | **2025-09-20更新**

## 🚀 快速开始
- 还原依赖：dotnet restore LYBT.Server.sln
- 构建：dotnet build LYBT.Server.sln -c Release --no-restore
- 运行 WebAPI：dotnet run --project src/Server/Services/LYBT.WebAPI
## 🔌 API 接口
- 控制器:   路由前缀: /api/v1/Herbs
- 控制器:   路由前缀: /api/v1/herbs/operation
## 📚 相关文档
- docs/architecture/overview.md
- docs/api/README.md
- docs/modules/index.md
- src/Shared/LYBT.Shared.Interfaces/Api/IHerbsApi.cs

