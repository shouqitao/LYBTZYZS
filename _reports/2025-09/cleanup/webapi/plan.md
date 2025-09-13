# LYBT.WebAPI 死代码清理计划

**执行时间**: 2025-09-12  
**目标项目**: LYBT.WebAPI  
**分析范围**: src/Server/Services/LYBT.WebAPI/ 及其子目录  
**护栏原则**: 保持所有/api/v1路由契约和数据库结构不变，仅清理确认未使用的内部代码  

## 🎯 分析总览

### 发现的问题

- **已标记Obsolete但未删除**: CompatibilityNotesController整个控制器已标记过时
- **Record-Only模式遗留**: 统计端点已标记过时但保留在代码中
- **配置冗余**: 部分服务注册和中间件配置可能存在冗余
- **清理价值**: 中等，WebAPI项目整体结构清晰但存在历史遗留

### 清理价值

- **API精简**: 移除已废弃的配伍检查功能相关路由
- **配置优化**: 清理过时的服务注册和中间件配置
- **架构清晰**: 移除Record-Only模式下已废弃的功能端点

## 📋 死代码候选清单

### 阶段1: 确证未用项 (可安全删除)

#### 1.1 已标记Obsolete的控制器

| 文件路径 | 控制器名 | 标记状态 | 风险评估 | 操作 |
|---------|---------|---------|---------|------|
| Controllers/Prescriptions/CompatibilityNotesController.cs | CompatibilityNotesController | [Obsolete("Compatibility checking feature removed in Record-Only mode")] | 低风险 | 删除 |

**删除证据**:
- ✅ 整个控制器已标记为Obsolete，配伍检查功能已在Record-Only模式下移除
- ✅ 路由 `/api/v1/prescriptions/{prescriptionId}/compat-notes` 属于已废弃功能
- ✅ 前端应该已不再调用此API

#### 1.2 已标记Obsolete的Action方法

| 控制器 | Action方法 | 路由 | 标记状态 | 操作 |
|-------|-----------|------|---------|------|
| ConsultationController | GetStatistics | `/api/v1/consultations/statistics` | [Obsolete("Statistics endpoint removed in Record-Only mode")] | 删除 |
| MedicalCaseController | GetStatistics | `/api/v1/medicalcases/statistics` | [Obsolete("Statistics endpoint removed in Record-Only mode")] | 删除 |

**删除证据**:
- ✅ 统计功能已在Record-Only模式下移除
- ✅ 这些端点不符合简化的业务需求

### 阶段2: 可疑项 (需要进一步验证)

#### 2.1 服务注册检查

需要验证以下服务是否仍在使用：
- ICompatibilityNoteService注册 (如果CompatibilityNotesController被删除)
- 与配伍检查相关的其他服务注册

#### 2.2 中间件和过滤器

检查是否有专门为已废弃功能服务的中间件：
- 配伍检查相关的验证过滤器
- 统计功能相关的缓存中间件

## 🛡️ 保护清单 (不删除)

### 核心API控制器 (完全保护)

**业务核心控制器**:
- ✅ **AuthController** - 认证和授权，核心安全功能
- ✅ **UsersController** - 用户管理核心API
- ✅ **PatientsController** - 患者档案管理API
- ✅ **MedicalCaseController** - 医疗案例管理API (保留非统计功能)
- ✅ **ConsultationController** - 看诊记录API (保留非统计功能)
- ✅ **PrescriptionsController** - 处方管理核心API
- ✅ **HerbsController** - 中药材管理API
- ✅ **FormulasController** - 验方管理API
- ✅ **HerbImportExportController** - 数据导入导出功能

### 基础设施组件 (绝对保护)

**统一架构组件**:
- ✅ **BaseApiController** - 统一API响应封装
- ✅ **GlobalExceptionHandler/Middleware** - 统一异常处理
- ✅ **SecurityHeadersMiddleware** - 安全头中间件
- ✅ **UnifiedServiceRegistration** - 统一服务注册系统
- ✅ **UnifiedMiddlewareConfiguration** - 统一中间件配置
- ✅ **UnifiedApplicationInitialization** - 统一应用初始化

