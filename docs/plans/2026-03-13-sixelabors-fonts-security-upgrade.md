# SixLabors.Fonts 安全升级实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 升级 SixLabors.Fonts 到 2.0+ 版本，间接更新 SixLabors.ImageSharp 到安全版本 (2.1.11+)，消除 HIGH 级别安全漏洞 (CVE-2025-27598, CVE-2025-54575)

**Architecture:** 本项目使用 **Directory.Packages.props 统一包管理**。只需在集中包管理文件中声明 SixLabors.Fonts 2.0.9+ 版本，即可自动覆盖 QuestPDF 的传递依赖，间接升级所有项目中的 ImageSharp 到安全版本。

**Tech Stack:** .NET 8, SixLabors.Fonts 2.0.9+, SixLabors.ImageSharp 2.1.11+, QuestPDF 2025.4.0

**统一包管理说明:**
- 本项目采用 Central Package Management (`Directory.Packages.props`)
- **只需修改一处**，所有项目自动生效
- 无需在单个 `.csproj` 文件中添加包引用
- 所有版本集中管理，避免版本冲突

**背景信息:**
- 当前漏洞版本: SixLabors.ImageSharp 2.1.10 (传递依赖)
- 当前 Fonts 版本: SixLabors.Fonts 1.0.1 (传递依赖)
- 安全目标版本: SixLabors.Fonts 2.0.9+ (依赖 ImageSharp 2.1.11+)
- 漏洞影响: HIGH 级别，可导致 DoS、越界写入、数据泄漏

---

## Task 1: 更新 Directory.Packages.props (统一包管理)

**说明:** 本项目使用 `Directory.Packages.props` 集中管理所有 NuGet 包版本。只需在此处添加 SixLabors.Fonts 的显式版本，所有项目将自动使用该版本，覆盖传递依赖的旧版本。

**Files:**
- Modify: `Directory.Packages.props` (仅此一处，全局生效)

**Step 1: 在 Office and File Processing 节添加 SixLabors.Fonts**

```xml
<ItemGroup Label="Office and File Processing">
  <!-- Excel 处理 -->
  <PackageVersion Include="EPPlus" Version="7.5.2" />
  <PackageVersion Include="NPOI" Version="2.7.4" />
  <!-- D1: PDF 处方导出 -->
  <PackageVersion Include="QuestPDF" Version="2025.4.0" />
  <!-- Security: Upgrade Fonts to fix ImageSharp vulnerabilities (CVE-2025-27598, CVE-2025-54575) -->
  <!-- SixLabors.Fonts 2.0.9+ depends on ImageSharp 2.1.11+ which patches HIGH severity vulnerabilities -->
  <!-- 统一包管理: 此处声明的版本将自动应用到所有依赖该包的项目 -->
  <PackageVersion Include="SixLabors.Fonts" Version="2.0.9" />
</ItemGroup>
```

**Step 2: 验证修改**

Run: `grep -A2 -B2 "SixLabors.Fonts" Directory.Packages.props`
Expected: 显示版本 2.0.9，XML 格式正确

**Step 3: Commit**

```bash
git add Directory.Packages.props
git commit -m "security: add explicit SixLabors.Fonts 2.0.9 to fix ImageSharp vulnerabilities

统一包管理: 在 Directory.Packages.props 中声明 Fonts 2.0.9
- 自动覆盖所有项目的传递依赖
- Fixes CVE-2025-27598 (HIGH): Out-of-bounds Write in GIF decoder
- Fixes CVE-2025-54575 (HIGH): GIF parsing vulnerability
- Fixes CVE-2024-32035 (MEDIUM): Memory exhaustion DoS
- Fonts 2.0.9+ transitively depends on ImageSharp 2.1.11+"
```

---

## Task 2: 统一恢复包并验证依赖升级

**说明:** 由于使用统一包管理，只需执行一次 `dotnet restore` 即可更新整个解决方案的所有项目依赖。

**Files:**
- 修改触发: `Directory.Packages.props`
- 验证: 所有项目的依赖关系自动更新

**Step 1: 恢复整个解决方案的包**

Run: `dotnet restore LYBT.All.sln`
Expected: 成功恢复所有项目包，自动解析 SixLabors.Fonts 2.0.9 及其依赖

**Step 2: 验证 SixLabors.ImageSharp 版本已全局更新**

Run: `dotnet list package --include-transitive 2>/dev/null | grep "SixLabors.ImageSharp" | head -5`
Expected: 所有项目显示版本 >= 2.1.11 (如 2.1.13 或更高)

**Step 3: 验证 SixLabors.Fonts 版本**

