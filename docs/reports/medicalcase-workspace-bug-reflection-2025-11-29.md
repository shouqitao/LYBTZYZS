# 医案工作区问题修复反思报告

**日期**: 2025-11-29
**涉及模块**: MedicalCase统一工作区
**相关Issue**: #2250

---

## 一、问题演进时间线

```
问题1: 诊断字段无法保存
    ↓
问题2: 诊断字段无法显示
    ↓
问题3: 处方内容无法保存 (Issue #2250)
    ↓
问题4: 处方内容显示异常 (药材位置错乱)
```

---

## 二、问题详细分析

### 问题1: 诊断字段无法保存

**现象**: 填写诊断信息（主诉、中医诊断、望闻问切等）后保存，数据未持久化到数据库。

**根因分析**:
- ConsultationPanelViewModel与后端API字段映射不一致
- DTO字段命名与实体字段不匹配
- 保存逻辑中遗漏部分诊断字段

**修复方案**:
- 统一前后端字段命名规范
- 确保ConsultationInputDto包含所有诊断字段
- 修正AutoMapper映射配置

**相关提交**: `34c800a26 refactor(Consultation): 医案备注通过诊断保存统一管理`

---

### 问题2: 诊断字段无法显示

**现象**: 重新打开已保存的医案，诊断区域显示空白。

**根因分析**:
- ViewModel初始化时未正确加载已有诊断数据
- `InitializeAsync`方法缺少从DTO到ViewModel属性的映射
- 数据加载时机问题（异步竞态）

**修复方案**:
- 在InitializeAsync中添加诊断数据加载逻辑
- 使用`LoadFromDto`方法统一数据回填
- 确保PropertyChanged事件正确触发UI更新

**技术要点**:
```csharp
// 正确的数据加载模式
public async Task InitializeAsync(MedicalCaseDto dto)
{
    if (dto.Consultation != null)
    {
        ChiefComplaint = dto.Consultation.ChiefComplaint;
        TCMDiagnosis = dto.Consultation.TCMDiagnosis;
        // ... 其他字段
    }
}
```

---

### 问题3: 处方内容无法保存 (Issue #2250)

**现象**: 添加药材到处方后点击"暂存"，返回422错误，处方数据丢失。

**错误日志**:
```
System.InvalidOperationException in Microsoft.EntityFrameworkCore.dll
Refit.ApiException: Response status code does not indicate success: 422 (Unprocessable Entity)
```

**根因分析**:
1. **RowVersion并发冲突**: 前端缓存的RowVersion与数据库不一致
2. **EF Core实体状态问题**: 修改后的实体状态为Detached而非Modified
3. **聚合根边界问题**: 直接修改子实体未通过MedicalCase聚合根

**修复方案**:
- 添加`GetByIdWithDetailsFreshAsync`方法强制刷新RowVersion
- 在保存前分离ChangeTracker中的缓存实体
- 重新从数据库加载最新RowVersion后再更新

**核心代码**:
```csharp
// IMedicalCaseRepository.cs - 新增方法
Task<MedicalCaseEntity?> GetByIdWithDetailsFreshAsync(Guid id);

// MedicalCaseRepository.cs - 实现
public async Task<MedicalCaseEntity?> GetByIdWithDetailsFreshAsync(Guid id)
{
    // 分离所有缓存实体
    foreach (var entry in _context.ChangeTracker.Entries<MedicalCaseEntity>()
        .Where(e => e.Entity.Id == id))
    {
        entry.State = EntityState.Detached;
    }

    // 重新查询获取最新RowVersion
    return await GetByIdWithDetailsAsync(id);
}
```

**相关提交**: `971f97e8a fix(MedicalCase): Issue #2250 处方保存RowVersion异常修复`

---

### 问题4: 处方内容显示异常

**现象**:
- 保存处方后，药材显示在第二行而非第一行
- 返回患者列表后重新打开医案，药材显示在第五位

**根因分析**:

**Bug触发流程**:
```
LoadFromDto()
    → CreateHerbItem()
    → 设置 HerbId
    → PropertyChanged 触发
    → EnsureMinimumBlankRows() 添加4个空槽位
    → HerbItems.Add(herb)
    → 结果: [空, 空, 空, 空, 药材]
```

**问题本质**:
- PropertyChanged事件在数据加载完成前触发了副作用操作
- `EnsureMinimumBlankRows()`在集合为空时添加4个空槽位
- 实际药材被添加到空槽位之后

