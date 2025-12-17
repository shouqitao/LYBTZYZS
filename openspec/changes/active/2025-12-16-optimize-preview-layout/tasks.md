# Tasks: optimize-preview-layout

## Phase 1: 基础样式准备

### Task 1.1: 创建PreviewStyles.xaml
- [x] 在`LYBT.Desktop.Infrastructure/Themes/`创建`PreviewStyles.xaml`
- [x] 定义`PreviewTitleStyle` - 预览标题样式
- [x] 定义`PreviewFieldRowStyle` - 字段行容器样式
- [x] 定义`PreviewSectionHeaderStyle` - 分组标题样式
- [x] 定义状态Badge样式变体(Success/Warning/Info/Default/Primary)
- [x] 将样式合并到UnifiedComponents.xaml

**验证**: 样式资源可在各模块正确引用 - PASSED

---

## Phase 2: 简单模块优化

### Task 2.1: 优化UserViewControl
- [x] 重构布局结构:
  - 顶部: 用户名(标题) + 真实姓名(副标题) + 状态Badge
  - 主体: 2列布局(角色、手机 | 邮箱全宽)
- [x] 应用新样式
- [x] 验证数据绑定正确

**验证**: 界面显示正常，信息层次清晰 - PASSED

### Task 2.2: 优化PatientViewControl
- [x] 重构布局结构:
  - 顶部: 患者姓名(标题) + 拼音码(副标题) + 状态Badge
  - 分组1-基本信息: 性别、出生日期、年龄、身份证
  - 分组2-联系方式: 手机号码、地址(全宽)
- [x] 应用新样式
- [x] 验证数据绑定正确

**验证**: 界面显示正常，分组合理 - PASSED

---

## Phase 3: 中等复杂度模块

### Task 3.1: 优化HerbViewControl
- [x] 重构布局结构:
  - 顶部: 药材名称(标题) + 拼音码(副标题) + 状态Badge
  - 分组1-基本信息: 产地、规格、单位
  - 分组2-价格信息: 零售价、成本价
  - 分组3-功效用法: 功效(全宽)、用法用量(全宽)、备注(全宽)
- [x] 应用新样式
- [x] 空值自动隐藏

**验证**: 价格信息突出，长文本展示完整 - PASSED

### Task 3.2: 优化FormulaViewControl
- [x] 重构布局结构:
  - 顶部: 验方名称(标题) + 分类Badge
  - 分组1-中医属性: 性味归经(全宽)、功效(全宽)
  - 分组2-用法说明: 用法(全宽)、备注(全宽)
  - 分组3-药材组成: HerbListEditor + 药材数量Badge
  - 底部: 创建/更新时间(可选显示)
- [x] 应用新样式
- [x] 优化药材数量显示

**验证**: 中医属性内容完整展示，药材列表正常 - PASSED

---

## Phase 4: 复杂模块优化

### Task 4.1: 优化MedicalCaseViewControl
- [x] 重构头部区域:
  - 医疗案例(标题) + 病历号(副标题) + 状态Badge组(已诊疗/已开方)
  - 分组-基本信息: 患者、医生、创建时间、状态
- [x] 重构诊疗信息区域:
  - 使用Expander保持可折叠
  - 内部使用统一PreviewFieldRowStyle
  - 字段: 主诉→现病史→既往史→诊断→治疗方案→诊疗时间
- [x] 重构处方信息区域:
  - 基本信息: 处方编号、配方来源、剂数、总价 (2列布局)
  - 用法用量(全宽)
  - 处方明细DataGrid
- [x] 简化底部更新时间显示

**验证**: 信息层次清晰，展开/折叠正常，数据完整 - PASSED

---

## Phase 5: 收尾验证

### Task 5.1: 统一性检查
- [x] 检查所有预览界面样式一致性
- [x] 验证字体大小、间距符合规范
- [x] 确认颜色使用统一资源

### Task 5.2: 回归测试
- [x] 编译测试通过 (0错误 0警告)
- [x] 所有ViewControl使用统一样式系统

---

## Dependencies

```
Task 1.1 (基础样式)
    ├── Task 2.1 (User)
    ├── Task 2.2 (Patient)
    ├── Task 3.1 (Herb)
    ├── Task 3.2 (Formula)
    └── Task 4.1 (MedicalCase)
            │
            └── Task 5.1, 5.2 (验证)
```

## Effort Estimation

| Task | 复杂度 | 预估 | 实际 |
|------|--------|------|------|
| 1.1 | 低 | 0.5h | DONE |
| 2.1 | 低 | 0.5h | DONE |
| 2.2 | 低 | 0.5h | DONE |
| 3.1 | 中 | 1h | DONE |
| 3.2 | 中 | 1h | DONE |
| 4.1 | 高 | 2h | DONE |
| 5.1-5.2 | 低 | 0.5h | DONE |
| **总计** | - | **6h** | **ALL COMPLETED** |
