# OMO Agent 模型配置说明

> 配置文件: `~/.config/opencode/oh-my-opencode.jsonc`
> 最后更新: 2026-04-08

---

## 可用 Provider 与模型

| Provider | 模型 ID | 说明 |
|----------|---------|------|
| **github-copilot** | `claude-opus-4.5` | Anthropic 最强推理 (Claude Opus) |
| **github-copilot** | `claude-opus-4.6` | Anthropic 次新推理 (Claude Opus) |
| **github-copilot** | `claude-sonnet-4.6` | Anthropic 平衡型 (Claude Sonnet) |
| **github-copilot** | `claude-haiku-4.5` | Anthropic 轻量快速 (Claude Haiku) |
| **github-copilot** | `gpt-5.4` | OpenAI 旗舰推理 (GPT-5.4) |
| **github-copilot** | `gpt-5.4-mini` | OpenAI 轻量快速 (GPT-5.4 Mini) |
| **github-copilot** | `gemini-3.1-pro-preview` | Google 高端 (Gemini Pro) |
| **github-copilot** | `gemini-3-flash-preview` | Google 轻量 (Gemini Flash) |
| **kimi** | `kimi-k2.5` | Moonshot 旗舰 (支持 image input) |
| **zhipu** | `glm-5` | 智谱旗舰 (200K context) |

### 不可用 Provider (已从配置移除)

| Provider | 原因 |
|----------|------|
| `mimo/*` | 服务不可用 |
| `bailian-coding-plan/*` | 服务不可用 |
| `google/*` (直接调用) | 未配置 provider，仅通过 github-copilot 使用 |

---

## 当前 Agent 配置一览

| Agent | 角色 | 当前主模型 | Fallback 链 | OMO 默认推荐 |
|-------|------|-----------|-------------|-------------|
| **sisyphus** | 主力编排 | `claude-opus-4.5` | kimi-k2.5 → glm-5 | `claude-opus-4.6` |
| **build** | 执行代理 | `gpt-5.4` | kimi-k2.5 → glm-5 | `gpt-5.4` |
| **sisyphus-junior** | 子任务执行 | `gpt-5.4-mini` | kimi-k2.5 → glm-5 | `gpt-5.4-mini` |
| **oracle** | 只读高IQ顾问 | `gpt-5.4` | kimi-k2.5 → glm-5 | `claude-opus-4.6` |
| **librarian** | 外部参考搜索 | `claude-haiku-4.5` | kimi-k2.5 → glm-5 | `claude-opus-4.6` |
| **explore** | 代码库搜索 | `gpt-5.4` | kimi-k2.5 → glm-5 | `gpt-5.4` |
| **prometheus** | 计划生成 | `claude-haiku-4.5` | kimi-k2.5 → glm-5 | `claude-opus-4.6` |
| **metis** | 预规划分析 | `claude-opus-4.6` | kimi-k2.5 → glm-5 | `claude-opus-4.6` |
| **momus** | 计划评审 | `gpt-5.4` | kimi-k2.5 → glm-5 | `claude-opus-4.6` |
| **hephaestus** | 高级构建 | `gpt-5.4` | kimi-k2.5 → glm-5 | `gpt-5.4` |
| **plan** | 规划代理 | `claude-sonnet-4.6` | kimi-k2.5 → glm-5 | `claude-opus-4.6` |
| **atlas** | 计划执行 | `gpt-5.4` | glm-5 → kimi-k2.5 | `gpt-5.4` |
| **multimodal-looker** | 多模态查看 | `gpt-5.4` | kimi-k2.5 → glm-5 | `gemini-1.5-pro` |

## 当前 Category 配置一览

| Category | 用途 | 当前主模型 | Fallback 链 | OMO 默认推荐 |
|----------|------|-----------|-------------|-------------|
| **visual-engineering** | 前端/UI | `claude-sonnet-4.6` | kimi-k2.5 → glm-5 | `claude-opus-4.6` |
| **ultrabrain** | 硬逻辑/算法 | `gpt-5.4` | kimi-k2.5 → glm-5 | `gpt-5.4` |
| **deep** | 自治深度构建 | `gpt-5.4` | kimi-k2.5 → glm-5 | `gpt-5.4` |
| **artistry** | 创意方案 | `claude-sonnet-4.6` | kimi-k2.5 → glm-5 | `claude-opus-4.6` |
| **quick** | 轻量任务 | `gpt-5.4-mini` | glm-5 → kimi-k2.5 | `gpt-5.4-mini` |
| **unspecified-low** | 通用低 | `claude-sonnet-4.6` | glm-5 → kimi-k2.5 | `claude-opus-4.6` |
| **unspecified-high** | 通用高 | `claude-sonnet-4.6` | kimi-k2.5 → glm-5 | `claude-opus-4.6` |
| **writing** | 文档写作 | `claude-sonnet-4.6` | kimi-k2.5 → glm-5 | `claude-opus-4.6` |

