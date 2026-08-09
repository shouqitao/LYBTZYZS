# 任务 A-31-C3c：Desktop Herbs+Formula→Catalog 同步合并

> 派发对象：Mimo Code
> 版本：v1.0 | 日期：2026-08-09
> 依据：Server 端 C-3b 已完成（commit `5b94893f5`），Desktop 需同步
> **⚠️ T2 方案调整——命名空间改写+引用更新**

## 任务

将 Desktop 侧 `LYBT.Desktop.Herbs` + `LYBT.Desktop.Formula` 合并为 `LYBT.Desktop.Catalog`，与 Server 端对齐。

## 范围

- ✅ `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/`（13 cs 文件）
- ✅ `src/Client/Desktop/Modules/LYBT.Desktop.Formula/`（14 cs 文件）
- ✅ `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IFormulaApi.cs`（Contracts 层接口）
- ✅ 外部引用（4 个其他 Desktop 模块引用 Herbs/Formula）
- ❌ 排除：Server 端（已完成 C-3b）
- ❌ 排除：Shared 实体层（保持不动）

## 动作

1. **新建 `LYBT.Desktop.Catalog/`**（空项目）
2. **迁移 Herbs 13 文件 → Catalog**（命名空间改写 `LYBT.Desktop.Herbs` → `LYBT.Desktop.Catalog`）
3. **迁移 Formula 14 文件 → Catalog**（同上）
4. **合并重复**：Editor VM 已由 C5-4 提取 `EditorViewModelBase` → 保持不变（基类已共享）
5. **Contracts 层**：`IFormulaApi` → `ICatalogFormulaApi` 或保持原名（路由不变）
6. **外部引用更新**：4 个外部模块改 using（`LYBT.Desktop.Herbs`/`LYBT.Desktop.Formula` → `LYBT.Desktop.Catalog`）
7. **DI 注册**：HerbsModule + FormulaModule → CatalogModule
8. **删除旧项目**：`LYBT.Desktop.Herbs/` + `LYBT.Desktop.Formula/` 整目录
9. **sln 更新**

## 关键约束

- **路由保持**：Desktop 侧路由 `/api/v1/herbs/*` + `/api/v1/formulas/*` 不变（与 Server 对齐）
- **命名空间改写**：`LYBT.Desktop.Herbs` → `LYBT.Desktop.Catalog` / `LYBT.Desktop.Formula` → `LYBT.Desktop.Catalog`
- **实体引用不变**：Shared 实体层的 Herb/Formula/FormulaHerbItem 保持不动
- **0 错误 0 警告**：`dotnet build LYBTZYZS.sln --no-incremental`
- **架构测试**：`dotnet test tests/LYBT.Tests.Architecture/` 全绿

## 产出

- 1 个 commit + push
- 报告：`docs/compose/reports/a31-c3c-desktop-catalog-merge.md`
