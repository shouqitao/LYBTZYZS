# unify-xaml-resources

## Why

### 发现的问题

| 位置 | 问题类型 | 当前状态 | 期望状态 |
|------|----------|----------|----------|
| Shell/Styles/*.xaml | 资源碎片化 | 5个独立文件，部分重复 | 统一到Infrastructure层 |
| CommonStyles.xaml | 层级错误 | Shell层定义公共样式 | 迁移到Infrastructure |
| 配色方案 | 缺乏中医特色 | 通用医疗蓝色 | 融入中医五行元素 |
| 样式命名 | 不一致 | XXXStyle/XXX混用 | 统一命名规范 |
| 字体系统 | 未优化 | 硬编码字体 | 系统化字体层级 |

### 设计目标

1. **保持清新简约风格** - Minimalism + Neumorphism元素
2. **融入中医传统元素** - 五行配色、水墨意境、传统纹样装饰
3. **不使用第三方主题库** - 纯XAML实现
4. **医疗行业专业感** - 可信赖、清洁、易读

## What Changes

### Phase 1: 设计系统定义

#### 1.1 中医五行配色方案 (TCM Five Elements Palette)

**核心理念**: 五行（木火土金水）对应五色（青赤黄白黑），现代化演绎

| 五行 | 传统色 | 现代演绎 | 用途 | HEX |
|------|--------|----------|------|-----|
| **木(Wood)** | 青 | 松石青/翠绿 | Primary主色 | `#2E9E8E` |
| **火(Fire)** | 赤 | 朱砂红 | CTA/强调 | `#C74B50` |
| **土(Earth)** | 黄 | 杏黄/米色 | 警告/暖调 | `#E8B86D` |
| **金(Metal)** | 白 | 宣纸白 | 背景/卡片 | `#FAFAF8` |
| **水(Water)** | 黑 | 墨色/深灰 | 文字/边框 | `#2C3E50` |

**扩展配色**:

```
Primary Palette (木/青系 - 生机与疗愈)
├── Primary:       #2E9E8E  (松石青 - 主色)
├── PrimaryLight:  #4DB8A8  (浅松石 - 悬停)
├── PrimaryDark:   #1E7A6D  (深松石 - 按下)
└── PrimaryMuted:  #E8F5F3  (青雾 - 背景)

Accent Palette (火/赤系 - 活力与警示)
├── Accent:        #C74B50  (朱砂红 - CTA)
├── AccentLight:   #D96E72  (浅朱砂 - 悬停)
├── AccentDark:    #A33D41  (深朱砂 - 按下)
└── AccentMuted:   #F8E8E8  (浅红 - 背景)

Neutral Palette (金+水系 - 清净与沉稳)
├── Background:    #FAFAF8  (宣纸白 - 主背景)
├── Surface:       #FFFFFF  (纯白 - 卡片)
├── Border:        #E8E6E3  (淡墨 - 边框)
├── BorderLight:   #F0EEEB  (烟灰 - 分隔)
├── TextPrimary:   #2C3E50  (墨色 - 主文字)
├── TextSecondary: #6B7B8A  (淡墨 - 次文字)
└── TextMuted:     #9CAAB8  (浅墨 - 提示)

Semantic Palette (语义色)
├── Success:       #52B788  (草青 - 成功)
├── Warning:       #E8B86D  (杏黄 - 警告)
├── Error:         #E57373  (淡红 - 错误)
└── Info:          #5C9EAD  (水青 - 信息)
```

#### 1.2 字体系统 (Typography System)

**中文字体选择**: 保持清晰可读，兼顾文化底蕴

```
Font Stack (按优先级)
├── 标题: "HarmonyOS Sans SC", "Microsoft YaHei UI", "微软雅黑", sans-serif
├── 正文: "Microsoft YaHei", "微软雅黑", "Noto Sans SC", sans-serif
└── 数字: "DIN Alternate", "Microsoft YaHei", sans-serif

Font Sizes (px)
├── H1:     28px / Bold    (页面标题)
├── H2:     22px / SemiBold (区块标题)
├── H3:     18px / Medium  (卡片标题)
├── Body:   14px / Regular (正文)
├── Small:  12px / Regular (辅助文字)
└── Tiny:   11px / Regular (标签/徽章)

Line Heights
├── Tight:   1.25  (标题)
├── Normal:  1.5   (正文)
└── Relaxed: 1.75  (长文本)
```

#### 1.3 间距与圆角系统 (Spacing & Border Radius)

```
Spacing Scale (基于4px网格)
├── xs:  4px   (紧凑)
├── sm:  8px   (小)
├── md:  12px  (中)
├── lg:  16px  (大)
├── xl:  24px  (特大)
└── 2xl: 32px  (超大)

Border Radius
├── None:   0px    (直角)
├── Small:  4px    (轻微圆角)
├── Medium: 6px    (标准圆角) - 默认
├── Large:  8px    (大圆角)
├── XLarge: 12px   (特大圆角)
└── Full:   9999px (完全圆角/胶囊)

Shadows (柔和水墨风)
├── None:   none
├── Small:  0 1px 3px rgba(44,62,80,0.08)
├── Medium: 0 2px 8px rgba(44,62,80,0.12)
├── Large:  0 4px 16px rgba(44,62,80,0.16)
└── Focus:  0 0 0 3px rgba(46,158,142,0.25)
```

### Phase 2: 资源文件重构

#### 2.1 文件结构

```
Infrastructure/Themes/
├── DesignTokens.xaml       [NEW] 设计令牌(颜色/字体/间距)
├── UnifiedComponents.xaml  [MODIFY] 合并所有公共组件样式
└── ValidationStyles.xaml   [KEEP] 验证相关样式

Shell/Styles/
├── Colors.xaml             [DELETE] 迁移到DesignTokens
├── Typography.xaml         [DELETE] 迁移到DesignTokens
├── CommonStyles.xaml       [DELETE] 迁移到UnifiedComponents
├── Controls.xaml           [KEEP] Shell专用控件
└── DialogStyles.xaml       [KEEP] 对话框专用
```

#### 2.2 样式命名规范

```
命名模式: {Component}{Variant}{State}Style

示例:
├── ButtonPrimaryStyle      (主按钮)
├── ButtonSecondaryStyle    (次按钮)
├── ButtonDangerStyle       (危险按钮)
├── ButtonLinkStyle         (链接按钮)
├── TextBoxEditableStyle    (可编辑文本框)
├── TextBoxReadOnlyStyle    (只读文本框)
├── CardDefaultStyle        (默认卡片)
├── CardElevatedStyle       (悬浮卡片)
└── BorderSectionStyle      (区块边框)
```

### Phase 3: 中医视觉元素

#### 3.1 装饰性元素 (可选)

```
水墨背景纹理:
- 淡墨山水渐变 (用于登录页/欢迎页背景)
- 云纹装饰 (用于卡片角落装饰)
- 印章图标 (用于完成状态标记)

图标风格:
- 线性图标为主 (stroke-width: 1.5px)
- 配合中医元素图标 (药材、诊脉、经络等)
- 颜色使用Primary或TextSecondary
```

#### 3.2 组件风格示例

**按钮组件**:
```
Primary Button:
├── Background: #2E9E8E (松石青)
├── Foreground: #FFFFFF
├── BorderRadius: 6px
├── Hover: #4DB8A8
├── Pressed: #1E7A6D
└── Shadow: Small

Secondary Button:
├── Background: Transparent
├── Foreground: #2E9E8E
├── Border: 1px solid #2E9E8E
├── Hover: #E8F5F3 (PrimaryMuted)
└── Shadow: None
```

**卡片组件**:
```
Card:
├── Background: #FFFFFF
├── Border: 1px solid #E8E6E3
├── BorderRadius: 8px
├── Shadow: Small
├── Padding: 16px
└── Hover: Shadow升级为Medium
```

## Architecture

### 资源加载顺序

```
App.xaml
├── MergedDictionaries
│   ├── DesignTokens.xaml        (设计令牌 - 最先加载)
│   ├── UnifiedComponents.xaml   (公共组件 - 依赖DesignTokens)
│   └── [Module-specific].xaml   (模块专用 - 最后加载)
```

### 依赖关系

```
DesignTokens.xaml (无依赖)
    ↓
UnifiedComponents.xaml (依赖 DesignTokens)
    ↓
各模块Resources (依赖 UnifiedComponents)
```

## Impact

- **文件变更**: 预估10-15个文件
- **风险等级**: Medium (涉及全局样式)
- **测试要求**: 全模块UI验证

## Risks

| 风险 | 缓解措施 |
|------|----------|
| 样式迁移遗漏 | 编译验证 + 运行时检查 |
| 颜色对比度不足 | 遵循WCAG 4.5:1标准 |
| 字体加载失败 | 完整fallback链 |
| 破坏现有布局 | 分Phase逐步迁移 |

## References

- UI/UX Pro Max Skill搜索结果
- 医疗行业UI最佳实践
- 中医五行配色理论
- WPF资源字典最佳实践

---

**创建时间**: 2026-01-11
**状态**: 提案草稿
