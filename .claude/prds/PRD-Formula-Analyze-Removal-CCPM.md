# PRD：Formula 模块“验方分析”功能移除（CCPM）

## 一、背景与目标
- 背景：项目中存在“验方分析”相关代码（AnalyzeFormulaAsync 及其 DTO/端点），但当前产品不提供该分析能力，Server 控制器已返回“功能不支持”。保留相关实现与契约会增加维护成本与歧义。
- 目标：在不影响现有业务功能（增删改查、复制、分享等）的前提下，移除验方分析相关代码与端点，清理全仓库引用与测试，保持编译与测试通过。

## 二、代码清点（范围确认）
- Server/WebAPI 控制器（返回不支持）：
  - `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs:273-294` — [HttpPost("{id}/analyze")] AnalyzeFormula(...)，返回 FEATURENOTIMPLEMENTED
- Server/Formula 模块：
  - 接口：`src/Server/Modules/LYBT.Module.Formula/Interfaces/IFormulaBusinessService.cs` — `AnalyzeFormulaAsync(Guid)`
  - Service 委托层：`src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs` — `AnalyzeFormulaAsync(Guid)`
  - 业务实现：`src/Server/Modules/LYBT.Module.Formula/Services/FormulaBusinessService.cs` — `AnalyzeFormulaAsync(...)` 及内部辅助方法（`DetermineComplexity/AssessSafetyLevel/GenerateRecommendations`）
- Shared DTO：
  - `src/Shared/LYBT.Shared.Models/Contracts/Formula/FormulaAnalysisDtos.cs` 中的 `HerbCompatibilityWarning`、`FormulaAnalysisResult`（同文件其他 DTO 如历史/类型/统计保留）
- Desktop 客户端：
  - `src/Client/Desktop/Modules/Formula/Services/FormulaModule.cs` — `AnalyzeFormulaAsync(Guid)` 本地占位（返回“版本不支持”）
- 测试：
  - `tests/UnitTests/Modules/Formula.UnitTests/Services/FormulaServiceTests.cs` — `AnalyzeFormulaAsync_Should_Delegate_To_BusinessService()`

## 三、变更范围与原则
- 移除验方分析的接口、实现、DTO、控制器端点、客户端占位方法与相关测试。
- 不改动与分析无关的功能（如复制、查询、分享、从处方创建等）。
- 不破坏公共 API 合同（现状端点返回不支持；移除端点将返回 404）。本 PRD按“彻底移除”执行，如需保留路由占位，请在评审时改为“保留端点+固定 501”。

## 四、实施方案（任务分解）
1) 移除 Shared 分析 DTO
- 删除 `HerbCompatibilityWarning`、`FormulaAnalysisResult` 定义（保留同文件其他 DTO）。
- 全仓替换/删除引用（见后续步骤）。

2) 移除 Formula 模块的分析接口与实现
- `IFormulaBusinessService`：删除 `AnalyzeFormulaAsync(Guid)` 签名。
- `FormulaBusinessService`：删除 `AnalyzeFormulaAsync(...)` 方法与辅助方法（`DetermineComplexity/AssessSafetyLevel/GenerateRecommendations`）。
- `FormulaService`（委托层）：删除 `AnalyzeFormulaAsync(Guid)` 委托方法。

3) 移除 WebAPI 分析端点
- `FormulasController`：删除 `[HttpPost("{id}/analyze")] AnalyzeFormula(...)` 动作方法（包含 using 的 `FormulaAnalysisResult` 泛型替换/删除）。

4) 移除 Desktop 占位实现
- `Client/Desktop/Modules/Formula/Services/FormulaModule.cs`：删除 `AnalyzeFormulaAsync(Guid)` 占位方法。

5) 测试清理
- 删除或更新 `tests/UnitTests/Modules/Formula.UnitTests/Services/FormulaServiceTests.cs` 中 AnalyzeFormulaAsync 的用例与相关引用。

6) 全仓复核
- 搜索关键词：`AnalyzeFormulaAsync`、`FormulaAnalysisResult`、`HerbCompatibilityWarning`、`/analyze`，确保无残留引用（包括注释和字符串）。

## 五、验收标准
- 解决方案编译通过；单元测试与架构门禁通过。
- 全仓库无上述分析相关类型/方法/端点的引用。
- 服务端功能（公式增删改查、复制、分享、从处方创建、查询与模板等）与客户端 UI 不受影响。

## 六、回滚与兼容策略
- 如需保留 API 契约：保留控制器端点方法签名，固定返回 FEATURENOTIMPLEMENTED（501/业务失败码），但模块与 DTO 可移除；本 PRD默认彻底删除端点。
- 回滚路径：还原提交，恢复接口签名与 DTO；客户端可按需恢复占位方法。

## 七、风险与缓解
- 编译失败风险：遗漏某处引用 → 两轮全仓搜索 + CI。
- API 兼容性：第三方若调用 analyze 端点将从 501 变为 404 → 在发布说明中明确“分析功能已移除”。

## 八、CCPM 关键链
- 主链：
  1. DTO 与接口签名移除（0.5D）
  2. 业务实现与委托层移除（0.5D）
  3. 控制器端点清理（0.25D）
  4. 客户端占位清理（0.25D）
  5. 测试清理与全仓复核（0.5D）
- 缓冲：关键链工期 30%。

## 九、交付物
- 代码变更（Server/Shared/Desktop/Tests）。
- 搜索清单与差异报告（前后对比）。
- 发布说明（移除功能与影响说明）。

## 十、附：引用清单（供实施参照）
- WebAPI：`FormulasController.cs:273-294`（AnalyzeFormula 动作）。
- Module：
  - `IFormulaBusinessService.cs`：AnalyzeFormulaAsync 声明。
  - `FormulaService.cs`：AnalyzeFormulaAsync 委托。
  - `FormulaBusinessService.cs`：AnalyzeFormulaAsync + DetermineComplexity/AssessSafetyLevel/GenerateRecommendations。
- Shared DTO：`FormulaAnalysisDtos.cs` 中的 `HerbCompatibilityWarning`、`FormulaAnalysisResult`。
- Desktop：`FormulaModule.cs`：AnalyzeFormulaAsync（返回不支持）。
- Tests：`FormulaServiceTests.cs` AnalyzeFormulaAsync_* 用例。
