import urllib.request
import json

# Login first
login_url = "http://localhost:5000/api/v1/auth/login"
login_data = json.dumps({"username": "sysadmin", "password": "SysAdmin@2026!"}).encode('utf-8')
login_req = urllib.request.Request(login_url, data=login_data, headers={"Content-Type": "application/json"})

with urllib.request.urlopen(login_req, timeout=10) as response:
    login_result = json.loads(response.read().decode('utf-8'))
    token = login_result['data']['token']
    print(f"Token obtained: {token[:50]}...")

# Test creating a user
create_url = "http://localhost:5000/api/v1/users"
create_data = json.dumps({
    "userName": "testuser_newman",
    "realName": "测试用户",
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
        print(f"Create user: {result.get('success', False)}")
        if result.get('success'):
            user_id = result['data']['id']
            print(f"User ID: {user_id}")
            
            # Test getting user detail
            get_url = f"http://localhost:5000/api/v1/users/{user_id}"
            get_req = urllib.request.Request(get_url, headers={"Authorization": f"Bearer {token}"})
            with urllib.request.urlopen(get_req, timeout=10) as get_response:
                get_result = json.loads(get_response.read().decode('utf-8'))
                print(f"Get user: {get_result.get('success', False)}")
except Exception as e:
    print(f"Error: {e}")
