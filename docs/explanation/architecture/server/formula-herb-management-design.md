# 验方药材管理粒度设计讨论

**文档创建时间**: 2025-11-10  
**讨论参与者**: 用户（产品负责人）+ Claude（架构师）  
**决策编号**: Formula-Design-Decision-002  
**关联问题**: 验方药材组成逻辑缺失问题

---

## 1. 问题背景

### 1.1 核心业务需求

验方（Formula）包含三个核心要素：
- **名称**（Name）：如"白虎汤"
- **功用**（Effect）：如"清热生津"
- **主治**（Indication）：如"阳明气分热盛。症见壮热面赤，烦渴饮引，大汗恶热，脉洪大有力或滑数"

**最重要的是药方组成**（Herb Composition）：
```
示例：白虎汤组成
- 生石膏 21g
- 肥知母 9g
- 象贝 9g
- 炙草 3g
```

### 1.2 当前实现状态

**Entity层**（✅ 已实现）:
```csharp
public class Formula : BaseEntity
{
    public List<FormulaHerbItem> Herbs { get; set; } = new();
}

public class FormulaHerbItem
{
    public Guid FormulaId { get; set; }
    public Guid? HerbId { get; set; }
    public string HerbName { get; set; }
    public int Quantity { get; set; }  // 剂量
    public string Unit { get; set; } = "g";
    public string? ProcessingMethod { get; set; }  // 炮制方法
}
```

**Service层**（❌ 逻辑缺失）:
- ✅ 批量导入时创建 Herbs 集合
- ✅ UpdateAsync 接受完整 FormulaInputDto（包含 Herbs[]）
- ❌ 缺少单个药材的增删改方法
- ❌ 缺少药材相关的查询方法

**关键发现**: 实体层设计完整，但 Service/Repository 层的**药材管理逻辑几乎完全缺失**。

---

## 2. 设计决策：药材管理粒度

### 2.1 方案对比

#### 方案A：粗粒度（全量替换）

**设计思路**:
- 继续使用现有 `UpdateAsync(Guid id, FormulaInputDto dto)` 方法
- `FormulaInputDto` 包含完整的 `List<FormulaHerbItemDto> Herbs`
- 每次修改验方时，Desktop端提交完整的药材列表
- Service层删除旧的 Herbs，插入新的 Herbs

**实现方式**:
```csharp
public async Task<Result<FormulaOutputDto>> UpdateAsync(Guid id, FormulaInputDto dto)
{
    var formula = await _repository.GetByIdAsync(id);
    
    // 清空现有药材
    formula.Herbs.Clear();
    
    // 重新添加所有药材
    foreach (var herbDto in dto.Herbs)
    {
        formula.Herbs.Add(new FormulaHerbItem
        {
            HerbName = herbDto.HerbName,
            Quantity = herbDto.Quantity,
            Unit = herbDto.Unit,
            ProcessingMethod = herbDto.ProcessingMethod
        });
    }
    
    await _repository.UpdateAsync(formula);
    return Result<FormulaOutputDto>.Success(MapToDto(formula));
}
```

**优点**:
- ✅ 实现简单，无需新增API
- ✅ Desktop端逻辑清晰：维护 `ObservableCollection<HerbItemViewModel>`，一次性提交
- ✅ 事务边界明确（一次Update覆盖所有变更）
- ✅ 无并发冲突问题

**缺点**:
- ⚠️ 每次修改都传输完整列表（网络开销）
- ⚠️ 无法记录单个药材的修改历史
- ⚠️ 批量操作时缺少细粒度验证

---

#### 方案B：细粒度（单个药材操作）

**设计思路**:
- 新增专门的药材管理API
- 支持单个药材的增删改操作
- Desktop端可以针对性地调用API

