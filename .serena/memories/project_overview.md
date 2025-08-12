# LYBTZYZS 项目概述

## 项目目的
凌隐宝堂中医诊所诊疗系统 - 为中医诊所提供完整的数字化管理解决方案

## 技术栈
- **后端**: .NET 8, ASP.NET Core Web API, Entity Framework Core 8.0.17
- **前端**: WPF (.NET 8), Prism.DryIoc 9.0.537, Refit
- **数据库**: SQL Server (localhost/LYBTDB)
- **认证**: JWT Bearer Token
- **测试**: xUnit (后端), Python (API测试)
- **文档**: Swagger/Swashbuckle 9.0.1

## 项目结构
```
LYBTZYZS/
├── LYBT.All.sln              # 总解决方案
├── src/
│   ├── Backend/
│   │   ├── Core/            # 核心库（Infrastructure, Models）
│   │   ├── Modules/         # 8个业务模块
│   │   └── Services/        # Web API服务
│   ├── Frontend/
│   │   └── Desktop/         # WPF客户端
│   └── Shared/              # 共享模型和工具
├── docs/                    # 文档库
├── scripts/                 # 自动化脚本
├── tests/                   # 测试项目
└── BIN/                     # 统一输出目录
```

## 8个核心业务模块
1. **Auth** - 身份认证和授权
2. **Users** - 用户管理  
3. **Patients** - 患者档案（包含基础挂号功能）
4. **Consultation** - 看诊管理（核心模块，支持中医四诊）
5. **MedicalCase** - 医疗案例（诊疗流程聚合根）
6. **Prescriptions** - 处方管理
7. **Herbs** - 中药材管理（仅处方用药）
8. **Formula** - 验方管理（经典验方模板）

## 开发环境
- Windows 10/11
- Visual Studio 2022
- SQL Server 
- .NET SDK 8.0+