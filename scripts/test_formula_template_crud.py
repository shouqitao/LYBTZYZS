"""
验方模板CRUD功能测试脚本
测试验方模板的创建、读取、更新、删除功能
"""

import requests
import json
import time
from datetime import datetime
import uuid

# 配置
BASE_URL = "https://localhost:7001/api/v1"
USERNAME = "sysadmin"
PASSWORD = "Admin@123456"

# 禁用SSL警告（仅用于测试）
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

class FormulaTemplateAPITest:
    def __init__(self):
        self.token = None
        self.headers = {
            'Content-Type': 'application/json'
        }
        self.created_template_id = None
        
    def login(self):
        """登录获取Token"""
        print("=" * 60)
        print("1. 登录测试")
        print("=" * 60)
        
        login_data = {
            "username": USERNAME,
            "password": PASSWORD,
            "rememberMe": False
        }
        
        try:
            response = requests.post(
                f"{BASE_URL}/auth/login",
                json=login_data,
                verify=False
            )
            
            if response.status_code == 200:
                result = response.json()
                if result.get("success") and result.get("data"):
                    self.token = result["data"]["token"]
                    self.headers['Authorization'] = f"Bearer {self.token}"
                    print(f"✅ 登录成功")
                    print(f"   用户: {USERNAME}")
                    return True
                else:
                    print(f"❌ 登录失败: {result.get('message', '未知错误')}")
                    return False
            else:
                print(f"❌ 登录失败: HTTP {response.status_code}")
                return False
                
        except Exception as e:
            print(f"❌ 登录异常: {e}")
            return False
    
    def test_create_template(self):
        """测试创建验方模板"""
        print("\n" + "=" * 60)
        print("2. 创建验方模板测试")
        print("=" * 60)
        
        template_data = {
            "name": f"测试验方_{datetime.now().strftime('%Y%m%d_%H%M%S')}",
            "category": "内科方",
            "indications": "感冒、发热、头痛",
            "efficacy": "疏风解表，清热解毒",
            "usage": "水煎服，每日一剂，分两次服用",
            "remark": "测试用验方模板",
            "herbs": [
                {
                    "herbId": str(uuid.uuid4()),
                    "herbName": "金银花",
                    "dosage": 15,
                    "unit": "g",
                    "remark": "清热解毒"
                },
                {
                    "herbId": str(uuid.uuid4()),
                    "herbName": "连翘",
                    "dosage": 10,
                    "unit": "g",
                    "remark": "清热解毒"
                },
                {
                    "herbId": str(uuid.uuid4()),
                    "herbName": "薄荷",
                    "dosage": 6,
                    "unit": "g",
                    "remark": "疏风散热"
                }
            ]
        }
        
        try:
            response = requests.post(
                f"{BASE_URL}/FormulaTemplate",
                json=template_data,
                headers=self.headers,
                verify=False
            )
            
            if response.status_code in [200, 201]:
                result = response.json()
                if result.get("success"):
                    self.created_template_id = result["data"]["id"]
                    print(f"✅ 创建验方模板成功")
                    print(f"   模板ID: {self.created_template_id}")
                    print(f"   模板名称: {template_data['name']}")
                    print(f"   包含药材: {len(template_data['herbs'])}种")
                    return True
                else:
                    print(f"❌ 创建失败: {result.get('message', '未知错误')}")
                    return False
            else:
                print(f"❌ 创建失败: HTTP {response.status_code}")
                try:
                    error_detail = response.json()
                    print(f"   错误详情: {error_detail}")
                except:
                    print(f"   响应内容: {response.text}")
                return False
                
        except Exception as e:
            print(f"❌ 创建异常: {e}")
            return False
    
    def test_get_templates(self):
        """测试获取验方模板列表"""
        print("\n" + "=" * 60)
        print("3. 获取验方模板列表测试")
        print("=" * 60)
        
        try:
            response = requests.get(
                f"{BASE_URL}/FormulaTemplate",
                headers=self.headers,
                verify=False
            )
            
            if response.status_code == 200:
                result = response.json()
                if result.get("success"):
                    templates = result["data"]["items"] if "items" in result["data"] else result["data"]
                    print(f"✅ 获取列表成功")
                    print(f"   模板总数: {len(templates) if isinstance(templates, list) else 0}")
                    
                    # 显示前5个模板
                    if isinstance(templates, list) and templates:
                        print("   最近的模板:")
                        for i, template in enumerate(templates[:5], 1):
                            print(f"   {i}. {template.get('name', '未知')} - {template.get('category', '未分类')}")
                    return True
                else:
                    print(f"❌ 获取失败: {result.get('message', '未知错误')}")
                    return False
            else:
                print(f"❌ 获取失败: HTTP {response.status_code}")
                return False
                
        except Exception as e:
            print(f"❌ 获取异常: {e}")
            return False
    
    def test_get_template_by_id(self):
        """测试根据ID获取验方模板"""
        print("\n" + "=" * 60)
        print("4. 获取单个验方模板测试")
        print("=" * 60)
        
        if not self.created_template_id:
            print("⚠️  没有可用的模板ID，跳过测试")
            return False
        
        try:
            response = requests.get(
                f"{BASE_URL}/FormulaTemplate/{self.created_template_id}",
                headers=self.headers,
                verify=False
            )
            
            if response.status_code == 200:
                result = response.json()
                if result.get("success"):
                    template = result["data"]
                    print(f"✅ 获取模板成功")
                    print(f"   模板名称: {template.get('name', '未知')}")
                    print(f"   分类: {template.get('category', '未分类')}")
                    print(f"   适应症: {template.get('indications', '无')}")
                    
                    herbs = template.get('herbs', [])
                    if herbs:
                        print(f"   药材配方 ({len(herbs)}种):")
                        for herb in herbs:
                            print(f"     - {herb.get('herbName', '未知')}: {herb.get('dosage', 0)}{herb.get('unit', 'g')}")
                    return True
                else:
                    print(f"❌ 获取失败: {result.get('message', '未知错误')}")
                    return False
            else:
                print(f"❌ 获取失败: HTTP {response.status_code}")
                return False
                
        except Exception as e:
            print(f"❌ 获取异常: {e}")
            return False
    
    def test_update_template(self):
        """测试更新验方模板"""
        print("\n" + "=" * 60)
        print("5. 更新验方模板测试")
        print("=" * 60)
        
        if not self.created_template_id:
            print("⚠️  没有可用的模板ID，跳过测试")
            return False
        
        update_data = {
            "id": self.created_template_id,
            "name": f"更新的验方_{datetime.now().strftime('%H%M%S')}",
            "category": "外科方",
            "indications": "跌打损伤、淤血肿痛",
            "efficacy": "活血化瘀，消肿止痛",
            "usage": "外敷患处，每日2-3次",
            "remark": "更新后的验方模板",
            "herbs": [
                {
                    "herbId": str(uuid.uuid4()),
                    "herbName": "红花",
                    "dosage": 10,
                    "unit": "g",
                    "remark": "活血化瘀"
                },
                {
                    "herbId": str(uuid.uuid4()),
                    "herbName": "当归",
                    "dosage": 15,
                    "unit": "g",
                    "remark": "补血活血"
                }
            ]
        }
        
        try:
            response = requests.put(
                f"{BASE_URL}/FormulaTemplate/{self.created_template_id}",
                json=update_data,
                headers=self.headers,
                verify=False
            )
            
            if response.status_code == 200:
                result = response.json()
                if result.get("success"):
                    print(f"✅ 更新验方模板成功")
                    print(f"   新名称: {update_data['name']}")
                    print(f"   新分类: {update_data['category']}")
                    print(f"   药材数量: {len(update_data['herbs'])}种")
                    return True
                else:
                    print(f"❌ 更新失败: {result.get('message', '未知错误')}")
                    return False
            else:
                print(f"❌ 更新失败: HTTP {response.status_code}")
                try:
                    error_detail = response.json()
                    print(f"   错误详情: {error_detail}")
                except:
                    print(f"   响应内容: {response.text}")
                return False
                
        except Exception as e:
            print(f"❌ 更新异常: {e}")
            return False
    
    def test_delete_template(self):
        """测试删除验方模板"""
        print("\n" + "=" * 60)
        print("6. 删除验方模板测试")
        print("=" * 60)
        
        if not self.created_template_id:
            print("⚠️  没有可用的模板ID，跳过测试")
            return False
        
        try:
            response = requests.delete(
                f"{BASE_URL}/FormulaTemplate/{self.created_template_id}",
                headers=self.headers,
                verify=False
            )
            
            if response.status_code in [200, 204]:
                print(f"✅ 删除验方模板成功")
                print(f"   已删除模板ID: {self.created_template_id}")
                self.created_template_id = None
                return True
            else:
                print(f"❌ 删除失败: HTTP {response.status_code}")
                try:
                    error_detail = response.json()
                    print(f"   错误详情: {error_detail}")
                except:
                    pass
                return False
                
        except Exception as e:
            print(f"❌ 删除异常: {e}")
            return False
    
    def run_all_tests(self):
        """运行所有测试"""
        print("\n" + "=" * 60)
        print("验方模板CRUD功能测试")
        print("=" * 60)
        print(f"测试时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print(f"API地址: {BASE_URL}")
        
        # 测试结果统计
        results = {
            "登录": False,
            "创建": False,
            "列表": False,
            "详情": False,
            "更新": False,
            "删除": False
        }
        
        # 执行测试
        if self.login():
            results["登录"] = True
            
            # 创建测试
            if self.test_create_template():
                results["创建"] = True
                time.sleep(1)  # 等待数据持久化
                
                # 列表测试
                if self.test_get_templates():
                    results["列表"] = True
                
                # 详情测试
                if self.test_get_template_by_id():
                    results["详情"] = True
                
                # 更新测试
                if self.test_update_template():
                    results["更新"] = True
                    time.sleep(1)
                
                # 删除测试
                if self.test_delete_template():
                    results["删除"] = True
        
        # 显示测试结果
        print("\n" + "=" * 60)
        print("测试结果汇总")
        print("=" * 60)
        
        total = len(results)
        passed = sum(1 for v in results.values() if v)
        
        for test_name, passed_flag in results.items():
            status = "✅ 通过" if passed_flag else "❌ 失败"
            print(f"{test_name}测试: {status}")
        
        print("-" * 60)
        print(f"总计: {passed}/{total} 测试通过")
        
        if passed == total:
            print("\n🎉 所有测试通过！验方模板CRUD功能正常。")
        else:
            print(f"\n⚠️  有 {total - passed} 个测试失败，请检查相关功能。")
        
        return passed == total

def main():
    """主函数"""
    tester = FormulaTemplateAPITest()
    success = tester.run_all_tests()
    
    # 返回退出码
    exit(0 if success else 1)

if __name__ == "__main__":
    main()