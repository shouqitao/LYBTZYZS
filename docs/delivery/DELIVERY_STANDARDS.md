# 凌隐宝堂中医诊所系统 - 交付标准规范

## 文档概述

本文档定义了凌隐宝堂中医诊所系统进入交付阶段的标准和规范，确保系统质量和可靠性。

---

## 🎯 交付阶段核心原则

### 1. 文档驱动开发 (Documentation-Driven Development)

#### 强制性要求
- **文档优先原则**：任何功能实现前必须先完善对应文档
- **文档完整性**：代码、接口、架构文档必须100%同步
- **变更控制**：任何代码变更必须先更新文档，再实施代码修改

#### 文档体系结构
```
docs/
├── delivery/           # 交付标准文档 (本文档所在目录)
├── api/               # API接口文档 (必须完善)
├── architecture/      # 架构设计文档 (必须同步)
├── development/       # 开发规范文档 (必须执行)
└── testing/           # 测试规范文档 (必须建立)
```

### 2. 接口数据获取标准

#### 禁止性规定
- ❌ **严禁使用硬编码数据**
- ❌ **严禁使用模拟数据**（除非明确标记为TODO并计划移除）
- ❌ **严禁直接返回静态数据**

#### 强制性规定
- ✅ **所有数据必须从API接口获取**
- ✅ **所有TODO标记的模拟数据必须替换为真实接口调用**
- ✅ **所有异常情况必须有完整的错误处理**

---

## 🏗️ 项目构建标准

### 1. 编译质量标准

#### 零容忍质量门禁
```bash
# 前端编译标准
dotnet build LYBT.Desktop.sln --configuration Release --verbosity minimal
# 要求：0 个错误，0 个警告

# 后端编译标准  
dotnet build LYBT.Server.sln --configuration Release --verbosity minimal
# 要求：0 个错误，0 个警告
```

#### 代码质量检查
```bash
# 代码格式化检查（强制执行）
dotnet format --verify-no-changes --verbosity diagnostic

# 静态代码分析（强制通过）
dotnet build --configuration Release -p:TreatWarningsAsErrors=true
```

### 2. 依赖管理标准

#### Package 版本控制
- **统一版本管理**：所有项目使用相同版本的共享包
- **依赖锁定**：生产环境包版本必须锁定，不允许浮动版本
- **安全扫描**：定期执行包安全漏洞扫描

#### 关键依赖版本标准
```xml
<!-- 强制版本标准 -->
<PackageReference Include="Microsoft.AspNetCore.App" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.17" />
<PackageReference Include="Prism.DryIoc" Version="9.0.537" />
<PackageReference Include="Refit" Version="7.0.0" />
```

---

## 🔌 API接口对接标准

### 1. 接口调用规范

#### 统一接口调用模式
```csharp
// ✅ 标准接口调用模式
public async Task<ServiceResult<TResult>> CallApiMethod<TResult>(TRequest request)
{
    try 
    {
        var response = await _apiClient.MethodAsync(request);
        return ServiceResult<TResult>.Success(response);
    }
    catch (ApiException apiEx)
    {
        _logger.LogError(apiEx, "API调用失败: {Method}", nameof(MethodAsync));
        return ServiceResult<TResult>.Failure($"接口调用失败: {apiEx.Message}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "系统异常: {Method}", nameof(MethodAsync));
        return ServiceResult<TResult>.Failure($"系统异常: {ex.Message}");
    }
}
```

#### 错误处理标准
```csharp
// 统一错误处理模式
public enum ApiErrorType 
{
    NetworkError,    // 网络连接错误
    ServerError,     // 服务器内部错误  
    ValidationError, // 数据验证错误
    AuthError,       // 认证授权错误
    BusinessError    // 业务逻辑错误
}

public class ApiErrorHandler 
{
    public static ServiceResult<T> HandleApiError<T>(Exception ex)
    {
        return ex switch
        {
            ApiException apiEx => HandleApiException<T>(apiEx),
            HttpRequestException httpEx => HandleNetworkError<T>(httpEx),
            TaskCanceledException timeoutEx => HandleTimeoutError<T>(timeoutEx),
            _ => HandleUnknownError<T>(ex)
        };
    }
}
```

### 2. TODO标记替换标准

#### 当前TODO标记识别
需要替换的TODO标记位置：
1. **Core Service层**：所有`// TODO: 调用真实API`标记
2. **Business Service层**：所有模拟数据返回
3. **Query Service层**：所有静态数据查询

#### TODO替换检查清单
```markdown
- [ ] AuthCoreService - 认证API调用
- [ ] UserCoreService - 用户管理API调用  
- [ ] PatientCoreService - 患者管理API调用
- [ ] MedicalCaseCoreService - 医案管理API调用
- [ ] ConsultationCoreService - 看诊API调用
- [ ] PrescriptionCoreService - 处方API调用
- [ ] HerbCoreService - 中药材API调用
- [ ] FormulaCoreService - 验方API调用
```

