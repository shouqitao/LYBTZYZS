# PRD：Server 端 User 模型清理（移除医生专用字段）— CCPM

## 一、背景与目标
- 背景：`User` 实体包含若干医生专用字段（历史遗留），与当前“精简双层架构 + 统一角色模型”不再匹配，且这些字段未在业务流中使用，持续保留会增加数据面与维护成本。
- 目标：在不改变业务功能与对外 API 合同的前提下，清理 `User` 实体的医生专用字段，并清理项目与测试代码中的相关引用，数据库层面做安全的 schema 迁移与回滚预案。

## 二、代码掌握情况（清点）
- 实体定义：`src/Server/Core/LYBT.Entities/Users/UserModel.cs`
  - 医生专用字段：
    - `Specialty`（专长，string?）
    - `RegistrationFee`（挂号费，decimal?）
    - `LicenseNumber`（执业证号，string?）
    - `Introduction`（简介，string?）
  - 通用字段：`Username/RealName/PinYinCode/PhoneNumber/Email/Role/Status/PasswordHash/FailedLoginCount/LockoutEnd/CreatedTime/UpdateTime/LastLoginTime/Remark/RowVersion`
- EF 配置：`src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs`
  - `ConfigureUsers(...)` 中对 `RegistrationFee` 设定了精度：`HasPrecision(18, 2)`
- 迁移与快照：`src/Server/Core/LYBT.Infrastructure/Migrations/*`
  - 历史迁移包含上述四个字段的建模，需在新迁移中 Drop Columns。
- 项目引用（代码搜索）：
  - 业务层/控制器：未发现直接使用四个字段（仅实体与迁移）
  - 测试：`tests/UnitTests/Entities/LYBT.Entities.Tests/Users/UserModelTests.cs` 存在对四个字段的读写与断言
- 共享 DTO：`src/Shared/LYBT.Shared.Models/Contracts/Users/UserDtos.cs` 未包含上述四个字段，无需联动。

## 三、变更范围
- 仅 Server 层实体与 EF 迁移、AppDbContext 配置与单元测试；不涉及对外 API 契约变更。

## 四、实施方案（分解任务）
1) 实体与映射清理
- 在 `UserModel.cs` 中删除以下属性：`Specialty`、`RegistrationFee`、`LicenseNumber`、`Introduction`。
- 在 `AppDbContext.ConfigureUsers(...)` 中删除 `entity.Property(u => u.RegistrationFee).HasPrecision(18, 2);` 相关配置。

2) 数据库迁移
- 新增 EF Core 迁移：`RemoveUserDoctorFields`
  - Up：`DropColumn("Users", "Specialty")`，`DropColumn("Users", "RegistrationFee")`，`DropColumn("Users", "LicenseNumber")`，`DropColumn("Users", "Introduction")`
  - Down：对应 `AddColumn`（string/decimal? 精度/长度与历史一致）
- 更新 `AppDbContextModelSnapshot`
- 变更前建议执行数据库备份；灰度环境先行验证。

3) 测试清理
- `tests/UnitTests/Entities/LYBT.Entities.Tests/Users/UserModelTests.cs`
  - 删除/更新所有引用四个字段的断言与用例（如 `Specialty_PropertyCanBeSetAndGet` 等）
  - 确保剩余用例覆盖通用字段与并发标记（`RowVersion`）

4) 交叉检查
- 全仓库再次搜索 `Specialty|RegistrationFee|LicenseNumber|Introduction`，确保引用清理干净（含注释/字符串常量）。

## 五、验收标准
- 代码编译通过；数据库迁移脚本生成并可在灰度环境顺利执行；
- 所有单元测试与架构门禁通过；
- 全仓库不再存在四个字段的代码引用；
- 运行应用核心功能（登录、用户管理、患者/处方/病历等）不受影响。

## 六、回滚与数据安全
- Down 迁移可恢复四个字段（无数据的情况下保障可逆）；
- 生产变更前必须做数据库快照/备份；
- 若回滚：
  - 执行 Down 迁移 → 切回旧版本代码 → 验证关键路径可用。

## 七、风险与缓解
- 风险：隐藏引用遗漏导致编译或运行失败；
  - 缓解：两轮全仓库搜索 + CI 编译 + 运行核心冒烟用例；
- 风险：某些历史数据流程读取这些字段（低概率）；
  - 缓解：调研确认无外部 API 契约；预留 Down 迁移快速恢复。

## 八、CCPM 关键链
- 主关键链：
  1. 实体/映射清理（约 0.5 天）
  2. 生成迁移 + 本地验证（约 0.5 天）
  3. 测试清理与通过（约 0.5 天）
  4. 全仓搜索/复核（约 0.25 天）
- 缓冲设置：关键链工期 30% 作为项目缓冲。

## 九、交付物
- 代码补丁（实体/映射/迁移/测试）；
- 迁移脚本与执行记录；
- 清理前后差异报告（搜索与编译/测试结果）。

## 十、里程碑
- M1：完成开发与本地迁移验证
- M2：CI 通过 + 灰度数据验证
- M3：生产变更与观测通过

> 注：本 PRD 仅定义“服务器端 User 实体医生专用字段清理”任务，不含桌面端与其他业务层面的改动；落地时注意与并行分支协调，避免迁移冲突。

