# Issue #1114 Phase 1 执行计划

**文档日期**: 2025-10-10
**规划范围**: Desktop 基础设施重组（Foundation + Presentation 项目创建）
**预估工期**: 7-10 小时（采用分阶段 + 风险缓解策略）
**优先级**: P3（可选架构优化）

---

## 📋 执行摘要

### 目标
将 Desktop.Services 拆分为三个职责明确的项目：
- **LYBT.Desktop.Foundation**：技术基础设施（HTTP、缓存、配置、安全、性能）
- **LYBT.Desktop.Presentation**：UI 基础设施（导航、通知、主题、打印、用户体验）
- **LYBT.Desktop.Services**：保留认证服务（Auth、TokenStorage 等）

### 预期收益
- ✅ 命名空间清晰度提升（技术/UI 职责分离）
- ✅ 开发便利性增强（新增功能时归属明确）
- ✅ 架构一致性（对齐 Server 端分层理念）

### 关键风险
- ⚠️ 130+ 文件修改（命名空间引用更新）
- ⚠️ 50+ 命名空间变更（可能遗漏引用）
- ⚠️ Prism DI 配置调整（运行时错误风险）

---

## 📂 当前 Desktop.Services 目录分类

### A. 技术基础设施 → Foundation（15个目录）

| 目录 | 职责 | 文件数（预估） |
|------|------|---------------|
| **Http** | ApiService, AuthorizationMessageHandler, RetryPolicy | 8 |
| **Caching** | MemoryCache 封装 | 3 |
| **Configuration** | 配置绑定与验证 | 4 |
| **Security** | 加密、签名、Token 验证 | 5 |
| **Performance** | 性能监控、启动优化 | 6 |
| **Diagnostics** | 日志、诊断工具 | 4 |
| **ErrorHandling** | 统一错误处理 | 5 |
| **Exceptions** | 自定义异常类 | 3 |
| **Handlers** | HTTP 消息处理器 | 2 |
| **HealthCheck** | API 健康检查 | 3 |
| **Session** | 用户会话管理 | 4 |
| **Settings** | 设置持久化 | 3 |
| **Api** | API 客户端管理 | 2 |
| **Repositories** | BaseApiRepository（基类） | 1 |
| **Extensions** | 服务注册扩展 | 2 |

**小计**: 约 55 个文件

---

### B. UI 基础设施 → Presentation（5个目录）

| 目录 | 职责 | 文件数（预估） |
|------|------|---------------|
| **Navigation** | Prism Region 导航服务 | 4 |
| **Notifications** | 用户通知服务 | 5 |
| **Print** | 处方打印服务 | 3 |
| **Theming** | 主题管理服务 | 4 |
| **UserExperience** | 用户体验优化（快捷键、反馈） | 5 |

**小计**: 约 21 个文件

---

### C. 保留在 Desktop.Services（3个目录 + 3个根文件）

| 目录/文件 | 职责 | 保留原因 |
|-----------|------|----------|
| **Business** | AuthService, TokenStorageService 等6个认证服务 | MVP 核心功能 |
| **Auth** | 认证相关辅助类 | 与 Business 强耦合 |
| **Interfaces** | ILocalAuthService 等接口 | 认证服务接口 |
| **Modules** | 模块加载服务 | 跨层服务 |
| ServiceRegistration.cs | 服务注册入口 | DI 配置根 |
| BaseApiService.cs | API 服务基类 | 认证依赖 |

**小计**: 约 15 个文件

---

## 🎯 分阶段执行方案

### Phase 1.1：创建 Foundation + 迁移核心技术基础设施（2-3小时）

#### 任务清单
1. ✅ 创建 `LYBT.Desktop.Foundation.csproj`
   - 目标框架：net8.0-windows
   - 依赖：Shared.Models, Shared.Interfaces, Shared.Utilities
   - NuGet: Prism.Core, Microsoft.Extensions.Caching.Memory, Polly, Refit
2. 迁移目录（优先核心依赖）：
   - Http/
   - Caching/
   - Configuration/
   - Exceptions/
   - ErrorHandling/
3. 更新命名空间：
   - `LYBT.Desktop.Services.Http` → `LYBT.Desktop.Foundation.Http`
   - `LYBT.Desktop.Services.Caching` → `LYBT.Desktop.Foundation.Caching`
   - ...（5个目录）
