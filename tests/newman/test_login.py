import json
import subprocess

data = json.dumps({"username": "sysadmin", "password": "SysAdmin@2026!"})
with open("/tmp/login.json", "w") as f:
    f.write(data)

result = subprocess.run(
    ["curl", "-s", "-X", "POST", "http://localhost:5000/api/v1/auth/login",
     "-H", "Content-Type: application/json", "-d", "@/tmp/login.json"],
    capture_output=True, text=True
)
print(result.stdout)
