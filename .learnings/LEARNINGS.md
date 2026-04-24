[LRN-20260411-001] code-standards

**Logged**: 
**Priority**: low
**Status**: promoted
### Summary
Validation UI display for MedicalCaseEditControl TcmDiagnosis
### Details
Added TextBlock binding to Consultation.ValidationMessage to display validation errors
### Metadata
- Source: conversation
- Related Files: src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml

---

## [LRN-20260421-001] cross-compile-deployment

**Logged**: 2026-04-21T14:14 CST
**Priority**: high
**Status**: resolved

### Summary
Ubuntu 交叉编译 .NET 8 到 Windows Server 部署全流程

### Details
1. **交叉编译必须加 `-p:EnableWindowsTargeting=true`**，否则 restore 阶段报 NETSDK1100
2. **Server 2012 R2 PowerShell 4.0 没有 `New-LocalUser` / `Add-LocalGroupMember`**，只能用传统 `net user` 命令
3. **Server 2012 R2 密码复杂度策略**：必须含大小写+数字+特殊字符，纯数字 `123456` 不行
4. **Server 2012 R2 ping(ICMP) 可能被防火墙禁用**，但 SSH 端口 22 通常正常。不能以 ping 不通判断主机不可达
5. **SSH 脚本自动化用 `sshpass`**：`sshpass -p '密码' ssh user@host '命令'`
6. **编码问题**：Server 2012 R2 cmd 用 `chcp 65001` 切 UTF-8 避免中文乱码

### 部署流程
```
Ubuntu: dotnet publish -c Release -r win-x64 --self-contained false -p:EnableWindowsTargeting=true
    → scp dist/* player@server:C:/Services/LYBT-API/
    → ssh 远程执行 PowerShell 创建 Windows Service + 防火墙规则
```

### Suggested Action
已创建 `scripts/deploy-to-server.sh`（一键部署）和 `deploy/windows/prepare-dotnet8.ps1`（环境准备）

### Metadata
- Source: conversation
- Related Files: scripts/deploy-to-server.sh, deploy/windows/prepare-dotnet8.ps1
- Tags: deployment, cross-compile, dotnet8, windows-server
