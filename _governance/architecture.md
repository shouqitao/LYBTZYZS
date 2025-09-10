# 凌隐宝堂中医诊所系统 - 架构治理规范

**版本**: 1.0  
**更新时间**: 2025-09-10  
**治理级别**: 强制执行

## 🏗️ 架构模式定义

### 混合架构设计

本系统采用**混合架构设计**，前后端使用不同的架构模式以适应各自的技术特点：

```
前端WPF客户端: UltraThink双层架构
后端Web API: 传统三层架构
```

## 📐 四层边界定义

### 前端UltraThink双层架构

```
Layer 1: Desktop.ViewModels (表现层)
├── 职责: UI绑定、用户交互、数据展示
├── 依赖: Desktop.Core、Shared.Interfaces
└── 禁止: 直接访问Infrastructure、Entities

Layer 2: Desktop.Modules (业务模块层)  
├── 职责: 业务逻辑、服务调用、数据处理
├── 依赖: Shared.Models、Shared.Interfaces
└── 禁止: 直接访问Infrastructure、Database

Layer 3: Desktop.Core (核心服务层)
├── 职责: 公共服务、基础设施抽象
├── 依赖: Shared.Utilities、第三方库
└── 禁止: 业务逻辑耦合

Shared: 共享层
├── 职责: 跨层共享的模型、接口、工具
├── 依赖: 仅依赖.NET BCL和基础库
└── 禁止: 特定层的实现细节
```

### 后端传统三层架构

```
Layer 1: WebAPI.Controllers (控制器层)
├── 职责: HTTP请求处理、参数验证、响应格式化  
├── 依赖: Modules.Services、Shared.Models
└── 禁止: 直接访问Infrastructure、数据库操作

Layer 2: Modules.Services (业务服务层)
├── 职责: 业务逻辑、事务管理、数据验证
├── 依赖: Infrastructure、Entities、Shared.Models
└── 禁止: HTTP相关代码、UI相关逻辑

Layer 3: Infrastructure (基础设施层)
├── 职责: 数据访问、外部服务集成、技术实现
├── 依赖: Entities、第三方库、数据库
└── 禁止: 业务逻辑、UI相关代码

Layer 4: Entities (实体层)
├── 职责: 数据模型定义、实体关系
├── 依赖: 仅依赖.NET BCL和EF Core注解
└── 禁止: 业务逻辑、基础设施代码
```

## 🚫 层间依赖禁止规则

### 严格禁止的依赖关系

```
❌ UI层 ↛ Infrastructure层
❌ UI层 ↛ Entities层  
❌ Controllers ↛ Infrastructure实现类
❌ ViewModels ↛ 数据库上下文
❌ 任何层 ↛ 具体实现（必须通过接口）
```

### 允许的依赖关系

```
✅ 上层 → 下层（通过接口）
✅ 任何层 → Shared层
✅ 服务层 → Infrastructure接口
✅ Infrastructure → Entities
✅ 控制器 → 服务接口
```

## 🎯 业务模块红线定义

### Record-Only系统原则

**核心原则**: 系统8个业务模块**仅用于数据记录和历史查询**，禁止复杂业务逻辑。

### 8个核心业务模块

| 模块 | 职责范围 | 允许操作 | 禁止功能 |
|------|---------|----------|----------|
| **Auth** | 身份认证记录 | 登录、登出、会话管理 | AI身份识别、复杂权限计算 |
| **Users** | 用户信息记录 | 用户CRUD、角色分配 | 用户行为分析、智能推荐 |
| **Patients** | 患者档案记录 | 患者信息管理、历史查询 | 健康预测、智能诊断 |
| **MedicalCase** | 医疗案例记录 | 案例创建、状态更新、查询 | 案例智能分析、自动分类 |
| **Consultation** | 看诊记录 | 四诊数据记录、历史回顾 | AI辅助诊断、症状智能识别 |
| **Prescriptions** | 处方记录 | 处方开具、价格计算、打印 | 智能配伍、自动推荐 |
| **Herbs** | 中药材记录 | 药材信息管理、库存记录 | 智能采购、价格预测 |
| **Formula** | 验方记录 | 验方模板管理、历史查询 | 智能组方、疗效分析 |

