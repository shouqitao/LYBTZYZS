# Repository泛型接口统一重构 - 任务分解文档

## 📋 元数据

- **Epic**: #1498 Repository泛型接口统一重构
- **设计文档**: `docs/explanation/architecture/shared/repository-generic-interface-refactoring-design.md`
- **需求文档**: `docs/explanation/architecture/shared/repository-generic-interface-refactoring-discussion.md`
- **合规报告**: `docs/explanation/architecture/shared/repository-generic-interface-refactoring-compliance-report.md`
- **总工作量**: 80-96小时（12工作日）
- **实施阶段**: Phase 1-6
- **优先级**: P0（核心架构重构）

---

## 🎯 任务清单（Task Checklist）

### Phase 1: 创建基础接口和实现类（2天，16小时）

#### Task 1.1: 创建IReadRepository<T>接口

- **工作量**: 2-3小时
- **依赖**: 无
- **类型**: Interface
- **优先级**: P0
- **风险**: 🟢 低

**文件范围**:
- `src/Infrastructure/LYBT.Infrastructure/Interfaces/IReadRepository.cs` (新建)

**实施要点**:
1. 在Infrastructure层创建 `IReadRepository<T>` 接口
2. 定义5个核心查询方法：
   - `Task<T?> GetByIdAsync(Guid id)`
   - `Task<IEnumerable<T>> GetAllAsync()`
   - `Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)`
   - `Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate)`
   - `Task<long> CountAsync()`
3. 添加完整XML注释和使用示例
4. 添加泛型约束：`where T : class`

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] XML注释完整（包含<summary>、<param>、<returns>、<example>）
- [ ] 接口定义符合设计文档第2.1节
- [ ] 命名空间正确：`LYBT.Infrastructure.Interfaces`

**技术要点**:
- 适用场景：从属实体模块（Consultation, Prescription）
- 只读Repository，不包含写操作
- 符合DDD聚合根边界原则（AR-001）

---

#### Task 1.2: 创建BaseReadRepository<T>实现类

- **工作量**: 4-5小时
- **依赖**: Task 1.1
- **类型**: Repository Implementation
- **优先级**: P0
- **风险**: 🟢 低

**文件范围**:
- `src/Infrastructure/LYBT.Infrastructure/Persistence/BaseReadRepository.cs` (新建)

**实施要点**:
1. 在Infrastructure层创建 `BaseReadRepository<T>` 实现类
2. 实现 `IReadRepository<T>` 接口的5个方法
3. 构造函数注入 `ApplicationDbContext`
4. 初始化 `DbSet<T>` 字段
5. 所有方法标记为 `virtual` 允许子类重写
6. 使用EF Core LINQ实现查询逻辑

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 5个方法实现完整
- [ ] 依赖注入配置正确
- [ ] 异步方法使用async/await
- [ ] 实现符合设计文档第3.1节

**技术要点**:
- 使用 `DbSet<T>` 封装EF Core查询
- `GetByIdAsync` 使用 `FindAsync`（主键查询）
- `FindAsync` 使用 `Where + ToListAsync`（条件查询）
- `GetSingleAsync` 使用 `SingleOrDefaultAsync`（确保唯一性）
- `CountAsync` 使用 `LongCountAsync`（支持大数据量）

---

#### Task 1.3: 更新依赖注入配置（BaseReadRepository）

- **工作量**: 1-1.5小时
- **依赖**: Task 1.2
- **类型**: Configuration
- **优先级**: P0
- **风险**: 🟢 低

**文件范围**:
- `src/Server/LYBT.Server/Startup.cs`

**实施要点**:
1. 在 `ConfigureServices` 方法中注册泛型接口：
   ```csharp
   services.AddScoped(typeof(IReadRepository<>), typeof(BaseReadRepository<>));
   ```
2. 确认注册位置在其他Repository注册之前
3. 验证依赖注入容器配置正确

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] DI容器启动成功
- [ ] 可以通过构造函数注入 `IReadRepository<T>`

**技术要点**:
- 使用泛型注册：`typeof(IReadRepository<>)`
- Scoped生命周期（与DbContext一致）

---

#### Task 1.4: 编写BaseReadRepository单元测试

- **工作量**: 4-5小时
- **依赖**: Task 1.2
- **类型**: Unit Test
- **优先级**: P0
- **风险**: 🟢 低

**文件范围**:
- `tests/UnitTests/Infrastructure/BaseReadRepositoryTests.cs` (新建)

**实施要点**:
1. 创建测试类 `BaseReadRepositoryTests`
2. Mock `ApplicationDbContext` 和 `DbSet<T>`
3. 编写15+测试用例覆盖5个方法：
   - `GetByIdAsync`: 正常查询、不存在、null处理（3个用例）
   - `GetAllAsync`: 空列表、正常列表（2个用例）
   - `FindAsync`: 有结果、无结果、空条件（3个用例）
   - `GetSingleAsync`: 唯一结果、多个结果抛异常、无结果（3个用例）
   - `CountAsync`: 空表、有数据（2个用例）
   - 异步操作验证（2个用例）

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 所有测试用例通过（15+ test cases）
- [ ] 测试覆盖率≥90%
- [ ] 使用AAA模式（Arrange-Act-Assert）
- [ ] Mock配置正确（NSubstitute）

**技术要点**:
- 使用NSubstitute Mock DbContext和DbSet
- 使用xUnit测试框架
- 异步测试使用 `async Task` 方法
- 验证异常场景（如SingleOrDefault多个结果）

---

#### Task 1.5: 验证Phase 1完成

- **工作量**: 1-1.5小时
- **依赖**: Task 1.1, Task 1.2, Task 1.3, Task 1.4
- **类型**: Verification
- **优先级**: P0
- **风险**: 🟢 低

**验收标准**:
- [ ] 编译通过：`dotnet build LYBT.All.sln -c Release` 0 errors, 0 warnings
- [ ] 单元测试通过：15+ test cases, 100%通过率
- [ ] XML注释完整
- [ ] DI配置验证通过

**验证清单**:
1. 运行编译命令验证无错误
2. 运行单元测试验证全部通过
3. 检查XML注释生成文档
4. 确认 `IReadRepository<T>` 和 `BaseReadRepository<T>` 可用

