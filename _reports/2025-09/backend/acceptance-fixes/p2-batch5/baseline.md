# P2-Batch5 修复基线记录

## 系统基线状态
**记录时间**: 2025-09-15 18:50:00  
**Git分支**: release/p2-fix-batch5-auth-routing  
**基础分支**: release/backend-acceptance-smoke-full  

## 网络配置
**WebAPI端口**: 8080 (HTTP)  
**基础URL**: http://localhost:8080  
**健康检查**: http://localhost:8080/health  

## 系统状态快照

### 服务运行状态
- **WebAPI进程**: 运行中 (多个实例)
- **数据库**: SQL Server运行正常
- **健康检查**: Healthy状态

### 当前问题基线
**整体通过率**: 22.2% (2/9 测试用例通过)  
**P0级问题**: 5个模块JWT认证失败 (401错误)  
**P1级问题**: 2个模块路由配置缺失 (404错误)  
**正常模块**: Auth模块 (100%通过率)  

### 具体问题清单

#### JWT认证失败 (P0)
- Users: `GET /api/v1/users` → 401
- Patients: `GET /api/v1/patients` → 401  
- Herbs: `GET /api/v1/herbs` → 401
- Formula: `GET /api/v1/formulas` → 401
- Prescriptions: `GET /api/v1/prescriptions` → 401

#### 路由配置缺失 (P1)
- Consultation: `GET /api/v1/consultations` → 404
- MedicalCase: `GET /api/v1/medicalcases` → 404

#### 正常工作
- Auth: `POST /api/v1/auth/login` → 200 (JWT令牌生成成功)
- Auth: `GET /api/v1/auth` → 405 (正确拒绝GET方法)

## 环境配置

### JWT配置状态
**令牌生成**: ✅ 正常 (Auth模块可以生成JWT)  
**令牌验证**: ❌ 失败 (其他模块无法验证JWT)  
**令牌格式**: Bearer Token正确  
**令牌内容**: 包含用户信息、角色、过期时间等  

### 预期修复目标
1. **P0修复**: 5个模块恢复200状态码 (JWT认证正常)
2. **P1修复**: 2个模块恢复200状态码 (路由配置正常)  
3. **整体目标**: 通过率从22.2%提升至100%

## 测试数据来源
- **详细日志**: `_reports/2025-09/backend/acceptance-full/logs.jsonl`
- **失败分析**: `_reports/2025-09/backend/acceptance-full/failures.csv`
- **总体报告**: `_reports/2025-09/backend/acceptance-full/overview.md`

## 下一步计划
1. 检查JWT中间件配置和注册顺序
2. 修复控制器路由标注和服务注册
3. 执行最小冒烟验证
4. 确认问题全部解决

## 回滚准备
**回滚点**: release/backend-acceptance-smoke-full分支  
**回滚策略**: 每个修复步骤独立提交，可按需git revert  
**风险控制**: 不更改数据库结构和API契约，仅修复配置