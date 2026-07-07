import urllib.request
import json

url = "http://localhost:5000/api/v1/auth/login"
data = json.dumps({"username": "sysadmin", "password": "SysAdmin@2026!"}).encode('utf-8')
req = urllib.request.Request(url, data=data, headers={"Content-Type": "application/json"})

try:
    with urllib.request.urlopen(req, timeout=10) as response:
        result = json.loads(response.read().decode('utf-8'))
        print(json.dumps(result, indent=2))
except Exception as e:
    print(f"Error: {e}")
