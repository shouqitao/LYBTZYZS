# 任务 A-30-S3：Desktop 层方法级深审

> 派发对象：Mimo Code（审查只读，不改代码）
> 版本：v1.0 | 日期：2026-08-08
> 依据：`docs/compose/plans/2026-08-08-method-audit-plan.md`（S3 阶段）+ S0 基线 + S1/S2 结论
> 用户方针：先收敛再完善

## 任务

对 **Desktop 层 16 项目**进行方法级深度审查（A/B/C/D/E 分级），重点是 VM 方法/命令重复、DTO↔Model 映射方法、双轨差异方法、项目合并候选，为 Desktop 合并方案提供依据。

## 范围

- ✅ `src/Client/Desktop/` 全部 16 项目：Core 6（Contracts/Controls/Foundation/Infrastructure/Printing/LocalWebAPI）+ Modules 7（Auth/Users/Patients/Herbs/Formula/MedicalCase/Registrations）+ Roles 2（Admin/Clinical）+ Shell 1
- ✅ 消费方核实（Server/Shared 被 Desktop 引用位置，只读）
- ❌ 排除：Shared（S1）、Server（S2）
- 仓库根：`D:\source\repos\LYBTZYZS`｜分支 `master`｜基线 `4a956a851` + S0/S1/S2 产出

## 审查维度（方法级）

沿用 A/B/C/D/E 分级，重点：

### 1. VM 方法/命令重复（C 级重点）
- 同名命令（RelayCommand）跨模块重复（如 Save/Delete/Refresh/Load 命令实现）
- VM 生命周期方法（OnNavigatedTo/Initialize/LoadAsync）重复模式
- 事件订阅/取消订阅方法重复

### 2. DTO↔Model 映射方法（C 级）
- Desktop 各模块 Model 层与 DTO 映射方法分布（Mapperly vs 手写）
- 是否统一"直用 DTO"或"DTO→Model"（A-26 P2-11 遗留）
- 映射方法重复（同实体多处映射）

### 3. 双轨差异方法（C 级）
- Remote/Local 两条路径方法差异（SwitchingApiClient 分支方法）
- LocalWebAPI 12 Controller 与 WebAPI 12 Controller 方法重复（A-16 已发现 Controller 级复制，方法级复核）

### 4. Repository/Service 方法（A-28 后复核）
- Desktop Repository 方法（ApiClientRepositoryBase/EntityApiClientRepositoryBase 继承者 vs 裸实现）
- Service 接口方法 vs 调用点

### 5. 死方法（D 级）
- 0 调用方法（结合 S0 死方法候选符号级复核，含 XAML 绑定/命令绑定核实）

### 6. 合并候选识别（本次新增，供 S4 整合）
- **Core 项目合并**：Contracts/Controls/Foundation/Infrastructure/Printing 方法分布——哪些项目方法集可合并（如 Controls+Printing → Infrastructure）？
- **模块合并候选**：Auth/Users/Herbs/Formula 小模块（10-15 类）方法集相似度 → 合并可行性
- **Roles 职责**：Admin/Clinical 方法分布是否清晰（角色边界）？
- **ViewModelLocator/DI 注册方法**：模块注册方法重复模式

## 产出

1. 16 项目方法分级表（项目/类/方法/级/证据）
2. C 级重复清单（VM/映射/双轨/Repo）+ 收敛方向
3. D 级死方法清单（符号级确认，含 XAML/命令绑定）
4. **Desktop 合并候选分析**（Core 合并 + 模块合并 + Roles，方法级证据）
5. 与 S1 日志/异常专项的衔接（Desktop 侧日志/异常方法迁移）

## 硬性约束

1. **只读**：不修改任何 src 代码；只写报告
2. 每发现带证据：`文件:行号` + 调用链
3. XAML 绑定/命令绑定必须核实（VM 方法可能被 XAML 反射调用，不能仅凭 C# 引用判死）
4. **不要 commit**：报告由技术总监统一提交
5. 产出单一报告：`docs/compose/reports/method-audit-desktop-2026-08-08.md`

## 报告结构

```
# Desktop 层方法级深审报告（Mimo 独立分析）
## 0. 方法说明
## 1. 16 项目方法分级总表（A/B/C/D/E 计数）
## 2. VM 方法/命令重复清单
## 3. DTO↔Model 映射方法分析
## 4. 双轨差异方法清单（LocalWebAPI vs WebAPI）
## 5. Repository/Service 方法复核
## 6. C 级重复清单 + 收敛方向
## 7. D 级死方法清单（含 XAML 绑定核实）
## 8. 合并候选分析（Core/模块/Roles）
## 9. 与 S1 日志/异常专项衔接
## 10. 统计汇总
```

## 明确不做（防发散）

- ❌ 不审 Shared/Server（S1/S2 已审）
- ❌ 不修改任何代码
- ❌ 不执行合并（只出候选分析）
- ❌ 不评 UI/交互（方法级审查，非 UX 审查）