**新增方法**:
```csharp
// Service层接口扩展
public interface IFormulaService
{
    // 现有方法
    Task<Result<FormulaOutputDto>> GetByIdAsync(Guid id);
    Task<Result<FormulaOutputDto>> AddAsync(FormulaInputDto dto);
    Task<Result<FormulaOutputDto>> UpdateAsync(Guid id, FormulaInputDto dto);
    
    // 🆕 药材管理方法
    Task<Result<FormulaHerbItemDto>> AddHerbToFormulaAsync(Guid formulaId, FormulaHerbItemDto herbDto);
    Task<Result> RemoveHerbFromFormulaAsync(Guid formulaId, Guid herbItemId);
    Task<Result<FormulaHerbItemDto>> UpdateHerbInFormulaAsync(Guid formulaId, Guid herbItemId, FormulaHerbItemDto herbDto);
}

// Controller层扩展
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class FormulasController : BaseApiController
{
    // 🆕 药材管理端点
    [HttpPost("{formulaId}/herbs")]
    public async Task<IActionResult> AddHerb(Guid formulaId, [FromBody] FormulaHerbItemDto dto)
    {
        var result = await _service.AddHerbToFormulaAsync(formulaId, dto);
        return Ok(result);
    }
    
    [HttpDelete("{formulaId}/herbs/{herbItemId}")]
    public async Task<IActionResult> RemoveHerb(Guid formulaId, Guid herbItemId)
    {
        var result = await _service.RemoveHerbFromFormulaAsync(formulaId, herbItemId);
        return Ok(result);
    }
    
    [HttpPut("{formulaId}/herbs/{herbItemId}")]
    public async Task<IActionResult> UpdateHerb(Guid formulaId, Guid herbItemId, [FromBody] FormulaHerbItemDto dto)
    {
        var result = await _service.UpdateHerbInFormulaAsync(formulaId, herbItemId, dto);
        return Ok(result);
    }
}
```

**优点**:
- ✅ 网络传输最小化（仅传输变更项）
- ✅ 支持细粒度权限控制（例如：仅允许修改剂量，不允许删除药材）
- ✅ 便于记录药材级别的审计日志
- ✅ 支持部分验证逻辑（例如：添加药材时检查配伍禁忌）

**缺点**:
- ❌ 增加API数量（+3个端点）
- ❌ Desktop端需要维护更复杂的状态管理（跟踪每个药材的增删改）
- ❌ 多次API调用存在并发问题（例如：用户同时添加2个药材）
- ❌ 事务边界模糊（多次API调用无法保证原子性）

---

### 2.2 用户交互场景分析

#### 场景1：创建新验方

**流程**:
1. 用户输入验方名称、功用、主治
2. 用户逐个添加药材（药材名、剂量、炮制方法）
3. 用户点击"保存"

**方案A实现**:
```
Desktop端:
- ViewModel维护 ObservableCollection<HerbItemViewModel>
- 用户"添加药材"按钮 → 向集合添加新项
- "保存"按钮 → 调用 POST /api/v1/formulas（传递完整Herbs数组）

优点: 一次API调用完成
缺点: 无
```

**方案B实现**:
```
Desktop端:
- 先调用 POST /api/v1/formulas（不含Herbs）
- 对每个药材调用 POST /api/v1/formulas/{id}/herbs
- 若某个药材添加失败，需要回滚？

优点: 无
缺点: 多次API调用，复杂化流程
```

**结论**: 场景1中，**方案A明显优于方案B**。

---

#### 场景2：编辑现有验方（修改药材剂量）

**流程**:
1. 用户打开验方详情
2. 修改某个药材的剂量（21g → 30g）
3. 点击"保存"

**方案A实现**:
```
Desktop端:
- ViewModel加载 ObservableCollection<HerbItemViewModel>（10个药材）
- 用户修改其中1个药材的剂量
- "保存"按钮 → 调用 PUT /api/v1/formulas/{id}（传递完整10个药材）

优点: 实现简单
缺点: 传输了9个未变更的药材数据
```

**方案B实现**:
```
Desktop端:
- ViewModel加载 ObservableCollection<HerbItemViewModel>
- 用户修改剂量后，标记该药材为"已修改"
- "保存"按钮 → 仅调用 PUT /api/v1/formulas/{id}/herbs/{herbItemId}

优点: 仅传输变更数据
缺点: 需要实现变更追踪逻辑
```

**结论**: 场景2中，**方案B在网络效率上有优势，但实现复杂度显著增加**。

---

#### 场景3：克隆验方并修改

**流程**:
1. 用户选择"白虎汤"
2. 点击"克隆"按钮，创建"白虎汤加味"
3. 在克隆版本中添加3个新药材，删除1个药材

**方案A实现**:
```
Desktop端:
- 调用 GET /api/v1/formulas/{id} 获取原验方
- 复制 Herbs 集合到新的 ObservableCollection
- 用户修改集合（添加、删除）
- 调用 POST /api/v1/formulas（新验方+完整Herbs）

优点: 一次API调用
```

**方案B实现**:
```
Desktop端:
- 先调用 POST /api/v1/formulas 创建新验方
- 对原Herbs逐个调用 POST /api/v1/formulas/{newId}/herbs
- 对新药材调用 POST /api/v1/formulas/{newId}/herbs
- 对删除的药材调用 DELETE（等等，克隆时原药材还没复制过来？）

优点: 无
缺点: 流程混乱，逻辑复杂
```

**结论**: 场景3中，**方案A明显优于方案B**。

---

