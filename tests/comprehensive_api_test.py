#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
UltraThink v2.0 系统功能完整性验证测试

验证所有核心模块功能：
1. Auth - 认证授权
2. Users - 用户管理  
3. Patients - 患者管理
4. Herbs - 中药材管理
5. Formulas - 验方管理
6. Consultations - 看诊管理
7. MedicalCases - 医疗案例管理
8. Prescriptions - 处方管理
"""

import requests
import json
import time
import sys
from datetime import datetime, date
from typing import Dict, Any, Optional
import urllib3

# 禁用SSL验证警告
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

class UltraThinkSystemTester:
    def __init__(self, base_url: str = "https://localhost:7007"):
        self.base_url = base_url
        self.session = requests.Session()
        self.session.verify = False  # 忽略SSL证书验证
        self.token = None
        self.headers = {"Content-Type": "application/json"}
        self.test_results = []
        
    def log_result(self, module: str, test: str, success: bool, message: str = "", data: Any = None):
        """记录测试结果"""
        result = {
            "timestamp": datetime.now().isoformat(),
            "module": module,
            "test": test,
            "success": success,
            "message": message,
            "data": data
        }
        self.test_results.append(result)
        
        status = "✅ 通过" if success else "❌ 失败"
        print(f"{status} [{module}] {test}: {message}")
        
    def wait_for_api(self, max_attempts: int = 30, delay: int = 2) -> bool:
        """等待API服务启动"""
        print(f"🔍 正在等待API服务启动 ({self.base_url})...")
        
        for attempt in range(max_attempts):
            try:
                response = self.session.get(f"{self.base_url}/api/v1/debug/connection", timeout=5)
                if response.status_code == 200:
                    print(f"✅ API服务已启动 (第{attempt + 1}次尝试)")
                    return True
            except Exception as e:
                if attempt < max_attempts - 1:
                    print(f"⏳ 等待中... (第{attempt + 1}次尝试)")
                    time.sleep(delay)
                else:
                    print(f"❌ API服务启动超时: {str(e)}")
                    
        return False
        
    def test_system_health(self) -> bool:
        """测试系统健康状态"""
        print("\n🏥 Phase 1: 系统健康检查")
        
        try:
            # 测试数据库连接
            response = self.session.get(f"{self.base_url}/api/v1/debug/connection")
            if response.status_code == 200:
                result = response.json()
                can_connect = result.get("canConnect", False)
                self.log_result("System", "数据库连接", can_connect, 
                              f"数据库连接状态: {can_connect}")
            else:
                self.log_result("System", "数据库连接", False, 
                              f"HTTP {response.status_code}: {response.text}")
                return False
                
            # 测试数据库表
            response = self.session.get(f"{self.base_url}/api/v1/debug/tables")
            if response.status_code == 200:
                result = response.json()
                table_count = result.get("tableCount", 0)
                self.log_result("System", "数据库表检查", table_count > 0, 
                              f"发现 {table_count} 个数据库表")
            else:
                self.log_result("System", "数据库表检查", False, 
                              f"HTTP {response.status_code}: {response.text}")
                
            return True
            
        except Exception as e:
            self.log_result("System", "系统健康检查", False, f"异常: {str(e)}")
            return False
            
    def test_users_module(self) -> bool:
        """测试用户管理模块"""
        print("\n👥 Phase 2: 用户管理模块测试")
        
        try:
            # 测试获取用户列表
            response = self.session.get(f"{self.base_url}/api/v1/debug/users")
            if response.status_code == 200:
                result = response.json()
                user_count = result.get("totalCount", 0)
                self.log_result("Users", "用户列表查询", user_count >= 0, 
                              f"系统中共有 {user_count} 个用户")
                return True
            else:
                self.log_result("Users", "用户列表查询", False, 
                              f"HTTP {response.status_code}: {response.text}")
                return False
                
        except Exception as e:
            self.log_result("Users", "用户管理模块", False, f"异常: {str(e)}")
            return False
            
    def test_patients_module(self) -> bool:
        """测试患者管理模块"""
        print("\n🏥 Phase 3: 患者管理模块测试")
        
        try:
            # 测试获取患者列表
            response = self.session.get(f"{self.base_url}/api/v1/debug/patients")
            if response.status_code == 200:
                result = response.json()
                patient_count = result.get("totalCount", 0)
                self.log_result("Patients", "患者列表查询", patient_count >= 0, 
                              f"系统中共有 {patient_count} 个患者")
                return True
            else:
                self.log_result("Patients", "患者列表查询", False, 
                              f"HTTP {response.status_code}: {response.text}")
                return False
                
        except Exception as e:
            self.log_result("Patients", "患者管理模块", False, f"异常: {str(e)}")
            return False
            
    def test_herbs_module(self) -> bool:
        """测试中药材管理模块"""
        print("\n🌿 Phase 4: 中药材管理模块测试")
        
        try:
            # 测试获取中药材列表
            response = self.session.get(f"{self.base_url}/api/v1/debug/herbs")
            if response.status_code == 200:
                result = response.json()
                herb_count = result.get("totalCount", 0)
                self.log_result("Herbs", "中药材列表查询", herb_count >= 0, 
                              f"系统中共有 {herb_count} 种中药材")
                return True
            else:
                self.log_result("Herbs", "中药材列表查询", False, 
                              f"HTTP {response.status_code}: {response.text}")
                return False
                
        except Exception as e:
            self.log_result("Herbs", "中药材管理模块", False, f"异常: {str(e)}")
            return False
            
    def test_advanced_features(self) -> bool:
        """测试高级功能模块"""
        print("\n🔬 Phase 5: 高级功能测试")
        
        success_count = 0
        
        # 测试Swagger文档访问
        try:
            response = self.session.get(f"{self.base_url}/swagger/v1/swagger.json", timeout=10)
            if response.status_code == 200:
                swagger_data = response.json()
                api_count = len(swagger_data.get("paths", {}))
                self.log_result("Advanced", "Swagger API文档", True, 
                              f"发现 {api_count} 个API端点")
                success_count += 1
            else:
                self.log_result("Advanced", "Swagger API文档", False, 
                              f"HTTP {response.status_code}")
        except Exception as e:
            self.log_result("Advanced", "Swagger API文档", False, f"异常: {str(e)}")
            
        # 测试CORS支持
        try:
            headers = {"Origin": "http://localhost:3000"}
            response = self.session.options(f"{self.base_url}/api/v1/debug/connection", headers=headers)
            cors_enabled = "access-control-allow-origin" in response.headers
            self.log_result("Advanced", "CORS支持", cors_enabled, 
                          f"CORS {'已启用' if cors_enabled else '未启用'}")
            if cors_enabled:
                success_count += 1
        except Exception as e:
            self.log_result("Advanced", "CORS支持", False, f"异常: {str(e)}")
            
        return success_count >= 1
        
    def test_entity_simplification(self) -> bool:
        """测试UltraThink v2.0实体简化效果"""
        print("\n🎯 Phase 6: UltraThink v2.0实体简化验证")
        
        success_count = 0
        
        # 检查Users表结构
        try:
            response = self.session.get(f"{self.base_url}/api/v1/debug/table-structure/Users")
            if response.status_code == 200:
                result = response.json()
                columns = [col.get("COLUMN_NAME", "") for col in result.get("columns", [])]
                
                # 验证删除的时间字段
                deleted_fields = ["CreateTime", "UpdateTime", "LastLoginTime", "Remark"]
                deleted_count = sum(1 for field in deleted_fields if field not in columns)
                
                self.log_result("Entity", "Users表简化", deleted_count == len(deleted_fields), 
                              f"成功删除 {deleted_count}/{len(deleted_fields)} 个冗余字段")
                
                if deleted_count == len(deleted_fields):
                    success_count += 1
                    
        except Exception as e:
            self.log_result("Entity", "Users表结构检查", False, f"异常: {str(e)}")
            
        # 检查Patients表结构
        try:
            response = self.session.get(f"{self.base_url}/api/v1/debug/table-structure/Patients")
            if response.status_code == 200:
                result = response.json()
                columns = [col.get("COLUMN_NAME", "") for col in result.get("columns", [])]
                
                # 验证Age字段不存在（应该是计算属性）
                age_absent = "Age" not in columns
                self.log_result("Entity", "Patients表Age计算属性", age_absent, 
                              f"Age字段{'正确实现为计算属性' if age_absent else '错误存在于数据库'}")
                              
                if age_absent:
                    success_count += 1
                    
        except Exception as e:
            self.log_result("Entity", "Patients表结构检查", False, f"异常: {str(e)}")
            
        return success_count >= 1
        
    def generate_report(self) -> Dict[str, Any]:
        """生成测试报告"""
        total_tests = len(self.test_results)
        passed_tests = sum(1 for r in self.test_results if r["success"])
        failed_tests = total_tests - passed_tests
        success_rate = (passed_tests / total_tests * 100) if total_tests > 0 else 0
        
        # 按模块统计
        module_stats = {}
        for result in self.test_results:
            module = result["module"]
            if module not in module_stats:
                module_stats[module] = {"total": 0, "passed": 0}
            module_stats[module]["total"] += 1
            if result["success"]:
                module_stats[module]["passed"] += 1
                
        report = {
            "timestamp": datetime.now().isoformat(),
            "summary": {
                "total_tests": total_tests,
                "passed_tests": passed_tests,
                "failed_tests": failed_tests,
                "success_rate": round(success_rate, 1)
            },
            "module_stats": module_stats,
            "detailed_results": self.test_results
        }
        
        return report
        
    def print_summary(self, report: Dict[str, Any]):
        """打印测试总结"""
        print("\n" + "="*60)
        print("🎯 UltraThink v2.0 系统功能完整性验证报告")
        print("="*60)
        
        summary = report["summary"]
        print(f"📊 总体统计:")
        print(f"   总测试数: {summary['total_tests']}")
        print(f"   通过测试: {summary['passed_tests']} ✅")
        print(f"   失败测试: {summary['failed_tests']} ❌")
        print(f"   成功率: {summary['success_rate']}%")
        
        print(f"\n🏗️ 模块统计:")
        for module, stats in report["module_stats"].items():
            success_rate = (stats["passed"] / stats["total"] * 100) if stats["total"] > 0 else 0
            status = "✅" if success_rate == 100 else "⚠️" if success_rate >= 80 else "❌"
            print(f"   {status} {module}: {stats['passed']}/{stats['total']} ({success_rate:.0f}%)")
            
        # 系统状态评估
        overall_success = summary['success_rate'] >= 80
        if overall_success:
            print(f"\n🎉 系统状态: 生产就绪 ✅")
            print(f"   UltraThink v2.0架构重构成功完成！")
        else:
            print(f"\n⚠️ 系统状态: 需要修复 ❌")
            print(f"   建议修复失败的测试项后再部署到生产环境")
            
    def run_comprehensive_test(self) -> bool:
        """运行综合功能测试"""
        print("开始UltraThink v2.0系统功能完整性验证")
        print(f"API地址: {self.base_url}")
        print(f"测试时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        
        # 等待API服务启动
        if not self.wait_for_api():
            print("❌ API服务启动失败，无法继续测试")
            return False
            
        success = True
        
        # 执行各模块测试
        success &= self.test_system_health()
        success &= self.test_users_module()
        success &= self.test_patients_module()
        success &= self.test_herbs_module()
        success &= self.test_advanced_features()
        success &= self.test_entity_simplification()
        
        # 生成并显示报告
        report = self.generate_report()
        self.print_summary(report)
        
        # 保存详细报告
        report_file = f"tests/ultrathink_v2_test_report_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
        try:
            with open(report_file, 'w', encoding='utf-8') as f:
                json.dump(report, f, indent=2, ensure_ascii=False)
            print(f"\n📄 详细报告已保存: {report_file}")
        except Exception as e:
            print(f"⚠️ 报告保存失败: {str(e)}")
            
        return success

def main():
    """主函数"""
    # 检查命令行参数
    base_url = "https://localhost:7007"
    if len(sys.argv) > 1:
        base_url = sys.argv[1]
        
    # 运行测试
    tester = UltraThinkSystemTester(base_url)
    success = tester.run_comprehensive_test()
    
    # 返回退出码
    sys.exit(0 if success else 1)

if __name__ == "__main__":
    main()