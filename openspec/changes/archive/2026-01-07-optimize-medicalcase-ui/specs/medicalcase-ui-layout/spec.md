# medicalcase-ui-layout Delta

## MODIFIED Requirements

### Requirement: UI-LAYOUT-001 整体布局规范
系统 **SHALL** 采用25:75两列布局结构：左侧患者信息区(25%) + 右侧诊断处方区(75%)。

#### Scenario: 标准1920x1080分辨率布局
- **Given** 用户在1920x1080分辨率显示器上
- **When** 打开医案看诊界面
- **Then** 左侧患者信息区占25%宽度（包含PatientInfoCardControl和PendingQueueControl）
- **And** 右侧诊断处方区占75%宽度
- **And** Header仅显示标题和操作按钮，不重复显示患者信息

#### Scenario: 最小1366x768分辨率布局
- **Given** 用户在1366x768分辨率显示器上
- **When** 打开医案看诊界面
- **Then** 布局自适应调整，所有关键元素可见
- **And** 诊断区无滚动条，内容自适应填充

---

### Requirement: UI-LAYOUT-002 主内容区35:65分栏
系统 **SHALL** 将右侧主内容区分为上部诊断面板(35%)和下部处方面板(65%)，中间间距16px。

#### Scenario: 分栏比例
- **Given** 用户在医案看诊界面
- **When** 查看右侧主内容区
- **Then** 上部诊断面板占35%高度
- **And** 下部处方面板占65%高度
- **And** 两个面板之间有16px间距

---

### Requirement: UI-LAYOUT-004 诊断面板2x2网格布局
系统 **SHALL** 采用2x2网格布局显示4个诊断字段，无滚动条，自适应填充空间。

#### Scenario: 诊断面板字段布局
- **Given** 用户在诊断面板
- **When** 填写诊断信息
- **Then** 字段按2x2网格显示：现病史|中医诊断（上行）、舌诊|脉诊（下行）
- **And** 上行字段使用Star尺寸自适应填充空间
- **And** 下行字段使用Auto尺寸根据内容调整
- **And** 所有字段无滚动条
- **And** 底部显示"是否开处方"单选按钮组
- **And** 中医诊断为必填字段(带红色*标记)

---

## ADDED Requirements

### Requirement: UI-LAYOUT-009 待诊队列三种状态
系统 **SHALL** 在待诊队列中区分三种状态：等待(Waiting)、看诊中(InProgress)、挂起(Suspended)，并使用颜色区分。

#### Scenario: 等待状态显示
- **Given** 患者已挂号但医生未开始看诊
- **When** 查看待诊队列
- **Then** 该患者条目显示"等待"标签
- **And** 标签背景色为灰色(#E0E0E0)

#### Scenario: 看诊中状态显示
- **Given** 医生正在为患者进行诊疗
- **When** 查看待诊队列
- **Then** 该患者条目显示"看诊中"标签
- **And** 标签背景色为绿色(#4CAF50)

#### Scenario: 挂起状态显示
- **Given** 医生暂停了某患者的看诊
- **When** 查看待诊队列
- **Then** 该患者条目显示"挂起"标签
- **And** 标签背景色为橙色(#FF9800)

---

### Requirement: UI-LAYOUT-010 患者信息单一显示
系统 **SHALL** 确保患者信息仅在左侧PatientInfoCardControl显示一次，右侧Header不重复显示。

#### Scenario: 无重复患者信息
- **Given** 用户选择了患者开始看诊
- **When** 查看医案工作区界面
- **Then** 左侧PatientInfoCardControl显示完整患者信息
- **And** 右侧Header区域不显示患者姓名、性别、年龄等信息
