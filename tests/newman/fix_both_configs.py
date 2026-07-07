import json
import os

def fix_config(path):
    with open(path) as f:
        cfg = json.load(f)

    # Remove HTTPS endpoint
    if "Kestrel" in cfg and "Endpoints" in cfg["Kestrel"]:
        endpoints = cfg["Kestrel"]["Endpoints"]
        if "Https" in endpoints:
            del endpoints["Https"]

    # Add all required fields
    if "DefaultPasswords" not in cfg:
        cfg["DefaultPasswords"] = {}
    cfg["DefaultPasswords"]["SysAdminPassword"] = "SysAdmin@2026!"
    cfg["DefaultPasswords"]["NewUserPassword"] = "User@2026!Qwx"
    cfg["DefaultPasswords"]["AdminPassword"] = "Admin@123456"

    if "Jwt" not in cfg:
        cfg["Jwt"] = {}
    cfg["Jwt"]["SecretKey"] = "jin39uYqW840gYkGyxlHozWYwyTO/hjpM2ylVbbIniU="
    cfg["Jwt"]["Issuer"] = "LYBT.WebAPI"
    cfg["Jwt"]["Audience"] = "LYBT.Client"
    cfg["Jwt"]["AccessTokenExpirationMinutes"] = 30

    if "SystemAdmin" not in cfg:
        cfg["SystemAdmin"] = {}
    cfg["SystemAdmin"]["UserName"] = "sysadmin"
    cfg["SystemAdmin"]["Email"] = "sysadmin@lybtzyzs.local"
    cfg["SystemAdmin"]["DisplayName"] = "系统管理员"
    cfg["SystemAdmin"]["AllowAutoCreateInProduction"] = False
    cfg["SystemAdmin"]["InitialSetupToken"] = "lybt-2026-initial-setup-token-secure"

    if "Security" not in cfg:
        cfg["Security"] = {}
    cfg["Security"]["RateLimiting"] = {"Enabled": False}

    with open(path, "w") as f:
        json.dump(cfg, f, indent=2, ensure_ascii=False)

    print(f"Fixed: {path}")

# Fix both config files
base = os.path.expanduser("~/lybt-api/appsettings.json")
prod = os.path.expanduser("~/lybt-api/appsettings.Production.json")

if os.path.exists(base):
    fix_config(base)
if os.path.exists(prod):
    fix_config(prod)
