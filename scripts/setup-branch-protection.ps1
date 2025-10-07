<#
.SYNOPSIS
配置 GitHub 仓库分支保护规则

.DESCRIPTION
为 master 分支配置保护规则，符合 docs/development/standards.md 要求:
- 需要至少 1 次人工审批（CODEOWNERS）
- 需要 PR 自动审查通过
- 需要所有 CI 检查通过
- 需要线性提交历史
- 不允许绕过保护规则

.EXAMPLE
.\scripts\setup-branch-protection.ps1

.NOTES
需要 GitHub CLI (gh) 已登录且有仓库管理权限
#>

$ErrorActionPreference = "Stop"

# 配置参数
$owner = "shouqitao"
$repo = "LYBTZYZS"
$branch = "master"

Write-Host "🔧 配置 $owner/$repo 的 $branch 分支保护规则..." -ForegroundColor Cyan

# 分支保护配置 JSON
$protectionConfig = @{
    required_status_checks = @{
        strict = $true
        contexts = @(
            "Claude Code 自动审查"
        )
    }
    enforce_admins = $true  # 管理员也不能绕过
    required_pull_request_reviews = @{
        dismiss_stale_reviews = $true  # 新提交后取消旧审批
        require_code_owner_reviews = $true  # 需要 CODEOWNERS 审批
        required_approving_review_count = 1  # 至少 1 个审批
        require_last_push_approval = $false  # 最后推送者可以是审批者
    }
    restrictions = $null  # 不限制谁可以推送
    required_linear_history = $true  # 需要线性历史（禁止 merge commit）
    allow_force_pushes = $false  # 禁止强制推送
    allow_deletions = $false  # 禁止删除分支
    required_conversation_resolution = $true  # 需要解决所有对话
} | ConvertTo-Json -Depth 10

try {
    # 使用 gh api 配置分支保护
    Write-Host "📝 应用分支保护配置..." -ForegroundColor Yellow

    $protectionConfig | gh api `
        -X PUT `
        -H "Accept: application/vnd.github+json" `
        "repos/$owner/$repo/branches/$branch/protection" `
        --input -

    Write-Host "✅ 分支保护规则配置成功！" -ForegroundColor Green
    Write-Host ""
    Write-Host "配置内容:" -ForegroundColor Cyan
    Write-Host "  ✓ 需要 1 个 CODEOWNERS 审批" -ForegroundColor Green
    Write-Host "  ✓ 需要 'Claude Code 自动审查' 通过" -ForegroundColor Green
    Write-Host "  ✓ 需要线性提交历史（squash/rebase）" -ForegroundColor Green
    Write-Host "  ✓ 管理员也不能绕过规则" -ForegroundColor Green
    Write-Host "  ✓ 禁止强制推送和删除分支" -ForegroundColor Green
    Write-Host ""
    Write-Host "查看配置: gh api repos/$owner/$repo/branches/$branch/protection" -ForegroundColor Blue

} catch {
    Write-Host "❌ 配置失败: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "常见问题:" -ForegroundColor Yellow
    Write-Host "  1. 确保已登录 GitHub CLI: gh auth login" -ForegroundColor White
    Write-Host "  2. 确保有仓库管理权限: gh repo view $owner/$repo --json viewerPermission" -ForegroundColor White
    Write-Host "  3. 公开仓库需要 GitHub Pro 才能启用某些保护规则" -ForegroundColor White
    exit 1
}
