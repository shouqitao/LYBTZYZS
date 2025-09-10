# CCPM 培训指南

## 培训目标
建立分层次的CPM培训体系，确保团队成员具备相应的CPM知识和技能，支撑LYBTZYZS项目的长期维护和发展。

## 培训对象分层

### 🔰 初级用户 (开发人员)
**目标学员**: 普通开发人员、初级工程师、实习生
**培训目标**: 掌握CPM基础操作，能够进行日常开发工作

**核心知识点**:
- [ ] CPM基础概念和优势理解
- [ ] Directory.Packages.props文件结构认知
- [ ] 添加和引用NuGet包的正确方法
- [ ] 常见错误识别和基础故障排除
- [ ] 基本命令行操作

**实操技能**:
- [ ] 在项目中正确引用包（不指定版本）
- [ ] 使用Visual Studio NuGet包管理器界面
- [ ] 执行基本的dotnet restore和build命令
- [ ] 识别并上报NU1008等常见错误

**培训时长**: 2小时（1小时理论 + 1小时实操）

### 🔧 中级用户 (高级开发、Team Lead)
**目标学员**: 高级开发工程师、Team Lead、技术骨干
**培训目标**: 深入理解CPM架构，能够解决中等复杂度问题并指导团队

**核心知识点**:
- [ ] CPM架构原理和MSBuild集成机制
- [ ] 条件引用和包分类策略
- [ ] 版本冲突诊断和解决方法
- [ ] 性能优化和缓存策略
- [ ] UltraThink架构与CPM的集成

**实操技能**:
- [ ] 配置条件包引用（基于项目名称、配置类型）
- [ ] 解决复杂的版本冲突问题
- [ ] 使用PowerShell脚本进行包管理自动化
- [ ] 进行CPM性能调优和监控
- [ ] 指导初级开发人员解决CPM问题

**培训时长**: 4小时（2小时理论 + 2小时实操）

### 🎯 专家用户 (架构师、DevOps)
**目标学员**: 系统架构师、DevOps工程师、技术负责人
**培训目标**: 全面掌握CPM企业级应用，能够制定策略和解决复杂问题

**核心知识点**:
- [ ] CPM企业级架构设计原则
- [ ] 自定义MSBuild目标和高级配置
- [ ] CI/CD流程集成和优化
- [ ] 安全性和合规性管理
- [ ] 多环境和多租户CPM策略

**实操技能**:
- [ ] 设计和实施企业级CPM架构
- [ ] 开发自定义MSBuild任务和目标
- [ ] 配置高级缓存和性能监控
- [ ] 实施包安全扫描和合规检查
- [ ] 设计灾难恢复和应急响应方案

**培训时长**: 6小时（3小时理论 + 3小时实操）

## 培训内容详解

### Module 1: CPM基础概念 (所有用户)

#### 1.1 什么是CPM？ (30分钟)
**理论部分**:
- 传统NuGet包管理的痛点
- CPM的核心价值和优势
- LYBTZYZS项目的CPM实施成果

**演示内容**:
```bash
# 传统方式的版本冲突示例
Project A: Microsoft.Extensions.Hosting 8.0.8
Project B: Microsoft.Extensions.Hosting 9.0.0
Result: 编译错误和运行时异常

# CPM统一管理后
Directory.Packages.props: Microsoft.Extensions.Hosting 9.0.0
All projects: 自动使用统一版本
```

#### 1.2 LYBTZYZS项目CPM架构 (30分钟)
**实际配置讲解**:
```xml
<!-- 核心框架包 - 所有项目通用 -->
<ItemGroup Label="Core Framework">
  <PackageVersion Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
</ItemGroup>

<!-- 前端WPF包 - UltraThink双层架构支持 -->
<ItemGroup Label="WPF and Desktop" Condition="$(MSBuildProjectName.Contains('Desktop'))">
  <PackageVersion Include="Prism.DryIoc" Version="9.0.537" />
</ItemGroup>

<!-- 后端API包 - 传统三层架构支持 -->
<ItemGroup Label="Web API and Services" Condition="$(MSBuildProjectName.Contains('Server'))">
  <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.8" />
</ItemGroup>
```

### Module 2: 日常操作实训 (初级+中级)

#### 2.1 基础操作演练 (45分钟)
**实操练习**:
1. **添加新包**:
```bash
# 练习任务：为LYBT.WebAPI项目添加Serilog日志包
# 步骤1：在Directory.Packages.props中添加版本声明
<PackageVersion Include="Serilog" Version="3.1.1" />

# 步骤2：在项目.csproj中引用
<PackageReference Include="Serilog" />

# 步骤3：验证构建
dotnet build src/Server/Services/LYBT.WebAPI
```

2. **更新包版本**:
```bash
# 练习任务：将Microsoft.EntityFrameworkCore从8.0.8升级到8.0.10
# 修改Directory.Packages.props中的版本号
# 验证所有相关项目正常构建
```

