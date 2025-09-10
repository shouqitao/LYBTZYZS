# Prescriptions模块API路由扫描报告 (api-scan.md)

**分析目标**: 扫描当前/api路由，保留/api/v1/prescriptions*，标出需要移走的无关端点
**API标准**: 固定在/api/v1/prescriptions路由，移除/api/v2或与处方无关的端点

## 🔍 API路由全量扫描

### 1. 系统整体API结构
```
📊 API版本分布: 
- /api/v1/*: 主要API版本 (标准)
- /api/v2/*: 未发现 ✅
- 非版本化API: 仅health检查
```

#### 核心模块API清单
```
✅ 发现的API模块 (8个):
```

1. **Auth** - `/api/v1/auth/*` (认证授权)
2. **Users** - `/api/v1/users/*` (用户管理)  
3. **Patients** - `/api/v1/patients/*` (患者档案)
4. **Herbs** - `/api/v1/herbs/*` (中药材管理)
5. **Formulas** - `/api/v1/formulas/*` (验方管理)
6. **Consultation** - `/api/v1/consultation/*` (看诊诊断)
7. **MedicalCase** - `/api/v1/medicalcase/*` (医疗案例)
8. **🎯 Prescriptions** - `/api/v1/prescriptions/*` (处方管理 - 目标模块)

### 2. Prescriptions模块API详细分析

#### 📍 当前处方API端点 (IPrescriptionApi.cs)
```
🎯 路由: /api/v1/prescriptions
状态: ✅ 符合标准，保留
```

##### 核心CRUD端点 (保留)
```csharp
// 基础CRUD - 符合最小职责
[Get("/api/v1/prescriptions")]          // 获取处方列表
[Get("/api/v1/prescriptions/{id}")]     // 获取处方详情  
[Post("/api/v1/prescriptions")]         // 创建处方
[Put("/api/v1/prescriptions/{id}")]     // 更新处方
[Delete("/api/v1/prescriptions/{id}")]  // 删除处方
[Post("/api/v1/prescriptions/void/{id}")] // 作废处方
```

**评估结果**: ✅ 6个端点全部保留，符合标准RESTful设计

#### 📍 配伍检查API端点 (CompatibilityNotesController.cs)
```
🎯 路由: /api/v1/prescriptions/{prescriptionId}/compat-notes
状态: ✅ 子资源路由，符合处方模块范围
```

##### 配伍相关端点 (简化保留)
```csharp
// 基础配伍检查 - 简化后保留
[Route("api/v1/prescriptions/{prescriptionId}/compat-notes")]
```

**评估结果**: ✅ 保留，但需简化为基础18反19畏检查

### 3. 无关API端点识别

#### ❌ 与处方模块无关的API (其他模块，无需处理)
```
📂 其他业务模块API (不在本次分析范围):
```

- **Auth API** (`/api/v1/auth/*`) - 8个端点
  - 登录、注销、用户信息、令牌刷新、密码修改等
  - **处理**: 🔵 无关模块，无需处理

- **Users API** (`/api/v1/users/*`) - 10个端点  
  - 用户CRUD、状态管理、密码重置等
  - **处理**: 🔵 无关模块，无需处理

- **Patients API** (`/api/v1/patients/*`) - 9个端点
  - 患者档案管理、处方历史查询等
  - **处理**: 🔵 无关模块，但存在跨模块调用

- **Herbs API** (`/api/v1/herbs/*`) - 10个端点
  - 中药材管理、库存、导入导出等  
  - **处理**: 🔵 无关模块，但处方创建需要调用

- **Formulas API** (`/api/v1/formulas/*`) - 18个端点
  - 验方模板管理、复制、导入导出等
  - **处理**: 🔵 无关模块，但处方可能引用验方

- **Consultation API** (`/api/v1/consultation/*`) - 17个端点
  - 看诊流程、诊断记录、统计分析等
  - **处理**: 🔵 无关模块，但与处方有业务关联

- **MedicalCase API** (`/api/v1/medicalcase/*`) - 13个端点  
  - 医疗案例管理、状态流转、统计报表等
  - **处理**: 🔵 无关模块，但处方归属于医疗案例

#### ✅ 未发现需要移除的端点
```
🔍 扫描结果: 
- 无/api/v2版本端点
- 无处方模块外的冗余端点
- 无非标准化路由
```

## 🎯 处方模块API收敛建议

### 保留的API端点 (符合最小职责)
```
✅ KEEP - 核心处方CRUD (6个端点)
```

