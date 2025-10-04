# 诊疗台UI需求完整规格（最终版）

## 记录时间：2025-09-29
## 记录人：项目经理需求调研（含所有补充）
## 版本：v3.0 - 最终确认版

## 一、总体架构设计

### 1.1 角色驱动的工作台架构
- **统一登录界面** → 根据用户角色自动跳转到对应工作台
- **医生角色** → 诊疗台（Clinical Workstation）
- **管理员角色** → 管理台（Admin Console）
- **后期其他角色** → 对应的专用工作台
- **扩展性设计** → 角色驱动的工作台路由机制

### 1.2 管理台功能模块
管理台包含以下管理模块（标准增删查改）：
1. **用户管理** - 用户账号的增删查改
2. **药材管理** - 药材基础数据管理
3. **患者管理** - 患者档案管理
4. **验方管理** - 方剂模板管理
5. **病历管理** - 查看所有病历（含诊断和处方）

### 1.3 诊疗台响应式布局策略（简化版）
- **统一布局**: Tab切换模式 (诊断Tab + 处方Tab)
- **响应式调整**: 根据屏幕大小自动调整控件尺寸
- **不做并排显示**: 所有分辨率均使用Tab切换
- **控件自适应**: 自动缩放以适配不同分辨率屏幕

### 1.3 核心模块
1. **诊断模块** - 基于现有设计框架 + 历史诊断列表
2. **处方模块** - 新的4×6可编辑网格设计 + 历史处方列表

## 二、诊断模块设计（增强版）

### 2.1 三列布局结构
```
┌─────────────────┬─────────────────┬─────────────────┐
│     四诊录入     │   历史诊断列表   │   诊断详情查看   │
│                 │                 │                 │
│  ┌───┬───┐     │ 2025-09-28     │   选中记录的     │
│  │望 │闻 │     │ 感冒/风寒证     │   详细内容显示   │
│  ├───┼───┤     │ ═══════════    │                 │
│  │问 │切 │     │ 2025-09-20     │   [导入此诊断]   │
│  └───┴───┘     │ 咳嗽/肺热证     │   按钮           │
│                 │ ═══════════    │                 │
│  主诉: ______   │ 2025-09-15     │                 │
│  现病史: ____   │ 胃痛/脾胃虚寒   │                 │
│                 │ ═══════════    │                 │
│  诊断: ______   │ [更多记录...]   │                 │
│  备注: ______   │                │                 │
└─────────────────┴─────────────────┴─────────────────┘
```

### 2.2 历史诊断功能
#### 2.2.1 历史记录显示
- 进入界面时自动加载第一页（10条记录）
- 分页控件：上一页、下一页、页码显示
- 列表显示：日期 + 主诉 + 诊断摘要
- 单击选中 → 右侧显示详细内容
- 显示诊疗医生信息

#### 2.2.2 诊断导入功能
- 双击或点击"导入此诊断"按钮
- 确认后导入到当前诊断表单
- 医生可在导入基础上修改
- 保持诊断来源的追踪记录

### 2.3 字段特性
- 四诊(望闻问切)：可选填，2×2网格布局
- 主诉+现病史：可选填，1×2布局
- 诊断结果：大输入框
- 备注/医嘱：底部区域
- 所有字段均为非必填

## 三、处方模块设计（增强版）

### 3.1 整体布局（带历史面板）
```
┌─────────────────────────────────────────────────────────┐
│  🔍 [搜索药物] [验方导入▼] [历史导入▼] [清空]            │ 搜索区
├─────────────────────────────────────────────────────────┤
│ 处方网格 (4×6)                    │   历史处方列表    │
│ ┌───────────┬───────────┬───────  │ ┌─────────────────┐ │
│ │当归   12g │白芍   15g │川芎  6g │ │ 2025-09-28     │ │
│ ├───────────┼───────────┼───────  │ │ 感冒方(7味药)   │ │
│ │党参   15g │白术   12g │茯苓 15g │ │ ═══════════    │ │
│ ├───────────┼───────────┼───────  │ │ 2025-09-20     │ │
│ │             │             │             │ │ 止咳散(9味药)   │ │
│ ├───────────┼───────────┼───────  │ │ ═══════════    │ │
│ │             │             │             │ │ [查看详情]     │ │
│ └───────────┴───────────┴───────  │ │ [导入此方]     │ │
│                                   │ └─────────────────┘ │
│ 煎煮说明：[水煎服，一日一剂，分两次温服_______]     │
│ [保存处方] [打印处方] [清空] [添加空行]                    │
└─────────────────────────────────────────────────────────┘
```