3. **解决NU1008错误**:
```bash
# 故意制造错误：在.csproj中添加Version属性
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />

# 学员任务：识别错误，移除Version属性，验证修复
```

#### 2.2 故障排除实训 (60分钟)
**模拟故障场景**:
1. **网络包还原失败**
2. **版本冲突解决**
3. **Visual Studio IntelliSense问题**
4. **构建性能问题**

每个场景提供完整的诊断和解决流程演练。

### Module 3: 高级配置与优化 (中级+专家)

#### 3.1 条件引用进阶 (90分钟)
**实际配置示例**:
```xml
<!-- 基于项目类型的智能包分配 -->
<ItemGroup Label="Test Frameworks" Condition="$(MSBuildProjectName.EndsWith('.Tests'))">
  <PackageVersion Include="xUnit" Version="2.4.2" />
  <PackageVersion Include="Moq" Version="4.18.4" />
</ItemGroup>

<!-- 基于目标框架的包选择 -->
<ItemGroup Label="Windows-specific" Condition="$(TargetFramework.Contains('windows'))">
  <PackageVersion Include="Microsoft.WindowsAPICodePack" Version="1.1.4" />
</ItemGroup>

<!-- 基于配置类型的开发工具 -->
<ItemGroup Label="Debug Tools" Condition="'$(Configuration)' == 'Debug'">
  <PackageVersion Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.8" />
</ItemGroup>
```

#### 3.2 性能调优实践 (90分钟)
**实际优化配置**:
```xml
<PropertyGroup>
  <!-- 包缓存优化 -->
  <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  <RestoreLockedMode Condition="'$(CI)' == 'true'">true</RestoreLockedMode>
  
  <!-- 构建输出优化 -->
  <UseArtifactsOutput>true</UseArtifactsOutput>
  <ArtifactsPath>$(MSBuildThisFileDirectory)artifacts</ArtifactsPath>
  
  <!-- 传递依赖管理 -->
  <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
</PropertyGroup>
```

### Module 4: 企业级部署与维护 (专家用户)

#### 4.1 CI/CD集成 (120分钟)
**GitHub Actions工作流**:
```yaml
name: CPM Build and Test
jobs:
  build:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v4
    
    - name: Cache NuGet packages
      uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('Directory.Packages.props', '**/packages.lock.json') }}
    
    - name: Restore packages (CPM)
      run: dotnet restore --locked-mode
      
    - name: Build solution
      run: dotnet build --no-restore --configuration Release
```

#### 4.2 安全性和合规性管理 (120分钟)
**包安全扫描自动化**:
```powershell
# CPM-SecurityPipeline.ps1
function Invoke-CPMSecurityScan {
    # 1. 扫描漏洞
    $vulnerabilities = dotnet list package --vulnerable --include-transitive
    
    # 2. 检查许可证合规
    $licenses = Get-PackageLicenses
    
    # 3. 生成安全报告
    Generate-SecurityReport -Vulnerabilities $vulnerabilities -Licenses $licenses
    
    # 4. 自动修复（如果配置允许）
    if ($AutoFix -and $vulnerabilities) {
        Update-VulnerablePackages -Vulnerabilities $vulnerabilities
    }
}
```

## 实践项目和考核

### 初级用户认证项目
**项目名称**: CPM基础操作认证
**项目描述**: 为一个示例项目配置CPM，添加指定包，解决基础问题

**具体任务**:
1. 为示例项目创建Directory.Packages.props文件
2. 迁移3个.csproj文件到CPM模式
3. 添加5个指定的NuGet包
4. 解决故意制造的3个NU1008错误
5. 验证项目正常构建和运行

**通过标准**: 
- 所有任务在60分钟内完成
- 构建成功无错误无警告
- 能够解释每个操作的原因

### 中级用户认证项目
**项目名称**: CPM架构优化项目
**项目描述**: 为现有复杂项目设计并实施CPM架构，解决版本冲突

**具体任务**:
1. 分析包含15个项目的解决方案的包使用情况
2. 设计分层包管理策略（至少3个分类）
3. 配置条件引用逻辑
4. 解决至少5个版本冲突问题
5. 优化构建性能（目标提升15%）
6. 编写PowerShell自动化脚本

**通过标准**:
- 项目在120分钟内完成
- 包版本100%一致无冲突
- 构建性能提升达到目标
- 脚本功能完整且可复用

### 专家用户认证项目
**项目名称**: 企业级CPM解决方案设计
**项目描述**: 设计多租户、多环境的企业级CPM架构

**具体任务**:
1. 设计支持Dev/Test/Prod环境的CPM架构
2. 实现包安全扫描和合规检查自动化
3. 配置CI/CD流水线集成CPM
4. 设计灾难恢复和应急响应方案
5. 建立监控和告警机制
6. 编写完整的运维文档