#### 标准RESTful端点
```http
GET    /api/v1/prescriptions                    # 获取处方列表(分页+搜索)
GET    /api/v1/prescriptions/{id}               # 获取处方详情
POST   /api/v1/prescriptions                    # 创建新处方
PUT    /api/v1/prescriptions/{id}               # 更新处方
DELETE /api/v1/prescriptions/{id}               # 删除处方
POST   /api/v1/prescriptions/void/{id}          # 作废处方(软删除)
```

#### 基础配伍检查端点  
```http
GET    /api/v1/prescriptions/{id}/compat-notes  # 获取配伍检查记录
POST   /api/v1/prescriptions/check-compatibility # 基础18反19畏检查
```

**总计**: 8个API端点，符合处方管理最小职责

### 需要简化的API实现
```
⚠️ SIMPLIFY - 保留端点，简化内部实现
```

#### 1. 处方列表查询简化
```http
GET /api/v1/prescriptions?page=1&size=20&patientId={id}&status={status}
```
**简化措施**:
- 移除复杂过滤条件 (智能搜索、AI推荐等)
- 保留基础过滤 (患者ID、状态、日期范围)
- 移除性能统计和分析数据

#### 2. 处方创建简化
```http
POST /api/v1/prescriptions
```
**简化措施**:
- 移除复杂事务处理 (CreatePrescriptionTransaction)
- 保留基础验证 (数据完整性、基础配伍检查)
- 移除智能推荐和自动优化

#### 3. 配伍检查简化
```http
POST /api/v1/prescriptions/check-compatibility
```
**简化措施**:
- 移除高级配伍分析 (评分系统、动态规则)
- 保留基础18反19畏检查
- 移除外部数据源同步

## 📊 API端点统计

### 当前状态
```
🔢 处方模块API统计:
- 标准端点: 6个 (IPrescriptionApi)
- 配伍端点: 2个 (CompatibilityNotesController)
- 路由版本: 仅v1 ✅
- 路由规范: 全部符合 /api/v1/prescriptions/* ✅
```

### 收敛后状态  
```
🎯 收敛后API统计:
- 保留端点: 8个 (100%保留)
- 移除端点: 0个  
- 简化实现: 8个 (内部逻辑简化)
- API破坏性变更: 0个 ✅
```

## 🔍 跨模块API依赖扫描

### 处方模块对外部API的调用
```
📡 对外依赖 (需要保持):
```

1. **Herbs API** - 获取药材信息和价格
   - `GET /api/v1/herbs/{id}` - 药材详情
   - `GET /api/v1/herbs/available` - 可用药材列表

2. **Patients API** - 验证患者信息  
   - `GET /api/v1/patients/{id}` - 患者基础信息

3. **MedicalCase API** - 关联医疗案例
   - `GET /api/v1/medicalcase/{id}` - 医疗案例信息

### 外部模块对处方API的调用
```
📨 被调用情况 (需要保持兼容):
```

1. **Patients模块** - 查询患者处方历史
   - 调用: `GET /api/v1/prescriptions?patientId={id}`

2. **前端WPF客户端** - 处方管理界面
   - 调用: 全部8个处方API端点

## ✅ API收敛验证清单

### 路由标准检查
```
✅ 版本化路由: 全部使用 /api/v1/ 前缀
✅ 资源命名: 使用复数形式 prescriptions
✅ 子资源: 配伍检查正确嵌套在处方资源下
✅ HTTP动词: 符合RESTful标准 (GET/POST/PUT/DELETE)
❌ 无发现: /api/v2端点、非标准路由、重复端点
```

### 功能范围检查
```  
✅ 基础CRUD: 创建、读取、更新、删除全覆盖
✅ 业务操作: 作废处方、配伍检查等核心功能
✅ 查询支持: 分页、过滤、搜索等基础查询
❌ 移除超范围: 无智能推荐、复杂分析、报表统计等
```

### 兼容性检查
```
✅ 向下兼容: 所有现有端点保持不变
✅ 前端兼容: WPF客户端无需修改API调用
✅ 跨模块兼容: 其他模块调用的处方API保持稳定
❌ 破坏性变更: 无API签名变更、端点移除
```

## 🎯 总结

**API收敛成果**:
- ✅ **路由规范**: 100%符合 `/api/v1/prescriptions/*` 标准
- ✅ **功能聚焦**: 8个端点全部围绕处方核心职责  
- ✅ **向下兼容**: 0个破坏性变更，100%保持现有API契约
- ✅ **实现简化**: 内部逻辑大幅简化，API接口保持稳定

**执行建议**:
1. **Phase 1**: 保持所有API端点不变，确保外部兼容性
2. **Phase 2**: 简化端点内部实现，移除复杂事务和智能功能  
3. **Phase 3**: 清理内部代码，但API接口层面100%向下兼容

处方模块API设计良好，无需移除任何端点，仅需简化内部实现即可达到最小职责收敛目标。