### 2.3 技术实现考量

#### MVP原则约束

根据项目 Constitution：
- ✅ **够用即好**：方案A满足所有业务场景
- ❌ **拒绝过度设计**：方案B引入不必要的复杂性
- ✅ **快速交付**：方案A无需新增API
- ✅ **简单直接**：方案A代码逻辑清晰

#### EF Core 事务处理

**方案A**:
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    formula.Herbs.Clear();  // EF Core追踪删除
    formula.Herbs.AddRange(newHerbs);  // EF Core追踪添加
    await _context.SaveChangesAsync();  // 一次性提交
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
}
```
- 事务边界明确
- EF Core自动优化SQL（批量DELETE + 批量INSERT）

**方案B**:
```csharp
// 三次独立的API调用，如何保证事务一致性？
await AddHerbToFormulaAsync(formulaId, herb1);  // Transaction 1
await AddHerbToFormulaAsync(formulaId, herb2);  // Transaction 2
await RemoveHerbFromFormulaAsync(formulaId, herbId);  // Transaction 3

// 若Transaction 3失败，Transaction 1和2已提交，如何回滚？
```
- 分布式事务问题
- 需要引入Saga模式或补偿事务（过度设计）

---

## 3. 架构决策

### 3.1 业务场景分析（2025-11-10）

**通过Q&A确认的关键场景**：

#### 场景1：药材独立性
- **结论**：场景A - 药材不会被单独查询或操作
- **特征**：药材仅作为验方的组成部分，无独立生命周期
- **用途**：验方是"处方模板"，用于快速创建处方（直接使用或在其基础上调整）

#### 场景2：编辑流程
- **结论**：一次性批量保存
- **界面设计**：Excel表格式（一行4组，每组：药名+剂量）
- **用户操作**：在表格中完成所有修改（添加、删除、修改剂量），最后点击"保存"按钮
- **药材匹配**：输入框支持中文名/拼音码自动匹配系统药材库
- **删除行为**：右键删除或点击"x"后，后续药材自动前移（WPF ObservableCollection自动处理）

#### 场景3：离线编辑
- **结论**：纯在线操作
- **行为**：必须联网才能编辑，中途掉线则作废
- **未来计划**：后期可能开发本地版

#### 场景4：并发编辑
- **结论**：单人编辑，无并发冲突
- **权限模型**：
  - 医生创建的验方默认私有（可选择共享，但他人只读）
  - 管理员创建的验方默认共享（只读）
  - 规则：谁创建谁编辑（创建者独占编辑权）

---

### 3.2 业界实践对比

**DDD聚合根模式中的"值对象集合"**：

| 业界案例 | 主实体 | 子实体 | API设计 | 适用特征 |
|---------|--------|--------|---------|----------|
| **电商订单系统** | Order | OrderItems | `PUT /orders/{id}` (含items[]) | 明细是订单组成部分 |
| **发票系统** | Invoice | InvoiceLines | `PUT /invoices/{id}` (含lines[]) | 行是发票组成部分 |
| **LYBTZYZS验方** | Formula | FormulaHerbItems | `PUT /formulas/{id}` (含herbs[]) | 药材是验方组成部分 |
| **博客评论（对比）** | Post | Comments | `POST /posts/{id}/comments` | 评论有独立生命周期 |

**关键发现**：
- LYBTZYZS验方场景完全对应"订单-明细"模式，而非"博客-评论"模式
- 业界标准做法：**聚合根整体更新（Whole Aggregate Update）**

---

### 3.3 最终决策

**选择方案A：粗粒度（全量替换）**

**决策编号**：Formula-Design-Decision-002  
**决策日期**：2025-11-10  
**决策依据**：业务场景分析 + 业界实践 + MVP原则 + 技术黑名单检查

#### 决策理由（5个维度验证）

**1. 业务需求匹配度**：✅ 100%
- 验方是处方模板，药材是组成部分（非独立实体）
- Excel表格式编辑，符合用户"一次性保存"的心智模型
- 无单独操作药材的需求
- 单人编辑，无并发冲突问题

**2. 技术实现复杂度**：✅ 最简
- 无需新增API端点（利用现有 `UpdateAsync`）
- EF Core自动处理事务（一次 SaveChanges）
- 代码量最少，维护成本最低

**3. MVP原则符合度**：✅ 完全合规
- 够用即好：满足所有业务场景
- 拒绝过度设计：无需引入细粒度API
- 快速交付：无需额外开发

**4. 业界实践符合度**：✅ 标准模式
- DDD聚合根整体更新模式
- RESTful API标准（PUT更新整个资源）
- 订单-明细经典模式

**5. 性能与网络开销**：✅ 完全可接受
- 验方药材数量：5-15个（典型8个）
- 单次传输：约1.5KB
- 数据库操作：1 DELETE + 1 批量INSERT（<10ms）
- **对比细粒度**：3次HTTP请求 × 50ms延迟 = 150ms（反而更慢）

### 3.2 实现计划

#### Phase 1: 完善现有方法（✅ 已基本实现）

**当前状态**:
```csharp
// ✅ AddAsync 已支持 Herbs
public async Task<Result<FormulaOutputDto>> AddAsync(FormulaInputDto dto)
{
    var formula = new Formula { ... };
    foreach (var herbDto in dto.Herbs)
    {
        formula.Herbs.Add(new FormulaHerbItem { ... });
    }
    await _repository.AddAsync(formula);
}

