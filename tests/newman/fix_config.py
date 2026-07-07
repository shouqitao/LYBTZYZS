import json
import os

config_path = os.path.expanduser("~/lybt-api/appsettings.json")
with open(config_path) as f:
    cfg = json.load(f)

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
    "AllowAutoCreateInProduction": False,
    "InitialSetupToken": "lybt-2026-initial-setup-token-secure"
}
cfg["Security"] = {"RateLimiting": {"Enabled": False}}

with open(config_path, "w") as f:
    json.dump(cfg, f, indent=2, ensure_ascii=False)

print("Config fixed successfully")
