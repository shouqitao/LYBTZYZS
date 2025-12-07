# shell-layout Specification Delta

## MODIFIED Requirements

### Requirement: REQ-SHELL-001 无底部状态栏

主窗口 SHALL NOT 包含底部状态栏，所有状态信息应整合到侧边栏或顶部元素中。

**Acceptance Criteria:**
- 主窗口Grid仅包含主内容区，无固定高度的状态栏行
- 窗口垂直空间100%用于内容显示
- 原状态栏信息(API状态、时间)移至侧边栏底部

#### Scenario: 窗口布局无状态栏
- **GIVEN** 程序启动
- **WHEN** 主窗口显示
- **THEN** 窗口底部无状态栏
- **AND** 内容区域占满整个窗口高度

---

### Requirement: REQ-SHELL-002 侧边栏状态集成

登录后的侧边栏 SHALL 在底部区域集成时间显示和API状态指示器。

**Acceptance Criteria:**
- 侧边栏底部显示网络状态和时间
- 侧边栏可展开/收缩
- 收缩时仅显示图标，展开时显示完整信息
- 时间格式为HH:mm
- API状态使用圆点指示器 + 文字标签

#### Scenario: 侧边栏状态显示正常
- **GIVEN** 用户已登录
- **AND** API服务连接正常
- **WHEN** 用户查看侧边栏底部
- **THEN** 显示当前时间(HH:mm格式)
- **AND** 显示绿色圆点API状态指示器

#### Scenario: 侧边栏展开/收缩
- **GIVEN** 用户已登录
- **WHEN** 用户点击汉堡菜单按钮
- **THEN** 侧边栏在展开和收缩状态间切换
- **AND** 收缩时宽度为64px
- **AND** 展开时宽度为240px

---

### Requirement: REQ-SHELL-003 时间显示规范

侧边栏的时间显示 SHALL 使用简洁的HH:mm格式。

**Acceptance Criteria:**
- 时间格式：24小时制，HH:mm
- 不显示秒数
- 字体大小14px
- 每分钟更新一次

#### Scenario: 时间显示格式
- **GIVEN** 用户已登录
- **AND** 当前时间为14:30:45
- **WHEN** 用户查看侧边栏底部
- **THEN** 时间显示为"14:30"
- **AND** 不显示秒数

## ADDED Requirements

### Requirement: REQ-SHELL-004 健康检查协调器

系统 SHALL 使用独立的HealthCheckCoordinator服务管理API健康检查的调度和状态。

**Acceptance Criteria:**
- 健康检查逻辑从MainWindowViewModel分离到独立服务
- 健康检查间隔可配置(默认30秒)
- 支持手动触发重新检查
- 状态变更通过事件通知订阅者

#### Scenario: 定时健康检查
- **GIVEN** 用户已登录
- **AND** 系统正常运行
- **WHEN** 健康检查间隔时间到达
- **THEN** HealthCheckCoordinator自动执行健康检查
- **AND** 更新ApiStatus属性
- **AND** 触发状态变更事件

#### Scenario: 手动重试健康检查
- **GIVEN** 用户已登录
- **AND** API状态显示连接失败
- **WHEN** 用户点击重试按钮
- **THEN** HealthCheckCoordinator立即执行健康检查
- **AND** 更新显示最新状态
