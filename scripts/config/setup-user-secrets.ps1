# Configuration Governance P1 - UserSecrets Setup
param([switch]$Verify)

$WebAPIProject = "src/Server/Services/LYBT.WebAPI"
$ProjectFile = "$WebAPIProject/LYBT.WebAPI.csproj"

Write-Host "Setting up UserSecrets for development..." -ForegroundColor Green

if (-not (Test-Path $ProjectFile)) {
    Write-Error "Project file not found: $ProjectFile"
    exit 1
}

try {
    Push-Location $WebAPIProject
    Write-Host "Current directory: $(Get-Location)" -ForegroundColor Cyan

    # Initialize UserSecrets
    Write-Host "Initializing UserSecrets..." -ForegroundColor Yellow
    dotnet user-secrets init
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "UserSecrets initialization failed"
        exit 1
    }

    # Set default password configurations
    Write-Host "Setting default password configurations..." -ForegroundColor Yellow
    
    # System admin default password
    dotnet user-secrets set "DefaultPasswords:SystemAdmin" "ChangeMe!DevOnly2025@Admin"
    Write-Host "  DefaultPasswords:SystemAdmin configured" -ForegroundColor Green
    
    # New user default password
    dotnet user-secrets set "DefaultPasswords:NewUser" "ChangeMe!DevOnly2025#User"
    Write-Host "  DefaultPasswords:NewUser configured" -ForegroundColor Green
    
    # User options default password
    dotnet user-secrets set "UserOptions:DefaultUserPassword" "ChangeMe!DevOnly2025#User"
    Write-Host "  UserOptions:DefaultUserPassword configured" -ForegroundColor Green
    
    # System admin options default password
    dotnet user-secrets set "SysAdminOptions:DefaultPassword" "ChangeMe!DevOnly2025@Admin"
    Write-Host "  SysAdminOptions:DefaultPassword configured" -ForegroundColor Green

    # Set JWT secret
    Write-Host "Setting JWT configuration..." -ForegroundColor Yellow
    
    $JwtSecret = "DevOnly_JWT_Secret_Key_2025_For_LYBT_System_32Plus_Characters_Strong!"
    dotnet user-secrets set "JwtOptions:Secret" $JwtSecret
    Write-Host "  JwtOptions:Secret configured" -ForegroundColor Green

    # Verify settings if requested
    if ($Verify) {
        Write-Host "Verifying UserSecrets configuration..." -ForegroundColor Yellow
        Write-Host ""
        
        $secrets = dotnet user-secrets list
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Current UserSecrets configuration:" -ForegroundColor Cyan
            $secrets | ForEach-Object {
                if ($_ -match ".*Secret.*") {
                    $key = ($_ -split " = ")[0]
                    Write-Host "  $key = [HIDDEN]" -ForegroundColor Gray
                } else {
                    Write-Host "  $_" -ForegroundColor White
                }
            }
        } else {
            Write-Warning "Could not verify UserSecrets configuration"
        }
    }

    Write-Host ""
    Write-Host "Development environment sensitive configuration completed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Configuration items:" -ForegroundColor Cyan
    Write-Host "  - DefaultPasswords:SystemAdmin" -ForegroundColor White
    Write-Host "  - DefaultPasswords:NewUser" -ForegroundColor White
    Write-Host "  - UserOptions:DefaultUserPassword" -ForegroundColor White
    Write-Host "  - SysAdminOptions:DefaultPassword" -ForegroundColor White
    Write-Host "  - JwtOptions:Secret" -ForegroundColor White

} catch {
    Write-Error "Configuration setup failed: $($_.Exception.Message)"
    exit 1
} finally {
    Pop-Location
}

Write-Host "Configuration Governance Step 2 completed!" -ForegroundColor Green