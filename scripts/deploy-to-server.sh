#!/bin/bash
# =============================================================================
# 凌隐宝堂 WebAPI 部署脚本 (Ubuntu → Windows Server)
# 用法: ./deploy-to-server.sh [选项]
# =============================================================================
set -euo pipefail

# ========================= 配置区 =========================
SERVER_HOST="192.168.190.248"
SERVER_USER="player"                     # LYBT 服务账户
SERVER_PORT=22                         # SSH 端口（Windows 开了 OpenSSH）
SERVER_DEPLOY_PATH="C:/Services/LYBT-API"

LOCAL_PROJECT="src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj"
LOCAL_DIST_DIR="./dist/deploy"
SSH_KEY=""                             # 留空用默认 key，或指定路径

# ========================= 颜色输出 =========================
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

log_info()  { echo -e "${CYAN}[INFO]${NC}  $*"; }
log_ok()    { echo -e "${GREEN}[OK]${NC}    $*"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC}  $*"; }
log_error() { echo -e "${RED}[ERROR]${NC} $*"; }

# ========================= 参数解析 =========================
SKIP_BUILD=false
SKIP_TRANSFER=false
SKIP_DEPLOY=false
DRY_RUN=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-build)     SKIP_BUILD=true;     shift ;;
        --skip-transfer)  SKIP_TRANSFER=true;   shift ;;
        --skip-deploy)    SKIP_DEPLOY=true;     shift ;;
        --dry-run)        DRY_RUN=true;         shift ;;
        --server)         SERVER_HOST="$2";     shift 2 ;;
        --user)           SERVER_USER="$2";     shift 2 ;;
        --ssh-key)        SSH_KEY="$2";         shift 2 ;;
        --help)
            echo "用法: $0 [选项]"
            echo ""
            echo "选项:"
            echo "  --skip-build      跳过编译，使用 dist/ 中已有的文件"
            echo "  --skip-transfer   跳过传输，假设文件已在远程服务器"
            echo "  --skip-deploy     跳过远程部署，只传输文件"
            echo "  --dry-run         仅显示将执行的命令，不实际执行"
            echo "  --server IP       指定服务器 IP（默认: $SERVER_HOST）"
            echo "  --user USER       指定 SSH 用户（默认: $SERVER_USER）"
            echo "  --ssh-key PATH    指定 SSH 私钥路径"
            exit 0
            ;;
        *) log_error "未知参数: $1"; exit 1 ;;
    esac
done

# ========================= SSH 构建 =========================
SSH_OPTS="-o StrictHostKeyChecking=no -o ConnectTimeout=10"
[[ -n "$SSH_KEY" ]] && SSH_OPTS="$SSH_OPTS -i $SSH_KEY"

ssh_cmd() { ssh $SSH_OPTS "${SERVER_USER}@${SERVER_HOST}" "$@"; }
scp_cmd() { scp $SSH_OPTS "$@"; }

# ========================= Step 1: 编译 =========================
step_build() {
    log_info "[1/4] 交叉编译 WebAPI (win-x64)..."

    if ! command -v dotnet &>/dev/null; then
        log_error "dotnet SDK 未安装"
        exit 1
    fi

    local dotnet_ver=$(dotnet --version)
    log_ok "dotnet SDK: $dotnet_ver"

    rm -rf "$LOCAL_DIST_DIR"
    mkdir -p "$LOCAL_DIST_DIR"

    if [[ "$DRY_RUN" == true ]]; then
        log_warn "[DRY-RUN] dotnet publish $LOCAL_PROJECT -c Release -r win-x64 --self-contained false -o $LOCAL_DIST_DIR"
        return
    fi

    dotnet publish "$LOCAL_PROJECT" \
        -c Release \
        -r win-x64 \
        --self-contained false \
        -p:EnableWindowsTargeting=true \
        -o "$LOCAL_DIST_DIR" \
        2>&1 | tail -5

    if [[ ${PIPESTATUS[0]} -ne 0 ]]; then
        log_error "编译失败"
        exit 1
    fi

    local file_count=$(find "$LOCAL_DIST_DIR" -type f | wc -l)
    local total_size=$(du -sh "$LOCAL_DIST_DIR" | cut -f1)
    log_ok "编译完成: $file_count 个文件, 总大小 $total_size"
}

# ========================= Step 2: 传输文件 =========================
step_transfer() {
    log_info "[2/4] 传输文件到 $SERVER_HOST..."

    if [[ "$DRY_RUN" == true ]]; then
        log_warn "[DRY-RUN] scp -r $LOCAL_DIST_DIR/* ${SERVER_USER}@${SERVER_HOST}:${SERVER_DEPLOY_PATH}/"
        return
    fi

    ssh_cmd "mkdir -p '${SERVER_DEPLOY_PATH}/logs'"
    scp_cmd -r "${LOCAL_DIST_DIR}/"* "${SERVER_USER}@${SERVER_HOST}:${SERVER_DEPLOY_PATH}/"
    log_ok "文件传输完成"
}