---

### Phase 2: 重命名IBaseRepository为IRepository（1.5天，12小时）

#### Task 2.1: 重命名Shared层IBaseRepository为IRepository

- **工作量**: 2-3小时
- **依赖**: Phase 1完成
- **类型**: Refactoring
- **优先级**: P0
- **风险**: 🟡 中

**文件范围**:
- `src/Shared/LYBT.Shared.Models/Interfaces/IBaseRepository.cs` → `IRepository.cs`

**实施要点**:
1. 使用IDE重构工具（Rename Symbol）重命名接口：
   - `IBaseRepository<T>` → `IRepository<T>`
2. 更新接口继承关系：
   - 继承 `IReadRepository<T>`（从Infrastructure层）
3. 保留15个扩展方法（不含已继承的5个查询方法）
4. 更新XML注释说明继承关系

**验收标准**:
- [ ] 编译通过：0 errors（IDE自动更新所有引用）
- [ ] 接口定义符合设计文档第2.2节
- [ ] 继承关系正确：`IRepository<T> : IReadRepository<T>`
- [ ] XML注释完整

**技术要点**:
- 使用Visual Studio/Rider的Rename Symbol功能（Ctrl+R, Ctrl+R）
- 自动更新所有引用，避免手动替换
- 检查生成的更改列表，确认无遗漏

---

#### Task 2.2: 标记Infrastructure层旧IRepository为Obsolete

- **工作量**: 1-1.5小时
- **依赖**: Task 2.1
- **类型**: Deprecation
- **优先级**: P0
- **风险**: 🟢 低

**文件范围**:
- `src/Infrastructure/LYBT.Infrastructure/Interfaces/IRepository.cs` → `IRepositoryLegacy.cs`

**实施要点**:
1. 重命名文件和接口：
   - `IRepository.cs` → `IRepositoryLegacy.cs`
   - `IRepository<T>` → `IRepositoryLegacy<T>`
2. 添加 `[Obsolete]` 特性：
   ```csharp
   [Obsolete("请使用 LYBT.Shared.Models.Interfaces.IRepository<T>，此接口将在v1.1版本删除")]
   public interface IRepositoryLegacy<T> where T : class
   {
       // 保留现有方法定义
   }
   ```
3. 更新所有引用（编译器会提示警告）

**验收标准**:
- [ ] 编译通过：0 errors（允许Obsolete warnings）
- [ ] 旧接口标记为过时
- [ ] 编译器警告提示使用新接口
- [ ] 所有引用仍然可用（向后兼容）

**技术要点**:
- 使用 `[Obsolete(message)]` 特性提供迁移指导
- 不破坏现有代码（向后兼容）
- 为后续删除做准备（v1.1版本）

---

#### Task 2.3: 更新BaseRepository实现继承新IRepository

- **工作量**: 3-4小时
- **依赖**: Task 2.1
- **类型**: Repository Implementation
- **优先级**: P0
- **风险**: 🟡 中

**文件范围**:
- `src/Infrastructure/LYBT.Infrastructure/Persistence/BaseRepository.cs`

**实施要点**:
1. 更新 `BaseRepository<T>` 类定义：
   ```csharp
   public class BaseRepository<T> : BaseReadRepository<T>, IRepository<T> where T : class
   ```
2. 移除已继承的5个查询方法实现（来自BaseReadRepository）
3. 保留15个扩展方法实现：
   - 2个分页方法
   - 3个条件查询扩展方法
   - 3个写操作方法
   - 3个批量操作方法
   - 1个SaveChangesAsync方法
4. 更新XML注释说明继承关系

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 继承关系正确：BaseRepository → BaseReadRepository → IRepository
- [ ] 20个方法全部可用（5个继承 + 15个实现）
- [ ] 实现符合设计文档第3.2节

**技术要点**:
- 复用BaseReadRepository的5个查询方法（无需重复实现）
- 新增批量操作使用EF Core原生API（AddRangeAsync, RemoveRange）
- SaveChangesAsync返回受影响行数

---

#### Task 2.4: 更新依赖注入配置（BaseRepository）

- **工作量**: 1-1.5小时
- **依赖**: Task 2.3
- **类型**: Configuration
- **优先级**: P0
- **风险**: 🟢 低

**文件范围**:
- `src/Server/LYBT.Server/Startup.cs`

**实施要点**:
1. 在 `ConfigureServices` 方法中注册泛型接口：
   ```csharp
   // 注意：新IRepository在Shared层
   services.AddScoped(typeof(LYBT.Shared.Models.Interfaces.IRepository<>), typeof(BaseRepository<>));
   ```
2. 保留旧接口注册（向后兼容）：
   ```csharp
   #pragma warning disable CS0618 // 忽略Obsolete警告
   services.AddScoped(typeof(IRepositoryLegacy<>), typeof(BaseRepository<>));
   #pragma warning restore CS0618
   ```

**验收标准**:
- [ ] 编译通过：0 errors
- [ ] DI容器启动成功
- [ ] 新旧接口都可以注入（向后兼容）

**技术要点**:
- 使用完整命名空间避免歧义
- 暂时保留旧接口注册（Phase 3-5迁移完成后删除）
- 使用 `#pragma warning` 抑制Obsolete警告

---

#### Task 2.5: 更新BaseRepository单元测试

- **工作量**: 3-4小时
- **依赖**: Task 2.3
- **类型**: Unit Test
- **优先级**: P0
- **风险**: 🟡 中

**文件范围**:
- `tests/UnitTests/Infrastructure/BaseRepositoryTests.cs`

**实施要点**:
1. 更新测试类引用新 `IRepository<T>` 接口
2. 补充15个新方法的测试用例（原有5个方法测试继承自BaseReadRepositoryTests）
3. 测试用例分类：
   - 分页查询：基础分页、高级分页、排序、过滤（8个用例）
   - 条件查询扩展：ExistsAsync、CountAsync(predicate)（4个用例）
   - 写操作：AddAsync、UpdateAsync、DeleteAsync（6个用例）
   - 批量操作：AddRangeAsync、DeleteRangeAsync（6个用例）
   - 事务：SaveChangesAsync（3个用例）

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 所有测试用例通过（新增27+ test cases，总计42+ test cases）
- [ ] 测试覆盖率≥90%
- [ ] Mock配置正确