**通过标准**:
- 项目在240分钟内完成
- 架构设计合理且符合最佳实践
- 自动化脚本功能完整
- 文档完整且具有可操作性

## 培训资源制作

### 1. 培训课件 (PowerPoint)
**基础培训课件** (`training/CPM-基础培训.pptx`):
- 幻灯片1-5: CPM概念和价值
- 幻灯片6-15: LYBTZYZS项目CPM架构
- 幻灯片16-25: 基础操作演示
- 幻灯片26-30: 常见问题和解决方案

**高级培训课件** (`training/CPM-高级培训.pptx`):
- 深度架构设计原理
- 性能优化策略详解
- 企业级部署最佳实践
- 复杂故障诊断技术

### 2. 动手实验手册
**实验环境准备**:
```bash
# 创建培训环境
git clone https://github.com/shouqitao/LYBTZYZS-Training.git
cd LYBTZYZS-Training
git checkout training/cpm-exercises

# 安装必要工具
.\scripts\Setup-TrainingEnvironment.ps1
```

**实验项目结构**:
```
training/
├── exercises/
│   ├── 01-basic-cpm-setup/      # 基础配置练习
│   ├── 02-package-management/   # 包管理操作
│   ├── 03-troubleshooting/      # 故障排除练习
│   ├── 04-advanced-config/      # 高级配置练习
│   └── 05-enterprise-deploy/    # 企业级部署练习
├── solutions/                   # 参考答案
├── datasets/                   # 练习数据
└── tools/                      # 培训辅助工具
```

### 3. 在线学习资源
**知识库Wiki**:
- 基础概念解释和常见术语
- 操作步骤的分步图解
- 常见问题的视频解答
- 最佳实践案例分析

**交互式教程** (可选):
- 基于Web的CPM配置模拟器
- 错误诊断互动练习
- 性能调优参数配置工具

## 培训实施计划

### 阶段1: 基础培训全员覆盖 (Week 1-2)
**目标**: 所有开发团队成员完成基础培训

**实施安排**:
- **Week 1**: 前端团队CPM基础培训（15人）
- **Week 2**: 后端团队CPM基础培训（12人）
- **培训方式**: 2小时集中培训 + 1周内完成认证项目

### 阶段2: 中级培训骨干提升 (Week 3-4)
**目标**: Tech Lead和高级开发工程师完成中级培训

**实施安排**:
- **参与人员**: 8名Tech Lead + 6名高级开发
- **培训方式**: 4小时集中培训 + 实际项目指导实践

### 阶段3: 专家培训精英认证 (Week 5-6)
**目标**: 架构师和DevOps工程师完成专家认证

**实施安排**:
- **参与人员**: 2名架构师 + 2名DevOps工程师
- **培训方式**: 6小时深度培训 + 实际架构设计项目

### 阶段4: 知识传承机制建立 (Week 7-8)
**目标**: 建立可持续的内部培训和知识传递机制

**具体措施**:
- 建立CPM专家小组（4人）
- 设立每月技术分享会议
- 创建新员工CPM入职培训标准流程
- 建立CPM问题升级和处理机制

## 培训效果评估

### 量化指标
1. **培训参与度**:
   - 培训出席率：目标100%
   - 课程完成率：目标≥95%
   - 认证通过率：目标≥90%

2. **知识掌握度**:
   - 理论测试得分：目标≥80分
   - 实操项目质量：目标A级≥70%
   - 问题解决能力：独立解决率≥80%

3. **应用效果**:
   - CPM相关问题减少：目标减少70%
   - 包管理效率提升：目标提升60%
   - 团队自助解决率：目标≥75%

### 质性评估
1. **学员反馈调查**:
   - 培训内容实用性评分
   - 培训方式适合度评价
   - 改进建议收集

2. **实际工作表现观察**:
   - CPM操作熟练程度
   - 问题诊断思路清晰度
   - 团队协作和知识分享能力

3. **长期效果跟踪**:
   - 3个月后的技能保持率
   - 新问题学习和适应能力
   - 向新团队成员传授知识的能力

## 持续改进机制

### 培训内容更新
- **月度更新**: 根据新发现的问题和解决方案更新材料
- **版本同步**: CPM工具版本更新时同步更新培训内容
- **最佳实践补充**: 持续收集和分享团队最佳实践

### 培训方式优化
- **反馈驱动**: 根据学员反馈持续优化培训方式
- **技术创新**: 引入新的培训技术和工具
- **个性化学习**: 针对不同角色和经验水平定制内容

### 知识传承体系
- **专家轮换机制**: 定期轮换CPM专家角色，避免知识孤岛
- **文档维护责任**: 明确文档维护责任人和更新周期
- **跨团队交流**: 建立与其他项目团队的CPM经验交流机制

---
**文档版本**: v1.0  
**最后更新**: 2025-09-05  
**适用对象**: 培训组织者、技术管理者  
**审查周期**: 季度更新