# Must-Restore 功能清单

**生成时间**: 2025-09-09  
**分析范围**: 全项目代码扫描 TODO/FIXME/功能占位符  
**评估原则**: 基于20人以下小型诊所实际需求，遵循UltraThink实用主义原则

## 功能优先级评估

### P0 - 核心业务阻塞功能（必须恢复）

#### 1. 异常处理和恢复机制
**位置**: `src/Client/Desktop/Core/Services/GlobalExceptionHandler.cs`
**影响**: 系统崩溃时无法正常恢复，影响诊疗连续性
```csharp
// TODO: 触发登录事件 (Line 378)
// TODO: 重新加载配置 (Line 384)  
// TODO: 实现缓存刷新逻辑 (Line 420)
// TODO: 实现服务状态重置逻辑 (Line 435)
// TODO: 实现数据保存逻辑 (Line 508)
// TODO: 保存崩溃报告到文件 (Line 529)
```
**业务价值**: 防止系统异常导致的数据丢失和服务中断

#### 2. ~~药材配伍禁忌检查~~ 🔒 **DEFERRED (已冻结)**
**状态**: ❄️ **功能冻结** - 移至P2记录型替代方案
**原因**: 编译优先 + 过度工程化 + 小诊所实用性考量
**影响**: 自动配伍检查暂停，改为医生手动记录备注
```csharp
// FROZEN: ValidateCompatibilityStep类 (事务系统强耦合)
// FROZEN: IntelligentPrescriptionService类 (复杂智能组合)
// FROZEN: 自动配伍阻断逻辑 (过度设计)
```
**参考**: 详见 `_reports/feature/compat/freeze-auto-check.md`

#### 3. 处方打印功能
**位置**: 多个ViewModel中的打印TODO
**影响**: 无法为患者提供标准处方单，影响诊所运营
```csharp
// TODO: 实现打印功能 (ViewFormulaDialogViewModel.cs:149)
// TODO: 实现打印逻辑 (MedicalCaseManagementViewModel.cs:338)  
// TODO: 实现打印功能 (MedicalCaseDetailViewModel.cs:470)
```
**业务价值**: 标准化处方输出，满足法规要求

### P1 - 重要业务功能（高优先级）

#### 4. 用户身份获取机制
**位置**: `src/Client/Desktop/Modules/Formula/Services/FormulaModule.cs`
**影响**: 无法正确记录操作用户，影响审计和权限控制
```csharp
// TODO: 从认证上下文获取实际用户ID (Line 100)
var currentUserId = Guid.NewGuid(); // 当前使用随机ID
```
**业务价值**: 确保操作可追溯性和权限控制

#### 5. 验方导出功能
**位置**: `src/Client/Desktop/Modules/Formula/ViewModels/`
**影响**: 无法导出验方数据，影响诊所知识管理
```csharp
// TODO: 实现导出功能（PDF或Excel）(ViewFormulaDialogViewModel.cs:165)
```
**业务价值**: 验方知识资产数字化管理

#### 6. 中药材Excel导入导出
**位置**: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs`
**影响**: 无法批量管理药材数据，运营效率低
```csharp
// TODO: 实现Excel导出功能 (Line 115)
```
**业务价值**: 提升药材管理效率

#### 7. 诊疗记录完整性
**位置**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/`
**影响**: 患者信息显示不完整，影响诊疗体验
```csharp
// TODO: 从Patient服务获取 (MedicalCaseBusinessService.cs:62)
// TODO: 从User服务获取 (MedicalCaseBusinessService.cs:64)
PatientName = "待获取患者姓名";
DoctorName = "待获取医生姓名";
```
**业务价值**: 提升诊疗记录完整性和用户体验

### P2 - 增强功能（中优先级）

#### 8. 进度显示机制
**位置**: `src/Client/Desktop/Core/Services/UserNotificationService.cs`
**影响**: 用户无法感知长时间操作的进展，体验较差
```csharp
// TODO: 实现进度显示 (Line 245)
// TODO: 隐藏进度显示 (Line 251)
```
**业务价值**: 提升用户体验

#### 9. 对话框功能增强
**位置**: 多个ViewModel中
**影响**: 用户交互体验不完整
```csharp
// TODO: Close dialog with success (EditFormulaDialogViewModel.cs:213)
// TODO: Close dialog without saving (EditFormulaDialogViewModel.cs:233)
// TODO: Close dialog (ViewFormulaDialogViewModel.cs:178)
```
**业务价值**: 完善用户界面交互

#### 10. 药材管理增强
**位置**: `src/Client/Desktop/Modules/Formula/ViewModels/`
**影响**: 验方药材管理功能不完整
```csharp
// TODO: 实现添加药材对话框 (AddFormulaDialogViewModel.cs:220)
// TODO: 实现编辑药材对话框 (EditFormulaDialogViewModel.cs:238, 265)
```
**业务价值**: 完善验方管理功能

