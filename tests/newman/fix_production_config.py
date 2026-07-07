import json
import os

config_path = os.path.expanduser("~/lybt-api/appsettings.Production.json")
with open(config_path) as f:
    cfg = json.load(f)

# Fix DefaultPasswords
if "DefaultPasswords" in cfg:
    for key in cfg["DefaultPasswords"]:
        val = cfg["DefaultPasswords"][key]
        if val == "${SYSADMIN_PASSWORD}":
            cfg["DefaultPasswords"][key] = "SysAdmin@2026!"
        elif val == "${NEWUSER_PASSWORD}":
            cfg["DefaultPasswords"][key] = "User@2026!Qwx"

# Fix Jwt SecretKey
if "Jwt" in cfg and cfg["Jwt"].get("SecretKey") == "${JWT_SECRET}":
    cfg["Jwt"]["SecretKey"] = "jin39uYqW840gYkGyxlHozWYwyTO/hjpM2ylVbbIniU="

# Fix ConnectionStrings
if "ConnectionStrings" in cfg:
    conn = cfg["ConnectionStrings"]["DefaultConnection"]
    conn = conn.replace("${DB_SERVER}", "192.168.190.243")
    conn = conn.replace("${DB_USER}", "sa")
    conn = conn.replace("${DB_PASSWORD}", "Shou@850528")
    cfg["ConnectionStrings"]["DefaultConnection"] = conn

with open(config_path, "w") as f:
    json.dump(cfg, f, indent=2, ensure_ascii=False)

print("Production config fixed")