4. 更新项目引用：
   - Shell.csproj 添加 Foundation 引用
   - 7个业务模块添加 Foundation 引用（如需 IApiService）
5. 编译验证：`dotnet build LYBT.Desktop.sln -c Release`

#### 验收标准
- ✅ Foundation.csproj 编译通过
- ✅ 所有 using LYBT.Desktop.Services.Http 更新为 Foundation.Http
- ✅ Shell 和业务模块正常引用 Foundation

#### 风险缓解
- 使用 `mcp__serena__replace_regex` 批量替换命名空间
- 每次迁移后立即编译（快速失败）
- Git 提交独立阶段（可回滚）

---

### Phase 1.2：迁移剩余技术基础设施（2-3小时）

#### 任务清单
1. 迁移目录：
   - Security/
   - Performance/
   - Diagnostics/
   - Handlers/
   - HealthCheck/
   - Session/
   - Settings/
   - Api/
   - Repositories/BaseApiRepository.cs
   - Extensions/ServiceCollectionExtensions.cs
2. 更新命名空间（10个目录）
3. 更新 Prism DI 配置：
   - Shell/Extensions/ServiceCollectionExtensions.cs
   - 调整 Foundation 服务注册逻辑
4. 编译验证

#### 验收标准
- ✅ Foundation 包含全部15个技术基础设施目录
- ✅ BaseApiRepository 正确迁移（7个业务 Repository 依赖）
- ✅ DI 配置正确（HttpClient、IApiService 正常注册）

#### 风险缓解
- BaseApiRepository 迁移前运行完整测试套件（建立基线）
- DI 配置调整后运行 Desktop 应用（手动冒烟测试）

---

### Phase 1.3：创建 Presentation + 迁移 UI 基础设施（2-3小时）

#### 任务清单
1. 创建 `LYBT.Desktop.Presentation.csproj`
   - 目标框架：net8.0-windows
   - UseWPF：true
   - 依赖：Shared.Models, Foundation（IApiService 等）
   - NuGet: Prism.Wpf, MaterialDesignThemes
2. 迁移目录：
   - Navigation/
   - Notifications/
   - Print/
   - Theming/
   - UserExperience/
3. 更新命名空间（5个目录）
4. 更新项目引用：
   - Shell.csproj 添加 Presentation 引用
   - 7个业务模块添加 Presentation 引用（如需 INavigationService）
5. 编译验证

#### 验收标准
- ✅ Presentation.csproj 编译通过
- ✅ 所有 using LYBT.Desktop.Services.Navigation 更新为 Presentation.Navigation
- ✅ WPF 资源字典正确加载（主题、通知）

#### 风险缓解
- Theming 迁移前备份 Shell/App.xaml 的 ResourceDictionary
- Navigation 迁移后测试 Region 导航（LoginView → MainWindowView）

---

### Phase 1.4：最终清理 + 删除 Desktop.Services（1-2小时）

#### 任务清单
1. 验证 Desktop.Services 仅剩：
   - Business/（6个认证服务）
   - Auth/
   - Interfaces/
   - Modules/
   - ServiceRegistration.cs
   - BaseApiService.cs
2. 重命名（可选）：
   - `LYBT.Desktop.Services` → `LYBT.Desktop.Auth`（仅保留认证职责）
   - 或保持 Services 名称（兼容性考虑）
3. 更新 Solution 文件：
   - 添加 Foundation.csproj
   - 添加 Presentation.csproj
4. 全量编译验证：
   - `dotnet build LYBT.All.sln -c Release`
   - `dotnet test LYBT.Desktop.sln -c Release`（如有 Desktop 测试）
5. 生成完成报告

#### 验收标准
- ✅ Desktop.Services 仅包含认证相关代码（15个文件）
- ✅ All.sln 包含 Foundation + Presentation
- ✅ Release 编译 0 错误 0 警告
- ✅ 架构测试通过（DesktopLayerArchTests）

#### 风险缓解
- 最终清理前创建 Git tag（v1.0-before-phase1.4）
- 测试核心用户流程：登录 → 患者管理 → 处方开立

---

## 🛠️ 自动化工具链

### 命名空间批量替换脚本（PowerShell）

```powershell
# 示例：替换 Services.Http → Foundation.Http
$files = Get-ChildItem -Recurse -Include *.cs,*.xaml -Path "src/Client/Desktop"
foreach ($file in $files) {
    (Get-Content $file.FullName) -replace 'LYBT\.Desktop\.Services\.Http', 'LYBT.Desktop.Foundation.Http' |
    Set-Content $file.FullName -Encoding UTF8
}
```