// ✅ UpdateAsync 已支持 Herbs（需确认）
public async Task<Result<FormulaOutputDto>> UpdateAsync(Guid id, FormulaInputDto dto)
{
    var formula = await _repository.GetByIdAsync(id);
    
    // 🔍 需确认：是否正确处理 Herbs 集合更新
    formula.Herbs.Clear();
    foreach (var herbDto in dto.Herbs)
    {
        formula.Herbs.Add(new FormulaHerbItem { ... });
    }
    
    await _repository.UpdateAsync(formula);
}
```

**待验证事项**:
1. `UpdateAsync` 是否正确处理 Herbs 集合的清空和重建？
2. EF Core 是否正确追踪 Herbs 的删除和添加？

#### Phase 2: 新增辅助查询方法（未来扩展）

当业务需求明确后，可新增以下查询方法：

```csharp
// 按药材查询验方
Task<Result<List<FormulaOutputDto>>> GetFormulasByHerbAsync(string herbName);

// 按药材数量查询
Task<Result<List<FormulaOutputDto>>> GetFormulasByHerbCountAsync(int minCount, int maxCount);

// 查询包含特定药材组合的验方
Task<Result<List<FormulaOutputDto>>> GetFormulasByHerbCombinationAsync(List<string> herbNames);
```

**注意**: 仅在用户明确提出需求后再实现，遵循MVP原则。

---

## 4. Desktop端实现指南

### 4.1 ViewModel设计

```csharp
public class FormulaDetailViewModel : BindableBase
{
    private readonly IFormulaApi _formulaApi;
    
    // 验方基本信息
    public string Name { get; set; }
    public string Effect { get; set; }
    public string Indication { get; set; }
    
    // 药材集合
    public ObservableCollection<HerbItemViewModel> Herbs { get; set; } = new();
    
    // 命令
    public DelegateCommand AddHerbCommand { get; }
    public DelegateCommand<HerbItemViewModel> RemoveHerbCommand { get; }
    public DelegateCommand SaveCommand { get; }
    
    public FormulaDetailViewModel(IFormulaApi formulaApi)
    {
        _formulaApi = formulaApi;
        
        AddHerbCommand = new DelegateCommand(OnAddHerb);
        RemoveHerbCommand = new DelegateCommand<HerbItemViewModel>(OnRemoveHerb);
        SaveCommand = new DelegateCommand(OnSave);
    }
    
    private void OnAddHerb()
    {
        // 直接在内存集合中添加
        Herbs.Add(new HerbItemViewModel());
    }
    
    private void OnRemoveHerb(HerbItemViewModel herb)
    {
        // 直接从内存集合中删除
        Herbs.Remove(herb);
    }
    
    private async void OnSave()
    {
        // 一次性提交完整数据
        var dto = new FormulaInputDto
        {
            Name = Name,
            Effect = Effect,
            Indication = Indication,
            Herbs = Herbs.Select(h => new FormulaHerbItemDto
            {
                HerbName = h.HerbName,
                Quantity = h.Quantity,
                Unit = h.Unit,
                ProcessingMethod = h.ProcessingMethod
            }).ToList()
        };
        
        var result = await _formulaApi.UpdateAsync(FormulaId, dto);
        if (result.IsSuccess)
        {
            MessageBox.Show("保存成功");
        }
    }
}

public class HerbItemViewModel : BindableBase
{
    private string _herbName;
    private int _quantity;
    private string _unit = "g";
    private string _processingMethod;
    
    public string HerbName
    {
        get => _herbName;
        set => SetProperty(ref _herbName, value);
    }
    
