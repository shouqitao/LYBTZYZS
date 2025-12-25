# UI层数据验证框架

## Why

当前系统的数据验证存在以下问题：

1. **验证时机过晚** - 验证主要在服务端FluentValidation执行，用户填写表单后提交才能看到错误
2. **用户体验差** - 必填字段没有前端即时验证，用户不知道哪些是必填项
3. **验证规则分散** - Entity有DataAnnotation、DTO有DataAnnotation、Server有FluentValidation，但前端缺乏统一验证
4. **重复定义** - 相同的验证规则在多处定义，容易不一致

## What Changes

### 目标

建立从UI层开始的完整数据验证机制：

```
用户输入 → UI即时验证 → ViewModel验证 → DTO验证 → Server验证
    ↑           ↑              ↑            ↑           ↑
    │      实时反馈        属性变更时      提交前       最终保障
    └──────────────────────────────────────────────────────────┘
                        统一验证规则定义
```

### 核心原则

1. **即时反馈** - 用户输入时立即显示验证结果
2. **必填标识** - UI明确标识必填字段（红色星号）
3. **规则统一** - 验证规则从DTO/Entity的DataAnnotation自动同步到前端
4. **分层验证** - 前端验证不替代后端验证，但提供即时用户体验

### 影响范围

- 所有DetailModel（UserDetailModel, HerbDetailModel, PatientDetailModel等）
- 所有EditControl XAML（UserEditControl, HerbEditControl, PatientEditControl等）
- 验证规则定义（ValidationConstants, DataAnnotation同步）
- ViewModel基类（MasterDetailViewModelBase验证集成）

## Affected Specs

- `viewmodel-conventions` - 添加UI验证规范
- `dto-architecture` - 验证规则同步规范（新增）

## Status

- [x] Proposal created
- [x] Design approved
- [ ] Implementation started
- [ ] Implementation completed
- [ ] Archived
