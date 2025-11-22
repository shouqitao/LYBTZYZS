# Desktop层TODO注释扫描报告

**扫描时间**：2025-10-27
**扫描范围**：`src/Client/Desktop/Modules/` 下所有 `*ViewModel.cs` 文件
**扫描模式**：`TODO|FIXME|HACK`
**总计**：36个TODO注释
**关联任务**：Epic #1676 Task 3.1

---

## 📊 统计概览

| ViewModel | TODO数量 | 优先级 | 说明 |
|-----------|---------|-------|------|
| **MedicalCaseConsultationViewModel.cs** | 6 | 🔴 头部1 | 跨模块集成、事件实现、API调用 |
| **PatientImportWizardViewModel.cs** | 6 | 🔴 头部2 | 向导步骤UI、UnifiedViewModelBase迁移 |
| **CompletionViewModel.cs** | 4 | 🔴 头部3 | 打印功能、病案详情对话框 |
| UserProfileDialogViewModel.cs | 3 | 🟡 中优先级 | 头像加载、个人资料修改、头像上传 |
| HerbDetailViewModel.cs | 3 | 🟡 中优先级 | 编辑模式、打印、使用历史 |
| FormulaManagementViewModel.cs | 3 | 🟡 中优先级 | 导入、导出模板、导出逻辑 |
| ResetPasswordDialogViewModel.cs | 2 | 🟢 低优先级 | Mock实现 |
| ViewFormulaDialogViewModel.cs | 2 | 🟢 低优先级 | 打印、导出功能 |
| OtherCasesQueryViewModel.cs | 1 | 🟢 低优先级 | 全局消息提示 |
| MedicalCaseManagementViewModel.cs | 1 | 🟢 低优先级 | 搜索逻辑 |
| ChangePasswordDialogViewModel.cs | 1 | 🟢 低优先级 | Mock AuthService |
| UserDetailViewModel.cs | 1 | 🟢 低优先级 | Prism IDialogService |
| PatientDetailViewModel.cs | 1 | 🟢 低优先级 | 打印系统 |
| QuickCreatePatientDialogViewModel.cs | 1 | 🟢 低优先级 | 拼音码功能 |
| PrescriptionItemViewModel.cs | 1 | 🟢 低优先级 | 业务服务评估 |

---

## 🎯 头部3个ViewModel详细清单

### 1️⃣ MedicalCaseConsultationViewModel.cs（6个TODO）