### 引用完整性验证脚本

```powershell
# 检查是否还有 Services.Http 的遗留引用
Select-String -Path "src/Client/Desktop/**/*.cs" -Pattern "using LYBT\.Desktop\.Services\.(Http|Caching|Security)" -List
```

### 编译验证脚本

```bash
# 增量编译验证
dotnet build LYBT.Desktop.Foundation.csproj -c Release
dotnet build LYBT.Desktop.Presentation.csproj -c Release
dotnet build LYBT.Desktop.sln -c Release
```

---

## 📋 命名空间映射表

### 技术基础设施（Foundation）

| 原命名空间 | 新命名空间 |
|-----------|-----------|
| LYBT.Desktop.Services.Http | LYBT.Desktop.Foundation.Http |
| LYBT.Desktop.Services.Caching | LYBT.Desktop.Foundation.Caching |
| LYBT.Desktop.Services.Configuration | LYBT.Desktop.Foundation.Configuration |
| LYBT.Desktop.Services.Security | LYBT.Desktop.Foundation.Security |
| LYBT.Desktop.Services.Performance | LYBT.Desktop.Foundation.Performance |
| LYBT.Desktop.Services.Diagnostics | LYBT.Desktop.Foundation.Diagnostics |
| LYBT.Desktop.Services.ErrorHandling | LYBT.Desktop.Foundation.ErrorHandling |
| LYBT.Desktop.Services.Exceptions | LYBT.Desktop.Foundation.Exceptions |
| LYBT.Desktop.Services.Handlers | LYBT.Desktop.Foundation.Handlers |
| LYBT.Desktop.Services.HealthCheck | LYBT.Desktop.Foundation.HealthCheck |
| LYBT.Desktop.Services.Session | LYBT.Desktop.Foundation.Session |
| LYBT.Desktop.Services.Settings | LYBT.Desktop.Foundation.Settings |
| LYBT.Desktop.Services.Api | LYBT.Desktop.Foundation.Api |
| LYBT.Desktop.Services.Repositories | LYBT.Desktop.Foundation.Repositories |
| LYBT.Desktop.Services.Extensions | LYBT.Desktop.Foundation.Extensions |

### UI 基础设施（Presentation）

| 原命名空间 | 新命名空间 |
|-----------|-----------|
| LYBT.Desktop.Services.Navigation | LYBT.Desktop.Presentation.Navigation |
| LYBT.Desktop.Services.Notifications | LYBT.Desktop.Presentation.Notifications |
| LYBT.Desktop.Services.Print | LYBT.Desktop.Presentation.Print |
| LYBT.Desktop.Services.Theming | LYBT.Desktop.Presentation.Theming |
| LYBT.Desktop.Services.UserExperience | LYBT.Desktop.Presentation.UserExperience |

### 保留（Services/Auth）

| 原命名空间 | 新命名空间 |
|-----------|-----------|
| LYBT.Desktop.Services.Business | **不变** |
| LYBT.Desktop.Services.Auth | **不变** |
| LYBT.Desktop.Services.Interfaces | **不变** |
| LYBT.Desktop.Services.Modules | **不变** |

---

## ⚠️ 风险评估与缓解策略

### 风险1：命名空间遗漏导致编译失败

**概率**: ★★★★☆（高）
**影响**: ★★★☆☆（中，可快速修复）

**缓解策略**:
1. 使用 PowerShell 脚本批量替换
2. 每个 Phase 完成后立即编译
3. 使用 `Select-String` 检查遗漏引用
4. Git 提交前强制运行 `dotnet build`

---

### 风险2：Prism DI 配置错误导致运行时异常

**概率**: ★★★☆☆（中）
**影响**: ★★★★☆（高，影响应用启动）

**缓解策略**:
1. Phase 1.2 完成后手动运行 Desktop 应用
2. 验证核心服务：IApiService, INotificationService, IThemeService
3. 检查 Shell/Extensions/ServiceCollectionExtensions.cs 的注册顺序
4. 保留原有的 ServiceRegistration.cs 作为参考

---

### 风险3：BaseApiRepository 迁移导致 Repository 失效

**概率**: ★★☆☆☆（低）
**影响**: ★★★★★（极高，7个业务模块受影响）