**技术要点**:
- Mock DbContext的SaveChangesAsync返回值
- 测试批量操作的AddRangeAsync和RemoveRange
- 验证分页结果的totalCount和items正确性

---

#### Task 2.6: 验证Phase 2完成

- **工作量**: 1小时
- **依赖**: Task 2.1, Task 2.2, Task 2.3, Task 2.4, Task 2.5
- **类型**: Verification
- **优先级**: P0
- **风险**: 🟡 中

**验收标准**:
- [ ] 编译通过：0 errors（允许Obsolete warnings）
- [ ] 单元测试通过：42+ test cases, 100%通过率
- [ ] 新旧接口都可用（向后兼容）
- [ ] DI配置验证通过

**验证清单**:
1. 运行编译命令验证无错误
2. 运行单元测试验证全部通过
3. 确认新 `IRepository<T>`（Shared层）可用
4. 确认旧 `IRepositoryLegacy<T>` 标记Obsolete但仍可用

---

### Phase 3: 迁移简单聚合根模块（2天，16小时）

#### Task 3.1: 迁移Users模块到新IRepository

- **工作量**: 4-5小时
- **依赖**: Phase 2完成
- **类型**: Module Migration
- **优先级**: P0
- **风险**: 🟢 低

**文件范围**:
- `src/Server/Modules/LYBT.Server.Users/Repositories/IUserRepository.cs`
- `src/Server/Modules/LYBT.Server.Users/Repositories/UserRepository.cs`
- `tests/UnitTests/Server/Modules/Users/UserRepositoryTests.cs`

**实施要点**:
1. 更新 `IUserRepository` 接口定义：
   ```csharp
   public interface IUserRepository : IRepository<User>
   {
       // 移除重复方法：GetByIdAsync, GetAllAsync, AddAsync, UpdateAsync, DeleteAsync等
       // 保留特定方法（2个）
       Task<User?> GetByUsernameAsync(string username);
       Task<bool> IsUsernameExistsAsync(string username);
   }
   ```
2. 更新 `UserRepository` 实现类：
   ```csharp
   public class UserRepository : BaseRepository<User>, IUserRepository
   {
       public UserRepository(ApplicationDbContext context) : base(context) { }

       // 实现2个特定方法
       // 移除重复方法实现
   }
   ```
3. 更新单元测试（移除重复测试，保留特定方法测试）

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 接口只包含2个特定方法
- [ ] 单元测试通过（15+ test cases）
- [ ] 运行时验证：用户登录功能正常

**技术要点**:
- Users模块简单，业务逻辑少
- 特定方法：用户名查询、用户名唯一性检查
- 继承BaseRepository获得20个通用方法

---

#### Task 3.2: 迁移Patients模块到新IRepository

- **工作量**: 4-5小时
- **依赖**: Phase 2完成
- **类型**: Module Migration
- **优先级**: P0
- **风险**: 🟢 低

**文件范围**:
- `src/Server/Modules/LYBT.Server.Patients/Repositories/IPatientRepository.cs`
- `src/Server/Modules/LYBT.Server.Patients/Repositories/PatientRepository.cs`
- `tests/UnitTests/Server/Modules/Patients/PatientRepositoryTests.cs`

**实施要点**:
1. 更新 `IPatientRepository` 接口定义：
   ```csharp
   public interface IPatientRepository : IRepository<Patient>
   {
       // 保留特定方法（2个）
       Task<IEnumerable<Patient>> SearchPatientsAsync(string keyword);
       Task<Patient?> GetByPhoneNumberAsync(string phoneNumber);
   }
   ```
2. 更新 `PatientRepository` 实现类：
   ```csharp
   public class PatientRepository : BaseRepository<Patient>, IPatientRepository
   {
       // 实现2个特定方法
   }
   ```
3. 更新单元测试

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 接口只包含2个特定方法
- [ ] 单元测试通过（15+ test cases）
- [ ] 运行时验证：患者查询和创建功能正常

**技术要点**:
- Patients模块简单，业务逻辑少
- 特定方法：关键字搜索、手机号查询
- SearchPatientsAsync使用FindAsync(predicate)实现

---

#### Task 3.3: 迁移Herbs模块到新IRepository

- **工作量**: 4-5小时
- **依赖**: Phase 2完成
- **类型**: Module Migration
- **优先级**: P0
- **风险**: 🟢 低

**文件范围**:
- `src/Server/Modules/LYBT.Server.Herbs/Repositories/IHerbRepository.cs`
- `src/Server/Modules/LYBT.Server.Herbs/Repositories/HerbRepository.cs`
- `tests/UnitTests/Server/Modules/Herbs/HerbRepositoryTests.cs`

**实施要点**:
1. 更新 `IHerbRepository` 接口定义：
   ```csharp
   public interface IHerbRepository : IRepository<Herb>
   {
       // 保留特定方法（2个）
       Task<Herb?> GetByNameAsync(string name);
       Task<bool> ExistsByNameAsync(string name);
   }
   ```
2. 更新 `HerbRepository` 实现类：
   ```csharp
   public class HerbRepository : BaseRepository<Herb>, IHerbRepository
   {
       // 实现2个特定方法
   }
   ```
3. 更新单元测试

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 接口只包含2个特定方法
- [ ] 单元测试通过（15+ test cases）
- [ ] 运行时验证：药材查询功能正常

**技术要点**:
- Herbs模块简单，业务逻辑少
- 特定方法：名称查询、名称唯一性检查
- 批量导入药材使用AddRangeAsync（从BaseRepository）

---

#### Task 3.4: 运行时验证Phase 3完成

- **工作量**: 2-3小时
- **依赖**: Task 3.1, Task 3.2, Task 3.3
- **类型**: Runtime Verification
- **优先级**: P0
- **风险**: 🟢 低

**验证场景**:
1. **用户登录**:
   - 启动Server和Client
   - 输入用户名密码登录
   - 验证登录成功
