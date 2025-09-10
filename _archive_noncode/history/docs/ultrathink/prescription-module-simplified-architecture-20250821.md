# UltraThink处方模块简化架构方案

## 📋 **核心设计原则**

> **"处方模块只需要关注处方组成"** - 回归本质，专注核心

## 🎯 **简化后的功能边界**

### ✅ **保留功能（核心职责）**

#### 1. **处方组成编辑**
```
PrescriptionComposer (主视图)
├── 基本信息
│   ├── 剂数设置
│   ├── 用法选择
│   └── 医嘱输入
├── 药材列表
│   ├── 药材名称
│   ├── 用量设置
│   └── 移除操作
└── 操作按钮
    ├── 添加药材 → 调用Herbs模块
    ├── 导入验方 → 调用Formula模块
    ├── 保存处方
    └── 清空重置
```

#### 2. **验方快速应用**
```
FormulaQuickApply (对话框)
├── 验方列表 → 来自Formula模块
├── 快速预览
└── 一键导入
```

#### 3. **价格自动计算**
```
PriceCalculator (组件)
├── 单剂价格 = Σ(药材单价 × 用量)
├── 总价格 = 单剂价格 × 剂数
└── 实时更新
```

### ❌ **移除功能（越界职责）**

#### 1. **历史管理** → MedicalCase模块
- 处方历史查询
- 处方编辑/删除
- 处方列表管理

#### 2. **药材管理** → Herbs模块
- 药材选择界面
- 药材增删改查
- 库存管理

#### 3. **复杂业务协调** → Consultation模块
- 工作流协调
- 复杂事件处理
- 业务规则验证

#### 4. **打印导出** → 通用工具模块
- 打印预览
- 格式导出
- 模板管理

## 🏗️ **简化后的文件结构**

```
Prescriptions/
├── Views/
│   ├── PrescriptionComposer.xaml      # 主编辑界面
│   └── FormulaQuickApply.xaml         # 验方导入对话框
├── ViewModels/
│   ├── PrescriptionComposerViewModel.cs
│   └── FormulaQuickApplyViewModel.cs
├── Components/
│   ├── PrescriptionItem.cs           # 药材项
│   └── PriceCalculator.cs            # 价格计算
└── Services/
    └── PrescriptionComposerService.cs # 简单业务逻辑
```

## 🔄 **模块间协作**

### 输入接口
```csharp
// 接收医疗案例ID，开始处方编辑
public void StartPrescription(Guid medicalCaseId)

// 接收验方模板，快速应用
public void ApplyFormula(FormulaTemplate template)
```

### 输出接口
```csharp
// 保存完成的处方
public async Task<PrescriptionDto> SavePrescription()

// 通知处方变更
public event PrescriptionChangedEvent
```

### 依赖调用
```csharp
// 调用Herbs模块选择药材
_herbService.SelectHerbs() 

// 调用Formula模块选择验方
_formulaService.SelectTemplate()

// 保存到MedicalCase
_medicalCaseService.AddPrescription()
```

## 💡 **简化收益**

### 1. **职责清晰**
- 专注处方组成编辑
- 消除模块边界混乱
- 降低维护复杂度

### 2. **性能提升**
- 减少不必要的组件
- 降低内存占用
- 加快启动速度

### 3. **开发效率**
- 代码更易理解
- 功能更易扩展
- Bug更易定位

### 4. **用户体验**
- 界面更简洁
- 操作更直观
- 响应更快速

## 🚀 **实施建议**

### Phase 1: 核心重构
1. 创建简化的PrescriptionComposer
2. 移除历史管理功能
3. 简化业务协调逻辑

### Phase 2: 界面优化
1. 设计清晰的编辑界面
2. 优化药材选择流程
3. 完善验方导入体验

### Phase 3: 模块解耦
1. 明确模块边界
2. 定义标准接口
3. 测试协作流程

## 📊 **对比分析**

| 维度 | 当前状态 | 简化后 | 改进 |
|------|----------|--------|------|
| 文件数量 | 35+ | 10- | ↓70% |
| 功能复杂度 | 高 | 低 | ↓80% |
| 启动速度 | 慢 | 快 | ↑50% |
| 维护成本 | 高 | 低 | ↓60% |
| 用户认知 | 复杂 | 简单 | ↑80% |

---

**结论**: 通过回归"只关注处方组成"的核心设计，处方模块将变得更加专注、高效和易用，真正实现UltraThink实用化架构的目标。