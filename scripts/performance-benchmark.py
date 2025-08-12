#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
LYBT数据库性能基准测试脚本
UltraThink重构 - 数据库查询优化验证

功能:
1. 执行CQRS查询性能测试
2. 验证索引优化效果
3. 生成性能分析报告
4. 对比优化前后的性能指标
"""

import requests
import time
import json
import statistics
from datetime import datetime
from typing import Dict, List, Any
import urllib3

# 禁用SSL警告
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

class PerformanceBenchmark:
    def __init__(self, base_url: str = "https://localhost:7001"):
        self.base_url = base_url
        self.token = None
        self.results = {
            "test_start_time": datetime.now().isoformat(),
            "user_queries": [],
            "patient_queries": [],
            "herb_queries": [],
            "prescription_queries": [],
            "summary": {}
        }

    def authenticate(self) -> bool:
        """获取JWT认证令牌"""
        print("🔐 正在获取认证令牌...")
        
        try:
            auth_data = {
                "username": "sysadmin",
                "password": "Admin@123456",
                "rememberMe": False
            }
            
            response = requests.post(
                f"{self.base_url}/api/v1/auth/login",
                json=auth_data,
                verify=False,
                timeout=30
            )
            
            if response.status_code == 200:
                result = response.json()
                if result.get("success") and result.get("data", {}).get("token"):
                    self.token = result["data"]["token"]
                    print("✅ 认证成功")
                    return True
            
            print(f"❌ 认证失败: {response.status_code}")
            print(f"响应: {response.text}")
            return False
            
        except Exception as e:
            print(f"❌ 认证过程中发生错误: {str(e)}")
            return False

    def get_headers(self) -> Dict[str, str]:
        """获取请求头"""
        headers = {
            "Content-Type": "application/json",
            "Accept": "application/json"
        }
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"
        return headers

    def measure_request_time(self, url: str, method: str = "GET", data: Dict = None) -> Dict[str, Any]:
        """测量单个请求的执行时间"""
        start_time = time.time()
        
        try:
            if method.upper() == "GET":
                response = requests.get(url, headers=self.get_headers(), verify=False, timeout=30)
            elif method.upper() == "POST":
                response = requests.post(url, json=data, headers=self.get_headers(), verify=False, timeout=30)
            else:
                raise ValueError(f"不支持的HTTP方法: {method}")
            
            end_time = time.time()
            duration_ms = (end_time - start_time) * 1000
            
            return {
                "success": response.status_code == 200,
                "status_code": response.status_code,
                "duration_ms": duration_ms,
                "response_size": len(response.content) if response.content else 0,
                "error": None if response.status_code == 200 else response.text
            }
            
        except Exception as e:
            end_time = time.time()
            duration_ms = (end_time - start_time) * 1000
            
            return {
                "success": False,
                "status_code": 0,
                "duration_ms": duration_ms,
                "response_size": 0,
                "error": str(e)
            }

    def benchmark_user_queries(self):
        """用户查询性能测试"""
        print("\n🔍 执行用户查询性能测试...")
        
        test_cases = [
            {
                "name": "获取用户列表(分页)",
                "url": f"{self.base_url}/api/v1/users?pageIndex=0&pageSize=20",
                "method": "GET"
            },
            {
                "name": "按角色筛选用户",
                "url": f"{self.base_url}/api/v1/users?role=Doctor&pageIndex=0&pageSize=10",
                "method": "GET"
            },
            {
                "name": "搜索用户",
                "url": f"{self.base_url}/api/v1/users/search?keyword=admin",
                "method": "GET"
            },
            {
                "name": "获取用户统计",
                "url": f"{self.base_url}/api/v1/users/statistics",
                "method": "GET"
            }
        ]
        
        for test_case in test_cases:
            print(f"  ⏱️  {test_case['name']}...")
            
            # 执行多次测试取平均值
            measurements = []
            for i in range(5):
                result = self.measure_request_time(test_case["url"], test_case["method"])
                if result["success"]:
                    measurements.append(result["duration_ms"])
                time.sleep(0.1)  # 避免过于频繁的请求
            
            if measurements:
                test_result = {
                    "name": test_case["name"],
                    "url": test_case["url"],
                    "avg_duration_ms": round(statistics.mean(measurements), 2),
                    "min_duration_ms": round(min(measurements), 2),
                    "max_duration_ms": round(max(measurements), 2),
                    "median_duration_ms": round(statistics.median(measurements), 2),
                    "measurements": len(measurements),
                    "success": True
                }
                print(f"     ✅ 平均: {test_result['avg_duration_ms']}ms")
            else:
                test_result = {
                    "name": test_case["name"],
                    "url": test_case["url"],
                    "success": False,
                    "error": "所有测试都失败了"
                }
                print(f"     ❌ 测试失败")
            
            self.results["user_queries"].append(test_result)

    def benchmark_patient_queries(self):
        """患者查询性能测试"""
        print("\n🏥 执行患者查询性能测试...")
        
        test_cases = [
            {
                "name": "获取患者列表",
                "url": f"{self.base_url}/api/v1/patients?pageIndex=0&pageSize=20",
                "method": "GET"
            },
            {
                "name": "搜索患者",
                "url": f"{self.base_url}/api/v1/patients/search?keyword=张",
                "method": "GET"
            }
        ]
        
        for test_case in test_cases:
            print(f"  ⏱️  {test_case['name']}...")
            
            measurements = []
            for i in range(5):
                result = self.measure_request_time(test_case["url"], test_case["method"])
                if result["success"]:
                    measurements.append(result["duration_ms"])
                time.sleep(0.1)
            
            if measurements:
                test_result = {
                    "name": test_case["name"],
                    "url": test_case["url"],
                    "avg_duration_ms": round(statistics.mean(measurements), 2),
                    "min_duration_ms": round(min(measurements), 2),
                    "max_duration_ms": round(max(measurements), 2),
                    "median_duration_ms": round(statistics.median(measurements), 2),
                    "measurements": len(measurements),
                    "success": True
                }
                print(f"     ✅ 平均: {test_result['avg_duration_ms']}ms")
            else:
                test_result = {
                    "name": test_case["name"],
                    "url": test_case["url"],
                    "success": False,
                    "error": "所有测试都失败了"
                }
                print(f"     ❌ 测试失败")
            
            self.results["patient_queries"].append(test_result)

    def benchmark_herb_queries(self):
        """中药材查询性能测试"""
        print("\n🌿 执行中药材查询性能测试...")
        
        test_cases = [
            {
                "name": "获取中药材列表",
                "url": f"{self.base_url}/api/v1/herbs?pageIndex=0&pageSize=20",
                "method": "GET"
            },
            {
                "name": "按分类筛选中药材",
                "url": f"{self.base_url}/api/v1/herbs?category=补益类&pageIndex=0&pageSize=10",
                "method": "GET"
            }
        ]
        
        for test_case in test_cases:
            print(f"  ⏱️  {test_case['name']}...")
            
            measurements = []
            for i in range(5):
                result = self.measure_request_time(test_case["url"], test_case["method"])
                if result["success"]:
                    measurements.append(result["duration_ms"])
                time.sleep(0.1)
            
            if measurements:
                test_result = {
                    "name": test_case["name"],
                    "url": test_case["url"],
                    "avg_duration_ms": round(statistics.mean(measurements), 2),
                    "min_duration_ms": round(min(measurements), 2),
                    "max_duration_ms": round(max(measurements), 2),
                    "median_duration_ms": round(statistics.median(measurements), 2),
                    "measurements": len(measurements),
                    "success": True
                }
                print(f"     ✅ 平均: {test_result['avg_duration_ms']}ms")
            else:
                test_result = {
                    "name": test_case["name"],
                    "url": test_case["url"],
                    "success": False,
                    "error": "所有测试都失败了"
                }
                print(f"     ❌ 测试失败")
            
            self.results["herb_queries"].append(test_result)

    def benchmark_prescription_queries(self):
        """处方查询性能测试"""
        print("\n📋 执行处方查询性能测试...")
        
        test_cases = [
            {
                "name": "获取处方列表",
                "url": f"{self.base_url}/api/v1/prescriptions?pageIndex=0&pageSize=20",
                "method": "GET"
            }
        ]
        
        for test_case in test_cases:
            print(f"  ⏱️  {test_case['name']}...")
            
            measurements = []
            for i in range(3):  # 处方查询可能较慢，减少测试次数
                result = self.measure_request_time(test_case["url"], test_case["method"])
                if result["success"]:
                    measurements.append(result["duration_ms"])
                time.sleep(0.2)
            
            if measurements:
                test_result = {
                    "name": test_case["name"],
                    "url": test_case["url"],
                    "avg_duration_ms": round(statistics.mean(measurements), 2),
                    "min_duration_ms": round(min(measurements), 2),
                    "max_duration_ms": round(max(measurements), 2),
                    "median_duration_ms": round(statistics.median(measurements), 2),
                    "measurements": len(measurements),
                    "success": True
                }
                print(f"     ✅ 平均: {test_result['avg_duration_ms']}ms")
            else:
                test_result = {
                    "name": test_case["name"],
                    "url": test_case["url"],
                    "success": False,
                    "error": "所有测试都失败了"
                }
                print(f"     ❌ 测试失败")
            
            self.results["prescription_queries"].append(test_result)

    def calculate_summary(self):
        """计算性能测试摘要"""
        print("\n📊 计算性能测试摘要...")
        
        all_successful_tests = []
        total_tests = 0
        successful_tests = 0
        
        # 收集所有成功的测试结果
        for category in ["user_queries", "patient_queries", "herb_queries", "prescription_queries"]:
            for test in self.results[category]:
                total_tests += 1
                if test.get("success", False):
                    successful_tests += 1
                    all_successful_tests.append(test["avg_duration_ms"])
        
        if all_successful_tests:
            self.results["summary"] = {
                "total_tests": total_tests,
                "successful_tests": successful_tests,
                "failed_tests": total_tests - successful_tests,
                "success_rate": round((successful_tests / total_tests) * 100, 2),
                "overall_avg_duration_ms": round(statistics.mean(all_successful_tests), 2),
                "overall_median_duration_ms": round(statistics.median(all_successful_tests), 2),
                "fastest_query_ms": round(min(all_successful_tests), 2),
                "slowest_query_ms": round(max(all_successful_tests), 2),
                "queries_under_100ms": len([t for t in all_successful_tests if t < 100]),
                "queries_under_500ms": len([t for t in all_successful_tests if t < 500]),
                "queries_over_1000ms": len([t for t in all_successful_tests if t >= 1000])
            }
        else:
            self.results["summary"] = {
                "total_tests": total_tests,
                "successful_tests": 0,
                "failed_tests": total_tests,
                "success_rate": 0,
                "error": "没有成功的测试"
            }

    def print_results(self):
        """打印测试结果"""
        print("\n" + "="*60)
        print("           📈 性能基准测试结果报告")
        print("="*60)
        
        summary = self.results["summary"]
        print(f"\n📊 总体统计:")
        print(f"   总测试数: {summary.get('total_tests', 0)}")
        print(f"   成功测试: {summary.get('successful_tests', 0)}")
        print(f"   失败测试: {summary.get('failed_tests', 0)}")
        print(f"   成功率: {summary.get('success_rate', 0)}%")
        
        if summary.get('overall_avg_duration_ms'):
            print(f"\n⏱️  性能指标:")
            print(f"   平均响应时间: {summary['overall_avg_duration_ms']}ms")
            print(f"   中位数响应时间: {summary['overall_median_duration_ms']}ms")
            print(f"   最快查询: {summary['fastest_query_ms']}ms")
            print(f"   最慢查询: {summary['slowest_query_ms']}ms")
            
            print(f"\n🚀 性能分布:")
            print(f"   <100ms的查询: {summary['queries_under_100ms']}个")
            print(f"   <500ms的查询: {summary['queries_under_500ms']}个")
            print(f"   ≥1000ms的查询: {summary['queries_over_1000ms']}个")
            
            # 性能评级
            avg_time = summary['overall_avg_duration_ms']
            if avg_time < 100:
                grade = "🌟 优秀"
            elif avg_time < 300:
                grade = "✅ 良好"
            elif avg_time < 1000:
                grade = "⚠️  一般"
            else:
                grade = "❌ 需要优化"
            
            print(f"\n🏆 总体性能评级: {grade}")

    def save_results(self):
        """保存测试结果到文件"""
        self.results["test_end_time"] = datetime.now().isoformat()
        
        # 保存JSON格式结果
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        json_filename = f"docs/reports/performance/benchmark_results_{timestamp}.json"
        
        try:
            import os
            os.makedirs("docs/reports/performance", exist_ok=True)
            
            with open(json_filename, 'w', encoding='utf-8') as f:
                json.dump(self.results, f, ensure_ascii=False, indent=2)
            
            print(f"\n💾 测试结果已保存: {json_filename}")
            
        except Exception as e:
            print(f"\n❌ 保存结果时发生错误: {str(e)}")

    def run_full_benchmark(self):
        """运行完整的性能基准测试"""
        print("🚀 开始LYBT数据库性能基准测试")
        print(f"📅 测试时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print(f"🌐 测试目标: {self.base_url}")
        
        # 认证
        if not self.authenticate():
            print("❌ 认证失败，无法继续测试")
            return False
        
        # 执行各类查询测试
        try:
            self.benchmark_user_queries()
            self.benchmark_patient_queries()  
            self.benchmark_herb_queries()
            self.benchmark_prescription_queries()
            
            # 计算摘要
            self.calculate_summary()
            
            # 显示结果
            self.print_results()
            
            # 保存结果
            self.save_results()
            
            print("\n✅ 性能基准测试完成!")
            return True
            
        except KeyboardInterrupt:
            print("\n⏹️  测试被用户中断")
            return False
        except Exception as e:
            print(f"\n❌ 测试过程中发生错误: {str(e)}")
            return False

def main():
    """主函数"""
    print("LYBT 数据库性能基准测试工具")
    print("UltraThink重构 - 查询性能验证")
    print("-" * 50)
    
    # 创建基准测试实例
    benchmark = PerformanceBenchmark()
    
    # 运行测试
    success = benchmark.run_full_benchmark()
    
    if success:
        print("\n🎉 测试成功完成！")
        print("📋 请查看生成的报告文件了解详细结果")
    else:
        print("\n💔 测试未能完全完成")
        print("🔍 请检查API服务状态和网络连接")
    
    return 0 if success else 1

if __name__ == "__main__":
    exit(main())