    public int Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, value);
    }
    
    public string Unit
    {
        get => _unit;
        set => SetProperty(ref _unit, value);
    }
    
    public string ProcessingMethod
    {
        get => _processingMethod;
        set => SetProperty(ref _processingMethod, value);
    }
}
```

### 4.2 XAML布局示例

```xml
<UserControl>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- 验方基本信息 -->
        <StackPanel Grid.Row="0" Margin="10">
            <TextBox Text="{Binding Name}" Header="验方名称"/>
            <TextBox Text="{Binding Effect}" Header="功用"/>
            <TextBox Text="{Binding Indication}" Header="主治" TextWrapping="Wrap"/>
        </StackPanel>
        
        <!-- 药材列表 -->
        <DataGrid Grid.Row="1" 
                  ItemsSource="{Binding Herbs}" 
                  AutoGenerateColumns="False"
                  CanUserAddRows="False">
            <DataGrid.Columns>
                <DataGridTextColumn Header="药材名称" Binding="{Binding HerbName}" Width="*"/>
                <DataGridTextColumn Header="剂量" Binding="{Binding Quantity}" Width="80"/>
                <DataGridTextColumn Header="单位" Binding="{Binding Unit}" Width="60"/>
                <DataGridTextColumn Header="炮制方法" Binding="{Binding ProcessingMethod}" Width="100"/>
                <DataGridTemplateColumn Header="操作" Width="80">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <Button Content="删除" 
                                    Command="{Binding DataContext.RemoveHerbCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                    CommandParameter="{Binding}"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>
        
        <!-- 操作按钮 -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="10">
            <Button Content="添加药材" Command="{Binding AddHerbCommand}" Margin="0,0,10,0"/>
            <Button Content="保存" Command="{Binding SaveCommand}"/>
        </StackPanel>
    </Grid>
</UserControl>
```

---

## 5. 后续优化方向（可选）

### 5.1 性能优化（当验方数量>1000时考虑）

**问题**: 某些复杂验方可能包含>20个药材，全量传输可能影响性能。

**解决方案**: 引入Delta更新机制

```csharp
public class FormulaUpdateDto
{
    public string? Name { get; set; }
    public string? Effect { get; set; }
    public string? Indication { get; set; }
    
    // 🆕 Delta更新
    public List<FormulaHerbItemDto>? HerbsToAdd { get; set; }
    public List<Guid>? HerbIdsToRemove { get; set; }
    public List<FormulaHerbItemUpdateDto>? HerbsToUpdate { get; set; }
}
```

**触发条件**:
- 验方平均药材数量 > 20个
- 用户反馈编辑验方时响应慢
- 网络监控显示单次请求 > 10KB

### 5.2 审计日志（当需要追踪变更历史时考虑）

**问题**: 无法追踪"谁在何时修改了哪个药材"。

**解决方案**: 引入事件溯源（Event Sourcing）

```csharp
public class FormulaHerbChangedEvent
{
    public Guid FormulaId { get; set; }
    public string ChangeType { get; set; }  // Added, Removed, Updated
    public string HerbName { get; set; }
    public int? OldQuantity { get; set; }
    public int? NewQuantity { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; }
}
```

**触发条件**:
- 监管部门要求审计验方变更
- 发现验方被恶意篡改
- 需要分析验方演进历史

---

## 6. 决策记录

| 项目 | 决策内容 |
|-----|---------|
| **决策编号** | Formula-Design-Decision-002 |
| **决策日期** | 2025-11-10 |
| **决策者** | 用户（产品负责人）+ Claude（架构师） |
| **决策内容** | 采用方案A（粗粒度全量替换）管理验方药材 |
| **核心理由** | 符合MVP原则，满足所有业务场景，实现简单 |
| **实现方式** | 继续使用 UpdateAsync(Guid id, FormulaInputDto dto) |
| **Desktop端** | 维护 ObservableCollection<HerbItemViewModel>，一次性提交 |
| **未来扩展** | 仅在明确需求出现时，才考虑细粒度API或Delta更新 |

---

## 7. 验证清单

在完成实现后，需验证以下场景：

- [ ] 创建新验方（包含5个药材）
- [ ] 编辑验方：修改某个药材的剂量
- [ ] 编辑验方：删除1个药材
- [ ] 编辑验方：添加2个新药材
- [ ] 克隆验方并修改药材
- [ ] 导入验方（包含药材）
- [ ] 验证 EF Core 是否正确处理 Herbs 集合的清空和重建
- [ ] 验证数据库中药材记录的正确性（无重复、无遗漏）

---

## 附录：相关文档

- [验方Server端设计文档](formula-design.md)
- [验方Client端设计文档](../client/formula-design.md)
- [MVP设计原则](.claude/explanation/mvp-philosophy.md)
- [三层对齐架构](.claude/explanation/architecture-philosophy.md)

---

**文档版本**: v1.0  
**最后更新**: 2025-11-10
