# 任务 A-25：2 个遗留问题解决（命名空间统一复数 + 本地样例中文化）

> 依据：产品负责人确认解决结构梳理全部遗留问题
> 前置：A-16~A-24 全部完成

## 任务 1：Desktop Registration 项目名统一复数

**问题**：项目名 `LYBT.Desktop.Registration`（单数）vs 命名空间 `LYBT.Desktop.Registrations`（复数）——项目名与命名空间不一致（Q-03 先例已统一 Server 侧为复数 `LYBT.Module.Registrations`，Desktop 代码命名空间也已是复数，只有**项目名/目录名/文件名为单数**）

**动作**：
1. 项目目录 `src/Client/Desktop/Modules/LYBT.Desktop.Registration/` → `LYBT.Desktop.Registrations/`
2. 项目文件 `LYBT.Desktop.Registration.csproj` → `LYBT.Desktop.Registrations.csproj`（AssemblyName/RootNamespace 同步）
3. sln 中项目条目 + 引用路径更新（sln L92 项目定义 + 引用方的 ProjectReference 路径）
4. 引用方 csproj（LYBT.Desktop.Clinical / LYBT.Desktop.Shell）ProjectReference 更新
5. 全仓 grep 确认无残留 `LYBT.Desktop.Registration"`（单数，非 Registrations）
6. 对齐 Q-03 先例（Server 侧当时 `27b65e42b` 的做法：纯命名空间/项目名级重命名，不动业务逻辑）

## 任务 2：本地英文样例数据中文化

**问题**：LocalWebApiSeedData 用英文样例（Ginseng/Sample Formula/Sample Patient），与中文业务环境不符

**动作**：
1. `src/Client/Desktop/LocalWebAPI/Data/LocalWebApiSeedData.cs`：
   - `"Ginseng"` → `"人参"`（药材）
   - `"Sample Formula"` → `"示例验方"`（验方）
   - `"Sample Patient"` → `"示例患者"`（患者）
   - 检查该文件其他字段（如说明/备注/价格）是否也有英文样例，一并中文化
2. 同步测试注释：`tests/LYBT.Tests.Desktop/Integration/LocalWebAPI/HerbsControllerTests.cs`（`// seed data includes Ginseng`）和 `FormulasControllerTests.cs`（`// seed data includes Sample Formula`）——注释更新为中文样例名
3. 若测试有断言引用英文名（如 `Should().Contain(x => x.Name == "Ginseng")`）则同步改

## 验证

1. `dotnet build LYBTZYZS.sln --no-incremental`：0 错误 0 警告
2. `dotnet test tests/LYBT.Tests.Architecture/`：86/86
3. `dotnet test tests/LYBT.Tests.Desktop/`：相关 LocalWebAPI 集成测试（Herbs/Formulas）通过
4. git 干净 + commit + push

## 不做

- ❌ 不改业务逻辑/接口/API 语义（纯命名 + 数据文案）
- ❌ 不改 Remote 端种子数据（IdentitySeedData 等，若为中文则不动）
- ❌ 不清理其他 P2 遗留（只做本任务 2 项）
