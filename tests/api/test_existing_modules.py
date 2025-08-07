#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
LYBT中医诊所管理系统 - 现有模块API测试脚本
测试当前实际存在的模块
"""

import json
import requests
import time
from datetime import datetime
from typing import Dict, List, Tuple
import uuid

# 禁用SSL警告
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# 服务器配置
BASE_URL = "https://localhost:7001"
API_PREFIX = "/api/v1"

# 测试结果存储
test_results = []

# JWT 令牌存储
jwt_token = None

# 生成测试用的UUID
def generate_uuid():
    return str(uuid.uuid4())

# 现有模块的API定义
API_MODULES = {
    "Auth": {
        "endpoints": [
            {"path": "/Auth/Login", "method": "POST", "auth": False, "data": {"username": "sysadmin", "password": "Admin@123456"}},
            {"path": "/Auth/Logout", "method": "POST", "auth": True},
            {"path": "/Auth/RefreshToken", "method": "POST", "auth": True},
            {"path": "/Auth/ChangePassword", "method": "POST", "auth": True, "data": {"oldPassword": "Admin@123456", "newPassword": "Admin@123456"}}
        ]
    },
    "Users": {
        "endpoints": [
            {"path": "/Users", "method": "GET", "auth": True},
            {"path": "/Users", "method": "POST", "auth": True, "data": {
                "username": f"testuser_{int(time.time())}",
                "password": "ChangeMe123",
                "realName": "测试用户",
                "phoneNumber": "13800138000",
                "roleNames": ["Doctor"]
            }}
        ]
    },
    "Patients": {
        "endpoints": [
            {"path": "/Patients", "method": "GET", "auth": True},
            {"path": "/Patients", "method": "POST", "auth": True, "data": {
                "name": "测试患者",
                "gender": 1,  # Male
                "birthDate": "1990-01-01",
                "phoneNumber": "13900139000",
                "address": "测试地址"
            }}
        ]
    },
    "Herbs": {
        "endpoints": [
            {"path": "/Herbs", "method": "GET", "auth": True},
            {"path": "/Herbs", "method": "POST", "auth": True, "data": {
                "name": f"测试药材_{int(time.time())}",
                "pinYinCode": "CSYC",
                "category": "解表药",
                "efficacy": "测试功效",
                "dosage": "3-10克",
                "contraindication": "无",
                "unit": "克",
                "costPrice": 10.0,
                "retailPrice": 15.0
            }}
        ]
    },
    "Formulas": {
        "endpoints": [
            {"path": "/Formulas", "method": "GET", "auth": True},
            {"path": "/Formulas/categories", "method": "GET", "auth": True}
        ]
    },
    "Consultation": {
        "endpoints": [
            {"path": "/Consultation", "method": "GET", "auth": True, "params": {"pageIndex": 1, "pageSize": 10}},
            {"path": "/Consultation/start", "method": "POST", "auth": True, "data": {
                "medicalCaseId": generate_uuid(),
                "patientId": generate_uuid(),
                "userId": generate_uuid()
            }}
        ]
    },
    "MedicalCase": {
        "endpoints": [
            {"path": "/MedicalCase", "method": "GET", "auth": True, "params": {"pageIndex": 1, "pageSize": 10}},
            {"path": "/MedicalCase", "method": "POST", "auth": True, "data": {
                "patientId": generate_uuid(),
                "userId": generate_uuid(),
                "chiefComplaint": "测试主诉",
                "presentIllness": "测试现病史"
            }}
        ]
    },
    "Prescriptions": {
        "endpoints": [
            {"path": "/Prescriptions", "method": "GET", "auth": True},
            {"path": "/Prescriptions", "method": "POST", "auth": True, "data": {
                "consultationId": generate_uuid(),
                "medicalCaseId": generate_uuid(),
                "patientId": generate_uuid(),
                "userId": generate_uuid(),
                "prescriptionType": 0,  # TCM
                "items": []
            }}
        ]
    },
    "Health": {
        "endpoints": [
            {"path": "/Health", "method": "GET", "auth": False}
        ]
    }
}

def login() -> bool:
    """执行登录获取JWT令牌"""
    global jwt_token
    try:
        response = requests.post(
            f"{BASE_URL}{API_PREFIX}/Auth/Login",
            json={"username": "sysadmin", "password": "Admin@123456"},
            headers={"Content-Type": "application/json"},
            timeout=10,
            verify=False
        )
        if response.status_code == 200:
            data = response.json()
            if "token" in data:
                jwt_token = data["token"]
                print(f"[OK] 登录成功，获取到JWT令牌")
                return True
            else:
                print(f"[FAIL] 登录失败: 响应中没有token")
                return False
        print(f"[FAIL] 登录失败: {response.status_code} - {response.text}")
    except Exception as e:
        print(f"[ERROR] 登录异常: {str(e)}")
    return False

def build_url(endpoint: Dict[str, Any]) -> str:
    """构建完整的URL"""
    path = endpoint["path"]
    if "params" in endpoint:
        # 构建查询字符串
        query_params = "&".join(f"{k}={v}" for k, v in endpoint["params"].items())
        return f"{BASE_URL}{API_PREFIX}{path}?{query_params}"
    return f"{BASE_URL}{API_PREFIX}{path}"

def test_endpoint(module: str, endpoint: Dict[str, Any]) -> Tuple[bool, str]:
    """测试单个接口"""
    url = build_url(endpoint)
    method = endpoint["method"]
    auth = endpoint.get("auth", True)
    
    headers = {"Content-Type": "application/json"}
    if auth and jwt_token:
        headers["Authorization"] = f"Bearer {jwt_token}"
    
    try:
        data = endpoint.get("data")
        response = None
        
        if method == "GET":
            response = requests.get(url, headers=headers, timeout=10, verify=False)
        elif method == "POST":
            response = requests.post(url, json=data, headers=headers, timeout=10, verify=False)
        elif method == "PUT":
            response = requests.put(url, json=data, headers=headers, timeout=10, verify=False)
        elif method == "DELETE":
            response = requests.delete(url, headers=headers, timeout=10, verify=False)
        
        if response and response.status_code in [200, 201, 204]:
            return True, ""
        else:
            error_msg = f"状态码: {response.status_code}"
            try:
                error_data = response.json()
                if "message" in error_data:
                    error_msg = error_data["message"]
                elif "title" in error_data:
                    error_msg = error_data["title"]
                elif "errors" in error_data:
                    error_msg = str(error_data["errors"])
            except:
                error_msg += f" - {response.text[:200]}"
            return False, error_msg
            
    except requests.exceptions.ConnectionError:
        return False, "连接失败 - 请确保API服务正在运行"
    except requests.exceptions.Timeout:
        return False, "请求超时"
    except Exception as e:
        return False, str(e)

def run_all_tests():
    """运行所有API测试"""
    global test_results
    
    print("开始测试现有模块的API...")
    print(f"目标服务器: {BASE_URL}")
    print("-" * 80)
    
    # 首先登录
    if not login():
        print("登录失败，无法继续测试")
        return
    
    # 遍历所有模块和接口
    for module_name, module_config in API_MODULES.items():
        print(f"\n测试模块: {module_name}")
        
        for endpoint in module_config["endpoints"]:
            path = endpoint["path"]
            method = endpoint["method"]
            
            # 执行测试
            success, error = test_endpoint(module_name, endpoint)
            
            # 记录结果
            test_results.append({
                "module": module_name,
                "path": path,
                "method": method,
                "success": success,
                "error": error
            })
            
            # 打印进度
            status = "[OK]" if success else "[FAIL]"
            error_info = f" - {error}" if error else ""
            print(f"  {status} {method} {path}{error_info}")
            
            # 避免请求过快
            time.sleep(0.1)
    
    print("\n" + "-" * 80)
    print("测试完成！")

def generate_report():
    """生成测试报告"""
    report_content = f"""# 现有模块API测试报告

