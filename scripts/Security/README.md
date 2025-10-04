# Security Scripts

## ManageSecrets.ps1

PowerShell script for managing JWT secrets and passwords in the LYBT application.

### Usage

```powershell
# Validate current configuration
.\ManageSecrets.ps1 -Action Validate

# Generate new secrets
.\ManageSecrets.ps1 -Action Generate

# Rotate secrets (with backup)
.\ManageSecrets.ps1 -Action Rotate -Environment Production

# Clean development secrets
.\ManageSecrets.ps1 -Action Clean
```

### Actions

#### Validate
Checks configuration files for security issues:
- JWT secret length (recommends 64+ chars)
- Development keywords in secrets
- Password complexity

#### Generate
Generates secure secrets:
- 64-character JWT secret with alphanumeric and special characters
- 16-character strong password meeting complexity requirements

#### Rotate
Creates backup and generates new secrets:
- Backs up current configuration with timestamp
- Generates new JWT secret and admin password
- Provides environment variable commands

#### Clean
Removes hardcoded secrets from configuration:
- Replaces secrets with placeholder text
- Prepares configuration for production deployment

### Security Best Practices

1. **Never commit real secrets to source control**
   - Use environment variables in production
   - Use Azure Key Vault or similar for secret management

2. **Rotate secrets regularly**
   - Monthly rotation recommended
   - Immediate rotation after security incidents

3. **Use strong secrets**
   - JWT secrets: 64+ characters
   - Passwords: 16+ characters with complexity

4. **Environment-specific configuration**
   - Development: Can use default secrets for convenience
   - Production: Must use environment variables

### Environment Variables

Set these in production:

```powershell
# Windows
$env:JWT_SECRET = "your-secure-jwt-secret"
$env:SYSADMIN_PASSWORD = "your-secure-admin-password"

# Linux/Docker
export JWT_SECRET="your-secure-jwt-secret"
export SYSADMIN_PASSWORD="your-secure-admin-password"
```

### Integration with CI/CD

```yaml
# Azure DevOps Pipeline
- task: PowerShell@2
  inputs:
    targetType: 'filePath'
    filePath: 'scripts/Security/ManageSecrets.ps1'
    arguments: '-Action Validate'
  displayName: 'Validate Security Configuration'
```

### Backup Location

Backups are stored in: `{ProjectRoot}/backups/config/`

Format: `appsettings.backup.{yyyyMMddHHmmss}.json`