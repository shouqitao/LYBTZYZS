# DT-003 模块间依赖关系分析报告

## 分析目标

识别和梳理8个核心业务模块间的依赖关系，建立清晰的依赖层次，防止循环依赖。

## 模块清单

### 核心认证与用户模块
1. **Auth** - 身份认证和授权
2. **Users** - 用户管理

### 核心诊疗流程模块  
3. **Patients** - 患者档案
4. **MedicalCase** - 医疗案例
5. **Consultation** - 看诊诊断
6. **Prescriptions** - 处方管理

### 药材与验方模块
7. **Herbs** - 中药材管理
8. **Formula** - 验方管理

## 理想依赖层次结构

```
Layer 1: 基础模块 (无依赖)
├── Auth        # 认证基础
├── Users       # 用户基础  
├── Herbs       # 药材基础
└── Formula     # 验方基础

Layer 2: 业务基础模块 (依赖Layer 1)
└── Patients    # 依赖 Users (创建者信息)

Layer 3: 诊疗核心模块 (依赖Layer 1-2)
├── MedicalCase # 依赖 Patients, Users
└── Consultation # 依赖 Patients, Users

Layer 4: 处方模块 (依赖Layer 1-3)  
└── Prescriptions # 依赖 Herbs, Formula, MedicalCase, Consultation
```

## 依赖分析方法

1. **静态分析**: 检查using语句和接口依赖
2. **服务注册分析**: 检查IoC容器注册顺序
3. **运行时依赖**: 检查服务调用关系
4. **循环依赖检测**: 识别潜在的相互依赖

## 分析结果

### 当前依赖状态

**需要验证的依赖关系**:
- Auth → Users 关系
- Patients → Users 关系  
- MedicalCase → Consultation 关系
- Prescriptions → 其他模块关系

### 潜在问题点

1. **模块间相互引用**: 检查是否存在A→B同时B→A的情况
2. **服务注册顺序**: 确保被依赖的服务先注册
3. **接口依赖**: 验证接口依赖是否合理

## 修复方案

### Phase 1: 依赖分析
- 静态代码分析各模块using语句
- 检查服务注册顺序
- 识别潜在循环依赖

### Phase 2: 依赖梳理
- 建立清晰的依赖层次图
- 优化服务注册顺序
- 解决循环依赖问题

### Phase 3: 验证
- 编译测试
- 运行时依赖验证
- 文档更新

## 预期成果

- 清晰的模块依赖层次结构
- 无循环依赖的干净架构
- 优化的服务注册顺序
- 完整的依赖关系文档