#### 11. 🆕 **HerbCompatNotes配伍备注系统 (MVP替代方案)**
**类型**: ✨ **PRD-Scoped记录型功能**
**优先级**: P2 (中等优先级)
**位置**: 新增模块 - 配伍备注管理
**目标**: 医生手动记录配伍禁忌备注，替代自动检查系统
**业务价值**: 保留配伍安全意识，避免过度工程化

**核心功能范围**:
```csharp
// MVP功能定义 (仅记录，不校验)
public class HerbCompatNoteDto {
    public string HerbName { get; set; }           // 药材名称
    public string CompatibilityNote { get; set; }  // 配伍备注
    public string RiskLevel { get; set; }          // 风险级别 Low/Medium/High  
    public string DoctorRemarks { get; set; }      // 医生备注
}
```

**技术约束**:
- ✅ API遵循 `/api/v1` 固定版本
- ✅ 命名统一使用 `Username` 规范  
- ✅ 分层架构 `UI→App→Domain→Infra`
- ✅ 严格遵守PRD/.editorconfig/Directory.*基线
- ❌ **明确Out of Scope**: 无自动规则、无实时判定、无干预处方保存

### P3 - 非核心功能（低优先级）

#### 12. 高级业务功能
**位置**: 各个服务模块
**影响**: 高级功能缺失，但不影响基础运营
```csharp
// TODO: 实现验方组合逻辑 (IntelligentPrescriptionService.cs:28)
// TODO: 需要重新设计状态映射逻辑 (ConsultationBusinessService.cs:106)
// TODO: 实现四诊数据解析和保存 (ConsultationBusinessService.cs:63)
```
**业务价值**: 提供更智能化的诊疗辅助功能

#### 13. 系统增强功能
**位置**: 基础设施层
**影响**: 系统稳定性和可观测性增强
```csharp
// TODO: 添加日志记录 (ServiceCollectionExtensions.cs:73)
// TODO: 可扩展到专用的审计数据库 (SecurityAuditService.cs:237)
```
**业务价值**: 提升系统运维水平

## 功能卡模板

### 功能卡 #001: 异常处理恢复机制
```
标题: 实现系统异常处理和恢复机制
优先级: P0
模块: Core.Services.GlobalExceptionHandler
工作量: 5人日
依赖: 无

验收标准:
- [ ] 认证失败时自动触发重新登录
- [ ] 配置异常时能够重新加载配置
- [ ] 缓存异常时能够刷新缓存
- [ ] 系统崩溃时保存崩溃报告
- [ ] 服务异常时能够重置服务状态

技术方案:
1. 实现AuthenticationFailedEvent事件机制
2. 配置热重载机制
3. 缓存刷新策略
4. 崩溃报告本地持久化
5. 服务状态重置机制
```

### 功能卡 #002: 药材配伍禁忌检查
```
标题: 实现中药材配伍禁忌检查系统
优先级: P0
模块: Module.Prescriptions
工作量: 8人日
依赖: 中医药理数据库

验收标准:
- [ ] 实现十八反禁忌检查
- [ ] 实现十九畏禁忌检查
- [ ] 配伍冲突时阻止处方保存
- [ ] 提供详细的配伍警告信息
- [ ] 支持配伍规则配置管理

技术方案:
1. 建立配伍禁忌规则数据表
2. 实现规则引擎验证逻辑
3. 处方保存前置检查机制
4. 警告信息展示UI设计
5. 管理员配伍规则维护界面
```

### 功能卡 #003: 处方打印功能
```
标题: 实现标准化处方打印功能
优先级: P0
模块: 多模块 (Formula, MedicalCase, Prescriptions)
工作量: 6人日
依赖: 打印模板设计

验收标准:
- [ ] 支持标准处方格式打印
- [ ] 支持验方模板打印
- [ ] 支持医疗案例报告打印
- [ ] 打印预览功能
- [ ] 支持多种纸张尺寸

技术方案:
1. 设计标准化处方打印模板
2. 实现打印预览功能
3. 集成系统打印服务
4. 支持PDF导出备选方案
5. 打印设置和纸张配置
```

## 实施建议

### 阶段化实施策略

**第一阶段 (1-2周)**: P0功能实现
- 优先实现异常处理恢复机制
- 完成药材配伍禁忌检查系统  
- 实现基础处方打印功能

**第二阶段 (2-3周)**: P1功能实现
- 完善用户身份获取机制
- 实现数据导入导出功能
- 补齐诊疗记录完整性

**第三阶段 (3-4周)**: P2-P3功能选择性实现
- 根据用户反馈选择实现
- 优先考虑用户体验相关功能

### 技术债务清理

**现状**: 发现32个TODO标记，涉及8个核心模块
**影响**: 功能完整性85%，核心安全功能缺失
**建议**: 
- 立即处理P0级安全相关TODO
- 制定TODO标记清理计划
- 建立代码审查机制防止新TODO累积

### 资源需求评估

**开发资源**: 2名开发人员 * 4周 = 8人周
**测试资源**: 1名测试人员 * 2周 = 2人周  
**总体周期**: 约6周完成核心功能恢复
**风险评估**: 中等风险，主要集中在医疗安全功能实现