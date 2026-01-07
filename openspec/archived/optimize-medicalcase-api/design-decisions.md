# 设计决策讨论记录

## 讨论状态

| 问题编号 | 问题主题 | 状态 | 结论 |
|----------|----------|------|------|
| Q1 | 处方Items更新策略 | 已完成 | 全量替换 |
| Q2 | NeedsPrescription控制位置 | 已完成 | Server自动控制，移除该字段 |
| Q3 | 暂存医案机制设计 | 已完成 | 使用现有Draft状态，保存诊断+处方 |
| Q4 | 关闭医案后的数据不可变性 | 已完成 | 角色+时间权限控制，所有修改需审计 |

> **注意**: 审计相关设计已独立为 `create-audit-module` 提案，本提案不再包含审计内容。

---

## Q1: 处方Items更新策略

**背景**: 处方包含多个药材Items，保存时需要决定如何更新这些Items。

**方案选项**:
- **全量替换**: 删除旧Items → 插入新Items（简单，但Id每次变化）
- **差异更新**: 比对增删改（复杂，但Id稳定）

### 子问题

#### Q1.1: 处方药材是否需要独立追溯？

> 例如：查看"上次处方中黄芪的剂量是多少"，或追溯"这味药什么时候被修改过"

- [ ] A. 不需要 - 处方作为整体保存，看历史只看整个处方快照
- [ ] B. 需要 - 需要追溯每味药材的修改历史

**用户场景描述**:

1. **打印场景**: 医案完成后打印，包含处方药材信息
2. **查询场景**: 复诊时查询历史医案（医案界面 + 管理界面两个入口）
3. **导入场景**: 编辑时可导入历史医案的处方药材组合到当前医案
4. **审计场景**: 医案完成后被修改需要审计记录（原因+原始值+当前值）

**场景分析**:

| 场景 | 对ItemId稳定性要求 | 说明 |
|------|-------------------|------|
| 打印 | 无要求 | 读取当前快照即可 |
| 查询 | 无要求 | 读取历史快照即可 |
| 导入 | 无要求 | 复制数据，创建新Items |
| 审计 | 无要求 | 已独立为审计模块，采用整体快照 |

**Q1.1结论**: 所有场景均不需要ItemId稳定

---

#### Q1.2: 处方药材是否会被其他业务引用？

> 例如：发药系统按ItemId逐个发放，库存按ItemId扣减，用量统计关联ItemId

- [ ] A. 不会 - 处方是终态，其他模块不引用单味药材
- [ ] B. 会 - 发药/库存需要引用ItemId
- [x] C. 未来可能 - 当前不需要，后续可能扩展

**回答**: C - V2版本计划实现发药库存系统

**V2发药设计讨论**:

| 方式 | 说明 | 处方修改影响 |
|------|------|-------------|
| A. 引用ItemId | 发药记录关联处方Item | 处方修改导致关联断裂 |
| B. 快照模式 | 发药时复制处方内容 | 处方修改不影响发药记录 |

**业务场景确认**:
1. 发药前修改处方 → 按修改后的剂量发药
2. 发药后修改处方（学习/复盘）→ 发药记录不变，盘库按实际发药量核对

**Q1.2结论**: 采用**快照模式**，发药记录独立于处方Item，V1无需保持ItemId稳定

---

#### Q1.3: 处方的编辑频率如何？

> 医生开处方的典型模式

- [x] A. 一次成型 - 填完直接保存，很少修改（推断）
- [ ] B. 多次调整 - 反复调整药材和剂量后才最终确定

**回答**: 基于业务场景推断为A（即使是B，全量替换性能也可接受）

**分析**: 处方药材数量有限（通常5-15味），全量替换的性能开销可忽略

---

### Q1 结论

**最终决策**: 采用**全量替换**策略

**理由**:

| 决策依据 | 结论 |
|----------|------|
| Q1.1 业务场景 | 打印/查询/导入/审计均不需要ItemId稳定 |
| Q1.2 V2发药设计 | 采用快照模式，不引用ItemId |
| Q1.3 性能考量 | 处方药材数量有限，全量替换性能可接受 |