### 允许的操作类型

```
✅ Create: 创建新记录
✅ Read: 查询和读取数据  
✅ Update: 更新现有记录
✅ Delete: 删除记录
✅ Search: 搜索和筛选
✅ Export: 数据导出
✅ Import: 数据导入
✅ Calculate: 简单计算（价格、数量等）
✅ Validate: 基础数据验证
```

### 严格禁止的功能

```
❌ 智能推荐系统
❌ AI诊断辅助  
❌ 自动配伍检查（超出基础安全验证）
❌ 复杂工作流引擎
❌ 事件驱动架构
❌ 实时计算和分析
❌ 预测性分析
❌ 机器学习集成
❌ 复杂业务规则引擎
❌ 自动化流程管理
```

## 🔒 技术约束

### API设计约束

```json
{
  "路由前缀": "/api/v1",
  "禁止路由": ["/api/v2", "/api/v3", "/v2/", "/v3/"],
  "响应格式": "ApiResponse<T>",
  "错误处理": "统一异常处理",
  "认证方式": "JWT Bearer Token"
}
```

### 框架使用约束

```json
{
  "禁止引入": [
    "rule-engine", "workflow", "event-bus", 
    "transaction-pipeline", "saga-pattern", "cqrs",
    "hangfire", "quartz", "mediatr"
  ],
  "必须使用": [
    ".NET 8", "EF Core 8", "ASP.NET Core", 
    "WPF", "Prism.DryIoc", "AutoMapper"
  ]
}
```

### 命名约束

```json
{
  "禁止类名包含": [
    "Pipeline", "Workflow", "Bus", "Engine", "Saga",
    "EventHandler", "CommandHandler", "QueryHandler"
  ],
  "禁止命名空间": [
    "*.Workflows.*", "*.Pipelines.*", "*.Events.*", "*.Commands.*"
  ]
}
```

## 📊 质量门禁

### 编译质量要求

```
✅ 编译错误: 0个
✅ 编译警告: 0个  
✅ 代码格式: 通过dotnet format
✅ 单元测试: 覆盖率≥60%
```

### 架构测试要求

```
✅ 层间依赖检查
✅ API路由前缀检查
✅ 禁止命名检查
✅ 模块边界检查
✅ 简单验证检查
```

## 🚨 违规检测

### 自动检测规则

1. **NetArchTest**: 架构层间依赖检查
2. **路由扫描**: API路径合规性检查  
3. **命名扫描**: 类名和命名空间检查
4. **引用分析**: 禁止框架使用检查

### 违规处理流程

```
1. 自动检测 → CI失败
2. 人工review → 拒绝合并
3. 架构委员会 → 规则调整（极少数情况）
```

## 📋 例外处理

### 允许的例外情况

1. **遗留代码**: 现有功能保持不变
2. **基础设施**: 框架级别的Pipeline（如ASP.NET Core中间件）
3. **第三方集成**: 外部系统必需的复杂逻辑

### 例外申请流程

1. 提交例外申请（包含技术方案和风险评估）
2. 架构师审批
3. 记录到架构决策日志
4. 定期review例外必要性

## 📚 相关文档

- [.ai/rules.json](../.ai/rules.json) - 机器可读规则定义
- [CLAUDE.md](../CLAUDE.md) - 开发指南和约定
- [docs/requirements/](../docs/requirements/) - 需求文档
- [docs/architecture/](../docs/architecture/) - 架构设计文档

---

**最后更新**: 2025-09-10  
**治理状态**: 活跃强制执行  
**下次review**: 2025-12-10