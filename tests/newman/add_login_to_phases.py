import json

with open("D:/source/repos/LYBTZYZS/tests/newman/lybt-full-api-collection.json", "r", encoding="utf-8") as f:
    collection = json.load(f)

def make_login_request(name, username, password, token_var, user_id_var):
    return {
        "name": name,
        "request": {
            "method": "POST",
            "url": {"raw": "{{baseUrl}}/api/v1/auth/login", "host": ["{{baseUrl}}"], "path": ["api","v1","auth","login"]},
            "header": [{"key": "Content-Type", "value": "application/json"}],
            "body": {"mode": "raw", "raw": json.dumps({"username": username, "password": password})}
        },
        "event": [{"listen": "test", "script": {"exec": [
            f"pm.test('Status 200', function(){{pm.response.to.have.status(200)}});",
            f"pm.test('Has token', function(){{var d=pm.response.json().data;pm.expect(d).to.have.property('token');pm.globals.set('{token_var}',d.token);pm.globals.set('{user_id_var}',d.user.id);}});"
        ]}}]
    }

for phase in collection["item"]:
    name = phase["name"]
    
    if "Phase 1.3" in name:
        # SysAdmin system endpoints - reuse sysadmin token from Phase 1
        continue
    
    if "Phase 2: Admin" in name:
        login = make_login_request("2.1 Login as Admin", "newadmin", "Admin@123456", "admin_token", "admin_user_id")
        phase["item"].insert(0, login)
    
    elif "Phase 3: Receptionist" in name:
        login = make_login_request("3.1 Login as Receptionist", "reception1", "Recep@123", "receptionist_token", "receptionist_user_id")
        phase["item"].insert(0, login)
    
    elif "Phase 4: Doctor" in name:
        login = make_login_request("4.1 Login as Doctor", "doctor1", "Doctor@123", "doctor_token", "doctor_user_id")
        phase["item"].insert(0, login)

with open("D:/source/repos/LYBTZYZS/tests/newman/lybt-full-api-collection.json", "w", encoding="utf-8") as f:
    json.dump(collection, f, indent=2, ensure_ascii=False)

print("Added login requests to Phase 2, 3, 4")
