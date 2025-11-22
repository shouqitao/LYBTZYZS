# 代码中Emoji表情符号清理清单

生成时间: 2025-11-19

## 清理规则
- **日志输出**: emoji → 纯文本标签 (如 `❌` → `[ERROR]`, `✅` → `[SUCCESS]`, `🔍` → `[DEBUG]`)
- **注释**: emoji → 纯文本标记 (如 `⚠️` → `[WARNING]` 或 `注意`)
- **测试输出**: emoji → 纯文本描述

---

## 一、C#代码文件 (共21个文件)

### 1. Client端 - Shell项目 (3个文件)

**MainWindowViewModel.cs** (已部分清理)
- `src\Client\Desktop\Shell\ViewModels\MainWindowViewModel.cs`
- 第328行: `❌ CheckLoginStatusAsync 异常`
- 第415行: `❌ 角色导航失败`
- 第539行: `❌ [MainWindowViewModel] 资源清理异常`
- 第589行: `❌ [MainWindowViewModel] 取消EventAggregator订阅失败`

**StartupPerformanceMonitor.cs**
- `src\Client\Desktop\Shell\Services\StartupPerformanceMonitor.cs`
- 第107行: `❌ 启动性能: 较慢 (> 5秒)`

**NavigationManager.cs**
- `src\Client\Desktop\Shell\Services\NavigationManager.cs`
- 第129行: `❌ [NavigationManager] 取消Region监控失败`
- 第165行: `❌ 导航失败: Region={region.Name}`

### 2. Client端 - 业务模块 (4个文件)

**HerbManagementViewModel.cs**
- `src\Client\Desktop\Modules\LYBT.Desktop.Herbs\ViewModels\HerbManagementViewModel.cs`
- 第419行: `❌ 失败：{result.FailureCount}条`

**UserManagementViewModel.cs**
- `src\Client\Desktop\Modules\LYBT.Desktop.Users\ViewModels\UserManagementViewModel.cs`
- 第670行: `❌ 失败：{result.FailureCount}条`

**PatientManagementViewModel.cs**
- `src\Client\Desktop\Modules\LYBT.Desktop.Patients\ViewModels\PatientManagementViewModel.cs`
- 第284行: `❌ 失败：{result.FailureCount}条`

**MedicalCaseLifecycleHandler.cs**
- `src\Client\Desktop\Modules\LYBT.Desktop.MedicalCase\Services\MedicalCaseLifecycleHandler.cs`
- 第71行: `❌ DataManager返回null，创建失败`
- 第89行: `❌ 创建MedicalCase失败`
- 第294行: `❌ SessionManager为null`
- 第301行: `❌ SessionManager.CurrentUser为null`

### 3. Server端 - WebAPI项目 (3个文件)

**Program.cs**
- `src\Server\Services\LYBT.WebAPI\Program.cs`
- 第73行: `❌ Production 配置验证失败`

**DatabaseStartupDiagnostics.cs**
- `src\Server\Services\LYBT.WebAPI\HealthCheck\DatabaseStartupDiagnostics.cs`
- 第24行: `🔍 [DatabaseStartupDiagnostics] 开始数据库连接诊断...`
- 第32行: `❌ [DatabaseStartupDiagnostics] 未找到连接字符串`
- 第44行: `📊 [DatabaseStartupDiagnostics] 连接信息:`
- 第64行: `📊 [DatabaseStartupDiagnostics] 连接池配置:`
- 第74行: `❌ [DatabaseStartupDiagnostics] SQL Server连接失败！`
- 第79行: `🔧 [DatabaseStartupDiagnostics] 故障排查建议:`
- 第106行: `❌ [DatabaseStartupDiagnostics] 数据库诊断过程中发生未知错误`

**EnvironmentAwareHosting.cs**
- `src\Server\Services\LYBT.WebAPI\Extensions\EnvironmentAwareHosting.cs`
- 第101行: `[启动] ❌ 数据库连接失败`

### 4. Server端 - Infrastructure (1个文件)

**ProductionConfigurationValidator.cs**
- `src\Server\Core\LYBT.Infrastructure\Configuration\Validation\ProductionConfigurationValidator.cs`
- 第158行: `❌ Production 配置验证失败`
- 第190行: `🔧 验证脚本:`

### 5. Server端 - MedicalCase模块 (1个文件)

**MedicalCaseRepository.cs**
- `src\Server\Modules\LYBT.Module.MedicalCase\Repositories\MedicalCaseRepository.cs`
- 第123行: `🔍 [诊断] UpdateAsync开始`
- 第129行: `🔍 [诊断] Prescription状态`
- 第141行: `🔧 [修复] 检测到新Prescription被错误标记为Modified`
- 第206行: `🔍 [诊断] SaveChangesAsync前`
- 第212行: `🔍   - {EntityType}`

### 6. 测试项目 (3个文件)

