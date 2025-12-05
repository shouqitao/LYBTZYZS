# Spec: login-ui

## Overview

登录界面UI规范，定义无边框窗口、关闭按钮行为和程序退出策略。

**核心原则**: 程序退出入口**仅限登录界面**，已登录用户必须先退出登录才能关闭程序。

---

## ADDED Requirements

### Requirement: REQ-LOGIN-UI-001 无边框窗口

主窗口 SHALL 移除系统标题栏，实现无边框全屏界面。

**Acceptance Criteria:**
- 窗口无系统标题栏（无标题、无最小化/最大化/关闭按钮）
- 窗口保持最大化状态
- 窗口不可调整大小
- 窗口不可拖动移动

#### Scenario: 程序启动显示无边框窗口
```gherkin
Given 用户启动程序
When 主窗口显示
Then 窗口无系统标题栏
And 窗口为最大化状态
And 窗口填满整个屏幕
```

---

### Requirement: REQ-LOGIN-UI-002 登录框关闭按钮

登录界面的登录框右上角 SHALL 显示关闭按钮，作为**唯一的程序退出入口**。

**Acceptance Criteria:**
- 关闭按钮位于登录框右上角
- 按钮显示"X"符号
- 鼠标悬停时按钮背景变为红色
- 点击按钮直接退出程序（无需logout，因为仅在登录界面可见）

#### Scenario: 关闭按钮视觉样式
```gherkin
Given 用户在登录界面
When 用户查看登录框
Then 登录框右上角显示关闭按钮
And 按钮显示"X"符号
And 按钮为圆角矩形或圆形样式
```

#### Scenario: 关闭按钮悬停效果
```gherkin
Given 用户在登录界面
When 用户将鼠标悬停在关闭按钮上
Then 按钮背景变为红色
And 按钮文字变为白色
```

#### Scenario: 点击关闭按钮退出程序
```gherkin
Given 用户在登录界面
When 用户点击关闭按钮
Then 程序直接退出
And 无残留进程
```

---

### Requirement: REQ-LOGIN-UI-003 Alt+F4快捷键控制

系统 SHALL 根据当前界面状态控制Alt+F4行为：登录界面允许退出，工作台界面阻止退出。

**Acceptance Criteria:**
- 登录界面：Alt+F4可正常退出程序
- 工作台界面：Alt+F4被拦截，无任何响应
- 已登录用户必须先点击"退出登录"返回登录界面，才能关闭程序

#### Scenario: 登录界面使用Alt+F4退出
```gherkin
Given 用户在登录界面
And 用户尚未登录
When 用户按下Alt+F4
Then 程序退出
```

#### Scenario: 工作台界面Alt+F4被阻止
```gherkin
Given 用户已登录
And 用户在工作台界面
When 用户按下Alt+F4
Then 按键被拦截
And 程序继续运行
And 无任何提示或响应
```

#### Scenario: 已登录用户退出流程
```gherkin
Given 用户已登录
And 用户在工作台界面
When 用户想要关闭程序
Then 用户必须先点击"退出登录"按钮
And 系统返回登录界面
And 用户可通过关闭按钮或Alt+F4退出程序
```

---

### Requirement: REQ-LOGIN-UI-004 复选框布局优化

登录界面的"记住用户名"和"记住密码"复选框 SHALL 水平对齐在同一行。

**Acceptance Criteria:**
- 两个复选框在同一水平线上
- "记住密码"后显示警告文字"仅在可信设备使用"
- 勾选"记住密码"时自动勾选"记住用户名"

#### Scenario: 复选框水平对齐
```gherkin
Given 用户在登录界面
When 用户查看登录框
Then "记住用户名"和"记住密码"在同一行
And "记住密码"后显示橙色警告文字
```

#### Scenario: 记住密码联动记住用户名
```gherkin
Given 用户在登录界面
And "记住用户名"未勾选
When 用户勾选"记住密码"
Then "记住用户名"自动被勾选
```

---

## Related Requirements

- `authentication/REQ-AUTH-002`: 退出登录功能
- `ui-style-conventions/REQ-STYLE-001`: 按钮样式规范