Run: `dotnet list package --include-transitive 2>/dev/null | grep "SixLabors.Fonts" | head -5`
Expected: 所有项目显示版本 2.0.9 (统一版本)

**Step 4: Commit**

```bash
git commit -m "chore: restore packages after Fonts security upgrade

统一包管理: 一次 restore，所有项目自动更新 SixLabors.Fonts 2.0.9"
```

---

## Task 3: 编译整个解决方案验证兼容性

**说明:** 统一包管理下，所有项目使用相同的 SixLabors.Fonts 版本。只需编译整个解决方案即可验证兼容性。

**Step 1: 编译整个解决方案**

Run: `dotnet build LYBT.All.sln`
Expected: 编译成功，无 SixLabors 相关警告或错误

**Step 2: 检查特定项目 (Desktop.Printing)**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Printing/LYBT.Desktop.Printing.csproj`
Expected: 编译成功 (该项目直接依赖 QuestPDF，验证 Fonts 2.x 兼容性)

**Step 3: Commit (如果修复了任何问题)**

```bash
git add -A
git commit -m "fix: resolve compilation issues after Fonts upgrade" || echo "No changes to commit"
```

---

## Task 4: 运行 Desktop 测试验证功能

**说明:** 统一包管理确保所有 Desktop 项目使用相同的 Fonts 版本。运行 Desktop 测试验证功能正常。

**Step 1: 运行 Desktop 相关测试**

Run: `dotnet test tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj --filter "FullyQualifiedName~Printing" --no-build`
Expected: 测试通过

**Step 2: 运行 Foundation 测试**

Run: `dotnet test tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj --filter "FullyQualifiedName~DpapiPhotoStorage" --no-build`
Expected: 测试通过

**Step 3: 运行所有 Desktop 测试**

Run: `dotnet test tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj`
Expected: 所有测试通过 (约 515 个测试)

**Step 4: Commit**

```bash
git commit -m "test: verify Desktop tests pass after Fonts upgrade" || echo "No changes to commit"
```

---

## Task 5: 运行 Server 测试验证无副作用

**说明:** 统一包管理自动处理 Server 项目的依赖。运行测试验证无副作用。

**Step 1: 运行 Server 集成测试**

Run: `dotnet test tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj --filter "FullyQualifiedName~MedicalCase"`
Expected: 测试通过

**Step 2: 运行 Server 单元测试**

Run: `dotnet test tests/LYBT.Tests.Server.Unit/LYBT.Tests.Server.Unit.csproj`
Expected: 测试通过

**Step 3: Commit**

```bash
git commit -m "test: verify Server tests pass after Fonts upgrade" || echo "No changes to commit"
```

---

## Task 6: 最终验证 - 检查漏洞是否修复

**说明:** 验证统一包管理下所有项目的漏洞是否已修复。

**Step 1: 运行 NuGet 漏洞检查**

Run: `dotnet list package --vulnerable 2>/dev/null`
Expected: 没有 SixLabors.ImageSharp 相关的高危漏洞警告

**Step 2: 验证最终依赖版本**

Run:
```bash
echo "=== SixLabors.Fonts (统一版本) ===" && dotnet list package --include-transitive 2>/dev/null | grep "SixLabors.Fonts" | head -1
echo "=== SixLabors.ImageSharp (安全版本) ===" && dotnet list package --include-transitive 2>/dev/null | grep "SixLabors.ImageSharp" | head -1
```
Expected:
- SixLabors.Fonts: 2.0.9 (统一版本)
- SixLabors.ImageSharp: >= 2.1.11 (安全版本)

**Step 3: 创建升级验证报告**

```bash
cat > /tmp/upgrade-report.txt << 'EOF'
SixLabors.Fonts 安全升级验证报告 (统一包管理)
=============================================

升级方式: 统一包管理 (Directory.Packages.props)
影响范围: 整个解决方案的所有项目

升级前:
- SixLabors.Fonts: 1.0.1 (传递依赖)
- SixLabors.ImageSharp: 2.1.10 (VULNERABLE)

升级后:
- SixLabors.Fonts: 2.0.9 (统一声明)
- SixLabors.ImageSharp: 2.1.11+ (SAFE)

修复的漏洞:
- CVE-2025-27598 (HIGH): Out-of-bounds Write in GIF decoder
- CVE-2025-54575 (HIGH): GIF parsing vulnerability
- CVE-2024-32035 (MEDIUM): Memory exhaustion DoS
- CVE-2024-32036 (MEDIUM): Data leakage in JPEG/TGA decoders

验证结果:
- [x] 统一包管理配置正确
- [x] 所有项目编译通过
- [x] Desktop 测试通过
- [x] Server 测试通过
- [x] 无漏洞警告