### 3.2 处方网格规格
- **基础规格**: 4列×6行 = 24个药位
- **自动扩展**: 第6行有药材时自动添加第7行，第7行有药材时自动添加第8行，依此类推
- **显示格式**: 药名+剂量（如：当归 12g）
- **打印处理**: 头部（患者信息）在第一页顶部，药材列表可跨页，尾部（剂数、签名、价格）在最后页底部

### 3.3 五种药材添加方式

#### 3.3.1 直接编辑模式
1. **双击编辑**: 双击格子直接进入编辑模式
2. **光标选中**: 光标选中单元格 + 回车进入编辑模式
3. **编辑流程**: 药名 → 回车确定 → 自动跳转到剂量字段
4. **循环顺序**: 药材→剂量→药材→剂量...

#### 3.3.2 搜索添加模式
- 搜索框输入药材名称
- 支持拼音码快速匹配
- 回车快速添加到空白格子

#### 3.3.3 验方导入模式
- 下拉选择验方模板
- 支持多次导入不同方剂
- 记录导入来源追踪
- 重复药材冲突处理

#### 3.3.4 历史导入模式
- 从患者历史处方导入
- 查看历史处方详情
- 一键导入整个处方
- 支持修改后保存

#### 3.3.5 批量添加模式
- 弹窗连续录入
- 纯键盘操作
- 药名→回车→剂量→回车→循环

### 3.4 历史处方功能

#### 3.4.1 历史记录显示
- 进入界面时自动加载第一页（10条记录）
- 分页控件：上一页、下一页、页码显示
- 列表显示：日期 + 处方摘要 + 药味数量
- 显示开方医生和总价信息

#### 3.4.2 处方导入功能
- 点击"查看详情" → 弹窗显示完整处方内容
- 点击"导入此方" → 检查冲突 → 应用导入规则 → 更新网格
- 支持选择性导入部分药材

### 3.5 药材冲突处理机制

#### 3.5.1 冲突检测
- 自动检测重复药材
- 比较现有剂量与导入剂量
- 前端弹窗显示冲突详情

#### 3.5.2 冲突处理规则
- **默认规则**: 系统配置中设置（取最小值或取最大值）
- **个人偏好**: 医生可在配置中设置自己的默认冲突处理策略
- **用户选择**: 保持现有/使用导入/取最小值/取最大值
- **批量应用**: 相同规则应用到所有冲突项

#### 3.5.3 冲突处理界面
```
┌─────────────────────────────────────────┐
│           处方导入冲突处理               │
├─────────────────────────────────────────┤
│ 发现重复药材，请选择处理方式：           │
│                                         │
│ 当归: 现有 15g → 导入 12g              │
│ ○ 保持现有(15g) ○ 使用导入(12g)        │
│ ○ 取最小值(12g) ● 取最大值(15g)        │
│                                         │
│ 白术: 现有 10g → 导入 12g              │
│ ○ 保持现有(10g) ● 使用导入(12g)        │
│ ● 取最小值(10g) ○ 取最大值(12g)        │
│                                         │
│ □ 应用相同规则到所有冲突项               │
│                                         │
│ [确定导入] [取消] [应用默认规则(最小值)]  │
└─────────────────────────────────────────┘
```

### 3.6 来源追踪功能
- **导入记录**: 记录每次导入的来源（验方名称/历史处方日期）
- **多次导入**: 支持多次导入，保持所有来源的记录
- **显示追踪**: 在处方详情中显示药材来源
- **打印包含**: 打印处方时可选择是否包含来源信息