2. **患者管理**:
   - 创建新患者
   - 查询患者列表
   - 编辑患者信息
   - 删除患者
3. **药材管理**:
   - 查询药材列表
   - 搜索特定药材
   - 验证分页功能

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 单元测试通过：45+ test cases, 100%通过率
- [ ] 用户登录功能正常
- [ ] 患者CRUD功能正常
- [ ] 药材查询功能正常
- [ ] 无运行时异常

**技术要点**:
- 完整业务流程验证，非单元测试
- 验证数据库读写正常
- 验证前后端集成无问题

---

### Phase 4: 迁移复杂聚合根模块（2.5天，20小时）

#### Task 4.1: 迁移Formula模块到新IRepository

- **工作量**: 6-7小时
- **依赖**: Phase 3完成
- **类型**: Module Migration
- **优先级**: P0
- **风险**: 🔴 高

**文件范围**:
- `src/Server/Modules/LYBT.Server.Formula/Repositories/IFormulaRepository.cs`
- `src/Server/Modules/LYBT.Server.Formula/Repositories/FormulaRepository.cs`
- `tests/UnitTests/Server/Modules/Formula/FormulaRepositoryTests.cs`

**实施要点**:
1. 更新 `IFormulaRepository` 接口定义（从Legacy迁移到Shared）：
   ```csharp
   public interface IFormulaRepository : IRepository<Formula>
   {
       // 保留特定方法（3个）
       Task<IEnumerable<Formula>> GetByCategoryAsync(string category);
       Task<PagedResult<Formula>> SearchFormulasAsync(string keyword, int pageNumber, int pageSize);
       Task<Formula?> GetByNameAsync(string name);
   }
   ```
2. 更新 `FormulaRepository` 实现类：
   ```csharp
   public class FormulaRepository : BaseRepository<Formula>, IFormulaRepository
   {
       // 实现3个特定方法
       // 移除重复方法
   }
   ```
3. 更新 `FormulaService` 确认业务逻辑不受影响
4. 更新单元测试（25+ test cases）

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 接口只包含3个特定方法
- [ ] 单元测试通过（25+ test cases）
- [ ] 运行时验证：方剂查询和创建功能正常
- [ ] 方剂分类查询正常
- [ ] 方剂搜索分页正常

**技术要点**:
- Formula模块复杂度中等，包含分类和搜索
- SearchFormulasAsync结合关键字过滤和分页
- 方剂配方关系通过导航属性加载

---

#### Task 4.2: 迁移MedicalCase模块到新IRepository

- **工作量**: 8-10小时
- **依赖**: Phase 3完成
- **类型**: Module Migration
- **优先级**: P0
- **风险**: 🔴 高（核心业务模块）

**文件范围**:
- `src/Server/Modules/LYBT.Server.MedicalCase/Repositories/IMedicalCaseRepository.cs`
- `src/Server/Modules/LYBT.Server.MedicalCase/Repositories/MedicalCaseRepository.cs`
- `src/Server/Modules/LYBT.Server.MedicalCase/Services/MedicalCaseService.cs`
- `tests/UnitTests/Server/Modules/MedicalCase/MedicalCaseRepositoryTests.cs`

**实施要点**:
1. 更新 `IMedicalCaseRepository` 接口定义：
   ```csharp
   public interface IMedicalCaseRepository : IRepository<MedicalCase>
   {
       // 保留聚合方法（5个）
       Task<MedicalCase> UpdateConsultationAsync(Guid caseId, UpdateConsultationDto dto); // BF-002 Step 1
       Task<MedicalCase> SetPrescriptionFlagAsync(Guid caseId, bool needPrescription); // BF-002 Step 2
       Task<MedicalCase> CreatePrescriptionAsync(Guid caseId, CreatePrescriptionDto dto); // BF-002 Step 3
       Task<MedicalCase> CompleteAsync(Guid caseId);
       Task<IEnumerable<MedicalCase>> GetByPatientIdAsync(Guid patientId);
   }
   ```
2. 更新 `MedicalCaseRepository` 实现类（继承BaseRepository）
3. 确保聚合方法保留（管理Consultation和Prescription生命周期）
4. 更新 `MedicalCaseService` 确认三步看诊流程不受影响
5. 更新单元测试（35+ test cases）

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 接口包含5个聚合方法
- [ ] 单元测试通过（35+ test cases）
- [ ] 三步看诊流程完整（辨证→标记→开方）
- [ ] 聚合根边界验证通过（Consultation/Prescription通过MedicalCase写入）
- [ ] 数据库验证：外键关联正确

**技术要点**:
- **核心业务模块，风险最高**
- 保留聚合方法管理从属实体（AR-001）
- 三步看诊流程（BF-002）必须完整验证
- UpdateConsultationAsync修改辨证信息
- CreatePrescriptionAsync创建处方并关联病案
- CompleteAsync完成病案状态机

---

#### Task 4.3: 运行时验证Phase 4完成（三步看诊流程）

- **工作量**: 4-5小时
- **依赖**: Task 4.1, Task 4.2
- **类型**: Runtime Verification
- **优先级**: P0
- **风险**: 🔴 高

**验证场景**:
1. **方剂管理**:
   - 查询方剂列表
   - 按分类查询方剂
   - 搜索方剂（关键字+分页）
   - 创建新方剂
2. **三步看诊流程** (BF-002):
   - Step 1: 创建病案并辨证
     - 创建患者和病案
     - 更新辨证信息（主诉、现病史、舌脉象）
     - 验证Consultation记录创建
   - Step 2: 标记处方需求
     - 调用SetPrescriptionFlag标记需要开方
     - 验证病案状态更新
   - Step 3: 开处方
     - 调用CreatePrescription创建处方
     - 选择方剂和药材
     - 验证Prescription记录创建
     - 验证病案状态变为"已完成"
3. **聚合根边界验证** (AR-001):
   - 确认Consultation无法直接修改（只读Repository）
   - 确认Prescription无法直接修改（只读Repository）
   - 确认写操作必须通过MedicalCase聚合方法
