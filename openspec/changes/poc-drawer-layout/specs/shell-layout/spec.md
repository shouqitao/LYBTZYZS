## ADDED Requirements

### Requirement: REQ-SHELL-POC-001 隐藏式Drawer导航布局

**注意**: 这是POC验证需求，用于评估方案D的可行性。

登录后的Shell布局 SHALL 使用隐藏式Drawer导航，实现工作区空间最大化。

**Acceptance Criteria:**
- 默认状态下无固定导航占用，工作区占据100%窗口空间
- 左上角显示汉堡按钮（48x48px）触发Drawer
- 点击汉堡按钮，Drawer从左侧滑入（240px宽度）
- Drawer展开时，内容区显示半透明遮罩
- 点击遮罩或按Escape键关闭Drawer
- Ctrl+M快捷键切换Drawer开关状态

#### Scenario: 默认状态工作区最大化
- **GIVEN** 用户已登录
- **WHEN** Drawer处于关闭状态
- **THEN** 工作区占据整个窗口（除汉堡按钮区域外）
- **AND** Shell层固定占用为0px

#### Scenario: Drawer展开
- **GIVEN** 用户已登录
- **AND** Drawer处于关闭状态
- **WHEN** 用户点击汉堡按钮
- **THEN** Drawer从左侧滑入（动画时长300ms）
- **AND** 内容区显示半透明遮罩
- **AND** Drawer宽度为240px

#### Scenario: Drawer关闭
- **GIVEN** Drawer处于展开状态
- **WHEN** 用户点击遮罩区域
- **THEN** Drawer滑出隐藏（动画时长300ms）
- **AND** 遮罩消失
- **AND** 工作区恢复100%空间

#### Scenario: 快捷键支持
- **GIVEN** 用户已登录
- **WHEN** 用户按Ctrl+M
- **THEN** Drawer切换开关状态（关闭→展开 或 展开→关闭）

### Requirement: REQ-SHELL-POC-002 Drawer内容布局

Drawer面板 SHALL 包含所有原Header功能，布局紧凑清晰。

**Acceptance Criteria:**
- 顶部显示Logo和系统名称
- 显示当前时间（HH:mm格式）
- 显示API状态指示器（圆点+文字）
- 显示用户信息（名称、角色）
- 提供"修改个人信息"入口
- 提供"修改密码"入口
- 底部固定"退出登录"按钮

#### Scenario: Drawer内容完整性
- **GIVEN** Drawer处于展开状态
- **WHEN** 用户查看Drawer内容
- **THEN** 显示系统Logo和名称"凌隐宝堂中医诊所"
- **AND** 显示当前时间
- **AND** 显示API状态（绿色正常/红色异常）
- **AND** 显示当前用户名称和角色
- **AND** 显示"修改个人信息"菜单项
- **AND** 显示"修改密码"菜单项
- **AND** 底部显示"退出登录"按钮