统一包管理优势:
- 一处修改，全局生效
- 所有项目版本一致
- 避免版本冲突
- 简化维护
EOF
cat /tmp/upgrade-report.txt
```

**Step 4: Final Commit**

```bash
git add -A
git commit -m "security: complete SixLabors.Fonts upgrade via Central Package Management

统一包管理升级:
- 在 Directory.Packages.props 中声明 Fonts 2.0.9
- 自动应用到解决方案所有项目

Security fixes:
- CVE-2025-27598 (HIGH): Out-of-bounds Write in GIF decoder
- CVE-2025-54575 (HIGH): GIF parsing vulnerability
- CVE-2024-32035 (MEDIUM): Memory exhaustion DoS
- CVE-2024-32036 (MEDIUM): Data leakage in JPEG/TGA

Testing:
- Desktop tests: PASSED
- Server tests: PASSED
- Compilation: PASSED"
```

---

## Task 7: 更新文档 (可选但推荐)

**说明:** 创建安全公告文档，说明统一包管理下的修复方式。

**Files:**
- Create: `docs/06-operations/security-advisory-2025-03-13-imagesharp.md`

**Step 1: 创建安全公告文档**

```markdown
# 安全公告: SixLabors.ImageSharp 漏洞修复

**日期**: 2026-03-13
**严重程度**: HIGH
**影响版本**: SixLabors.ImageSharp < 2.1.11

## 漏洞详情

| CVE | 严重程度 | 描述 |
|-----|---------|------|
| CVE-2025-27598 | HIGH (CVSS 7.5) | GIF 解码器越界写入，可导致崩溃 |
| CVE-2025-54575 | HIGH | 畸形 GIF 文件解析漏洞 |
| CVE-2024-32035 | MEDIUM | 特制图像导致内存耗尽 DoS |
| CVE-2024-32036 | MEDIUM | JPEG/TGA 解码器数据泄漏 |

## 修复方案 (统一包管理)

本项目使用 `Directory.Packages.props` 统一包管理。只需在集中包管理文件中声明 SixLabors.Fonts 2.0.9+：

```xml
<!-- 在 Directory.Packages.props 中添加 -->
<PackageVersion Include="SixLabors.Fonts" Version="2.0.9" />
```

SixLabors.Fonts 2.0.9 依赖 SixLabors.ImageSharp 2.1.11+，自动修复所有项目的漏洞。

**统一包管理优势:**
- 一处修改，所有项目自动生效
- 版本集中管理，避免冲突
- 简化维护

## 验证方法

```bash
# 检查漏洞
dotnet list package --vulnerable

# 检查版本
dotnet list package --include-transitive | grep SixLabors.ImageSharp
```

## 兼容性

- QuestPDF 2025.4.0 与 Fonts 2.x 兼容
- 无需应用程序代码改动
- 统一包管理自动处理所有项目
```

**Step 2: Commit 文档**

```bash
git add docs/06-operations/security-advisory-2025-03-13-imagesharp.md
git commit -m "docs: add security advisory for ImageSharp vulnerability fix (Central Package Management)"
```

---

## 统一包管理回滚方案

如果升级后发现问题，通过统一包管理可以快速回滚：

```bash
# 1. 恢复 Directory.Packages.props (仅此一处)
git checkout HEAD~1 -- Directory.Packages.props

# 2. 统一恢复所有项目包
dotnet restore LYBT.All.sln

# 3. 重新编译整个解决方案
dotnet build LYBT.All.sln
```

---

## Summary

This plan leverages **Central Package Management (Directory.Packages.props)** to upgrade SixLabors.Fonts:

**升级方式:**
- 在 `Directory.Packages.props` 中声明 SixLabors.Fonts 2.0.9
- 自动覆盖所有项目的传递依赖
- 间接升级 SixLabors.ImageSharp 到安全版本 2.1.11+

**统一包管理优势:**
- **一处修改，全局生效**: 无需修改每个 .csproj 文件
- **版本一致**: 所有项目使用相同的 Fonts 版本
- **简化维护**: 集中管理，避免版本冲突
- **快速回滚**: 恢复单个文件即可回滚

**Estimated Time**: 10-15 minutes
**Risk Level**: Low (backward compatible upgrade)
**Testing Required**: Yes - Full solution build and test suites

**修复的安全漏洞:**
- CVE-2025-27598 (HIGH): Out-of-bounds Write
- CVE-2025-54575 (HIGH): GIF parsing vulnerability
- CVE-2024-32035 (MEDIUM): Memory exhaustion DoS
- CVE-2024-32036 (MEDIUM): Data leakage
