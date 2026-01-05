# Change: 创建独立打印模块

## Why

当前处方打印服务位于MedicalCase模块内，存在以下问题：
1. **耦合过紧**: 打印功能与医案业务逻辑混杂，违反单一职责原则
2. **扩展困难**: 无法轻松添加新的打印模板（如收费单、报告单等）
3. **复用受限**: 其他模块（如Formula、Patients）无法复用打印能力
4. **命名空间问题**: XAML模板引用MedicalCase.Models导致编译错误

## What Changes

### 新增模块
- 创建`LYBT.Desktop.Printing`作为Core层独立模块
- 位置: `src/Client/Desktop/Core/LYBT.Desktop.Printing/`

### 架构设计
- 通用打印服务接口 `IPrintService<TModel>`
- 模板注册表模式支持多模板扩展
- 打印预览/打印/导出三种操作模式
- 支持A4/A5等多种纸张规格

### 代码迁移
- 从MedicalCase模块迁移:
  - `IPrescriptionPrintService` → `IPrintService<PrescriptionPrintModel>`
  - `PrescriptionPrintService` → `PrintService` (通用实现)
  - `PrescriptionPrintModel` → `Printing/Models/`
  - `PrescriptionPrintTemplate.xaml` → `Printing/Templates/`

### **BREAKING**
- MedicalCase模块不再直接提供打印服务
- 改为依赖Printing模块获取打印能力

## Impact

- Affected specs: 新建 `printing-infrastructure` 规范
- Affected code:
  - `LYBT.Desktop.MedicalCase/` - 移除打印相关代码
  - `LYBT.Desktop.Clinical/` - 更新打印服务注入
  - `LYBT.Desktop.Shell/` - 注册Printing模块
- Affected tests: 打印服务单元测试需迁移

## Success Criteria

1. Printing模块独立编译成功
2. MedicalCase模块编译通过（无打印相关代码）
3. Clinical工作区打印功能正常工作
4. 预览/打印/导出XPS功能完整
5. 模板扩展机制可用（可添加新模板类型）