# ========================= Step 3: 远程部署 =========================
step_deploy() {
    log_info "[3/4] 远程部署..."

    if [[ "$DRY_RUN" == true ]]; then
        log_warn "[DRY-RUN] 远程执行部署脚本"
        return
    fi

    ssh_cmd powershell -Command "
        \$ErrorActionPreference = 'Stop'
        Write-Host '=== LYBT WebAPI 远程部署 ===' -ForegroundColor Cyan

        Write-Host '[1/4] 停止服务...' -ForegroundColor Yellow
        if (Get-Service -Name 'LYBT-API' -ErrorAction SilentlyContinue) {
            Stop-Service -Name 'LYBT-API' -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
            Write-Host '  服务已停止' -ForegroundColor Green
        } else {
            Write-Host '  服务不存在，跳过' -ForegroundColor Gray
        }

        Write-Host '[2/4] 设置环境变量...' -ForegroundColor Yellow
        [Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', 'Production', 'Process')

        Write-Host '[3/4] 检查 .NET 运行时...' -ForegroundColor Yellow
        try {
            \$dotnetVer = dotnet --version 2>\$null
            Write-Host '  .NET: ' -NoNewline -ForegroundColor Gray
            Write-Host \$dotnetVer -ForegroundColor Green
        } catch {
            Write-Warning '  .NET 运行时未安装！请参考安装脚本。'
        }

        Write-Host '[4/4] 重新创建 Windows Service...' -ForegroundColor Yellow
        \$exePath = Join-Path '${SERVER_DEPLOY_PATH}' 'LYBT.WebAPI.exe'

        \$existing = Get-Service -Name 'LYBT-API' -ErrorAction SilentlyContinue
        if (\$existing) {
            sc.exe delete 'LYBT-API' | Out-Null
            Start-Sleep -Seconds 2
        }

        sc.exe create 'LYBT-API' binPath= \"\$exePath\" start= auto displayName= '凌隐宝堂 WebAPI 服务' depend= 'MSSQLSERVER' | Out-Null
        sc.exe description 'LYBT-API' '凌隐宝堂中医诊所管理系统 WebAPI 服务' | Out-Null

        Write-Host '  配置防火墙...' -ForegroundColor Yellow
        \$rule = Get-NetFirewallRule -DisplayName 'LYBT-API-5000' -ErrorAction SilentlyContinue
        if (-not \$rule) {
            New-NetFirewallRule -DisplayName 'LYBT-API-5000' -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow -Profile Domain,Private | Out-Null
            New-NetFirewallRule -DisplayName 'LYBT-API-5001' -Direction Inbound -Protocol TCP -LocalPort 5001 -Action Allow -Profile Domain,Private | Out-Null
            Write-Host '  防火墙规则已创建' -ForegroundColor Green
        }

        Write-Host '  启动服务...' -ForegroundColor Yellow
        Start-Service -Name 'LYBT-API' -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 3
        \$svc = Get-Service -Name 'LYBT-API'
        if (\$svc.Status -eq 'Running') {
            Write-Host '  服务运行中' -ForegroundColor Green
        } else {
            Write-Host '  服务状态: ' -NoNewline -ForegroundColor Yellow
            Write-Host \$svc.Status -ForegroundColor Yellow
        }

        Write-Host ''
        Write-Host '=== 部署完成 ===' -ForegroundColor Cyan
        Write-Host 'API 地址: http://192.168.190.248:5000' -ForegroundColor White
        Write-Host 'Swagger:  http://192.168.190.248:5000/swagger' -ForegroundColor White
        Write-Host '健康检查: http://192.168.190.248:5000/health' -ForegroundColor White
    "

    log_ok "远程部署完成"
}

# ========================= Step 4: 验证 =========================
step_verify() {
    log_info "[4/4] 验证部署..."

    if [[ "$DRY_RUN" == true ]]; then
        log_warn "[DRY-RUN] 跳过验证"
        return
    fi

    sleep 3
    if curl -sf --connect-timeout 5 "http://${SERVER_HOST}:5000/health" >/dev/null 2>&1; then
        log_ok "健康检查通过 ✓"
    elif curl -sf --connect-timeout 5 "http://${SERVER_HOST}:5000/swagger" >/dev/null 2>&1; then
        log_ok "Swagger 可访问 ✓"
    else
        log_warn "健康检查未通过，可能是服务还在启动中"
        log_warn "请手动检查: http://${SERVER_HOST}:5000/health"
    fi
}

# ========================= 主流程 =========================
echo ""
echo -e "${CYAN}╔══════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║  凌隐宝堂 WebAPI 部署脚本               ║${NC}"
echo -e "${CYAN}║  Ubuntu → Windows Server                 ║${NC}"
echo -e "${CYAN}╚══════════════════════════════════════════╝${NC}"
echo ""
echo -e "  目标服务器: ${YELLOW}${SERVER_HOST}${NC}"
echo -e "  部署路径:   ${YELLOW}${SERVER_DEPLOY_PATH}${NC}"
echo ""

[[ "$SKIP_BUILD" != true ]]      && step_build
[[ "$SKIP_TRANSFER" != true ]]   && step_transfer
[[ "$SKIP_DEPLOY" != true ]]     && step_deploy
step_verify

echo ""
log_ok "全部完成 🎉"