测试时间: {datetime.now().strftime("%Y-%m-%d %H:%M:%S")}
服务器地址: {BASE_URL}

## 测试结果汇总

| 模块名 | 接口路径 | 方法 | 测试结果 | 备注 |
|--------|----------|------|----------|------|
"""
    
    # 统计信息
    total_count = len(test_results)
    success_count = sum(1 for r in test_results if r["success"])
    failure_count = total_count - success_count
    
    if total_count == 0:
        print("没有测试结果可生成报告")
        return
    
    # 生成表格
    for result in test_results:
        status = "✅ 成功" if result["success"] else "❌ 失败"
        error = result["error"] if result["error"] else "-"
        report_content += f"| {result['module']} | {result['path']} | {result['method']} | {status} | {error} |\n"
    
    # 添加统计
    report_content += f"\n## 统计信息\n\n"
    report_content += f"- 总接口数: {total_count}\n"
    report_content += f"- 成功数: {success_count}\n"
    report_content += f"- 失败数: {failure_count}\n"
    report_content += f"- 成功率: {success_count/total_count*100:.2f}%\n"
    
    # 失败接口汇总
    if failure_count > 0:
        report_content += "\n## 失败接口详情\n\n"
        for result in test_results:
            if not result["success"]:
                report_content += f"### {result['module']} - {result['method']} {result['path']}\n"
                report_content += f"- 错误信息: {result['error']}\n\n"
    
    # 保存报告
    report_path = "D:/source/repos/LYBTZYZS/tests/api/现有模块API测试报告.md"
    with open(report_path, "w", encoding="utf-8") as f:
        f.write(report_content)
    
    print(f"\n报告已生成: {report_path}")
    print(f"成功率: {success_count}/{total_count} ({success_count/total_count*100:.2f}%)")

if __name__ == "__main__":
    print("注意：请确保 Web API 服务正在运行（https://localhost:7001）")
    input("按 Enter 键开始测试...")
    run_all_tests()
    generate_report()