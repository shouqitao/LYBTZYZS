# shell-layout Specification

## Purpose
TBD - created by archiving change remove-statusbar-relocate-status. Update Purpose after archive.
## Requirements
### Requirement: REQ-SHELL-001 无底部状态栏

主窗口 SHALL NOT 包含底部状态栏，所有状态信息应整合到其他界面元素中。

**Acceptance Criteria:**
- 主窗口Grid仅包含主内容区，无固定高度的状态栏行
- 窗口垂直空间100%用于内容显示
- 原状态栏信息(API状态、时间)移至其他位置

#### Scenario: 窗口布局无状态栏
- **GIVEN** 程序启动
- **WHEN** 主窗口显示
- **THEN** 窗口底部无状态栏
- **AND** 内容区域占满整个窗口高度

---

### Requirement: REQ-SHELL-002 顶部工具栏状态集成

登录后的顶部工具栏 SHALL 在右侧集成时间显示和API状态指示器。

**Acceptance Criteria:**
- 顶部工具栏高度60px
- 左侧显示系统名称
- 右侧从左到右依次为：时间 -> API状态 -> 用户名 -> 退出登录按钮
- 时间格式为HH:mm
- API状态使用圆点指示器 + 文字标签

#### Scenario: 顶部栏右侧布局
- **GIVEN** 用户已登录
- **WHEN** 用户查看顶部工具栏
- **THEN** 右侧显示当前时间(HH:mm格式)
- **AND** 时间右侧显示API状态指示器
- **AND** API状态右侧显示当前用户名
- **AND** 最右侧显示退出登录按钮

#### Scenario: 顶部栏API状态显示正常
- **GIVEN** 用户已登录
- **AND** API服务连接正常
- **WHEN** 用户查看顶部工具栏
- **THEN** API状态显示绿色圆点
- **AND** 圆点旁显示"正常"文字

#### Scenario: 顶部栏API状态失败
- **GIVEN** 用户已登录
- **AND** API服务连接失败
- **WHEN** 用户查看顶部工具栏
- **THEN** API状态显示红色圆点
- **AND** 圆点旁显示"连接失败"文字
- **AND** 显示"重试"链接按钮

---

### Requirement: REQ-SHELL-003 时间显示规范

顶部工具栏的时间显示 SHALL 使用简洁的HH:mm格式。

**Acceptance Criteria:**
- 时间格式：24小时制，HH:mm
- 不显示秒数
- 字体大小14px，与用户名一致
- 每分钟更新一次

#### Scenario: 时间显示格式
- **GIVEN** 用户已登录
- **AND** 当前时间为14:30:45
- **WHEN** 用户查看顶部工具栏
- **THEN** 时间显示为"14:30"
- **AND** 不显示秒数