---

## 🧪 测试标准

### 1. 单元测试要求

#### 测试覆盖率标准
- **Core Service层**：≥ 90% 覆盖率
- **Business Service层**：≥ 85% 覆盖率  
- **Query Service层**：≥ 80% 覆盖率
- **主模块层**：≥ 95% 覆盖率

#### 测试命名规范
```csharp
// 标准测试方法命名
[Fact]
public async Task CreateAsync_ValidInput_ReturnsSuccess()
{
    // Arrange
    var createDto = new CreateDto { /* 测试数据 */ };
    
    // Act  
    var result = await _service.CreateAsync(createDto);
    
    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
}
```

### 2. 集成测试要求

#### API集成测试
```bash
# 自动化API测试执行
cd tests/api
python api_test_automation.py --environment staging

# 要求：所有核心业务流程测试通过
```

---

## 📋 交付质量门禁

### 1. 代码质量检查

#### 自动化质量检查脚本
```bash
#!/bin/bash
# 交付前质量检查脚本

echo "=== 开始交付质量检查 ==="

# 1. 编译检查
echo "1. 编译检查..."
dotnet build LYBT.All.sln --configuration Release --verbosity minimal
if [ $? -ne 0 ]; then
    echo "❌ 编译失败，交付阻止"
    exit 1
fi

# 2. 格式化检查  
echo "2. 代码格式化检查..."
dotnet format --verify-no-changes
if [ $? -ne 0 ]; then
    echo "❌ 代码格式不规范，交付阻止"
    exit 1  
fi

# 3. TODO标记检查
echo "3. TODO标记检查..."
TODO_COUNT=$(grep -r "// TODO:" src/ --include="*.cs" | wc -l)
if [ $TODO_COUNT -gt 0 ]; then
    echo "⚠️  发现 $TODO_COUNT 个待完成TODO标记"
    grep -r "// TODO:" src/ --include="*.cs"
    echo "❌ 存在未完成TODO，交付阻止"
    exit 1
fi

# 4. 测试执行
echo "4. 单元测试执行..."
dotnet test --configuration Release --logger trx
if [ $? -ne 0 ]; then
    echo "❌ 单元测试失败，交付阻止" 
    exit 1
fi

echo "✅ 所有质量检查通过，可以交付"
```

### 2. 文档完整性检查

#### 必需文档清单
- [ ] **API接口文档** - 所有接口必须有完整文档
- [ ] **架构设计文档** - 必须与代码实现一致
- [ ] **部署运维文档** - 必须包含完整部署步骤
- [ ] **用户使用手册** - 必须包含核心业务流程
- [ ] **测试报告文档** - 必须包含测试覆盖率和结果

---

## 🚀 持续集成/持续交付 (CI/CD)

### 1. 构建流水线标准

#### GitHub Actions 工作流
```yaml
name: 交付质量检查

on:
  pull_request:
    branches: [ master ]
  push:
    branches: [ master ]

jobs:
  quality-check:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: 依赖还原
      run: dotnet restore LYBT.All.sln
      
    - name: 编译检查
      run: dotnet build LYBT.All.sln --configuration Release --no-restore
      
    - name: 代码格式检查
      run: dotnet format --verify-no-changes
      
    - name: TODO标记检查
      run: |
        $todoCount = (Select-String -Path src/**/*.cs -Pattern "// TODO:").Count
        if ($todoCount -gt 0) { 
          Write-Error "发现 $todoCount 个待完成TODO标记"
          exit 1 
        }
      
    - name: 单元测试
      run: dotnet test --configuration Release --no-build --logger trx
      
    - name: 测试报告上传
      uses: dorny/test-reporter@v1
      if: always()
      with:
        name: 测试结果报告
        path: "**/*.trx"
        reporter: dotnet-trx
```

### 2. 发布标准

#### 版本发布检查清单
- [ ] 所有单元测试通过
- [ ] 所有集成测试通过  
- [ ] 代码覆盖率达标
- [ ] 安全漏洞扫描通过
- [ ] 性能测试通过
- [ ] 文档同步完成
- [ ] 部署脚本验证
- [ ] 回滚方案准备

---

## 📞 支持和维护

### 联系信息
- **技术负责人**：UltraThink架构团队
- **文档维护**：开发团队全员
- **质量保证**：QA团队

### 文档更新频率
- **交付标准**：每次重大版本发布后更新
- **API文档**：每次接口变更后立即更新  
- **架构文档**：每次架构调整后立即更新

---

**文档版本**：v1.0  
**最后更新**：2025-09-01  
**适用范围**：凌隐宝堂中医诊所系统全项目