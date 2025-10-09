# API测试体系

## 概述

这是LYBT系统的API测试体系，替代失效的WebApplicationFactory集成测试。采用PowerShell脚本进行HTTP REST API契约测试，确保后端质量。

## 目录结构

```
tests/ApiTests/
├── README.md           # 本文档
├── Scripts/            # 测试脚本目录
│   ├── test-all-apis.ps1   # 完整API测试套件
│   └── test-login.ps1      # 登录专项测试
├── Data/               # 测试数据目录
│   ├── test-login.json     # 登录测试数据
│   └── test-doctor-login.json # 医生登录测试数据
└── Reports/            # 测试报告输出目录
```

## 测试策略

### 测试层级
- **单元测试 (70%)**：各模块Service/Repository层单元测试
- **API契约测试 (20%)**：HTTP API端点功能验证 
- **手工E2E测试 (10%)**：关键业务流程验证

### API测试覆盖范围
1. **认证模块**：登录、JWT Token验证
2. **用户管理**：用户CRUD操作、权限验证
3. **患者管理**：患者信息CRUD
4. **中药材管理**：药材信息CRUD
5. **处方管理**：处方创建、查询
6. **问诊模块**：问诊记录管理
7. **健康检查**：系统状态监控

## 使用方法

### 前置条件
1. 启动WebAPI服务：
   ```powershell
   cd src/Server/Services/LYBT.WebAPI
   dotnet run
   ```

2. 确保数据库连接正常

### 运行完整测试套件
```powershell
# 在项目根目录执行
./tests/ApiTests/Scripts/test-all-apis.ps1
```

### 运行单项测试
```powershell
# 仅测试登录功能
./tests/ApiTests/Scripts/test-login.ps1
```

## 测试结果说明

### 成功标准
- ✅ 绿色：测试通过
- ❌ 红色：测试失败  
- ℹ️ 蓝色：信息提示

### 通过率标准
- **100%**：完美通过 🎉
- **≥80%**：良好 ✅  
- **<80%**：需要改进 ❌

## 维护指南

### 添加新的API测试
1. 在`Scripts/`目录创建新脚本
2. 遵循现有脚本的结构和命名规范
3. 包含彩色输出和统计功能
4. 更新本README文档

### 测试数据管理
- 将测试用的JSON数据存放在`Data/`目录
- 使用有意义的文件名，如`test-{module}-{action}.json`
- 避免在脚本中硬编码测试数据

### 报告输出
- 测试运行日志和报告保存在`Reports/`目录
- 支持CI/CD集成时的结果分析

## 与单元测试的关系

API测试作为单元测试的补充：
- **单元测试**：验证业务逻辑正确性
- **API测试**：验证HTTP接口契约正确性
- **共同目标**：保证系统质量和稳定性

## 故障排除

### 常见问题
1. **连接失败**：检查WebAPI服务是否启动
2. **认证失败**：确认测试账号密码正确
3. **端口冲突**：检查5001端口是否被占用

### 调试方法
- 启用PowerShell详细输出：`$VerbosePreference = 'Continue'`
- 检查API响应内容和HTTP状态码
- 查看WebAPI服务日志

---

**最后更新**: 2025-10-09
**维护者**: LYBT开发团队