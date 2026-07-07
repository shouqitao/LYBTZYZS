import json
import os

def fix_db(path):
    with open(path) as f:
        cfg = json.load(f)

    if "ConnectionStrings" not in cfg:
        cfg["ConnectionStrings"] = {}
    cfg["ConnectionStrings"]["DefaultConnection"] = "Server=192.168.190.243;Database=LYBTDB_Dev;User ID=sa;Password=Shou@850528;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;"

    with open(path, "w") as f:
        json.dump(cfg, f, indent=2, ensure_ascii=False)

    print(f"Fixed DB config: {path}")

base = os.path.expanduser("~/lybt-api/appsettings.json")
prod = os.path.expanduser("~/lybt-api/appsettings.Production.json")

if os.path.exists(base):
    fix_db(base)
if os.path.exists(prod):
    fix_db(prod)
