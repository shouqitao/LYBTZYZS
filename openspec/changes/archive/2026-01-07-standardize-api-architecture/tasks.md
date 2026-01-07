# Tasks: standardize-api-architecture

## 重构原则

**彻底重构，不设计兼容模式**:
1. 删除所有MappingService，不保留任何一个
2. 删除所有IMapper/AutoMapper引用，不保留过渡代码
3. 删除所有DetailModel命名，强制使用Item
4. 每个Phase完成后立即删除旧文件

## Overview

| Phase | 任务数 | 预估工时 | 状态 |
|-------|--------|----------|------|
| Phase 1: Server端彻底清理 | 8 | 1天 | pending |
| Phase 2: Desktop端彻底重构 | 14 | 2天 | pending |
| Phase 3: 命名彻底统一 | 5 | 0.5天 | pending |
| Phase 4: 验证和文档 | 4 | 0.5天 | pending |
| **合计** | **31** | **4天** | - |

---

## Phase 1: Server端彻底清理

### Task 1.1: 清理AutoMapper NuGet引用
- [ ] 检查所有.csproj中的AutoMapper包引用
- [ ] 移除 `AutoMapper` 包
- [ ] 移除 `AutoMapper.Extensions.Microsoft.DependencyInjection` 包
- [ ] 验证无AutoMapper相关包引用

**搜索命令**: `rg "AutoMapper" --glob "*.csproj"`

### Task 1.2: 删除所有MappingProfile类
- [ ] 搜索所有 `*MappingProfile.cs` 或 `*Profile.cs` 文件
- [ ] 完全删除这些文件（不保留）
- [ ] 验证无MappingProfile类存在

**搜索命令**: `rg "class.*Profile.*:.*Profile" --glob "*.cs"`

### Task 1.3: Herbs模块彻底清理
- [ ] 移除HerbService中的IMapper依赖注入
- [ ] 改为直接实例化 `private readonly HerbMapper _mapper = new();`
- [ ] 更新所有Map调用为具体方法调用
- [ ] 删除HerbsModule中的AutoMapper注册代码

### Task 1.4: Users模块彻底清理
- [ ] 移除UserService中的IMapper依赖注入
- [ ] 改为直接实例化UserMapper
- [ ] 删除UsersModule中的AutoMapper注册

### Task 1.5: Patients模块彻底清理
- [ ] 移除PatientService中的IMapper依赖注入
- [ ] 改为直接实例化PatientMapper
- [ ] 删除PatientsModule中的AutoMapper注册

### Task 1.6: Formula模块彻底清理
- [ ] 移除FormulaService中的IMapper依赖注入
- [ ] 改为直接实例化FormulaMapper
- [ ] 删除FormulaModule中的AutoMapper注册

### Task 1.7: MedicalCase/Consultation/Prescriptions模块彻底清理
- [ ] 移除所有Service中的IMapper依赖
- [ ] 改为直接实例化对应Mapper
- [ ] 删除Module中的AutoMapper注册

### Task 1.8: Server端最终验证
- [ ] 运行 `rg "IMapper" --glob "*.cs"` 验证0引用
- [ ] 运行 `rg "AutoMapper" --glob "*.cs"` 验证0引用
- [ ] 运行 `dotnet build LYBT.All.sln -c Release`
- [ ] 运行Server端单元测试

**验收标准**:
- 0个IMapper引用
- 0个AutoMapper引用
- 0个MappingProfile类
- 编译通过

---

## Phase 2: Desktop端彻底重构

### Task 2.1: 扫描所有MappingService文件
- [ ] 执行 `rg -l "MappingService" --glob "*.cs"` 获取完整列表
- [ ] 记录所有待删除的MappingService文件
- [ ] 分析每个MappingService的职责（纯映射/计算属性/业务逻辑）

**待删除文件清单**:
```
src/Client/Desktop/Modules/LYBT.Desktop.Users/Mappers/UserMappingService.cs
src/Client/Desktop/Modules/LYBT.Desktop.Patients/Mappers/PatientMappingService.cs
src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Mappers/HerbMappingService.cs
src/Client/Desktop/Modules/LYBT.Desktop.Formula/Mappers/FormulaMappingService.cs
src/Client/Desktop/Modules/LYBT.Desktop.Formula/Mappers/FormulaDetailModelMappingService.cs
src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/MedicalCaseItemMappingService.cs
src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/MedicalCaseDetailModelMappingService.cs
src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/ConsultationMappingService.cs
src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/PrescriptionMappingService.cs
```

### Task 2.2: Users模块彻底清理
- [ ] 扩展UserMapper添加所有必要映射方法
- [ ] 将计算属性移到UserItem的getter中
- [ ] **删除** UserMappingService.cs
- [ ] 更新所有调用点（ViewModel等）
- [ ] 移除Module中的MappingService DI注册

### Task 2.3: Patients模块彻底清理
- [ ] 扩展PatientMapper添加所有必要映射方法
- [ ] 将计算属性移到PatientItem的getter中
- [ ] **删除** PatientMappingService.cs
- [ ] 更新所有调用点
- [ ] 移除Module中的MappingService DI注册

### Task 2.4: Herbs模块彻底清理
- [ ] 扩展HerbMapper添加所有必要映射方法
- [ ] 将计算属性移到HerbItem的getter中
- [ ] **删除** HerbMappingService.cs
- [ ] 更新所有调用点
- [ ] 移除Module中的MappingService DI注册