**实现方案**:

```csharp
// MedicalCaseCommandService.HandlePrescriptionUpdate()
private async Task HandlePrescriptionUpdate(
    MedicalCaseModel medicalCase,
    PrescriptionInputDto? prescriptionInput)
{
    if (prescriptionInput?.NeedsPrescription == true)
    {
        if (medicalCase.Prescription == null)
        {
            // 创建新处方
            medicalCase.Prescription = new PrescriptionModel { ... };
        }

        // 全量替换Items
        medicalCase.Prescription.Items.Clear();
        foreach (var item in prescriptionInput.Items)
        {
            medicalCase.Prescription.Items.Add(new PrescriptionItemModel
            {
                HerbId = item.HerbId,
                HerbName = item.HerbName,
                Dosage = item.Dosage,
                Unit = item.Unit,
                // ...
            });
        }
    }
    else if (medicalCase.Prescription != null)
    {
        // 删除处方
        _context.Prescriptions.Remove(medicalCase.Prescription);
    }
}
```

**优势**:
1. 实现简单，代码清晰
2. 事务安全，原子操作
3. 无需复杂的差异对比算法
4. 为V2发药系统预留了正确的设计空间

---

## Q2: NeedsPrescription控制位置

**背景**: 处方的创建/删除由NeedsPrescription字段控制，需要决定这个控制权在哪里。

**方案选项**:
- **Client控制**: inputDto传入NeedsPrescription，Client决定是否需要处方
- **Server控制**: Server根据业务规则自动判断（如有药材就需要处方）

### 子问题

#### Q2.1: 是否存在"有药材但不需要处方"的场景？

> 例如：医生只是记录建议用药，但不正式开方

- [x] A. 不存在 - 有药材必然要处方
- [ ] B. 存在 - 有些场景只记录不开方

**回答**: A - 有药材就必须有处方

**业务场景确认**:
- 针灸、艾灸等物理治疗 → 不需要处方，也没有药材
- 生活建议等简单处理 → 不需要处方，也没有药材
- 开药方 → 有药材，就有处方

**结论**: 处方和药材是绑定的，不存在"有药材但无处方"的情况

---

#### Q2.2: 是否存在"无药材但需要处方"的场景？

> 例如：空处方占位，或纯医嘱处方

- [x] A. 不存在 - 无药材就无处方
- [ ] B. 存在 - 可能有空处方或纯医嘱

**回答**: A - 处方就是药材的组合，药材数量为0则无处方

---

### Q2 结论

**业务规则确认**:

| 药材数量 | 处方状态 |
|----------|----------|
| > 0 | 必须有处方 |
| = 0 | 必须无处方 |

**最终决策**: 采用 **Server自动控制**，移除 `NeedsPrescription` 字段

**理由**:
1. 业务规则明确：有药材=有处方，无药材=无处方
2. 无需Client显式声明，减少出错可能
3. Server根据 `Items.Count` 自动判断，逻辑更简洁

**实现方案**:

```csharp
// MedicalCaseCommandService.HandlePrescriptionUpdate()
private async Task HandlePrescriptionUpdate(
    MedicalCaseModel medicalCase,
    PrescriptionInputDto? prescriptionInput)
{
    var hasItems = prescriptionInput?.Items?.Any() == true;

    if (hasItems)
    {
        // 有药材 → 创建/更新处方
        if (medicalCase.Prescription == null)
        {
            medicalCase.Prescription = new PrescriptionModel();
        }

        // 全量替换Items
        medicalCase.Prescription.Items.Clear();
        foreach (var item in prescriptionInput!.Items!)
        {
            medicalCase.Prescription.Items.Add(MapToModel(item));
        }
    }
    else
    {
        // 无药材 → 删除处方
        if (medicalCase.Prescription != null)
        {
            _context.Prescriptions.Remove(medicalCase.Prescription);
        }
    }
}
```

**DTO变更**:

```csharp
// 移除 NeedsPrescription 字段
public class PrescriptionInputDto
{
    // [移除] public bool NeedsPrescription { get; set; }

    public List<PrescriptionItemInputDto> Items { get; set; } = new();
    public string? Instructions { get; set; }  // 服用说明
    // ...
}
```

**判断逻辑**: `Items.Any()` 替代 `NeedsPrescription`

---

## Q3: 暂存医案机制设计

**背景**: 
- **原始理解**: SaveDraft用于中途保存防丢失
- **实际需求**: **暂存医案**功能，支持医生临时离开或处理急诊后继续

**业务场景**:
1. 医生正在诊疗，突然有急诊患者需要优先处理
2. 医生需要临时离开（接电话、休息等）
3. 当前医案需要"暂存"，稍后恢复继续处理

**设计目标**:
- 暂存时保存当前所有工作进度（诊断+处方）
- 恢复时完整还原工作现场
- 支持多个暂存医案（待处理队列）

### 子问题

#### Q3.1: 暂存时应保存哪些数据？

> 确定暂存状态需要持久化的数据范围

| 数据类型 | 是否保存 | 说明 |
|----------|----------|------|
| 基本信息 | ✅ | 患者、就诊时间等 |
| 诊断内容 | ✅ | 四诊合参、辨证分析 |
| 处方药材 | ✅ | 已选药材列表、剂量、医嘱 |
| UI状态 | ❌ | 当前Tab、滚动位置等 |

**结论**: 
- [x] 保存诊断内容（Consultation）
- [x] 保存处方药材（Prescription Items，不校验完整性）
- [ ] 不保存UI状态（客户端临时存储即可）

---

#### Q3.2: 暂存状态如何表示？

> 数据库中如何区分暂存的医案

**现有状态枚举** (MedicalCaseStatus):

```csharp
public enum MedicalCaseStatus
{
    Draft = 0,      // 暂存（用户暂时保存，稍后继续）
    Active = 1,     // 进行中（正在诊疗）
    Completed = 2,  // 已完成
    Cancelled = 3   // 已取消
}
```

**结论**: 使用现有 `Draft` 状态表示暂存，无需新增状态

**状态流转**:

```
Draft (暂存) ←→ Active (进行中) → Completed (已完成)
                               ↘ Cancelled (已取消)
```

| 操作 | 状态变化 |
|------|----------|
| 新建医案 | → Active |
| 点击"暂存医案" | Active → Draft |
| 离开编辑界面(编辑状态) | Active → Draft (选择暂存时) |
| 恢复暂存医案 | Draft → Active |
| 完成医案 | Active → Completed |
| 取消医案 | 任意状态 → Cancelled |

---

#### Q3.3: 暂存医案如何管理？

> 医生如何查看和恢复暂存的医案

**UI设计**:
1. **待处理队列**: 侧边栏显示所有暂存的医案（按暂存时间排序）
2. **恢复操作**: 点击暂存医案 → 进入查看状态 → 点击"继续编辑"恢复
3. **自动提醒**: 长时间暂存的医案提醒处理

**API设计**:
```csharp
// 暂存医案（保存当前数据 + 状态→Draft）
POST /api/v1/medicalcases/{id}/draft
Body: { consultation: {...}, prescription: {...} }

// 恢复编辑（状态Draft→Active）
POST /api/v1/medicalcases/{id}/resume

// 获取暂存列表
GET /api/v1/medicalcases?status=Draft
```

---

#### Q3.4: 暂存触发方式与UI交互

> 暂存医案的触发时机和界面行为

**触发方式**:

| 触发方式 | 场景 | 说明 |
|----------|------|------|
| **手动触发** | 点击"暂存医案"按钮 | 医生主动暂存当前医案 |
| **自动触发** | 离开医案编辑界面 | 编辑状态下返回其他界面时触发确认 |

**自动触发的具体场景**:
1. 返回"患者选择"界面
2. 返回"医生主页"界面

---

**UI状态与离开行为**:

| 当前UI状态 | 医案状态 | 离开时行为 |
|------------|----------|------------|
| **编辑状态** | Active | 弹出选择对话框 |
| **查看状态** | Draft | 直接离开，无对话框 |

