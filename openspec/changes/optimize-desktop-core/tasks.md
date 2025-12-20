# Tasks: optimize-desktop-core

## Phase 1: 简化Token管理 (simplify-token-management)

### 1.1 接口合并
- [ ] 分析ITokenStorage和ITokenStorageService的差异
- [ ] 设计统一的ITokenService接口
- [ ] 创建ITokenService.cs替代两个重复接口
- [ ] 更新所有引用点

### 1.2 实现合并
- [ ] 合并SecureTokenStorage和TokenStorageService为TokenService
- [ ] 简化TokenLifecycleService，移除过度设计的状态机
- [ ] 将TokenLifecycleState改为简单枚举（内联到TokenService）
- [ ] 删除TokenLifecycleStateChangedEvent（使用简单事件）

### 1.3 凭证存储合并
- [ ] 合并ISecureCredentialStorage和IUsernameStorageService为ICredentialStorage
- [ ] 更新SecureCredentialStorage实现
- [ ] 删除UsernameStorageService

### 1.4 验证
- [ ] 更新单元测试
- [ ] 更新集成测试
- [ ] 全量编译验证
- [ ] 启动冒烟测试

## Phase 2: 提取Controls项目 (extract-desktop-controls)

### 2.1 项目创建
- [ ] 创建LYBT.Desktop.Controls.csproj
- [ ] 配置项目引用（仅WPF基础库）
- [ ] 添加到解决方案

### 2.2 控件迁移
- [ ] 移动Infrastructure/Controls/*.xaml到Controls/Controls/
- [ ] 移动Infrastructure/Controls/*.cs到Controls/Controls/
- [ ] 更新命名空间

### 2.3 转换器迁移
- [ ] 移动Infrastructure/Converters/*.cs到Controls/Converters/
- [ ] 更新命名空间
- [ ] 统一转换器命名规范

### 2.4 资源迁移
- [ ] 移动Infrastructure/Templates/到Controls/Templates/
- [ ] 移动Infrastructure/Themes/到Controls/Themes/
- [ ] 更新资源字典引用

### 2.5 引用更新
- [ ] 更新Infrastructure项目引用
- [ ] 更新Shell项目引用
- [ ] 更新各业务模块引用
- [ ] 更新App.xaml资源引用

### 2.6 验证
- [ ] 全量编译验证
- [ ] UI冒烟测试
- [ ] 验证控件样式正常

## Phase 3: 修复Models依赖 (fix-models-dependencies)

### 3.1 接口提取
- [ ] 分析Models对Infrastructure的依赖点
- [ ] 将必要接口移动到Contracts
- [ ] 确保ViewModelBase只依赖Contracts中的接口

### 3.2 依赖重构
- [ ] 更新ViewModelBase.cs移除Infrastructure依赖
- [ ] 更新MasterDetailViewModelBase.cs
- [ ] 更新UnifiedViewModelBase.cs
- [ ] 更新其他ViewModel基类

### 3.3 项目引用更新
- [ ] 更新LYBT.Desktop.Models.csproj
- [ ] 移除对Infrastructure的直接引用
- [ ] 添加对Contracts的引用

### 3.4 验证
- [ ] 全量编译验证
- [ ] ViewModel单元测试
- [ ] 启动冒烟测试

## Phase 4: 整合Infrastructure服务 (consolidate-infrastructure-services)

### 4.1 HTTP合并
- [ ] 移动Infrastructure/Http/到Foundation/Http/
- [ ] 更新ProblemDetailsParser命名空间
- [ ] 更新ProblemDetailsResponse命名空间

### 4.2 核心服务迁移
- [ ] 分析SessionManager是否应移到Foundation
- [ ] 分析ValidationService是否应移到Foundation
- [ ] 执行必要的服务迁移
- [ ] 更新DI注册

### 4.3 目录清理
- [ ] 删除Infrastructure/Http/（已合并）
- [ ] 删除Infrastructure/Controls/（已迁移）
- [ ] 删除Infrastructure/Converters/（已迁移）
- [ ] 删除Infrastructure/Templates/（已迁移）
- [ ] 删除Infrastructure/Themes/（已迁移）

### 4.4 验证
- [ ] 全量编译验证
- [ ] 执行完整测试套件
- [ ] 应用程序启动测试
- [ ] 核心业务流程测试

## 最终验证

- [ ] 所有单元测试通过
- [ ] 所有集成测试通过
- [ ] 应用程序正常启动
- [ ] 登录流程正常
- [ ] 核心业务功能正常（患者、病历、处方）
- [ ] UI控件显示正常
- [ ] 更新架构文档
