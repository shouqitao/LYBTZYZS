import json
import os

config_path = os.path.expanduser("~/lybt-api/appsettings.Production.json")
with open(config_path) as f:
    cfg = json.load(f)

cfg["SystemAdmin"]["InitialSetupToken"] = "lybt-2026-initial-setup-token-secure"

with open(config_path, "w") as f:
    json.dump(cfg, f, indent=2, ensure_ascii=False)

print("Token fixed")