**编辑状态离开对话框**:

```
┌────────────────────────────────────┐
│  您有未保存的医案，请选择操作：    │
├────────────────────────────────────┤
│                                    │
│  [暂存医案]  [取消医案]  [取消]    │
│                                    │
└────────────────────────────────────┘
```

| 选项 | 操作 |
|------|------|
| 暂存医案 | 保存当前数据 → Active→Draft → 离开界面 |
| 取消医案 | Active→Cancelled → 离开界面 |
| 取消 | 关闭对话框，留在当前界面 |

**状态与UI模式对应**:

| 医案状态 | UI模式 | 说明 |
|----------|--------|------|
| Active | 编辑状态 | 可修改诊断和处方 |
| Draft | 查看状态 | 只读，需点击"继续编辑"恢复 |
| Completed | 查看状态 | 只读（已完成不可编辑） |
| Cancelled | 查看状态 | 只读（已取消不可编辑） |

---

#### Q3.5: 暂存时的数据校验策略

> 暂存保存时是否校验数据完整性

- [x] A. 不校验 - 暂存是临时状态，允许不完整数据
- [ ] B. 基础校验 - 只校验格式，不校验业务规则
- [ ] C. 完整校验 - 与正式保存相同校验

**结论**: 选择 **A. 不校验**

理由：
1. 暂存的目的是快速保存当前状态
2. 数据可能只填了一半，强制校验会阻碍暂存操作
3. 正式完成时再做完整校验

---

### Q3 结论

**最终决策**: 使用现有 `Draft` 状态实现**暂存医案**机制

**设计要点**:

| 决策项 | 结论 |
|--------|------|
| 保存范围 | Consultation + Prescription Items（无校验） |
| 状态表示 | 使用现有 `Draft` 状态 |
| 状态转换 | Active ↔ Draft |
| UI交互 | 编辑状态离开时弹出选择框，查看状态直接离开 |
| 校验策略 | 暂存不校验，完成时校验 |

**实现方案**:

```csharp
// 1. 状态枚举（现有，无需修改）
public enum MedicalCaseStatus
{
    Draft = 0,      // 暂存
    Active = 1,     // 进行中
    Completed = 2,  // 已完成
    Cancelled = 3   // 已取消
}

// 2. 暂存操作
public async Task<ApiResponse> SaveDraftAsync(Guid id, MedicalCaseDraftDto input)
{
    var medicalCase = await _repository.GetByIdAsync(id);
    
    if (medicalCase.Status != MedicalCaseStatus.Active)
        return ApiResponse.Fail("只有进行中的医案可以暂存");
    
    // 保存当前数据（不校验）
    await SaveWithoutValidationAsync(medicalCase, input);
    
    // 更新状态
    medicalCase.Status = MedicalCaseStatus.Draft;
    
    await _repository.SaveChangesAsync();
    return ApiResponse.Success();
}

// 3. 恢复编辑操作
public async Task<ApiResponse<MedicalCaseDetailDto>> ResumeAsync(Guid id)
{
    var medicalCase = await _repository.GetByIdAsync(id);
    
    if (medicalCase.Status != MedicalCaseStatus.Draft)
        return ApiResponse.Fail("只有暂存的医案可以恢复编辑");
    
    medicalCase.Status = MedicalCaseStatus.Active;
    
    await _repository.SaveChangesAsync();
    return await GetDetailAsync(id);
}
```

**优势**:
1. 复用现有状态枚举，无需修改数据模型
2. 完整保存工作现场（诊断+处方药材）
3. UI状态清晰：编辑状态(Active) vs 查看状态(Draft)
4. 暂存操作快速，不阻塞医生工作

---

## Q4: 关闭医案后的数据不可变性

**背景**: 医案关闭后状态变为Completed/Cancelled，需要决定是否允许后续修改。

### 子问题

#### Q4.1: 关闭后是否有修正需求？

> 例如：发现诊断写错了，需要修正