**MedicalCaseControllerIntegrationTests.cs**
- `tests\IntegrationTests\WebAPI.IntegrationTests\Controllers\MedicalCaseControllerIntegrationTests.cs`
- 第57行: `⚠️ 注意：此时_output还未初始化`
- 第67行: `🔍 使用测试患者ID`
- 第78行: `⚠️ 临时调试代码`
- 第473行: `⚠️ Issue #1669 Phase 6`
- 第479行: `⚠️ 模拟审计字段的用户ID`
- 第481行: `⚠️ 在数据库中创建患者实体`
- 第494行: `⚠️ Issue #1669: 必须设置CreatedBy`
- 第499行: `✅ 患者实体已创建`
- 第510行: `⚠️ 临时调试代码：打印错误的详细信息`
- 第514-516行: `❌ 创建病案失败` (3处)
- 第542行: `⚠️ Issue #1669: 验证更新请求`
- 第564行: `⚠️ Issue #1669: 验证标记请求`
- 第617行: `⚠️ Issue #1669: 验证完成请求`

**AuthControllerIntegrationTests.cs**
- `tests\IntegrationTests\WebAPI.IntegrationTests\Controllers\AuthControllerIntegrationTests.cs`
- 第58行: `✅ 创建测试用户`
- 第67行: `📝 测试场景: Token撤销后刷新应返回401`
- 第113行: `✅ 验证通过`
- 第124行: `📝 测试场景: 登录成功应记录审计日志`
- 第140行: `✅ 登录成功`
- 第177行: `📝 测试场景: Token刷新应撤销旧Token并生成新Token`
- 第228行: `✅ 旧Token已撤销:`
- 第242行: `✅ 新Token已创建:`

**MedicalCaseBusinessRulesTests.cs**
- `tests\IntegrationTests\Server\Modules\LYBT.Module.MedicalCase.IntegrationTests\MedicalCaseBusinessRulesTests.cs`
- 第62-67行: InlineData注释中的 `✅` 和 `❌` (6处)

---

## 二、XAML文件 (共7个文件)

**UserProfileDialog.xaml**
- `src\Client\Desktop\Modules\LYBT.Desktop.Users\Views\UserProfileDialog.xaml`

**ChangePasswordDialog.xaml**
- `src\Client\Desktop\Modules\LYBT.Desktop.Users\Views\ChangePasswordDialog.xaml`

**UnifiedManagementToolBar.xaml**
- `src\Client\Desktop\Core\LYBT.Desktop.Infrastructure\Controls\UnifiedManagementToolBar.xaml`

**Controls.xaml**
- `src\Client\Desktop\Shell\Styles\Controls.xaml`

**PrescriptionManagementView.xaml**
- `src\Client\Desktop\Modules\LYBT.Desktop.Prescriptions\Views\PrescriptionManagementView.xaml`

**UnfinishedCaseDialog.xaml**
- `src\Client\Desktop\Modules\LYBT.Desktop.Patients\Views\UnfinishedCaseDialog.xaml`

**PatientSelectionView.xaml**
- `src\Client\Desktop\Modules\LYBT.Desktop\Patients\Views\PatientSelectionView.xaml`

---

## 三、Emoji替换建议

### 日志标记类
- `🔍` → `[DEBUG]` 或 `[TRACE]`
- `📊` → `[INFO]` 或 `[STATS]`
- `🔧` → `[FIX]` 或 `[REPAIR]`
- `❌` → `[ERROR]` 或 `[FAILED]`
- `✅` → `[SUCCESS]` 或 `[PASSED]`
- `⚠️` → `[WARNING]` 或 `[NOTICE]`
- `📝` → `[TEST]` 或 `[SCENARIO]`

### 注释标记类
- `⚠️` → `注意:` 或 `[WARNING]`
- `✅` → `正确` 或 `通过`
- `❌` → `错误` 或 `失败`

---

## 四、清理优先级

### 高优先级 (生产代码)
1. **DatabaseStartupDiagnostics.cs** - 数据库诊断日志 (7处emoji)
2. **MedicalCaseRepository.cs** - 业务仓储诊断日志 (5处emoji)
3. **MainWindowViewModel.cs** - 主窗口错误日志 (4处emoji)
4. **MedicalCaseLifecycleHandler.cs** - 生命周期错误日志 (4处emoji)
5. **ProductionConfigurationValidator.cs** - 生产配置验证 (2处emoji)

### 中优先级 (用户提示)
6. **HerbManagementViewModel.cs** - 导入结果提示 (1处)
7. **UserManagementViewModel.cs** - 导入结果提示 (1处)
8. **PatientManagementViewModel.cs** - 导入结果提示 (1处)
9. **NavigationManager.cs** - 导航错误日志 (2处)
10. **StartupPerformanceMonitor.cs** - 性能警告 (1处)

### 低优先级 (测试代码)
11. **MedicalCaseControllerIntegrationTests.cs** - 测试输出 (约16处)
12. **AuthControllerIntegrationTests.cs** - 测试输出 (约8处)
13. **MedicalCaseBusinessRulesTests.cs** - 测试注释 (6处)

### XAML文件 (需人工检查)
14. 7个XAML文件 - 可能在按钮文本或ToolTip中使用emoji

---

## 五、统计汇总

- **C#代码文件**: 21个文件，约70+处emoji使用
- **XAML文件**: 7个文件 (需逐个检查)
- **总计**: 约28个文件需要清理

---

**建议**: 从高优先级的生产代码开始清理，保证核心功能的日志输出符合规范。
