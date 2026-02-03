# relocate-cardreader-to-core Tasks

## Overview

- **变更类型**: Refactor
- **风险等级**: Low
- **预估工作量**: 20分钟

## Phase 1: 目录迁移

### 1.1 移动CardReader目录
- **命令**: `git mv src/Client/Desktop/Modules/LYBT.Desktop.CardReader src/Client/Desktop/Core/`
- **验证**: 目录存在于Core/下

### 1.2 更新LYBT.Desktop.sln
- **文件**: `LYBT.Desktop.sln:40`
- **变更**: `src\Client\Desktop\Modules\LYBT.Desktop.CardReader\` → `src\Client\Desktop\Core\LYBT.Desktop.CardReader\`
- **验证**: 路径正确

### 1.3 更新LYBT.All.sln
- **文件**: `LYBT.All.sln:166`
- **变更**: `src\Client\Desktop\Modules\LYBT.Desktop.CardReader\` → `src\Client\Desktop\Core\LYBT.Desktop.CardReader\`
- **验证**: 路径正确

### 1.4 更新Shell.csproj
- **文件**: `src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj:96`
- **变更**: `..\Modules\LYBT.Desktop.CardReader\` → `..\Core\LYBT.Desktop.CardReader\`
- **验证**: 引用解析正确

### 1.5 更新Clinical.csproj
- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/LYBT.Desktop.Clinical.csproj:91`
- **变更**: `..\..\Modules\LYBT.Desktop.CardReader\` → `..\..\Core\LYBT.Desktop.CardReader\`
- **验证**: 引用解析正确

### 1.6 更新Patients.csproj
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/LYBT.Desktop.Patients.csproj:90`
- **变更**: `..\LYBT.Desktop.CardReader\` → `..\..\Core\LYBT.Desktop.CardReader\`
- **验证**: 引用解析正确

### 1.7 编译验证
- **命令**: `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- **验证**: 0 errors, 0 warnings

## Phase 2: 文档更新

### 2.1 更新CardReader CLAUDE.md路径说明
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.CardReader/CLAUDE.md`
- **变更**: 更新目录结构说明中的路径

## Dependencies

```
1.1 (git mv) ──► 1.2-1.6 (更新引用) ──► 1.7 (编译) ──► 2.1 (文档)
```

## Validation Checklist

- [ ] Desktop解决方案编译通过
- [ ] All解决方案编译通过
- [ ] CardReader目录位于Core/下
- [ ] 所有项目引用路径正确

---

**生成时间**: 2026-01-20
**状态**: 完整版 (已完成设计阶段细化)