- [ ] A. 不需要 - 关闭就是终态，错了也不改
- [x] B. 需要修正 - 发现错误需要能修正，但有权限和时间限制
- [ ] C. 需要重开 - 可以重新打开继续诊疗

**回答**: B - 需要修正，但有限制条件

---

#### Q4.2: 权限控制规则

**确认的权限设计**:

| 角色 | 时间限制 | 允许操作 |
|------|----------|----------|
| **医生** | 仅当天 (day=today) | 修改 |
| **管理员** | 无限制 | 修改、恢复、完整操作 |

**医生权限说明**:
- 当天完成的医案，医生可以修改（发现错误及时修正）
- 隔天后，医生无法修改已完成的医案
- 需要修改时，联系管理员处理

**管理员权限说明**:
- 可以修改任意时间的已完成/已取消医案
- 可以恢复已取消的医案

> **注**: 审计功能由独立的审计模块(create-audit-module)提供，本模块仅负责权限控制

---

#### Q4.3: 已取消医案的处理

| 操作 | 医生(当天) | 医生(非当天) | 管理员 |
|------|------------|--------------|--------|
| 查看 | ✅ | ✅ | ✅ |
| 修改 | ✅ | ❌ | ✅ |
| 恢复(→Active) | ❌ | ❌ | ✅ |
| 删除 | ❌ | ❌ | ❌ (软删除，不真删) |

> 恢复操作仅管理员可执行，医生当天只能修改内容，不能改变状态

---

### Q4 结论

**最终决策**: 基于**角色+时间**的有限可变性

**权限矩阵**:

| 医案状态 | 医生(当天) | 医生(非当天) | 管理员 |
|----------|------------|--------------|--------|
| Active | 编辑 | 编辑 | 编辑 |
| Draft | 编辑 | 编辑 | 编辑 |
| Completed | 修改 | 只读 | 修改 |
| Cancelled | 修改 | 只读 | 修改/恢复 |

> **当天判断**: Completed基于CompletedAt，Cancelled基于CancelledAt

**时间判断基准**: 基于状态变更时间（CompletedAt / CancelledAt）

**实现方案**:

```csharp
public class MedicalCaseAuthorizationService
{
    public bool CanModify(MedicalCaseModel medicalCase, UserContext user)
    {
        // Active/Draft状态：所有人可编辑
        if (medicalCase.Status is MedicalCaseStatus.Active or MedicalCaseStatus.Draft)
            return true;
        
        // 管理员：无限制
        if (user.IsAdmin)
            return true;
        
        // 医生：仅当天的Completed/Cancelled可修改
        if (medicalCase.Status == MedicalCaseStatus.Completed)
        {
            return medicalCase.CompletedAt?.Date == DateTime.Today;
        }
        
        if (medicalCase.Status == MedicalCaseStatus.Cancelled)
        {
            return medicalCase.CancelledAt?.Date == DateTime.Today;
        }
        
        return false;
    }
}
```

> **审计说明**: 审计功能由独立的审计模块(create-audit-module)提供，集成时通过IAuditService注入

---

## 讨论记录

### 2025-12-31 讨论（第一轮）

**参与者**: 架构师 + 产品负责人

**讨论内容**:

1. **Q1 处方Items更新策略** - 确认采用全量替换，V2发药系统采用快照模式
2. **Q2 NeedsPrescription控制** - 确认Server自动控制，移除该字段
3. **Q3 暂存医案机制** - 确认使用Draft状态，保存诊断+处方，编辑状态离开弹选择框
4. **Q4 数据不可变性** - 确认基于角色+时间的权限控制（审计由独立模块提供）

---

### 2025-12-31 讨论（第二轮）- 细节确认

**参与者**: 架构师 + 产品负责人

#### 1. 发药场景影响确认

**结论**: 当前无药房模块，全量替换策略无影响

- 当前系统仅打印处方单，无发药追踪需求
- 未来药房模块建议采用快照模式（发药时记录处方快照）

#### 2. Draft暂存机制详细设计

**三个入口场景**:

