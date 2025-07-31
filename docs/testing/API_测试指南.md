# LYBT API 测试指南

## 🚀 快速开始

### 方法1: Postman GUI测试（推荐）

1. **导入测试集合**
   
   - 打开Postman
   - 点击 `Import` → 选择 `LYBT_API_Tests.postman_collection.json`
   - 导入环境配置: `LYBT_Dev_Environment.postman_environment.json`

2. **运行测试**
   
   - 选择 "LYBT开发环境" 环境
   - 点击集合右侧的 `Run` 按钮
   - 选择要运行的测试项目
   - 点击 `Run LYBT医疗系统API测试集合`

### 方法2: Newman CLI测试（批量自动化）

1. **安装Newman**
   
   ```bash
   npm install -g newman
   ```

2. **运行测试脚本**
   
   ```bash
   run_api_tests.bat
   ```

## 📋 测试覆盖范围

### 1. 认证测试 ✅

- [x] 健康检查 (`/api/health`)
- [x] 数据库健康检查 (`/api/health/database`)  
- [x] 超级管理员登录 (`/api/v1/Auth/login`)
- [x] 密码哈希生成 (`/api/v1/Auth/hashPassword`)

### 2. 业务模块测试 ✅

- [x] **用户管理** (`/api/v1/Users/*`)
  - 获取活跃用户
  - 用户搜索
- [x] **患者管理** (`/api/v1/Patients/*`)
  - 获取活跃患者
  - 分页查询患者
- [x] **医生管理** (`/api/v1/Doctors/*`)
  - 获取活跃医生
  - 分页查询医生
- [x] **收费管理** (`/api/v1/Billing/*`)
- [x] **处方管理** (`/api/v1/Prescriptions/*`)
- [x] **排队管理** (`/api/v1/Queueing/*`)

### 3. 授权测试 ✅

- [x] 无token访问受保护端点（应返回401）
- [x] 无效token访问（应返回401）
- [x] 有效token访问（应返回200）

### 4. 性能测试 ✅

- [x] 响应时间测试（< 2秒）
- [x] 并发登录测试

## 🔧 测试配置

### 环境变量

- `base_url`: API基础URL (默认: http://localhost:5297)
- `jwt_token`: JWT认证令牌（自动获取）
- `admin_username`: 管理员用户名 (sysadmin)
- `admin_password`: 管理员密码 (Admin@123456)

### 自动化特性

- ✅ 自动获取JWT令牌
- ✅ 自动设置Authorization头
- ✅ 响应时间监控
- ✅ 状态码验证
- ✅ 数据结构验证

## 📊 测试报告

### Newman HTML报告

运行 `run_api_tests.bat` 后会生成 `test_results.html` 报告，包含:

- 测试通过率
- 响应时间统计
- 失败请求详情
- 性能指标

### 测试反馈格式

请按以下格式提供测试结果:

```
=== LYBT API测试结果 ===

1. 认证测试:
   - 健康检查: ✅/❌
   - 数据库检查: ✅/❌  
   - 管理员登录: ✅/❌
   - 密码哈希: ✅/❌

2. 业务模块测试:
   - 用户管理: ✅/❌ (详情: ...)
   - 患者管理: ✅/❌ (详情: ...)
   - 医生管理: ✅/❌ (详情: ...)
   - 收费管理: ✅/❌ (详情: ...)
   - 处方管理: ✅/❌ (详情: ...)
   - 排队管理: ✅/❌ (详情: ...)

3. 授权测试:
   - 无token访问: ✅/❌
   - 无效token: ✅/❌

4. 性能数据:
   - 平均响应时间: XXX ms
   - 最慢请求: XXX ms
   - 测试通过率: XX%

5. 发现的问题:
   - [问题1描述]
   - [问题2描述]
   - ...
```

## 🛠️ 故障排除

### 常见问题

1. **连接失败**
   
   - 确保WebAPI服务运行在 http://localhost:5297
   - 检查防火墙设置

2. **认证失败**
   
   - 检查admin_password是否正确
   - 确认数据库中AdminSecrets表有sysadmin记录

3. **业务模块404错误**
   
   - 检查服务注册配置
   - 确认AddAllModules()已调用

### 调试模式

在Postman中启用 `Console` 查看详细请求/响应日志。

## 📈 测试扩展

### 添加新测试

1. 在对应的业务模块文件夹中添加新请求
2. 设置适当的测试脚本
3. 配置必要的环境变量

### 数据驱动测试

可以创建CSV文件进行批量数据测试:

```csv
username,password,expected_result
sysadmin,Admin@123456,success
testuser,wrongpass,failure
```

---

**创建时间**: 2025-07-30  
**版本**: v1.0.0  
**维护者**: Claude Code Assistant