**缓解策略**:
1. Phase 1.2 开始前运行完整测试套件（建立基线）
2. 迁移后重新运行测试
3. 手动测试至少2个 Repository（PatientRepository, UserRepository）
4. 确认 GetPagedAsync 服务端分页逻辑未破坏

---

### 风险4：WPF 资源字典路径错误

**概率**: ★★★☆☆（中）
**影响**: ★★☆☆☆（低，主题加载失败）

**缓解策略**:
1. Phase 1.3 前备份 Shell/App.xaml
2. 更新 ResourceDictionary Source 路径：
   ```xaml
   <!-- 原路径 -->
   <ResourceDictionary Source="pack://application:,,,/LYBT.Desktop.Services;component/Theming/Themes/Light.xaml"/>

   <!-- 新路径 -->
   <ResourceDictionary Source="pack://application:,,,/LYBT.Desktop.Presentation;component/Theming/Themes/Light.xaml"/>
   ```
3. 运行应用验证主题加载

---

## ✅ 验收标准

### 功能验收
- [ ] 登录流程正常（AuthService 正常工作）
- [ ] 患者管理正常（PatientRepository 服务端分页）
- [ ] 处方开立正常（PrescriptionRepository + ConsultationRepository）
- [ ] 主题切换正常（ThemeService 资源字典加载）
- [ ] 通知显示正常（NotificationService）

### 技术验收
- [ ] `dotnet build LYBT.All.sln -c Release` 0错误0警告
- [ ] `dotnet test LYBT.Desktop.sln -c Release` 全部通过
- [ ] 架构测试通过（DesktopLayerArchTests）
- [ ] 无遗留 `using LYBT.Desktop.Services.<已迁移目录>` 引用

### 文档验收
- [ ] 更新 `docs/architecture/client/unified-design-standard.md`（新增 Foundation/Presentation 说明）
- [ ] 更新 `docs/index.md`（添加新项目导航）
- [ ] 生成 Phase 1 完成报告（Issue #1114 关联）

---

## 📅 执行时间预估

| Phase | 工作内容 | 预估时间 | 累计时间 |
|-------|---------|---------|---------|
| 1.1 | Foundation + 核心5个目录 | 2-3h | 2-3h |
| 1.2 | 剩余10个目录 + DI调整 | 2-3h | 4-6h |
| 1.3 | Presentation + UI 5个目录 | 2-3h | 6-9h |
| 1.4 | 最终清理 + 全量验证 | 1-2h | 7-11h |

**总计**: 7-11 小时（建议分2-3天执行，避免疲劳失误）

---

## 🔄 回滚预案

### 场景1：Phase 1.1 编译失败
**操作**: `git reset --hard HEAD~1` 回滚到 Phase 1.1 开始前

### 场景2：Phase 1.2 DI 配置错误导致应用无法启动
**操作**:
1. `git stash` 保存当前修改
2. 回滚到 Phase 1.1 完成的 commit
3. 逐个引入 Phase 1.2 的修改，定位问题

### 场景3：Phase 1.4 测试失败
**操作**:
1. 记录失败测试清单
2. 使用 Git bisect 定位引入问题的 commit
3. 修复或回滚特定 commit

---

## 📊 成功指标

### 量化指标
- **编译成功率**: 100%（0错误0警告）
- **测试通过率**: 100%（Desktop 测试套件）
- **命名空间迁移完成率**: 100%（无遗留 Services.* 引用）
- **项目数量**: 5个（Shell + Foundation + Presentation + Services + 7个业务模块）

### 质量指标
- **代码可维护性**: 新增功能时归属明确（技术 → Foundation，UI → Presentation）
- **架构一致性**: Desktop 分层理念对齐 Server 端（三层架构）
- **开发体验**: 命名空间清晰度提升，减少"放哪里"的决策困扰

---

## 📝 相关文档

- [Issue #1114 - Desktop架构模块化重构](https://github.com/user/repo/issues/1114)
- [Issue #1114 Phase 4 性能验证报告](docs/reports/issue-1114-phase4-performance-verification.md)
- [Desktop 模块化架构决策深度分析](docs/reports/desktop-modular-architecture-decision.md)
- [Client 端统一设计标准](docs/architecture/client/unified-design-standard.md)

---

**文档生成**: Claude Code
**最后更新**: 2025-10-10

🤖 Generated with [Claude Code](https://claude.com/claude-code)