4. **数据库验证**:
   - 检查MedicalCase、Consultation、Prescription外键关联
   - 验证一诊一方约束（AR-003）

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 单元测试通过：60+ test cases, 100%通过率
- [ ] 方剂查询和创建功能正常
- [ ] 三步看诊流程完整可用（辨证→标记→开方→完成）
- [ ] Consultation/Prescription通过MedicalCase写入（聚合根边界验证）
- [ ] 数据库外键关联正确
- [ ] 无运行时异常

**技术要点**:
- **最关键的验证环节**
- 必须完整测试三步看诊流程
- 验证聚合根边界强制执行
- 数据库状态检查（SQL查询外键）

---

### Phase 5: 迁移从属实体模块（2天，16小时）

#### Task 5.1: 迁移Consultation模块到IReadRepository

- **工作量**: 5-6小时
- **依赖**: Phase 4完成
- **类型**: Module Migration
- **优先级**: P0
- **风险**: 🟡 中

**文件范围**:
- `src/Server/Modules/LYBT.Server.Consultation/Repositories/IConsultationRepository.cs` (新建)
- `src/Server/Modules/LYBT.Server.Consultation/Repositories/ConsultationRepository.cs` (新建)
- `src/Server/Modules/LYBT.Server.Consultation/Services/ConsultationService.cs`
- `tests/UnitTests/Server/Modules/Consultation/ConsultationRepositoryTests.cs` (新建)

**实施要点**:
1. 创建 `IConsultationRepository` 接口（继承 `IReadRepository<ConsultationEntity>`）：
   ```csharp
   public interface IConsultationRepository : IReadRepository<ConsultationEntity>
   {
       // 保留特定查询方法（2个）
       Task<IEnumerable<ConsultationEntity>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
       Task<ConsultationEntity?> GetByIdWithDetailsAsync(Guid id);
   }
   ```
2. 创建 `ConsultationRepository` 实现类（继承 `BaseReadRepository<ConsultationEntity>`）
3. 移除所有写操作方法（Add, Update, Delete）
4. 更新 `ConsultationService` 确认写操作通过 `IMedicalCaseService`
5. 更新依赖注入配置
6. 编写单元测试（15+ test cases，只读操作）

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 接口只包含查询方法（无写操作）
- [ ] 单元测试通过（15+ test cases）
- [ ] ConsultationService写操作通过MedicalCaseService
- [ ] 运行时验证：查询辨证记录功能正常

**技术要点**:
- **Consultation是从属实体**（AR-001）
- 只读Repository强制聚合根边界
- 写操作必须通过MedicalCase聚合方法
- GetByIdWithDetailsAsync包含导航属性

---

#### Task 5.2: 迁移Prescription模块到IReadRepository

- **工作量**: 5-6小时
- **依赖**: Phase 4完成
- **类型**: Module Migration
- **优先级**: P0
- **风险**: 🟡 中

**文件范围**:
- `src/Server/Modules/LYBT.Server.Prescription/Repositories/IPrescriptionRepository.cs` (新建)
- `src/Server/Modules/LYBT.Server.Prescription/Repositories/PrescriptionRepository.cs` (新建)
- `src/Server/Modules/LYBT.Server.Prescription/Services/PrescriptionService.cs`
- `tests/UnitTests/Server/Modules/Prescription/PrescriptionRepositoryTests.cs` (新建)

**实施要点**:
1. 创建 `IPrescriptionRepository` 接口（继承 `IReadRepository<PrescriptionEntity>`）：
   ```csharp
   public interface IPrescriptionRepository : IReadRepository<PrescriptionEntity>
   {
       // 保留特定查询方法（2个）
       Task<IEnumerable<PrescriptionEntity>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
       Task<PagedResult<PrescriptionEntity>> GetPagedWithDetailsAsync(int pageNumber, int pageSize);
   }
   ```
2. 创建 `PrescriptionRepository` 实现类（继承 `BaseReadRepository<PrescriptionEntity>`）
3. 移除所有写操作方法
4. 更新 `PrescriptionService` 确认写操作通过 `IMedicalCaseService`
5. 更新依赖注入配置
6. 编写单元测试（15+ test cases，只读操作）

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 接口只包含查询方法（无写操作）
- [ ] 单元测试通过（15+ test cases）
- [ ] PrescriptionService写操作通过MedicalCaseService
- [ ] 运行时验证：查询处方记录功能正常

**技术要点**:
- **Prescription是从属实体**（AR-001）
- 只读Repository强制聚合根边界
- 写操作必须通过MedicalCase.CreatePrescriptionAsync
- GetPagedWithDetailsAsync包含方剂和药材信息

---

#### Task 5.3: 验证聚合根边界强制执行（AR-001）

- **工作量**: 2-3小时
- **依赖**: Task 5.1, Task 5.2
- **类型**: Verification
- **优先级**: P0
- **风险**: 🟡 中

**验证场景**:
1. **编译级别验证**:
   - 尝试调用 `IConsultationRepository.AddAsync()` - 应无此方法
   - 尝试调用 `IPrescriptionRepository.UpdateAsync()` - 应无此方法
   - 确认只读接口无写方法
2. **运行时验证**:
   - 辨证信息修改必须通过 `MedicalCase.UpdateConsultationAsync`
   - 处方创建必须通过 `MedicalCase.CreatePrescriptionAsync`
   - 直接修改Consultation/Prescription实体无法保存（Service层拦截）
3. **数据库验证**:
   - 查询数据库确认Consultation/Prescription的MedicalCaseId外键正确
   - 验证级联删除规则（如果删除MedicalCase，Consultation/Prescription也删除）

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] Consultation/Prescription Repository只有查询方法
- [ ] 写操作强制通过MedicalCase聚合方法
- [ ] 运行时验证：无法直接写入从属实体
- [ ] 数据库外键和级联规则正确

**技术要点**:
- **架构级别强制聚合根边界**
- IReadRepository机制杜绝直接写入
- Service层二次验证（防御性编程）

---

#### Task 5.4: 运行时验证Phase 5完成

- **工作量**: 2-3小时
- **依赖**: Task 5.1, Task 5.2, Task 5.3
- **类型**: Runtime Verification
- **优先级**: P0
- **风险**: 🟡 中