### 3.7 导航方式（兼容设计）
- **顺序导航**: Tab/回车按从左到右、从上到下顺序移动
- **方向键导航**: 上下左右箭头键在网格中自由移动焦点
- **混合模式**: 两种导航方式同时支持，提供最大灵活性

## 四、技术架构与集成

### 4.1 业务模型集成
- **复用现有实体**: 基于MedicalCase→Consultation→Prescription的一对一关系
- **数据兼容性**: 新UI完全兼容现有业务逻辑和数据结构
- **服务层复用**: 集成现有IConsultationService和IPrescriptionService

### 4.2 组件复用策略
1. **患者管理** - 完全复用现有Patients模块
2. **药材选择** - 复用现有HerbSelectionDialog和相关服务
3. **方剂导入** - 复用现有方剂管理功能
4. **权限控制** - 沿用MVP的当天可改、过期锁定规则

### 4.3 新增组件需求
- **ClinicalWorkstationView** - 诊疗台主界面
- **AdminConsoleView** - 管理台主界面
- **PrescriptionGridControl** - 4×6网格自定义控件
- **ResponsiveLayoutControl** - 响应式布局容器
- **HistoryPanelControl** - 历史记录面板控件

### 4.4 响应式适配技术
- 统一使用Tab切换布局（不做并排显示）
- DPI自动缩放字体和控件大小
- 控件根据屏幕大小自适应调整
- 支持1080p到4K的主流分辨率

## 五、数据模型扩展

### 5.1 历史记录数据结构
```csharp
// 历史诊断记录
public class HistoricalConsultationDto
{
    public Guid ConsultationId { get; set; }
    public DateTime ConsultationDate { get; set; }
    public string ChiefComplaint { get; set; }
    public string Diagnosis { get; set; }
    public string DoctorName { get; set; }
    // 完整诊断内容
    public string Inspection { get; set; }     // 望诊
    public string Auscultation { get; set; }   // 闻诊
    public string Inquiry { get; set; }        // 问诊
    public string Palpation { get; set; }      // 切诊
    public string PresentIllness { get; set; } // 现病史
    public string Remarks { get; set; }        // 备注
}

// 历史处方记录
public class HistoricalPrescriptionDto
{
    public Guid PrescriptionId { get; set; }
    public DateTime PrescriptionDate { get; set; }
    public string Summary { get; set; }        // 处方摘要
    public int HerbCount { get; set; }         // 药味数量
    public decimal TotalPrice { get; set; }    // 总价
    public string DoctorName { get; set; }     // 开方医生
    public List<PrescriptionItemDto> Items { get; set; }
    public string Instructions { get; set; }   // 煎煮说明
    public int DosageCount { get; set; }       // 剂数
}
```

### 5.2 导入冲突处理模型
```csharp
public class ImportResult
{
    public bool Success { get; set; }
    public List<ConflictItem> Conflicts { get; set; }
    public string Message { get; set; }
    public List<string> ImportSources { get; set; } // 来源追踪
}

public class ConflictItem
{
    public string HerbName { get; set; }
    public string ExistingDosage { get; set; }
    public string ImportingDosage { get; set; }
    public ConflictResolution Resolution { get; set; }
}

public enum ConflictResolution
{
    KeepExisting,   // 保持现有
    UseImporting,   // 使用导入的
    UseMinimum,     // 取最小值（默认规则）
    UseMaximum,     // 取最大值
    UserDecide      // 用户决定
}
```

## 六、服务接口设计

