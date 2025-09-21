# ManageSecrets.ps1 - Key Management Script
# For generating, rotating keys and cleaning development keys

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Generate", "Rotate", "Clean", "Validate")]
    [string]$Action = "Validate",

    [Parameter(Mandatory=$false)]
    [string]$Environment = "Development"
)

$ErrorActionPreference = "Stop"

# Color output function
function Write-ColoredOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Generate secure JWT key
function Generate-JwtSecret {
    param(
        [int]$Length = 64
    )

    $chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?"
    $secret = ""
    $random = New-Object System.Random

    for ($i = 0; $i -lt $Length; $i++) {
        $secret += $chars[$random.Next($chars.Length)]
    }

    return $secret
}

# Generate secure password
function Generate-SecurePassword {
    param(
        [int]$Length = 16
    )

    $password = ""
    $password += [char](Get-Random -Minimum 65 -Maximum 91)  # Uppercase
    $password += [char](Get-Random -Minimum 97 -Maximum 123) # Lowercase
    $password += Get-Random -Minimum 0 -Maximum 10           # Digit
    $password += "!@#$%^&*"[(Get-Random -Minimum 0 -Maximum 8)] # Special char

    $chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*"
    for ($i = 4; $i -lt $Length; $i++) {
        $password += $chars[(Get-Random -Minimum 0 -Maximum $chars.Length)]
    }

    # Shuffle
    $passwordArray = $password.ToCharArray()
    $passwordArray = $passwordArray | Get-Random -Count $passwordArray.Length
    return -join $passwordArray
}

# Validate secrets in config file
function Validate-Secrets {
    param(
        [string]$FilePath
    )

    if (-not (Test-Path $FilePath)) {
        Write-ColoredOutput "File not found: $FilePath" -Color Red
        return @()
    }

    $content = Get-Content $FilePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $warnings = @()

    # Check JWT secret
    if ($content.JwtOptions -and $content.JwtOptions.Secret) {
        $secret = $content.JwtOptions.Secret
        if ($secret.Contains("Development") -or $secret.Contains("Default")) {
            $warnings += "JWT secret contains Development/Default keywords"
        }
        if ($secret.Length -lt 32) {
            $warnings += "JWT secret length less than 32 chars (recommend 64+)"
        }
    }

    # Check admin password
    if ($content.SysAdminOptions -and $content.SysAdminOptions.DefaultPassword) {
        $password = $content.SysAdminOptions.DefaultPassword
        if ($password.Length -lt 12) {
            $warnings += "Admin password length less than 12 chars"
        }
    }

    return $warnings
}

# Rotate secrets
function Rotate-Secrets {
    param(
        [string]$ConfigPath,
        [string]$BackupPath
    )

    Write-ColoredOutput "`n=== Starting Key Rotation ===" -Color Cyan

    # Create backup
    if (Test-Path $ConfigPath) {
        $timestamp = Get-Date -Format "yyyyMMddHHmmss"
        $backupFile = Join-Path $BackupPath "appsettings.backup.$timestamp.json"
        Copy-Item $ConfigPath $backupFile
        Write-ColoredOutput "Backup created: $backupFile" -Color Green
    }

    # Generate new keys
    $newJwtSecret = Generate-JwtSecret -Length 64
    $newAdminPassword = Generate-SecurePassword -Length 16

    Write-ColoredOutput "`nNew JWT Secret: $newJwtSecret" -Color Yellow
    Write-ColoredOutput "New Admin Password: $newAdminPassword" -Color Yellow

    # Update environment variables (recommended)
    Write-ColoredOutput "`nPlease set the following environment variables:" -Color Cyan
    Write-ColoredOutput "JWT_SECRET=$newJwtSecret" -Color White
    Write-ColoredOutput "SYSADMIN_PASSWORD=$newAdminPassword" -Color White

    return @{
        JwtSecret = $newJwtSecret
        AdminPassword = $newAdminPassword
    }
}

