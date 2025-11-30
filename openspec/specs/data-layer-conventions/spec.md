# data-layer-conventions Specification

## Purpose
TBD - created by archiving change unify-server-data-layer. Update Purpose after archive.
## Requirements
### Requirement: DLC-001 BaseEntity 审计字段标准

所有业务实体 MUST 继承 `BaseEntity`，包含统一的审计字段。

#### Scenario: 新实体创建时自动设置审计字段
- Given: 创建新的业务实体实例
- When: 调用 `DbContext.SaveChangesAsync()`
- Then: `CreatedAt` 自动设置为 `DateTime.UtcNow`
- And: `CreatedBy` 设置为当前用户ID
- And: `IsDeleted` 默认为 `false`

#### Scenario: 实体更新时自动更新审计字段
- Given: 修改现有业务实体
- When: 调用 `DbContext.SaveChangesAsync()`
- Then: `UpdatedAt` 自动设置为 `DateTime.UtcNow`
- And: `UpdatedBy` 设置为当前用户ID

---

### Requirement: DLC-002 DateTime UTC 标准化

所有日期时间字段 MUST 使用 UTC 时间。

#### Scenario: 新记录的时间字段为UTC
- Given: 创建新实体记录
- When: 保存到数据库
- Then: `CreatedAt` 值的 `Kind` 为 `DateTimeKind.Utc`
- And: 数据库存储值为 UTC 时间

#### Scenario: 读取时间字段返回UTC
- Given: 从数据库读取实体
- When: 访问 `CreatedAt` 或 `UpdatedAt`
- Then: 返回的 DateTime 为 UTC 时间

---

### Requirement: DLC-003 RowVersion 并发控制

所有核心业务实体 MUST 配置 RowVersion 并发控制。

#### Scenario: 并发更新检测冲突
- Given: 用户A和用户B同时读取实体（相同RowVersion）
- When: 用户A先保存更新
- And: 用户B随后尝试保存
- Then: 用户B收到 `DbUpdateConcurrencyException`

#### Scenario: RowVersion 自动更新
- Given: 实体被修改
- When: 保存到数据库
- Then: RowVersion 值自动递增

---

### Requirement: DLC-004 软删除全局过滤

所有查询 MUST 默认排除已删除记录。

#### Scenario: 默认查询排除已删除记录
- Given: 数据库中存在 `IsDeleted=true` 的记录
- When: 执行标准查询（无 `IgnoreQueryFilters`）
- Then: 已删除记录不出现在结果中

#### Scenario: 显式查询已删除记录
- Given: 数据库中存在 `IsDeleted=true` 的记录
- When: 使用 `IgnoreQueryFilters()` 查询
- Then: 已删除记录出现在结果中

---

### Requirement: DLC-005 EF Configuration 基类模式

所有 EntityTypeConfiguration MUST 继承 `BaseEntityConfiguration<T>`。

#### Scenario: Configuration 继承基类
- Given: 新建实体配置类
- When: 实现 `IEntityTypeConfiguration<T>`
- Then: 必须继承 `BaseEntityConfiguration<T>`
- And: 调用 `base.Configure(builder)`

---

### Requirement: DLC-006 命名规范

实体命名 MUST 遵循统一的单复数规范。

#### Scenario: Entity 类名使用单数
- Given: 新建实体类
- When: 定义类名
- Then: 类名使用单数形式（如 `Patient`，非 `Patients`）

#### Scenario: 数据库表名使用复数
- Given: 配置实体到表映射
- When: 调用 `ToTable()`
- Then: 表名使用复数形式（如 `Patients`）

#### Scenario: 命名空间目录使用复数
- Given: 在 `LYBT.Entities` 下创建实体目录
- When: 命名目录
- Then: 目录名使用复数形式（如 `Patients`）

---

### Requirement: DLC-007 StringLength 标准

常用字段 MUST 使用统一的长度限制。

#### Scenario: Name 字段长度统一
- Given: 任何包含 `Name` 字段的实体
- When: 配置字段长度
- Then: 使用 `HasMaxLength(100)`

#### Scenario: PinYinCode 字段长度统一
- Given: 任何包含 `PinYinCode` 字段的实体
- When: 配置字段长度
- Then: 使用 `HasMaxLength(50)`

---

### Requirement: DLC-008 导航属性规范

导航属性 MUST 遵循统一的定义模式。

#### Scenario: 导航属性使用 virtual
- Given: 定义导航属性
- When: 声明属性
- Then: 必须使用 `virtual` 关键字

#### Scenario: 集合导航属性初始化
- Given: 定义集合导航属性
- When: 声明属性类型
- Then: 使用 `ICollection<T>` 接口
- And: 初始化为 `new List<T>()`

