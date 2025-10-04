# 模块设计文档

本目录包含项目中所有模块的详细技术设计文档。

## 📁 文档结构

### 🖥️ Server端模块
- [Auth模块](./server/auth-module.md) - 认证与授权
- [Users模块](./server/users-module.md) - 用户管理
- [Patients模块](./server/patients-module.md) - 患者管理
- [Herbs模块](./server/herbs-module.md) - 中药管理
- [Consultation模块](./server/consultation-module.md) - 诊疗管理
- [Formula模块](./server/formula-module.md) - 方剂管理
- [Prescriptions模块](./server/prescriptions-module.md) - 处方管理
- [MedicalCase模块](./server/medicalcase-module.md) - 病历管理

### 💻 Client端模块
- [Auth模块](./client/auth-module.md) - 客户端认证
- [Users模块](./client/users-module.md) - 用户界面
- [Patients模块](./client/patients-module.md) - 患者管理界面
- [Herbs模块](./client/herbs-module.md) - 中药管理界面
- [Consultation模块](./client/consultation-module.md) - 诊疗工作台
- [Formula模块](./client/formula-module.md) - 方剂管理界面
- [Prescriptions模块](./client/prescriptions-module.md) - 处方管理界面
- [MedicalCase模块](./client/medicalcase-module.md) - 病历管理界面

### 🔗 Shared层
- [共享接口](./shared/interfaces.md) - 服务接口定义
- [共享模型](./shared/models.md) - DTO和契约模型
- [共享工具](./shared/utilities.md) - 通用工具类

### 🗺️ 架构图
- [模块依赖关系](./dependencies.md) - 模块间依赖关系图
- [数据流图](./dataflow.md) - 数据在模块间的流转

## 📋 模块设计原则

### 🎯 单一职责
每个模块专注于一个业务领域，职责边界清晰。

### 🔌 松耦合
模块间通过接口通信，降低直接依赖。

### 🏗️ 分层架构
- **Controller层**：HTTP请求处理
- **Service层**：业务逻辑处理
- **Repository层**：数据访问
- **DTO层**：数据传输对象

### 🔄 统一模式
所有模块遵循相同的架构模式和命名约定。

## 🚀 快速导航

| 业务域 | Server模块 | Client模块 | 主要功能 |
|-------|-----------|-----------|----------|
| 用户管理 | Users | Users | 用户CRUD、角色管理 |
| 患者管理 | Patients | Patients | 患者档案、病历管理 |
| 中药管理 | Herbs | Herbs | 药材库存、分类管理 |
| 诊疗管理 | Consultation | Consultation | 诊疗记录、诊断管理 |
| 方剂管理 | Formula | Formula | 方剂配方、组方管理 |
| 处方管理 | Prescriptions | Prescriptions | 处方开具、审核 |
| 病历管理 | MedicalCase | MedicalCase | 病历档案、历史记录 |
| 认证授权 | Auth | Auth | 登录、权限控制 |