**配置和扩展**:
- ✅ **AutoMapperConfiguration** - 对象映射配置
- ✅ **CacheExtensions** - 缓存扩展
- ✅ **CorsExtension** - 跨域配置

### 健康检查和监控 (完全保护)

- ✅ 所有健康检查端点
- ✅ 数据库状态显示功能
- ✅ 优雅关闭配置

## 📊 预期清理效果

### 代码量变化

- **删除控制器**: 1个完整控制器 (CompatibilityNotesController, ~197行)
- **删除Action**: 2个统计端点方法 (估计~50行)
- **清理配置**: 相关服务注册和路由配置 (估计~20行)
- **总计删除**: 约267行代码

### API端点变化

**删除的API端点**:
- `GET /api/v1/prescriptions/{prescriptionId}/compat-notes` - 获取配伍记录列表
- `GET /api/v1/prescriptions/{prescriptionId}/compat-notes/{noteId}` - 获取配伍记录详情
- `POST /api/v1/prescriptions/{prescriptionId}/compat-notes` - 创建配伍记录
- `PUT /api/v1/prescriptions/{prescriptionId}/compat-notes/{noteId}` - 更新配伍记录
- `DELETE /api/v1/prescriptions/{prescriptionId}/compat-notes/{noteId}` - 删除配伍记录
- `GET /api/v1/consultations/statistics` - 看诊统计
- `GET /api/v1/medicalcases/statistics` - 医疗案例统计

### 架构清晰度提升

**清理前**:
```
核心业务API + 已废弃配伍检查API + 已废弃统计API
↓ 开发者困惑：这些标记为Obsolete的API是否还需要维护？
```

**清理后**:
```
专注核心业务API：认证+用户+患者+医疗+处方+药材+验方
↓ 开发者清晰：API功能明确，无历史包袱
```

## 🚦 执行策略

### 清理顺序

1. **第一批**: 删除已标记Obsolete的完整控制器 (CompatibilityNotesController)
2. **第二批**: 删除已标记Obsolete的Action方法 (统计端点)
3. **第三批**: 清理相关的服务注册和配置
4. **第四批**: 清理未使用的using语句和配置项

### 验证策略

- 每次提交后立即运行: `dotnet format`, `dotnet build`, `dotnet test`
- 确保所有/api/v1核心路由仍然可用
- 验证Swagger文档生成正常
- 确保健康检查端点正常工作

### 回滚策略

- 如发现任何核心API受影响，立即使用 `git revert` 回滚
- 如发现前端仍在调用已删除的API，恢复并标记为[Obsolete]保留
- 将回滚项记录在notes.md的"暂缓清理列表"

## 📋 详细删除清单

### CompatibilityNotesController (完整删除)

**文件**: `Controllers/Prescriptions/CompatibilityNotesController.cs`
**原因**: 整个控制器已标记为Obsolete，配伍检查功能已移除
**影响**: 删除7个API端点，所有与配伍记录管理相关的功能

### 统计端点 (Action级删除)

**ConsultationController.GetStatistics**:
- 路由: `GET /api/v1/consultations/statistics`
- 标记: `[Obsolete("Statistics endpoint removed in Record-Only mode")]`

**MedicalCaseController.GetStatistics**:
- 路由: `GET /api/v1/medicalcases/statistics`  
- 标记: `[Obsolete("Statistics endpoint removed in Record-Only mode")]`

## 🔍 依赖清理检查

### 需要检查的服务注册

1. **ICompatibilityNoteService** - 如果CompatibilityNotesController被删除，此服务注册也应移除
2. **CompatibilityNoteService** - 具体实现类的注册
3. **配伍相关的DTO和映射配置**

### 需要检查的路由配置

1. 检查是否有硬编码的配伍API路由配置
2. 检查Swagger分组中是否有配伍相关的文档配置
3. 检查是否有配伍相关的权限配置

---

**清理计划制定完成** | **预计清理效果**: ~267行代码减少 | **风险等级**: 低风险  
**下一步**: 按阶段执行清理，确保每步都能构建和测试通过