import json
import os

config_path = os.path.expanduser("~/lybt-api/appsettings.Production.json")
with open(config_path) as f:
    cfg = json.load(f)

# Ensure all required fields exist
if "SystemAdmin" not in cfg:
    cfg["SystemAdmin"] = {}
cfg["SystemAdmin"]["DisplayName"] = cfg["SystemAdmin"].get("DisplayName", "系统管理员")
cfg["SystemAdmin"]["UserName"] = cfg["SystemAdmin"].get("UserName", "sysadmin")
cfg["SystemAdmin"]["Email"] = cfg["SystemAdmin"].get("Email", "sysadmin@lybtzyzs.local")
cfg["SystemAdmin"]["AllowAutoCreateInProduction"] = cfg["SystemAdmin"].get("AllowAutoCreateInProduction", False)
cfg["SystemAdmin"]["InitialSetupToken"] = cfg["SystemAdmin"].get("InitialSetupToken", "lybt-2026-initial-setup-token-secure")

with open(config_path, "w") as f:
    json.dump(cfg, f, indent=2, ensure_ascii=False)

print("All config fields fixed")