**验证场景**:
1. **查询辨证记录**:
   - 根据病案ID查询辨证记录
   - 查询辨证详情（包含关联数据）
2. **查询处方记录**:
   - 根据病案ID查询处方记录
   - 分页查询处方列表（包含方剂药材信息）
3. **三步看诊流程回归测试**:
   - 完整执行三步看诊流程
   - 验证Consultation和Prescription正确创建
   - 验证聚合根边界不被破坏

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 单元测试通过：30+ test cases, 100%通过率
- [ ] 辨证记录查询功能正常
- [ ] 处方记录查询功能正常
- [ ] 三步看诊流程回归测试通过
- [ ] 聚合根边界验证通过

---

### Phase 6: 补全批量操作与文档更新（2天，16小时）

#### Task 6.1: 补全IRepository批量操作方法实现

- **工作量**: 3-4小时
- **依赖**: Phase 5完成
- **类型**: Enhancement
- **优先级**: P1
- **风险**: 🟢 低

**文件范围**:
- `src/Shared/LYBT.Shared.Models/Interfaces/IRepository.cs`
- `src/Infrastructure/LYBT.Infrastructure/Persistence/BaseRepository.cs`

**实施要点**:
1. 确认 `IRepository<T>` 已定义3个批量操作方法：
   - `Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)`
   - `Task<int> DeleteRangeAsync(IEnumerable<T> entities)`
   - `Task<int> DeleteRangeAsync(IEnumerable<Guid> ids)`
2. 确认 `BaseRepository<T>` 已实现这3个方法
3. 补充高级分页方法（如果Phase 2未完成）：
   - `Task<PagedResult<T>> GetPagedAsync(predicate, pageNumber, pageSize, orderBy, ascending)`
4. 优化批量操作性能（使用EF Core原生API）

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 3个批量操作方法实现完整
- [ ] 高级分页方法实现完整
- [ ] 符合设计文档第2.2节和第3.2节

**技术要点**:
- AddRangeAsync使用 `DbSet.AddRangeAsync`
- DeleteRangeAsync(entities)使用 `DbSet.RemoveRange`
- DeleteRangeAsync(ids)先查询再批量删除
- 高级分页支持动态排序和过滤

---

#### Task 6.2: 批量操作性能测试

- **工作量**: 3-4小时
- **依赖**: Task 6.1
- **类型**: Performance Test
- **优先级**: P1
- **风险**: 🟢 低

**文件范围**:
- `tests/PerformanceTests/Repository/BatchOperationPerformanceTests.cs` (新建)

**实施要点**:
1. 编写批量操作性能测试：
   - 测试批量插入1000条药材记录
   - 测试批量删除1000条记录
   - 测试分页查询1万条记录
2. 设定性能基准：
   - 批量插入1000条 < 5秒
   - 批量删除1000条 < 5秒
   - 分页查询1万条翻页 < 1秒
3. 使用BenchmarkDotNet或Stopwatch计时
4. 记录性能测试结果

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 批量插入1000条 < 5秒
- [ ] 批量删除1000条 < 5秒
- [ ] 分页查询性能 < 1秒/页
- [ ] 性能测试报告生成

**技术要点**:
- 使用真实数据库（非Mock）
- 测试前清空数据库
- 使用事务包装批量操作
- 记录执行时间和内存占用

---

#### Task 6.3: 补全单元测试覆盖率

- **工作量**: 3-4小时
- **依赖**: Task 6.1
- **类型**: Unit Test
- **优先级**: P1
- **风险**: 🟢 低

**文件范围**:
- `tests/UnitTests/Infrastructure/BaseRepositoryTests.cs`
- 各模块Repository测试文件

**实施要点**:
1. 补充批量操作测试用例（15+ test cases）：
   - AddRangeAsync: 空列表、正常列表、大数据量（3个用例）
   - DeleteRangeAsync(entities): 空列表、正常删除、部分失败（3个用例）
   - DeleteRangeAsync(ids): 空列表、正常删除、不存在的ID（3个用例）
   - 批量操作事务测试（3个用例）
   - 批量操作异常处理（3个用例）
2. 补充高级分页测试用例（10+ test cases）：
   - 动态过滤、排序、分页组合（5个用例）
   - 空结果集、边界条件（3个用例）
   - 异常场景（2个用例）
3. 运行代码覆盖率工具确认≥90%

**验收标准**:
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 所有测试用例通过（新增25+ test cases）
- [ ] 总测试用例数≥157个
- [ ] 代码覆盖率≥90%
- [ ] 使用coverlet生成覆盖率报告

**技术要点**:
- 使用coverlet收集覆盖率数据
- 命令：`dotnet test --collect:"XPlat Code Coverage"`
- 生成HTML报告：`reportgenerator`
- 覆盖率目标：Repository层≥90%

---

#### Task 6.4: 更新架构文档

- **工作量**: 3-4小时
- **依赖**: Phase 5完成
- **类型**: Documentation
- **优先级**: P1
- **风险**: 🟢 低

**文件范围**:
- `CLAUDE.md`（第2.4节）
- `docs/explanation/architecture/patterns/repository-pattern.md`
- `docs/explanation/architecture/server/README.md`
- `docs/explanation/architecture/shared/README.md`

**实施要点**:
1. 更新 `CLAUDE.md` 第2.4节：
   - 更新Repository架构规范
   - 新增三层接口架构说明（IReadRepository → IRepository → IXxxRepository）
   - 更新模块分类表（聚合根 vs 从属实体）
2. 更新 `repository-pattern.md`：
   - 新增IReadRepository<T>模式说明
   - 更新IRepository<T>接口定义（20个方法）
   - 新增从属实体Repository模式（只读）
3. 更新 `server/README.md`：
   - 更新Infrastructure层说明
   - 新增BaseReadRepository<T>说明
4. 更新 `shared/README.md`：
   - 更新IRepository<T>接口位置说明（Shared层）

**验收标准**:
- [ ] 所有文档更新完整
- [ ] 架构图准确（Mermaid图表）
- [ ] 代码示例正确
- [ ] 文档间交叉引用正确