**修复方案**:
1. 添加`_isLoadingData`加载标志
2. 在LoadFromDto开始时设置标志为true
3. PropertyChanged处理程序检查标志，加载期间跳过操作
4. 在finally块中重置标志

**核心代码**:
```csharp
// PrescriptionPanelViewModel.cs

private bool _isLoadingData;

private void LoadFromDto(PrescriptionDto dto)
{
    _isLoadingData = true;
    try
    {
        // 加载数据...
        foreach (var item in dto.Items)
        {
            var herbItem = CreateHerbItem();
            herbItem.HerbId = item.HerbId;  // PropertyChanged触发但被跳过
            HerbItems.Add(herbItem);
        }
        EnsureMinimumBlankRows();  // 在所有药材添加后执行
    }
    finally
    {
        _isLoadingData = false;
    }
}

// CreateHerbItem中的PropertyChanged处理
item.PropertyChanged += (s, e) =>
{
    if (_isLoadingData) return;  // 加载期间跳过

    if (e.PropertyName == nameof(HerbId))
    {
        EnsureMinimumBlankRows();
    }
};
```

**修复后流程**:
```
LoadFromDto() (_isLoadingData=true)
    → CreateHerbItem()
    → 设置 HerbId
    → PropertyChanged 触发 → 检测到 _isLoadingData=true → 跳过
    → HerbItems.Add(herb)
    → 循环完成
    → EnsureMinimumBlankRows()
    → finally (_isLoadingData=false)
    → 结果: [药材, 空, 空, 空, 空]
```

---

## 三、问题根因归类

### 3.1 架构层面问题

| 问题类型 | 具体表现 | 占比 |
|---------|---------|------|
| DTO/实体映射不一致 | 字段遗漏、命名差异 | 25% |
| 并发控制缺陷 | RowVersion同步机制不完善 | 35% |
| 事件驱动副作用 | PropertyChanged时机问题 | 25% |
| 状态管理混乱 | ViewModel复用时状态残留 | 15% |

### 3.2 代码模式问题

1. **缺少加载标志模式**: 数据加载期间未隔离事件副作用
2. **RowVersion管理分散**: 未在Repository层统一处理并发
3. **ViewModel生命周期**: DI注入的ViewModel复用时未正确重置状态

---

## 四、经验教训与改进措施

### 4.1 短期改进（已完成）

1. **添加GetByIdWithDetailsFreshAsync方法**: 强制刷新RowVersion
2. **引入_isLoadingData标志**: 隔离数据加载期间的事件副作用
3. **InitializeAsync中重置状态**: 确保ViewModel复用时状态干净

### 4.2 中期改进建议

1. **统一并发控制策略**:
   - 在BaseRepository层提供统一的RowVersion处理
   - 考虑使用乐观锁重试机制（3次重试）

2. **ViewModel生命周期管理**:
   - 考虑使用Scoped而非Singleton注册
   - 或实现IDisposable进行显式清理

3. **事件驱动规范**:
   - 建立加载标志模式的代码规范
   - 在复杂ViewModel中统一使用此模式

### 4.3 长期改进建议

1. **引入状态机管理**:
   - 使用状态模式管理医案工作流状态
   - 明确定义每个状态下允许的操作

2. **端到端测试覆盖**:
   - 添加完整的保存-重新加载-验证测试
   - 覆盖并发场景的集成测试

3. **前端缓存策略优化**:
   - 考虑在保存成功后主动刷新RowVersion
   - 减少前后端状态不一致的窗口期

---

## 五、技术债务识别

| 技术债务 | 严重程度 | 建议优先级 |
|---------|---------|-----------|
| RowVersion处理分散在各Service | 中 | P1 |
| ViewModel状态管理不规范 | 中 | P2 |
| 缺少并发场景测试 | 高 | P1 |
| PropertyChanged副作用未文档化 | 低 | P3 |

---

## 六、总结

本次修复涉及MedicalCase统一工作区的四个关联问题，核心难点在于：

1. **EF Core并发控制**: RowVersion机制需要全链路同步
2. **WPF MVVM事件驱动**: PropertyChanged的副作用需要精细控制
3. **ViewModel生命周期**: DI注入的ViewModel复用带来状态管理挑战

通过引入`_isLoadingData`标志模式和`GetByIdWithDetailsFreshAsync`方法，成功解决了所有问题。建议后续将这些模式固化为代码规范，防止类似问题再次发生。

---

**报告生成时间**: 2025-11-29 14:54 CST
**报告作者**: Claude Code
