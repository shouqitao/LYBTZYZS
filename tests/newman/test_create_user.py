import urllib.request
import json

login_url = "http://localhost:5000/api/v1/auth/login"
login_data = json.dumps({"username": "sysadmin", "password": "SysAdmin@2026!"}).encode('utf-8')
login_req = urllib.request.Request(login_url, data=login_data, headers={"Content-Type": "application/json"})

with urllib.request.urlopen(login_req, timeout=10) as response:
    token = json.loads(response.read().decode('utf-8'))['data']['token']

create_url = "http://localhost:5000/api/v1/users"
create_data = json.dumps({
    "userName": "testuser_api",
    "realName": "API测试用户",
    "email": "test@lybt.com",
    "password": "Admin@123456",
    "role": 1
}).encode('utf-8')
create_req = urllib.request.Request(create_url, data=create_data, headers={
    "Content-Type": "application/json",
    "Authorization": f"Bearer {token}"
})

try:
    with urllib.request.urlopen(create_req, timeout=10) as response:
        result = json.loads(response.read().decode('utf-8'))
        print(json.dumps(result, indent=2, ensure_ascii=False))
except urllib.error.HTTPError as e:
    body = e.read().decode('utf-8')
    print(f"HTTP {e.code}: {body}")