**技术要点**:
- 使用Mermaid绘制架构图
- 提供完整代码示例
- 标注设计决策理由（ADR风格）

---

#### Task 6.5: 创建迁移指南文档

- **工作量**: 2-3小时
- **依赖**: Phase 5完成
- **类型**: Documentation
- **优先级**: P1
- **风险**: 🟢 低

**文件范围**:
- `docs/guides/repository-migration-guide.md` (新建)

**实施要点**:
1. 创建迁移指南文档，包含：
   - 从IBaseRepository迁移到IRepository（聚合根）
   - 从旧IRepository迁移到新IRepository（聚合根）
   - 创建只读Repository指南（从属实体）
2. 提供完整代码示例：
   - Before/After对比
   - 步骤清单
   - 常见问题FAQ
3. 迁移检查清单：
   - 接口继承关系检查
   - 方法移除清单
   - 依赖注入配置更新
   - 单元测试更新

**验收标准**:
- [ ] 迁移指南文档完整
- [ ] 包含3种迁移场景
- [ ] 代码示例清晰
- [ ] 检查清单完整

**技术要点**:
- 使用表格对比Before/After
- 提供可执行的代码片段
- 标注风险点和注意事项

---

#### Task 6.6: 清理工作与最终验证

- **工作量**: 2-3小时
- **依赖**: Task 6.1, Task 6.2, Task 6.3, Task 6.4, Task 6.5
- **类型**: Cleanup & Verification
- **优先级**: P1
- **风险**: 🟢 低

**清理工作**:
1. 删除 `IRepositoryLegacy<T>` 接口（如果已v1.1版本）
2. 清理过时代码注释和 `#pragma warning` 指令
3. 检查未使用的using语句
4. 格式化所有修改的代码文件

**最终验证清单**:
- [ ] 编译通过：`dotnet build LYBT.All.sln -c Release` 0 errors, ≤5 warnings
- [ ] 单元测试通过：157+ test cases, 100%通过率, 覆盖率≥90%
- [ ] 性能测试通过：批量操作<5秒/1000条
- [ ] 运行时验证通过：三步看诊流程完整
- [ ] 文档更新完整：架构文档+迁移指南
- [ ] Git提交记录清晰：每个Phase一个提交

**验收标准**:
- [ ] 所有清理工作完成
- [ ] 最终验证清单全部通过
- [ ] 准备合并到master分支

---

## 📊 任务统计

| 统计项 | 数量 |
|-------|------|
| **总任务数** | 26个 |
| **总工作量** | 80-96小时（12工作日） |
| **Phase数量** | 6个阶段 |
| **关键路径长度** | 12个任务 |
| **高风险任务** | 2个（Task 4.1, Task 4.2） |
| **中风险任务** | 5个（Phase 2和Phase 5） |
| **低风险任务** | 19个 |

---

## 🔗 依赖关系图

### Phase 1依赖
```
Task 1.1 (无依赖)
  ├─> Task 1.2
  │     ├─> Task 1.3
  │     └─> Task 1.4
  └─> Task 1.5 (依赖1.1+1.2+1.3+1.4)
```

### Phase 2依赖
```
Phase 1完成
  ├─> Task 2.1
  │     ├─> Task 2.3
  │     │     └─> Task 2.4
  │     │           └─> Task 2.5
  │     └─> Task 2.2
  └─> Task 2.6 (依赖所有Task 2.x)
```

### Phase 3依赖（并行）
```
Phase 2完成
  ├─> Task 3.1 (Users)
  ├─> Task 3.2 (Patients)   } 可并行
  └─> Task 3.3 (Herbs)
        └─> Task 3.4 (依赖3.1+3.2+3.3)
```

### Phase 4依赖（顺序）
```
Phase 3完成
  ├─> Task 4.1 (Formula) - 建议先完成
  └─> Task 4.2 (MedicalCase) - 依赖4.1稳定
        └─> Task 4.3 (依赖4.1+4.2)
```

### Phase 5依赖（并行）
```
Phase 4完成
  ├─> Task 5.1 (Consultation)
  └─> Task 5.2 (Prescription)   } 可并行
        ├─> Task 5.3 (聚合根边界验证)
        └─> Task 5.4 (依赖5.1+5.2+5.3)
```

### Phase 6依赖（顺序）
```
Phase 5完成
  └─> Task 6.1 (批量操作)
        ├─> Task 6.2 (性能测试)
        ├─> Task 6.3 (单元测试)
        ├─> Task 6.4 (架构文档)
        └─> Task 6.5 (迁移指南)
              └─> Task 6.6 (清理验证)
```

### 跨Phase关键路径
```
Task 1.1 → Task 1.2 → Task 1.5 (Phase 1完成)
  → Task 2.1 → Task 2.3 → Task 2.6 (Phase 2完成)
    → Task 3.1 → Task 3.4 (Phase 3完成)
      → Task 4.2 → Task 4.3 (Phase 4完成)
        → Task 5.3 → Task 5.4 (Phase 5完成)
          → Task 6.1 → Task 6.6 (Phase 6完成)
```

---

## ⚠️ 关键路径

**主线任务**（必须按顺序完成）：
1. Task 1.1: 创建IReadRepository接口
2. Task 1.2: 创建BaseReadRepository实现
3. Task 1.5: 验证Phase 1完成
4. Task 2.1: 重命名IBaseRepository为IRepository
5. Task 2.3: 更新BaseRepository继承
6. Task 2.6: 验证Phase 2完成
7. Task 3.1: 迁移Users模块（或3.2/3.3任一）
8. Task 3.4: 验证Phase 3完成
9. Task 4.2: 迁移MedicalCase模块 🔴 **最高风险**
10. Task 4.3: 验证三步看诊流程 🔴 **最关键验证**
11. Task 5.3: 验证聚合根边界
12. Task 5.4: 验证Phase 5完成