### Task 2.5: Formula模块Mapper合并
- [ ] 创建统一的FormulaMapper（合并所有映射方法）
- [ ] **删除** FormulaHerbItemMapper.cs
- [ ] **删除** FormulaDetailModelMapper.cs
- [ ] **删除** FormulaMappingService.cs
- [ ] **删除** FormulaDetailModelMappingService.cs
- [ ] 更新所有调用点
- [ ] 移除Module中的MappingService DI注册

### Task 2.6: MedicalCase模块Mapper合并
- [ ] 合并为2个Mapper: MedicalCaseMapper + PrescriptionMapper
- [ ] **删除** MedicalCaseItemMapper.cs
- [ ] **删除** MedicalCaseDetailModelMapper.cs
- [ ] **删除** ConsultationMapper.cs（合并到MedicalCaseMapper）
- [ ] **删除** MedicalCaseItemMappingService.cs
- [ ] **删除** MedicalCaseDetailModelMappingService.cs
- [ ] **删除** ConsultationMappingService.cs
- [ ] **删除** PrescriptionMappingService.cs
- [ ] 更新所有调用点
- [ ] 移除Module中的MappingService DI注册

### Task 2.7: Consultation模块检查
- [ ] 检查LYBT.Desktop.Consultation模块的Mapper
- [ ] 如果与MedicalCase模块重复，**删除**整个Mappers目录
- [ ] 保留唯一的ConsultationMapper在MedicalCase模块

### Task 2.8: 清理Module DI注册
- [ ] 移除所有模块中的MappingService DI注册
- [ ] 移除所有模块中的IMapper DI注册
- [ ] 验证Mapper通过 `new()` 实例化而非DI

### Task 2.9: Desktop端最终验证
- [ ] 运行 `rg "MappingService" --glob "*.cs"` 验证0个MappingService类
- [ ] 运行 `rg "IMapper" --glob "*.cs" --path src/Client` 验证0引用
- [ ] 运行 `dotnet build LYBT.All.sln -c Release`
- [ ] 验证Desktop项目编译通过

**验收标准**:
- 0个MappingService类
- 0个IMapper引用（Desktop端）
- 每模块仅1-2个Mapper文件
- 编译通过

---

## Phase 3: 命名彻底统一

### Task 3.1: 扫描所有DetailModel类
- [ ] 执行 `rg "class.*DetailModel" --glob "*.cs"` 获取完整列表
- [ ] 记录所有待重命名的DetailModel类

**待重命名清单**:
```
FormulaDetailModel → FormulaItem
MedicalCaseDetailModel → MedicalCaseItem
```

### Task 3.2: FormulaDetailModel → FormulaItem
- [ ] 使用IDE重命名工具重命名类（Rider: Shift+F6）
- [ ] 重命名文件 FormulaDetailModel.cs → FormulaItem.cs
- [ ] 验证所有引用自动更新
- [ ] **删除**原FormulaDetailModel.cs（如IDE未自动删除）

### Task 3.3: MedicalCaseDetailModel → MedicalCaseItem
- [ ] 使用IDE重命名工具重命名类
- [ ] 重命名文件 MedicalCaseDetailModel.cs → MedicalCaseItem.cs
- [ ] 验证所有引用自动更新
- [ ] **删除**原MedicalCaseDetailModel.cs

### Task 3.4: 搜索其他非标准命名
- [ ] 执行 `rg "DetailModel" --glob "*.cs"` 确认无遗留
- [ ] 执行 `rg "class.*Model\b" --glob "*.cs" --path src/Client` 检查其他Model命名
- [ ] 将所有数据模型类统一为xxxItem命名

### Task 3.5: 命名统一最终验证
- [ ] 运行 `rg "DetailModel" --glob "*.cs"` 验证0匹配
- [ ] 运行 `dotnet build LYBT.All.sln -c Release`
- [ ] 验证编译通过

**验收标准**:
- 0个*DetailModel数据模型类
- 100%使用xxxItem命名
- 编译通过

---

## Phase 4: 文档和验证

### Task 4.1: 更新API文档
- [ ] 更新 `docs/reference/api/` 相关文档
- [ ] 更新Mapper使用说明
- [ ] 添加新的Mapper示例代码

### Task 4.2: 更新架构说明
- [ ] 更新 `openspec/project.md` - Mapping Convention章节
- [ ] 添加Desktop Mapper整合说明
- [ ] 更新目录结构说明

### Task 4.3: 全量测试
- [ ] 运行全量单元测试 `dotnet test`
- [ ] 运行集成测试（如有）
- [ ] 手动验证关键功能路径

### Task 4.4: 更新CHANGELOG
- [ ] 添加standardize-api-architecture变更记录
- [ ] 记录Breaking Changes（如有）
- [ ] 记录迁移说明

---

## 验收标准

### Phase 1 验收
- [ ] Server端0个IMapper接口引用
- [ ] 所有Mapper通过 `new()` 实例化
- [ ] Server端单元测试通过

### Phase 2 验收
- [ ] Desktop端0个MappingService类
- [ ] 所有映射通过Mapper完成
- [ ] Desktop端编译通过

### Phase 3 验收
- [ ] 100%模块使用xxxItem命名
- [ ] 0个DetailModel类（作为数据模型）
- [ ] 编译通过

### Phase 4 验收
- [ ] 文档更新完成
- [ ] 全量测试通过
- [ ] CHANGELOG更新

---

## 进度跟踪

| 日期 | Phase | 完成任务 | 备注 |
|------|-------|----------|------|
| | | | |

---

**Author**: Claude Code
**Created**: 2026-01-07
**Status**: Draft
**Progress**: 0/27 (0%)