### 6.1 历史记录服务
```csharp
public interface IPatientHistoryService
{
    // 历史诊断记录
    Task<List<HistoricalConsultationDto>> GetPatientConsultationHistoryAsync(Guid patientId, int maxRecords = 10);
    Task<HistoricalConsultationDto> GetConsultationDetailsAsync(Guid consultationId);
    Task<bool> ImportConsultationDataAsync(Guid currentMedicalCaseId, Guid sourceConsultationId);

    // 历史处方记录
    Task<List<HistoricalPrescriptionDto>> GetPatientPrescriptionHistoryAsync(Guid patientId, int maxRecords = 10);
    Task<HistoricalPrescriptionDto> GetPrescriptionDetailsAsync(Guid prescriptionId);
    Task<ImportResult> ImportPrescriptionDataAsync(PrescriptionGridDto currentGrid, Guid sourcePrescriptionId);

    // 导入冲突处理
    Task<ImportResult> ImportFormulaWithConflictHandlingAsync(PrescriptionGridDto currentGrid, Guid formulaId);
    Task<bool> ResolveImportConflictsAsync(PrescriptionGridDto grid, List<ConflictItem> conflicts);
}
```

### 6.2 诊疗台服务增强
```csharp
public interface IClinicalWorkstationService
{
    // 会话管理
    Task<ClinicalWorkstationSessionDto> StartSessionAsync(Guid patientId);
    Task<bool> SaveSessionAsync(ClinicalWorkstationSessionDto session);
    Task<bool> CompleteSessionAsync(Guid sessionId);

    // 历史记录加载
    Task<List<HistoricalConsultationDto>> LoadPatientConsultationHistoryAsync(Guid patientId);
    Task<List<HistoricalPrescriptionDto>> LoadPatientPrescriptionHistoryAsync(Guid patientId);

    // 导入功能
    Task<bool> ImportHistoricalConsultationAsync(Guid currentMedicalCaseId, Guid sourceConsultationId);
    Task<ImportResult> ImportHistoricalPrescriptionAsync(PrescriptionGridDto currentGrid, Guid sourcePrescriptionId);
    Task<ImportResult> ImportFormulaAsync(PrescriptionGridDto currentGrid, Guid formulaId);
}
```

## 七、用户体验优先级

1. **功能可用性** - 确保所有屏幕尺寸下功能正常
2. **操作效率** - 快速录入，减少鼠标依赖
3. **历史记录便利性** - 快速查看和导入历史数据
4. **冲突处理友好性** - 清晰的冲突提示和处理选项
5. **视觉协调** - 美观且专业的医疗界面

## 七、数据处理策略

### 7.1 数据加载
- **历史记录**: 进入界面时加载第一页（10条）
- **翻页加载**: 点击分页按钮时加载对应页数据

### 7.2 数据保存
- **保存方式**: 手动保存（医生点击保存按钮）
- **暂存功能**: 支持暂存草稿状态

### 7.3 离线处理
- **MVP版本**: 不做本地缓存
- **网络断开**: 提示用户检查网络连接

## 八、开发实施阶段

### 阶段1: 基础架构搭建（3天）
- 角色驱动的工作台路由
- 响应式布局框架
- 诊疗台服务集成

### 阶段2: 诊断模块实现（3天）
- 三列布局界面
- 历史诊断列表和详情
- 诊断导入功能

### 阶段3: 处方网格开发（4天）
- 4×6网格控件
- 键盘导航
- 五种添加方式集成

### 阶段4: 历史处方功能（3天）
- 历史处方列表
- 处方详情查看
- 导入和冲突处理

### 阶段5: 完善和优化（2.5天）
- 来源追踪功能
- 界面优化
- 集成测试

**总工期：15.5天（约3.1周）**

## 九、验收标准

### 9.1 功能完整性
- 完整的诊疗流程（诊断+处方）
- 五种药材添加方式都能正常使用
- 历史记录查看和导入功能正常
- 冲突处理机制工作正常

### 9.2 响应式适配
- 支持1080p到4K分辨率
- Tab切换模式在所有分辨率下正常工作
- 控件自适应调整，界面在不同分辨率下显示美观

### 9.3 操作效率
- 支持纯键盘操作完成处方录入
- 历史记录查看响应<500ms
- 导入操作流畅无卡顿

### 9.4 数据准确性
- 导入数据与原始数据一致
- 冲突处理结果正确
- 来源追踪记录完整

---

**状态**: 需求调研完成（包含历史记录和管理台补充）
**下一步**: 基于本文档制定详细技术实现方案和开发计划