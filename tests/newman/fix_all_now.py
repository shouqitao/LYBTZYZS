import json
import os

configs = [
    os.path.expanduser("~/lybt-api/appsettings.json"),
    os.path.expanduser("~/lybt-api/appsettings.Production.json")
]

for path in configs:
    if not os.path.exists(path):
        continue
    
    with open(path) as f:
        cfg = json.load(f)
    
    # Remove HTTPS
    if "Kestrel" in cfg and "Endpoints" in cfg["Kestrel"]:
        endpoints = cfg["Kestrel"]["Endpoints"]
        if "Https" in endpoints:
            del endpoints["Https"]
    
    # Fix passwords
    if "DefaultPasswords" not in cfg:
        cfg["DefaultPasswords"] = {}
    cfg["DefaultPasswords"]["SysAdminPassword"] = "SysAdmin@2026!"
    cfg["DefaultPasswords"]["NewUserPassword"] = "User@2026!Qwx"
    cfg["DefaultPasswords"]["AdminPassword"] = "Admin@123456"
    
    # Fix JWT
    if "Jwt" not in cfg:
        cfg["Jwt"] = {}
    cfg["Jwt"]["SecretKey"] = "jin39uYqW840gYkGyxlHozWYwyTO/hjpM2ylVbbIniU="
    cfg["Jwt"]["Issuer"] = "LYBT.WebAPI"
    cfg["Jwt"]["Audience"] = "LYBT.Client"
    cfg["Jwt"]["AccessTokenExpirationMinutes"] = 30
    
    # Fix ConnectionString
    if "ConnectionStrings" not in cfg:
        cfg["ConnectionStrings"] = {}
    cfg["ConnectionStrings"]["DefaultConnection"] = "Server=192.168.190.243;Database=LYBTDB_Dev;User ID=sa;Password=Shou@850528;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;"
    
    # Fix SystemAdmin
    if "SystemAdmin" not in cfg:
        cfg["SystemAdmin"] = {}
    cfg["SystemAdmin"]["UserName"] = "sysadmin"
    cfg["SystemAdmin"]["Email"] = "sysadmin@lybtzyzs.local"
    cfg["SystemAdmin"]["DisplayName"] = "系统管理员"
    cfg["SystemAdmin"]["AllowAutoCreateInProduction"] = False
    cfg["SystemAdmin"]["InitialSetupToken"] = "lybt-2026-initial-setup-token-secure"
    
    # Fix Security
    if "Security" not in cfg:
        cfg["Security"] = {}
    cfg["Security"]["RateLimiting"] = {"Enabled": False}
    
    with open(path, "w") as f:
        json.dump(cfg, f, indent=2, ensure_ascii=False)
    
    print(f"Fixed: {path}")