| 入口 | 触发条件 | 行为 |
|------|----------|------|
| 入口1 | 医案编辑界面点击"暂存医案" | 编辑模式→查看模式，状态Active→Draft |
| 入口2 | 待诊列表双击暂存医案 | 直接进入编辑模式 |
| 入口3 | 患者列表选择患者进入 | 检测有暂存→弹出四项选择 |

**入口3的四项选择**:
1. 继续暂存医案 → 打开暂存医案继续编辑
2. 关闭暂存医案后新建 → 取消旧的(Cancelled)，创建新医案
3. 仅关闭暂存医案 → 取消旧的(Cancelled)，不进入编辑界面
4. 取消 → 返回患者列表

**业务规则**:
- 同一患者最多1个Draft状态医案
- Draft永不过期，手动取消或下次挂号时提醒
- "关闭暂存医案" = 状态变为Cancelled

**术语统一**:
- 旧术语"挂起" → 新术语"暂存" (Draft状态)

#### 3. 权限控制边界确认

**完整权限规则**:

```csharp
public bool CanModify(MedicalCaseModel medicalCase, UserContext user)
{
    // 管理员：无任何限制
    if (user.IsAdmin)
        return true;
    
    // 医生：只能操作自己的医案
    if (medicalCase.UserId != user.Id)
        return false;
    
    // Active/Draft：自己的可编辑
    if (medicalCase.Status is MedicalCaseStatus.Active or MedicalCaseStatus.Draft)
        return true;
    
    // Completed：自己的 + 当天完成的（本地日期）
    if (medicalCase.Status == MedicalCaseStatus.Completed)
        return medicalCase.CompletedAt?.Date == DateTime.Today;
    
    // Cancelled：自己的 + 当天取消的（本地日期）
    if (medicalCase.Status == MedicalCaseStatus.Cancelled)
        return medicalCase.CancelledAt?.Date == DateTime.Today;
    
    return false;
}
```

**权限矩阵**:

| 医案状态 | 本人医生(当天) | 本人医生(非当天) | 其他医生 | 管理员 |
|----------|---------------|-----------------|----------|--------|
| Active | 编辑 | 编辑 | 无权限 | 编辑 |
| Draft | 编辑 | 编辑 | 无权限 | 编辑 |
| Completed | 修改 | 只读 | 无权限 | 修改 |
| Cancelled | 修改 | 只读 | 无权限 | 修改/恢复 |

**"当天"判断基准**:
- 使用本地时间（非UTC）
- 按日期判断：CompletedAt.Date == DateTime.Today
- 无宽限期：2025-01-01完成，2025-01-02就不能修改

#### 4. Prescription生命周期确认

**核心规则**: Items为空时，Prescription对象应为null

```
Items.Any() == true  → Prescription存在
Items.Any() == false → Prescription = null
```

**创建时机**: 首次添加药品时创建Prescription
**删除时机**: Items清空时删除Prescription记录

**理由**: Prescription中的服法、剂数等信息都是针对药方的，无药品则无意义

**代码逻辑**:

```csharp
public async Task SavePrescriptionAsync(MedicalCaseModel medicalCase, List<PrescriptionItemDto> items)
{
    if (items.Any())
    {
        // 有药品：创建或更新Prescription
        if (medicalCase.Prescription == null)
        {
            medicalCase.Prescription = new PrescriptionModel();
        }
        
        // 全量替换Items
        medicalCase.Prescription.Items.Clear();
        medicalCase.Prescription.Items.AddRange(MapToItems(items));
    }
    else
    {
        // 无药品：删除Prescription
        if (medicalCase.Prescription != null)
        {
            _context.Prescriptions.Remove(medicalCase.Prescription);
            medicalCase.Prescription = null;
        }
    }
}
```

**UI显示规则**:

| 场景 | Prescription状态 | UI显示 |
|------|-----------------|--------|
| 新建医案，未添加药品 | null | "暂无处方" |
| 添加药品 | 创建记录 | 显示药品列表 |
| 删除部分药品 | 保留记录 | 显示剩余药品 |
| 删除全部药品 | 删除记录→null | "暂无处方" |
| 完成医案，无处方 | null | "本次未开药" |
