# Proposal: optimize-preview-layout

## Summary

优化所有前端预览界面的布局设计，统一字段排列方式，提升视觉层次和信息可读性。

## Motivation

### 现状问题

1. **布局不统一**: 各模块预览控件使用不同的列数布局
   - Patient/User/Herb: 2列布局
   - MedicalCase: 4列布局  
   - Formula: 混合布局(2列+全宽)

2. **字段排列缺乏优先级**: 字段按代码顺序而非信息重要性排列

3. **视觉层次不清晰**: 
   - 标签和值的视觉对比不足
   - 相关字段未分组
   - 缺少视觉分隔

4. **信息密度不均**: 部分界面过于紧凑，部分过于松散

### 优化目标

参考EHR/Healthcare UI最佳实践:
- **清晰的视觉层次**: 关键信息优先展示
- **一致的布局模式**: 所有预览界面使用统一的网格系统
- **分组展示**: 相关字段分组，使用卡片或分隔线区分
- **适当的信息密度**: 平衡紧凑性和可读性

## Scope

### In Scope

1. 优化5个ViewControl预览组件的布局:
   - PatientViewControl
   - UserViewControl
   - HerbViewControl
   - FormulaViewControl
   - MedicalCaseViewControl

2. 创建统一的预览布局样式资源

3. 统一字段排列原则和分组规范

### Out of Scope

- 编辑模式界面(EditControl)
- 列表页DataGrid布局
- 对话框布局
- 新增字段或功能

## Design Overview

### 统一布局规范

#### 1. 网格系统
- **标准2列布局**: 用于简单实体(Patient, User, Herb)
- **响应式3列布局**: 用于复杂实体(MedicalCase基本信息)
- **全宽段落**: 用于长文本字段(备注、描述、病史等)

#### 2. 字段排列原则
- **标识性字段优先**: 名称、编号等
- **核心业务字段次之**: 关键属性
- **辅助信息靠后**: 状态、时间戳等
- **长文本单独分组**: 使用全宽展示

#### 3. 视觉分组
- 使用InfoCard分组相关字段
- 标题清晰表达分组含义
- 组内使用一致的间距

#### 4. 样式统一
- 创建`PreviewFieldStyle`统一标签-值对样式
- 创建`PreviewSectionStyle`统一分组卡片样式
- 创建`PreviewGridStyle`统一网格布局

### 各模块具体优化

#### PatientViewControl
当前: 2列5行
优化: 
- 分组: 基本信息(姓名、性别、年龄) | 联系方式(电话、地址) | 身份信息(身份证)
- 状态字段移至顶部右上角作为Badge

#### UserViewControl
当前: 2列3行
优化:
- 分组: 账户信息(用户名、角色) | 个人信息(姓名、联系方式)
- 状态Badge显示

#### HerbViewControl
当前: 2列4行 + 功效用法单列
优化:
- 分组: 基本信息(名称、拼音、产地) | 价格信息(零售价、成本价) | 功效用法
- 保持功效用法全宽展示

#### FormulaViewControl
当前: 混合布局
优化:
- 分组: 基本信息(名称、分类) | 中医属性(性味归经、功效) | 用法说明 | 药材组成
- 药材列表保持当前HerbListEditor展示

#### MedicalCaseViewControl
当前: 4列+Expander嵌套
优化:
- 简化头部: 病历号+状态Badge+时间
- 诊疗信息: 使用有序的Section展示四诊内容
- 处方信息: 保持DataGrid但优化列宽
- 移除不必要的Expander嵌套

## Risks and Mitigations

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 破坏现有布局 | 中 | 渐进式修改，每个控件独立验证 |
| 样式冲突 | 低 | 新样式使用命名空间前缀 |
| 数据绑定问题 | 低 | 仅修改XAML布局，不改变绑定路径 |

## Success Criteria

1. 所有预览界面使用统一的布局规范
2. 字段按重要性排列，关键信息一目了然
3. 视觉层次清晰，分组合理
4. 通过视觉审查确认改进效果

## References

- [8 Best Practices for UI Card Design](https://uxdesign.cc/8-best-practices-for-ui-card-design-898f45bb60cc)
- [Healthcare UI Design Best Practices](https://www.eleken.co/blog-posts/user-interface-design-for-healthcare-applications)
- [EHR Usability Interface Design](https://digital.ahrq.gov/sites/default/files/docs/citation/09-10-0091-2-EF.pdf)
- 项目现有UnifiedComponents.xaml样式规范
