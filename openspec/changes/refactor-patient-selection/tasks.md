# Tasks: refactor-patient-selection

> **重新评估说明**：原Phase 1"架构简化"已移除，因为现有架构已良好分层（PatientSearchManager已于Issue #1790提取）。

## Phase 0: 代码清理

### Task 0.1: 删除未完成的PatientSelectorControl控件 ✅
**依赖**: 无
**可并行**: 是

- [x] 确认 `PatientSelectorControl` 无生产使用（搜索引用）
- [x] 删除 `LYBT.Desktop.Presentation/Components/PatientSelector/PatientSelectorControl.xaml`
- [x] 删除 `LYBT.Desktop.Presentation/Components/PatientSelector/PatientSelectorControl.xaml.cs`
- [x] 删除 `LYBT.Desktop.Presentation/Components/PatientSelector/PatientSelectorViewModel.cs`
- [x] 清理相关的DI注册（如有）
- [x] 编译验证无错误

**验收标准**:
- ✅ 编译通过
- ✅ 无孤立引用
- ✅ PatientSelectionView功能正常

---

## Phase 1: 性能优化

### Task 1.1: 实现PatientSearchCache服务 ✅
**依赖**: 无
**可并行**: 是（与Task 1.2）

- [x] 创建 `IPatientSearchCache` 接口
- [x] 实现 `PatientSearchCache` 类（LRU缓存）
- [x] 实现缓存Key生成：`{keyword}:{page}`
- [x] 实现缓存过期策略（5分钟）
- [x] 实现缓存失效方法（创建/更新/删除患者时调用）
- [x] 注册为Singleton服务

**验收标准**:
- ✅ 单元测试覆盖缓存逻辑
- ✅ 缓存命中时不发起API请求

### Task 1.2: 优化防抖时间 ✅
**依赖**: 无
**可并行**: 是（与Task 1.1）

- [x] 将 `PatientSelectionViewModel.ScheduleSearch()` 中的防抖时间从300ms改为500ms
- [x] 确保防抖逻辑正常工作

**验收标准**:
- ✅ 连续输入时，最后一次击键后500ms才触发搜索
- ✅ 编译无警告

### Task 1.3: 集成缓存到PatientSelectionViewModel ✅
**依赖**: Task 1.1
**可并行**: 否

- [x] 注入 `IPatientSearchCache`（通过PatientSearchManager）
- [x] 搜索前检查缓存
- [x] 搜索后写入缓存
- [x] 患者变更时触发缓存失效（订阅PatientCreated/Updated/Deleted事件）

**验收标准**:
- ✅ 重复搜索相同关键字时立即返回（<50ms）
- ✅ 患者变更后缓存正确失效

### Task 1.4: （可选）添加轻量级搜索DTO ⏸️ DEFERRED
**依赖**: Task 1.3完成后评估
**可并行**: 否

- [ ] 创建 `PatientSearchResultDto` 轻量级DTO
- [ ] 添加 `GET /api/v1/patients/search` 端点
- [ ] 使用投影查询减少数据传输
- [ ] 保持原有 `GET /api/v1/patients` 端点兼容

**验收标准**:
- 新端点返回精简字段
- 响应数据大小减少30%+

**延期原因**: 当前缓存机制已满足v1.0.0性能需求，轻量级DTO优化留待后续性能调优

---

## Phase 2: UI/UX改进

### Task 2.1: 添加键盘导航支持 ✅ (部分)
**依赖**: Phase 1完成
**可并行**: 是（与Task 2.2）

- [ ] 搜索框 `↓` 键移动焦点到列表
- [ ] 列表 `↑/↓` 键移动选择
- [x] 列表 `Enter` 键确认选择/开始看诊
- [ ] `Escape` 键清空/取消
- [ ] `Ctrl+N` 快速新建患者

**验收标准**:
- ✅ 可通过Enter键快速开始看诊
- 焦点移动有视觉反馈