**并行任务**（可同时进行）：
- **Phase 1**: Task 1.3 和 Task 1.4 可与 Task 1.2 并行（都依赖Task 1.1）
- **Phase 2**: Task 2.2 可与 Task 2.3 并行（都依赖Task 2.1）
- **Phase 3**: Task 3.1, 3.2, 3.3 完全并行（都依赖Phase 2，互不依赖）
- **Phase 5**: Task 5.1 和 5.2 完全并行（都依赖Phase 4，互不依赖）
- **Phase 6**: Task 6.2, 6.3, 6.4, 6.5 可部分并行（都依赖Task 6.1）

**高风险任务标注**:
- 🔴 **Task 4.1**: Formula模块迁移（业务逻辑复杂）
- 🔴 **Task 4.2**: MedicalCase模块迁移（核心业务，三步看诊流程）
- 🔴 **Task 4.3**: 三步看诊流程验证（最关键验证环节）

---

## 📝 实施建议

### 优先级排序

1. **🔴 P0级任务**（必须完成，12个）:
   - Phase 1-5 所有任务（建立架构+迁移核心模块）
   - Task 6.6 最终验证

2. **🟡 P1级任务**（增强功能，5个）:
   - Phase 6 文档和性能测试（Task 6.1-6.5）

3. **🟢 P2级任务**（可选优化，0个）:
   - 无

### 并行策略

1. **Phase 1**: 1个开发者线性完成（基础设施）
2. **Phase 2**: 1个开发者线性完成（重命名需全局协调）
3. **Phase 3**: 可3个开发者并行（Users/Patients/Herbs独立）
4. **Phase 4**: 建议线性完成（Formula → MedicalCase，后者依赖前者稳定）
5. **Phase 5**: 可2个开发者并行（Consultation/Prescription独立）
6. **Phase 6**: 可多人并行（文档+测试+性能）

### 风险提示

1. **Phase 4 (Task 4.2) 最高风险**:
   - MedicalCase模块是核心业务
   - 三步看诊流程复杂
   - **缓解**: 完整回归测试，详细运行时验证，数据库状态检查
2. **Phase 2 重命名影响广**:
   - 预计50+处引用需要更新
   - **缓解**: 使用IDE重构工具自动更新，编译器检查遗漏
3. **Phase 5 聚合根边界验证**:
   - 必须确保Consultation/Prescription无法直接写入
   - **缓解**: 编译级别强制+运行时验证+数据库检查

### 时间估算建议

- **理想情况**（无阻塞）: 12工作日
- **保守估算**（含缓冲）: 15工作日
- **最坏情况**（Phase 4重大问题）: 18工作日

### 里程碑检查点

| 里程碑 | 完成标志 | 预计时间 |
|-------|---------|---------|
| **M1**: Phase 1完成 | IReadRepository和BaseReadRepository可用 | Day 2 |
| **M2**: Phase 2完成 | IRepository统一命名 | Day 3.5 |
| **M3**: Phase 3完成 | 简单聚合根迁移完成 | Day 5.5 |
| **M4**: Phase 4完成 | 三步看诊流程验证通过 | Day 8 |
| **M5**: Phase 5完成 | 聚合根边界强制执行 | Day 10 |
| **M6**: Phase 6完成 | 文档和性能测试完成 | Day 12 |

---

## 🧪 测试策略

### 单元测试覆盖计划

| 测试类别 | 测试用例数 | 覆盖目标 |
|---------|----------|---------|
| **IReadRepository<T>** | 15+ | 5个查询方法 × 3场景 |
| **IRepository<T> 扩展** | 27+ | 15个扩展方法 × 平均2场景 |
| **Users模块** | 15+ | IUserRepository特定方法 |
| **Patients模块** | 15+ | IPatientRepository特定方法 |
| **Herbs模块** | 15+ | IHerbRepository特定方法 |
| **Formula模块** | 25+ | IFormulaRepository复杂查询 |
| **MedicalCase模块** | 35+ | 聚合方法+三步流程 |
| **Consultation模块** | 15+ | 只读查询方法 |
| **Prescription模块** | 15+ | 只读查询方法 |
| **批量操作** | 15+ | AddRangeAsync/DeleteRangeAsync |
| **高级分页** | 10+ | 动态过滤+排序 |
| **合计** | **157+** | **覆盖率目标≥90%** |

### 集成测试场景

| 场景编号 | 业务场景 | 验证点 |
|---------|---------|-------|
| **IT-001** | 用户登录 | UserRepository完整CRUD |
| **IT-002** | 患者管理 | PatientRepository分页查询 |
| **IT-003** | 药材管理 | HerbRepository搜索功能 |
| **IT-004** | 方剂管理 | FormulaRepository分类查询 |
| **IT-005** | 三步看诊 | MedicalCase聚合方法+Consultation/Prescription查询 |

### 性能测试基准

| 测试项 | 目标 | 验证方式 |
|-------|------|---------|
| **批量插入** | 1000条 <5秒 | BenchmarkDotNet |
| **批量删除** | 1000条 <5秒 | BenchmarkDotNet |
| **分页查询** | 1万条翻页 <1秒 | Stopwatch计时 |

---

## 💡 下一步行动

### 立即行动

1. ✅ **审查task文档**: 确认任务拆分合理
2. ✅ **调整任务粒度**: 如需要，细化或合并任务
3. 🚀 **批量生成Issues**: 使用 `lybtzyzs-issue-template` skill
4. 🚀 **启动Phase 1**: Task 1.1 创建IReadRepository接口

### 批量生成Issues命令

```bash
# 使用lybtzyzs-issue-template skill读取本task文档
# 自动生成26个GitHub Issues
# 每个Issue包含：标题、描述、验收标准、工作量、依赖关系、标签
```

**Issue标签建议**:
- `priority:p0` / `priority:p1`
- `phase:1` / `phase:2` / ... / `phase:6`
- `type:repository` / `type:test` / `type:docs`
- `risk:high` / `risk:medium` / `risk:low`
- `epic:repository-refactoring`

---

**文档元数据**:
- **生成时间**: 2025-11-11
- **生成工具**: lybtzyzs-task-breakdown skill
- **Epic编号**: #1498
- **文档版本**: v1.0
- **维护者**: Claude Code

**变更历史**:
- v1.0 (2025-11-11): 初始版本，完成6个Phase任务拆分