---

## 本次修改记录 (2026-04-08)

| 修改项 | 原值 | 新值 | 原因 |
|--------|------|------|------|
| `sisyphus` fallback | 含 `mimo/mimo-v2-pro` | 移除 mimo | provider 不可用 |
| `build` model | 未指定 (无 model 字段) | `claude-sonnet-4.6` | 避免依赖默认模型 |
| `build` fallback | 含 `mimo/mimo-v2-pro` | 移除 mimo | provider 不可用 |
| `librarian` model | `gpt-5-mini` (不存在) | `claude-haiku-4.5` | 模型名不存在 |
| `librarian` fallback | 含 `bailian-coding-plan/*` | 移除 | provider 不可用 |
| `explore` model | `gpt-5-mini` (不存在) | `claude-haiku-4.5` | 模型名不存在 |
| `explore` fallback | 含 `mimo/*`, `bailian-coding-plan/*` | 移除 | provider 不可用 |
| `hephaestus` fallback | 含 `mimo/mimo-v2-pro` | 移除 mimo | provider 不可用 |
| **`plan` model** | **未指定 (根本 bug)** | **`claude-sonnet-4.6`** | **plan agent 一直报 model 不可用的根因** |
| `atlas` fallback | 含 `mimo/*`, `bailian-coding-plan/*` | 移除 | provider 不可用 |
| `multimodal-looker` fallback | 含 `bailian-coding-plan/*` | 移除 | provider 不可用 |
| `visual-engineering` model | `google/gemini-3.1-pro-preview` | `claude-sonnet-4.6` | google provider 未配置 |
| `deep` model | `gpt-5.3-codex` (不存在) | `claude-opus-4.5` | 模型名不存在 |
| `deep` fallback | 含 `mimo/*` | 移除 | provider 不可用 |
| `artistry` model | `google/gemini-3.1-pro-preview` | `claude-sonnet-4.6` | google provider 未配置 |
| `quick` model | `gpt-5.4-mini` (不可用) | `claude-haiku-4.5` | 模型不可用 |
| `quick` fallback | 含 `mimo/*`, `bailian-coding-plan/*` | 移除 | provider 不可用 |
| `unspecified-low` fallback | 含 `mimo/*`, `bailian-coding-plan/*` | 移除 | provider 不可用 |
| `writing` model | `google/gemini-3-flash-preview` | `claude-sonnet-4.6` | google provider 未配置 |

---

## 如何替换模型

当某个模型不可用时，按以下步骤修改：

### 1. 定位配置项

打开 `~/.config/opencode/oh-my-opencode.jsonc`，找到对应 agent 或 category。

### 2. 修改 model 字段

```jsonc
"agent-name": {
  "model": "provider/model-id",   // ← 改这里
  "fallback_models": [             // ← 或改 fallback
    { "model": "provider/model-id" }
  ]
}
```

### 3. 模型选择指南

| 需求 | 推荐 | 备选 |
|------|------|------|
| **最强推理** (oracle, ultrabrain, deep) | `github-copilot/claude-opus-4.5` | `github-copilot/gpt-5.4` |
| **平衡质量/速度** (sisyphus, build, plan) | `github-copilot/claude-sonnet-4.6` | `github-copilot/gpt-5.4` |
| **轻量快速** (explore, librarian, quick) | `github-copilot/claude-haiku-4.5` | `github-copilot/gpt-5.4-mini` |
| **多模态** (multimodal-looker) | `kimi/kimi-k2.5` | `github-copilot/gpt-5.4` |
| **中文优化** | `zhipu/glm-5` | `kimi/kimi-k2.5` |
| **Fallback 通用** | `kimi/kimi-k2.5` | `zhipu/glm-5` |

### 4. 模型名格式

- GitHub Copilot 代理的模型: `github-copilot/模型名`
- Kimi: `kimi/kimi-k2.5`
- 智谱: `zhipu/glm-5`

> **注意**: `github-copilot/` 前缀下的模型名由 GitHub Copilot Connect 提供，Claude/GPT/Gemini 系都有，不需要分别配置 provider。
