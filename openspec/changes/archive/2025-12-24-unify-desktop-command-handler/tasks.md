# 任务清单: unify-desktop-command-handler

**Change ID**: unify-desktop-command-handler
**Created**: 2025-12-24
**Scope**: Desktop层Herbs模块重构

---

## 模块影响范围

### 需要变更
- Herbs模块 (DataManager → CommandHandler)

### 保持不变
- Users, Patients, Consultation, Formula, MedicalCase (已使用CommandHandler)
- Auth (使用Coordinator，无Repository)
- Prescriptions (工具服务，非CRUD)

---

## Phase 1: 创建HerbCommandHandler

### Task 1.1: 创建IHerbCommandHandler接口
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Contracts/IHerbCommandHandler.cs`
- **内容**:
  ```csharp
  public interface IHerbCommandHandler
  {
      Task<(bool success, HerbDetailDto? data, string? error)> CreateAsync(HerbInputDto input);
      Task<(bool success, HerbDetailDto? data, string? error)> UpdateAsync(Guid id, HerbInputDto input);
      Task<(bool success, string? error)> DeleteAsync(Guid id);
      Task<(bool success, HerbDetailDto? data, string? error)> GetByIdAsync(Guid id);
  }
  ```
- **验收**: 接口定义符合CommandHandler规范

### Task 1.2: 创建HerbCommandHandler实现
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Services/HerbCommandHandler.cs`
- **内容**:
  - 依赖IHerbRepository
  - 无状态设计（无Current/HasChanges属性）
  - 统一返回tuple类型
  - 所有日志添加[CMD]前缀
  - 统一异常处理
- **验收**: 实现符合CommandHandler规范，日志格式正确

---

## Phase 2: 重构HerbMasterDetailViewModel

### Task 2.1: 分析当前ViewModel依赖
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbMasterDetailViewModel.cs`
- **当前依赖**:
  - `IHerbDataManager _dataManager` (待移除)
  - `IHerbRepository _herbRepository` (待移除，改用CommandHandler)
- **目标依赖**:
  - `IHerbCommandHandler _commandHandler`

### Task 2.2: 重构ViewModel依赖注入
- **内容**:
  - 移除`_dataManager`字段和构造函数参数
  - 移除`_herbRepository`字段和构造函数参数
  - 添加`_commandHandler`字段和构造函数参数
- **验收**: 构造函数仅依赖IHerbCommandHandler

### Task 2.3: 重构SaveDetailAsync方法
- **原实现**: 调用`_dataManager.SaveAsync()`或`_dataManager.CreateAsync()`
- **新实现**:
  ```csharp
  protected override async Task<bool> SaveDetailAsync(HerbDetailDto detail)
  {
      var input = MapToInputDto(detail);
      var result = detail.Id == Guid.Empty
          ? await _commandHandler.CreateAsync(input)
          : await _commandHandler.UpdateAsync(detail.Id, input);

      if (!result.success)
          ErrorMessage = result.error;

      return result.success;
  }
  ```
- **验收**: 保存功能正常

### Task 2.4: 重构DeleteDetailAsync方法
- **原实现**: 调用`_dataManager.DeleteAsync()`
- **新实现**:
  ```csharp
  protected override async Task<bool> DeleteDetailAsync(HerbDetailDto detail)
  {
      var result = await _commandHandler.DeleteAsync(detail.Id);
      if (!result.success)
          ErrorMessage = result.error;
      return result.success;
  }
  ```
- **验收**: 删除功能正常

### Task 2.5: 重构LoadDetailAsync方法
- **原实现**: 调用`_herbRepository.GetByIdAsync()`
- **新实现**:
  ```csharp
  protected override async Task<HerbDetailDto?> LoadDetailAsync(HerbListDto item)
  {
      var result = await _commandHandler.GetByIdAsync(item.Id);
      if (!result.success)
          ErrorMessage = result.error;
      return result.data;
  }
  ```
- **验收**: 加载详情功能正常

---

## Phase 3: 更新DI注册

### Task 3.1: 更新HerbsModule注册
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/HerbsModule.cs`
- **内容**:
  - 移除`IHerbDataManager`注册
  - 添加`IHerbCommandHandler`注册
  ```csharp
  containerRegistry.RegisterScoped<IHerbCommandHandler, HerbCommandHandler>();
  ```
- **验收**: DI注册正确

---

## Phase 4: 清理废弃代码

### Task 4.1: 删除HerbDataManager相关文件
- **删除文件**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Services/HerbDataManager.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Contracts/IHerbDataManager.cs`
- **验收**: 文件已删除

### Task 4.2: 删除IDataManager基接口
- **文件**: 搜索并删除`IDataManager.cs`
- **位置**: 可能在`LYBT.Desktop.Contracts`或`LYBT.Desktop.Herbs`
- **验收**: 接口已删除，无编译错误

### Task 4.3: 全局搜索清理遗漏引用
- **搜索关键词**:
  - `IDataManager`
  - `IHerbDataManager`
  - `HerbDataManager`
- **验收**: 无遗漏引用

---

## Phase 5: 验证

### Task 5.1: 编译验证
- **命令**: `dotnet build LYBT.All.sln`
- **验收**: 零错误、零警告

### Task 5.2: 功能验证
- **测试项**:
  - [ ] Herbs列表加载
  - [ ] Herbs详情查看
  - [ ] Herbs新增
  - [ ] Herbs编辑保存
  - [ ] Herbs删除
- **验收**: 所有功能正常

### Task 5.3: 日志验证
- **检查项**:
  - [ ] CreateHerb日志包含[CMD]前缀
  - [ ] UpdateHerb日志包含[CMD]前缀
  - [ ] DeleteHerb日志包含[CMD]前缀
- **验收**: 日志格式符合规范

---

## 完成标准

### 代码质量
- [ ] 编译通过，零错误零警告
- [ ] IDataManager接口已删除
- [ ] IHerbDataManager接口已删除
- [ ] HerbDataManager实现已删除
- [ ] HerbCommandHandler已创建并注册
- [ ] HerbMasterDetailViewModel已重构

### 功能验证
- [ ] Herbs模块CRUD功能正常
- [ ] 日志输出包含[CMD]前缀

### 架构合规
- [ ] CommandHandler无状态设计
- [ ] 统一返回`(bool, T?, string?)` tuple
- [ ] ViewModel仅依赖CommandHandler

---

## 预估工作量

| Phase | 任务数 | 复杂度 |
|-------|--------|--------|
| Phase 1: 创建CommandHandler | 2 | 中 |
| Phase 2: 重构ViewModel | 5 | 高 |
| Phase 3: 更新DI | 1 | 低 |
| Phase 4: 清理代码 | 3 | 低 |
| Phase 5: 验证 | 3 | 中 |

**总计**: 14个任务

---

## 风险缓解

| 风险点 | 缓解措施 |
|--------|----------|
| ViewModel重构 | 保持方法签名不变，仅改内部实现 |
| 遗漏依赖 | 全局搜索关键词确认 |
| 运行时异常 | 编译验证 + 手动功能测试 |
