# Spec: medicalcase-ui-layout

## Overview

医案看诊界面(MedicalCaseWorkspaceView)的UI布局规范，确保界面简洁大气、布局合理、操作符合用户习惯。

## Context

- **目标分辨率**: 1920x1080 (Full HD)
- **最小支持**: 1366x768
- **技术栈**: WPF + Prism MVVM (不引入第三方UI控件)
- **相关视图**: MedicalCaseWorkspaceView, ConsultationPanel, PrescriptionEditorPanel

---

## ADDED Requirements

### Requirement: UI-LAYOUT-001 整体布局规范
系统 **SHALL** 采用三行布局结构：顶部患者信息栏(50px) + 主内容区(自适应) + 底部操作栏(64px)。

#### Scenario: 标准1920x1080分辨率布局
- **Given** 用户在1920x1080分辨率显示器上
- **When** 打开医案看诊界面
- **Then** 顶部患者信息栏高度为50px
- **And** 底部操作栏高度为64px
- **And** 主内容区占用剩余空间

#### Scenario: 最小1366x768分辨率布局
- **Given** 用户在1366x768分辨率显示器上
- **When** 打开医案看诊界面
- **Then** 布局自适应调整，所有关键元素可见
- **And** 滚动功能正常工作

---

### Requirement: UI-LAYOUT-002 主内容区4:6分栏
系统 **SHALL** 将主内容区分为左侧诊断面板(40%)和右侧处方面板(60%)，中间间距16px。

#### Scenario: 分栏比例
- **Given** 用户在医案看诊界面
- **When** 查看主内容区
- **Then** 左侧诊断面板占40%宽度
- **And** 右侧处方面板占60%宽度
- **And** 两个面板之间有16px间距

---

### Requirement: UI-LAYOUT-003 统一颜色规范
系统 **SHALL** 使用统一的颜色方案：Primary(#2196F3蓝色)、Success(#4CAF50绿色)、Warning(#FF9800橙色)、Danger(#F44336红色)。

#### Scenario: 按钮颜色语义
- **Given** 用户在医案看诊界面
- **When** 查看操作按钮
- **Then** "完成看诊"按钮使用绿色(Success)背景
- **And** "打印处方笺"按钮使用蓝色(Primary)背景
- **And** "暂停看诊"按钮使用橙色(Warning)背景
- **And** "取消看诊"按钮使用红色(Danger)背景

---

### Requirement: UI-LAYOUT-004 诊断面板布局
系统 **SHALL** 将诊断面板操作按钮(保存草稿、确认诊断)固定在面板底部。

#### Scenario: 诊断面板按钮位置
- **Given** 用户在诊断面板
- **When** 填写诊断信息
- **Then** "保存草稿"和"确认诊断"按钮始终可见于面板底部
- **And** 表单内容可滚动，按钮区域不随滚动移动

---

### Requirement: UI-LAYOUT-005 处方面板布局
系统 **SHALL** 将处方面板的快速导入按钮放置在顶部，药材卡片采用4列网格布局。

#### Scenario: 快速导入按钮位置
- **Given** 用户在处方面板
- **When** 需要导入验方或历史处方
- **Then** "从验方导入"和"从历史处方复制"按钮位于面板顶部

#### Scenario: 药材卡片布局
- **Given** 用户已添加药材到处方
- **When** 查看药材卡片区域
- **Then** 药材卡片以4列网格排列
- **And** 遵循N+1行原则（最后一行有空白卡片用于添加新药材）

---

### Requirement: UI-LAYOUT-006 底部操作栏布局
系统 **SHALL** 将底部操作栏按"左-中-右"三区布局：暂停(左)、状态指示(中)、主操作按钮(右)。

#### Scenario: 操作栏三区布局
- **Given** 用户在医案看诊界面
- **When** 查看底部操作栏
- **Then** "暂停看诊"按钮位于左侧
- **And** 状态指示器位于中间
- **And** "打印处方笺"和"完成看诊"按钮位于右侧

---

### Requirement: UI-LAYOUT-007 简化状态指示器
系统 **SHALL** 使用"●标签"格式的简洁状态指示器，颜色区分状态：绿色(完成)、灰色(待处理)、黄色(进行中)。

#### Scenario: 状态指示器显示
- **Given** 用户已完成诊断但未开处方
- **When** 查看底部状态指示器
- **Then** 显示"●已诊断"(绿色圆点)
- **And** 显示"●待开方"(灰色圆点)

---

### Requirement: UI-LAYOUT-008 移除重复字段
系统 **SHALL** 确保"治法方案"和"治疗原则"字段仅在诊断面板出现一次，处方面板不再重复。

#### Scenario: 无重复字段
- **Given** 用户在处方面板
- **When** 查看面板内容
- **Then** 不存在"治法方案"输入框
- **And** 不存在"治疗原则"输入框

---

## Related Specs

- `herb-card-control` - 药材卡片控件规范

## Changelog

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2025-11-27 | 初始版本 |