| 行号 | TODO内容 | 分类建议 | 工作量估算 |
|-----|---------|---------|----------|
| 278 | 跨模块集成 - 实现PrescriptionLoadedEvent通知PrescriptionEditor加载处方数据 | ⚠️ 重要功能 - 转为Issue | 1-2天 |
| 333 | Task 3.4 (#1661) - 实现ConsultationSavedEvent | ⚠️ 已关联Issue - 保留引用 | - |
| 461 | Task 3.5 (#1662) - 实现NavigateRequestEvent | ⚠️ 已关联Issue - 保留引用 | - |
| 497 | 实现完成病案的API调用 | 🔴 快速实现 | 0.5天 |
| 555 | 检查是否已创建处方 | 🔴 快速实现 | 0.5天 |

### 2️⃣ PatientImportWizardViewModel.cs（6个TODO）

| 行号 | TODO内容 | 分类建议 | 工作量估算 |
|-----|---------|---------|----------|
| 215 | Phase 4D - 考虑迁移到 UnifiedViewModelBase (需处理 IDisposable 冲突) | ⚠️ 架构优化 - 转为Issue | 2-3天 |
| 472 | 根据当前步骤更新内容视图 | 🔴 快速实现 | 0.5天 |
| 769 | 返回步骤1的具体UI内容 | 🔴 快速实现 | 0.5天 |
| 775 | 返回步骤2的具体UI内容 | 🔴 快速实现 | 0.5天 |
| 781 | 返回步骤3的具体UI内容 | 🔴 快速实现 | 0.5天 |
| 787 | 返回步骤4的具体UI内容 | 🔴 快速实现 | 0.5天 |

### 3️⃣ CompletionViewModel.cs（4个TODO）

| 行号 | TODO内容 | 分类建议 | 工作量估算 |
|-----|---------|---------|----------|
| 123 | 打印处方（TODO: 实现打印功能） | ⚠️ 重要功能 - 转为Issue | 1-2天 |
| 131 | Task #1502+ - 实现处方打印功能 | ⚠️ 已关联Issue - 保留引用 | - |
| 147 | 查看病案详情（TODO: 实现详情对话框） | ⚠️ 重要功能 - 转为Issue | 1-2天 |
| 155 | Task #1502+ - 实现病案详情详情对话框 | ⚠️ 已关联Issue - 保留引用 | - |

**头部3个ViewModel合计**：16个TODO
- 已关联Issue：4个（保留引用）
- 快速实现：7个（需要实施）
- 转为Issue：5个（重要功能）

---

## 📝 其他ViewModel详细清单（33-36个TODO）

### 4️⃣ UserProfileDialogViewModel.cs（3个TODO）

| 行号 | TODO内容 | 分类建议 | 工作量估算 |
|-----|---------|---------|----------|
| 240 | 加载头像（如果有头像 URL） | 🔴 快速实现 | 0.5天 |
| 382 | 当前 Client 端没有 ChangeProfileAsync 服务方法，暂时 Mock 成功 | ⚠️ Server端API - 转为Issue | 1天 |
| 388 | 如果有头像文件，需要上传到服务器 | ⚠️ Server端API - 转为Issue | 1天 |

### 5️⃣ HerbDetailViewModel.cs（3个TODO）

| 行号 | TODO内容 | 分类建议 | 工作量估算 |
|-----|---------|---------|----------|
| 348 | 实现编辑模式逻辑 | 🔴 快速实现 | 0.5天 |
| 368 | 实现打印逻辑 | ⚠️ 通用打印 - 转为Issue | 1天 |
| 394 | 实现查看使用历史逻辑 | ⚠️ 报表功能 - 转为Issue | 1-2天 |

### 6️⃣ FormulaManagementViewModel.cs（3个TODO）

| 行号 | TODO内容 | 分类建议 | 工作量估算 |
|-----|---------|---------|----------|
| 361 | 实现导入逻辑 | ⚠️ 数据迁移 - 转为Issue | 2天 |
| 378 | 实现导出模板逻辑 | ⚠️ 数据迁移 - 转为Issue | 1天 |
| 395 | 实现导出逻辑 | ⚠️ 数据迁移 - 转为Issue | 1天 |

### 7️⃣ ResetPasswordDialogViewModel.cs（2个TODO）

| 行号 | TODO内容 | 分类建议 | 工作量估算 |
|-----|---------|---------|----------|
| 14 | 当前使用 Mock 实现，待后续集成真实服务 | ⚠️ Server端API - 转为Issue | 1天 |
| 332 | 当前 Client 端没有 ResetPassword 服务方法，暂时 Mock 成功 | ⚠️ Server端API - 转为Issue | 1天 |

### 8️⃣ ViewFormulaDialogViewModel.cs（2个TODO）

| 行号 | TODO内容 | 分类建议 | 工作量估算 |
|-----|---------|---------|----------|
| 176 | 实现打印功能 | ⚠️ 通用打印 - 转为Issue | 1天 |
| 195 | 实现导出功能（PDF或Excel） | ⚠️ 数据导出 - 转为Issue | 1-2天 |

### 9️⃣ OtherCasesQueryViewModel.cs（1个TODO）

| 行号 | TODO内容 | 分类建议 | 工作量估算 |
|-----|---------|---------|----------|
| 250 | 集成全局消息提示服务 | 🔴 快速实现 | 0.5天 |

### 🔟 MedicalCaseManagementViewModel.cs（1个TODO）

| 行号 | TODO内容 | 分类建议 | 工作量估算 |
|-----|---------|---------|----------|
| 270 | 实现搜索逻辑或转发到子视图 | 🔴 快速实现 | 0.5天 |

### 1️⃣1️⃣ ChangePasswordDialogViewModel.cs（1个TODO）

| 行号 | TODO内容 | 分类建议 | 工作量估算 |
|-----|---------|---------|----------|
| 288 | 当前使用 Mock AuthService，只接受 2 个参数 | ⚠️ Server端API - 转为Issue | 1天 |

### 1️⃣2️⃣ UserDetailViewModel.cs（1个TODO）

| 行号 | TODO内容 | 分类建议 | 工作量估算 |
|-----|---------|---------|----------|
| 184 | 使用 Prism IDialogService 打开 ResetPasswordDialog | 🔴 快速实现 | 0.5天 |

### 1️⃣3️⃣ PatientDetailViewModel.cs（1个TODO）

| 行号 | TODO内容 | 分类建议 | 工作量估算 |
|-----|---------|---------|----------|
| 293 | (Issue #1202): 等待新的打印系统实现后重新启用此功能 | ⚠️ 已关联Issue - 保留引用 | - |

### 1️⃣4️⃣ QuickCreatePatientDialogViewModel.cs（1个TODO）

| 行号 | TODO内容 | 分类建议 | 工作量估算 |
|-----|---------|---------|----------|
| 193 | 拼音码功能待后续扩展（需要扩展PatientCreateDto） | ⚠️ 扩展功能 - 转为Issue | 1天 |

### 1️⃣5️⃣ PrescriptionItemViewModel.cs（1个TODO）

| 行号 | TODO内容 | 分类建议 | 工作量估算 |
|-----|---------|---------|----------|
| 117 | Phase 4C - 当前为DTO转换类,按需评估是否需要业务服务 | ⚠️ 架构优化 - 转为Issue | 1天 |

---

## 📋 分类汇总

### 按处理方式分类

| 处理方式 | 数量 | 占比 | 说明 |
|---------|-----|------|------|
| 🔴 **快速实现** | 13 | 36.1% | 工作量≤0.5天，可以立即实施 |
| ⚠️ **转为Issue** | 18 | 50.0% | 重要功能或复杂逻辑，需要创建Issue |
| ✅ **已关联Issue** | 5 | 13.9% | 已有Issue引用，保留格式 |
| **总计** | **36** | **100%** | - |

### 按功能领域分类

| 功能领域 | 数量 | 主要ViewModel |
|---------|-----|--------------|
| 🏥 **医案管理** | 11 | MedicalCaseConsultationViewModel, CompletionViewModel |
| 👤 **用户管理** | 7 | UserProfileDialogViewModel, ChangePasswordDialogViewModel |
| 🧑‍🤝‍🧑 **患者管理** | 8 | PatientImportWizardViewModel, PatientDetailViewModel |
| 🌿 **药材管理** | 3 | HerbDetailViewModel |
| 📜 **方剂管理** | 5 | FormulaManagementViewModel, ViewFormulaDialogViewModel |
| 💊 **处方管理** | 2 | CompletionViewModel, PrescriptionItemViewModel |

---

## 🎯 下一步行动

### Task 3.2：分类TODO生成清单
- ✅ 扫描完成，找到36个TODO
- ⏭️ 下一步：对36个TODO进行详细分类
- 📋 重点：生成头部3个ViewModel的详细清理计划

### Task 3.3：执行清理头部3个ViewModel
- 🎯 目标：清理16个TODO
  - 快速实现：7个
  - 转为Issue：5个
  - 保留引用：4个
- ⏱️ 估算工作量：2-4小时

---

**关联Issue**：#1685
**关联Epic**：#1676

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
