# STD-03: 跨模块通信规范

## 适用范围

Server 端模块间数据交互，Desktop 端模块间导航与数据共享。

## 规范内容

### 通信通道

`ICrossModuleService` (及其 ISP 拆分接口如 `ICrossModuleAuthService`) 作为跨模块通信的唯一通道。

### 规则

| 规则 | 说明 |
|------|------|
| 模块间禁止直接引用 Repository | PatientService 不可注入 HerbRepository |
| 模块间禁止直接引用 DbContext | 各模块通过自己的 Service/Repository 访问数据 |
| 跨模块查询走 ICrossModuleService | 如验方模块需要查询药材信息，调用 `ICrossModuleService.GetHerbByNameOrPinyinAsync()` |
| 模块内部自由调用 | 同一模块的 Service 可以互相调用 (如 MedicalCaseCommandService 调用 MedicalCaseAuditService) |

### 已有跨模块接口

| 接口 | 方法 | 用途 |
|------|------|------|
| `ICrossModuleService` | `GetHerbByNameOrPinyinAsync` | 验方导入时匹配药材 |
| `ICrossModuleService` | `GetHerbBasicInfoAsync` | 验方药材验证时获取药材信息 |
| `ICrossModuleAuthService` | `RevokeAllUserTokensAsync` | 用户管理操作后撤销 Token |

### 新增跨模块需求的流程

1. 在 `ICrossModuleService` (或对应 ISP 接口) 中定义方法签名
2. 在提供方模块实现该方法
3. 调用方通过 DI 注入接口调用
4. 禁止绕过接口直接访问其他模块内部实现

### Desktop 端

模块间导航通过 Prism `IRegionManager` 和 `NavigationCoordinator`，数据传递通过导航参数 (NavigationParameters)。禁止 ViewModel 之间直接引用。

## 参考

- 编码规范: `docs/05-development/code-standards.md`
- 设计模式: `docs/05-development/patterns.md`

---

创建日期: 2026-02-26