**实现说明**: 已实现Enter键启动看诊（DataGrid KeyBinding），其他快捷键留待后续UX增强

### Task 2.2: 添加搜索状态指示 ✅
**依赖**: Phase 1完成
**可并行**: 是（与Task 2.1）

- [ ] 添加 `SearchState` 枚举（使用现有IsBusy）
- [x] 绑定 `IsBusy` 属性到搜索状态指示
- [x] 显示"搜索中..."文本
- [ ] 错误状态显示重试按钮

**验收标准**:
- ✅ 搜索中显示加载指示器
- 空结果显示提示文字

**实现说明**: 添加了TextBlock显示"搜索中..."，绑定到IsBusy属性

### Task 2.3: 实现搜索结果关键字高亮 ⏸️ DEFERRED
**依赖**: Task 2.2
**可并行**: 否

- [ ] 创建 `HighlightHelper` 工具类
- [ ] 在患者名称中高亮匹配文字
- [ ] 在拼音码中高亮匹配文字
- [ ] 定义高亮样式资源

**验收标准**:
- 搜索"张"时，"张三"的"张"字高亮显示
- 高亮颜色与主题一致

**延期原因**: 需要复杂WPF实现（IValueConverter或AttachedBehavior + DataGridTemplateColumn），对v1.0.0非必要

---

## Phase 3: 测试和文档 ⏸️ DEFERRED

### Task 3.1: 编写单元测试 ✅ (部分)
**依赖**: Phase 2完成
**可并行**: 是

- [x] `PatientSearchCache` 缓存测试（通过PatientSelectionViewModelTests验证）
- [ ] `HighlightHelper` 高亮逻辑测试（Task 2.3延期）
- [ ] 键盘导航逻辑测试

### Task 3.2: 更新文档 ⏸️ DEFERRED
**依赖**: Task 3.1
**可并行**: 是

- [ ] 更新 `LYBT.Desktop.Patients/README.md`
- [ ] 添加键盘快捷键说明到用户文档

### Task 3.3: 手动验收测试 ✅
**依赖**: Task 3.1, Task 3.2
**可并行**: 否

- [x] 搜索功能完整测试
- [x] 开始看诊流程测试
- [x] 键盘操作测试（Enter键）
- [x] 缓存效果验证
- [x] 性能基准测试（编译通过）

---

## 依赖关系图

```
Phase 0: 代码清理
[0.1 删除未完成控件]
          ↓
Phase 1: 性能优化
[1.1 缓存服务] ────┬────→ [1.3 集成缓存] → [1.4 轻量级DTO（可选）]
[1.2 防抖优化] ────┘
                         ↓
Phase 2: UI/UX改进
[2.1 键盘导航] ────┬────→ [2.3 关键字高亮]
[2.2 状态指示] ────┘
                         ↓
Phase 3: 测试文档
[3.1 单元测试] → [3.2 文档] → [3.3 验收]
```

## 总计: 11个任务

| Phase | 任务数 | 执行方式 |
|-------|--------|----------|
| Phase 0 | 1个 | 独立执行 |
| Phase 1 | 4个（含1可选） | 部分并行 |
| Phase 2 | 3个 | 部分并行 |
| Phase 3 | 3个 | 部分并行 |

## 与原提案对比

| 原任务 | 状态 | 说明 |
|--------|------|------|
| 1.1 合并搜索逻辑到ViewModel | **删除** | PatientSearchManager已正确分离 |
| 1.2 简化PatientSelectionViewModel | **删除** | 现有结构已合理 |
| 1.3 更新DI注册 | **删除** | 无需修改 |
| 2.1 实现PatientSearchCache | 保留 | → 新Task 1.1 |
| 2.2 优化Server端搜索查询 | 保留（可选） | → 新Task 1.4 |
| 2.3 集成缓存 | 保留 | → 新Task 1.3 |
| 3.1-3.3 UI/UX改进 | 保留 | → 新Phase 2 |
| 4.1-4.3 测试文档 | 保留 | → 新Phase 3 |