# Clean development secrets
function Clean-DevelopmentSecrets {
    param(
        [string]$ProjectPath
    )

    Write-ColoredOutput "`n=== Cleaning Development Secrets ===" -Color Cyan

    $configFiles = @(
        "appsettings.json",
        "appsettings.Development.json"
    )

    foreach ($file in $configFiles) {
        $filePath = Join-Path $ProjectPath $file
        if (Test-Path $filePath) {
            $content = Get-Content $filePath -Raw | ConvertFrom-Json

            # Clean JWT secret
            if ($content.JwtOptions -and $content.JwtOptions.Secret) {
                if ($content.JwtOptions.Secret.Contains("Development")) {
                    $content.JwtOptions.Secret = "REPLACE_WITH_ENVIRONMENT_VARIABLE"
                    Write-ColoredOutput "Cleaned JWT secret in $file" -Color Yellow
                }
            }

            # Clean admin password
            if ($content.SysAdminOptions -and $content.SysAdminOptions.DefaultPassword) {
                $content.SysAdminOptions.DefaultPassword = "REPLACE_WITH_ENVIRONMENT_VARIABLE"
                Write-ColoredOutput "Cleaned admin password in $file" -Color Yellow
            }

            # Save changes
            $content | ConvertTo-Json -Depth 100 | Set-Content $filePath
        }
    }
}

# Main logic
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$webApiPath = Join-Path $projectRoot "src\Server\Services\LYBT.WebAPI"
$backupPath = Join-Path $projectRoot "backups\config"

# Ensure backup directory exists
if (-not (Test-Path $backupPath)) {
    New-Item -ItemType Directory -Path $backupPath -Force | Out-Null
}

switch ($Action) {
    "Generate" {
        Write-ColoredOutput "=== Generate New Secrets ===" -Color Cyan
        $jwtSecret = Generate-JwtSecret -Length 64
        $adminPassword = Generate-SecurePassword -Length 16

        Write-ColoredOutput "`nJWT Secret (64 chars):" -Color White
        Write-ColoredOutput $jwtSecret -Color Green

        Write-ColoredOutput "`nAdmin Password (16 chars):" -Color White
        Write-ColoredOutput $adminPassword -Color Green

        Write-ColoredOutput "`nTip: Save these values in a secure key management system" -Color Yellow
    }

    "Rotate" {
        $configPath = Join-Path $webApiPath "appsettings.$Environment.json"
        if (-not (Test-Path $configPath)) {
            $configPath = Join-Path $webApiPath "appsettings.json"
        }

        $newSecrets = Rotate-Secrets -ConfigPath $configPath -BackupPath $backupPath

        Write-ColoredOutput "`nKey rotation complete!" -Color Green
        Write-ColoredOutput "Please update production environment variables and restart the application" -Color Yellow
    }

    "Clean" {
        Clean-DevelopmentSecrets -ProjectPath $webApiPath

        Write-ColoredOutput "`nDevelopment secrets cleaned!" -Color Green
        Write-ColoredOutput "Please use environment variables for production secrets" -Color Yellow
    }

    "Validate" {
        Write-ColoredOutput "=== Validate Secret Configuration ===" -Color Cyan

        $configFiles = @(
            (Join-Path $webApiPath "appsettings.json"),
            (Join-Path $webApiPath "appsettings.Development.json"),
            (Join-Path $webApiPath "appsettings.Production.json")
        )

        foreach ($configFile in $configFiles) {
            if (Test-Path $configFile) {
                Write-ColoredOutput "`nChecking file: $(Split-Path $configFile -Leaf)" -Color White
                $warnings = Validate-Secrets -FilePath $configFile

                if ($warnings.Count -eq 0) {
                    Write-ColoredOutput "  OK - No security issues found" -Color Green
                }
                else {
                    foreach ($warning in $warnings) {
                        Write-ColoredOutput "  WARNING: $warning" -Color Yellow
                    }
                }
            }
        }

        Write-ColoredOutput "`n=== Validation Complete ===" -Color Cyan
    }

    default {
        Write-ColoredOutput "Invalid action: $Action" -Color Red
        Write-ColoredOutput "Available actions: Generate, Rotate, Clean, Validate" -Color White
    }
}

Write-ColoredOutput "`nScript completed" -Color Green