import json
import os

config_path = os.path.expanduser("~/lybt-api/appsettings.json")
with open(config_path) as f:
    cfg = json.load(f)

# Remove HTTPS endpoint
if "Kestrel" in cfg and "Endpoints" in cfg["Kestrel"]:
    endpoints = cfg["Kestrel"]["Endpoints"]
    if "Https" in endpoints:
        del endpoints["Https"]

# Add all required fields
cfg["DefaultPasswords"] = {
    "SysAdminPassword": "SysAdmin@2026!",
    "NewUserPassword": "User@2026!Qwx",
    "AdminPassword": "Admin@123456"
}
cfg["Jwt"] = {
    "SecretKey": "jin39uYqW840gYkGyxlHozWYwyTO/hjpM2ylVbbIniU=",
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client",
    "AccessTokenExpirationMinutes": 30
}
cfg["SystemAdmin"] = {
    "UserName": "sysadmin",
    "Email": "sysadmin@lybtzyzs.local",
    "DisplayName": "系统管理员",
    "AllowAutoCreateInProduction": False,
    "InitialSetupToken": "lybt-2026-initial-setup-token-secure"
}
cfg["Security"] = {"RateLimiting": {"Enabled": False}}

with open(config_path, "w") as f:
    json.dump(cfg, f, indent=2, ensure_ascii=False)

print("Base config fixed - HTTPS removed, all fields added")
