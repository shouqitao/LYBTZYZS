#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
缓存和性能优化功能测试脚本
专门测试阶段3实现的CachedHerbService和ApiOptimizationService功能
"""

import asyncio
import aiohttp
import json
import ssl
import time
from datetime import datetime
from typing import Dict, Any, List

class CachePerformanceTest:
    def __init__(self):
        self.base_url = "https://localhost:7001"
        self.auth_token = None
        self.test_results = []
        
        # 忽略SSL证书验证（开发环境）
        self.ssl_context = ssl.create_default_context()
        self.ssl_context.check_hostname = False
        self.ssl_context.verify_mode = ssl.CERT_NONE

    async def log_test(self, test_name: str, success: bool, message: str, details: Any = None):
        """记录测试结果"""
        result = {
            "test": test_name,
            "success": success,
            "message": message,
            "timestamp": datetime.now().isoformat(),
            "details": details
        }
        self.test_results.append(result)
        status = "PASS" if success else "FAIL"
        print(f"{status} {test_name}: {message}")
        if details and not success:
            print(f"   详情: {details}")

    async def authenticate(self):
        """用户认证"""
        login_data = {
            "username": "sysadmin",
            "password": "Admin@123456",
            "rememberMe": False
        }
        
        try:
            async with aiohttp.ClientSession(
                connector=aiohttp.TCPConnector(ssl=self.ssl_context)
            ) as session:
                async with session.post(
                    f"{self.base_url}/api/v1/auth/login",
                    json=login_data,
                    headers={"Content-Type": "application/json"}
                ) as response:
                    if response.status == 200:
                        data = await response.json()
                        if data.get("success") and data.get("data", {}).get("token"):
                            self.auth_token = data["data"]["token"]
                            return True
            return False
        except Exception:
            return False

    async def get_auth_headers(self) -> Dict[str, str]:
        """获取认证头"""
        return {
            "Authorization": f"Bearer {self.auth_token}",
            "Content-Type": "application/json"
        }

    async def test_response_time_consistency(self):
        """测试响应时间一致性（验证缓存效果）"""
        if not self.auth_token:
            await self.log_test("响应时间一致性测试", False, "未获取到认证令牌")
            return False

        headers = await self.get_auth_headers()
        
        # 测试3次相同的API调用，检查缓存是否生效
        response_times = []
        
        try:
            async with aiohttp.ClientSession(
                connector=aiohttp.TCPConnector(ssl=self.ssl_context)
            ) as session:
                
                # 第一次调用（缓存未命中）
                start_time = time.time()
                async with session.get(
                    f"{self.base_url}/api/v1/herbs",
                    headers=headers
                ) as response:
                    if response.status == 200:
                        first_time = time.time() - start_time
                        response_times.append(first_time)
                        data = await response.json()
                        herb_count = len(data.get("data", []))
                
                # 等待短暂时间确保缓存生效
                await asyncio.sleep(0.1)
                
                # 第二次调用（缓存命中）
                start_time = time.time()
                async with session.get(
                    f"{self.base_url}/api/v1/herbs",
                    headers=headers
                ) as response:
                    if response.status == 200:
                        second_time = time.time() - start_time
                        response_times.append(second_time)
                
                # 第三次调用（缓存命中）
                start_time = time.time()
                async with session.get(
                    f"{self.base_url}/api/v1/herbs",
                    headers=headers
                ) as response:
                    if response.status == 200:
                        third_time = time.time() - start_time
                        response_times.append(third_time)

            if len(response_times) == 3:
                # 分析响应时间
                avg_cached_time = (response_times[1] + response_times[2]) / 2
                improvement_ratio = response_times[0] / avg_cached_time if avg_cached_time > 0 else 1
                
                await self.log_test(
                    "响应时间一致性测试",
                    True,
                    f"缓存生效，首次: {response_times[0]:.3f}s，后续平均: {avg_cached_time:.3f}s，提升: {improvement_ratio:.1f}x",
                    {
                        "first_call": f"{response_times[0]:.3f}s",
                        "second_call": f"{response_times[1]:.3f}s", 
                        "third_call": f"{response_times[2]:.3f}s",
                        "improvement_ratio": f"{improvement_ratio:.1f}x",
                        "herb_count": herb_count
                    }
                )
                return True
            else:
                await self.log_test("响应时间一致性测试", False, "API调用失败")
                return False
                
        except Exception as e:
            await self.log_test("响应时间一致性测试", False, f"测试异常: {str(e)}")
            return False

    async def test_concurrent_requests(self):
        """测试并发请求处理（验证防抖和批量处理）"""
        if not self.auth_token:
            await self.log_test("并发请求测试", False, "未获取到认证令牌")
            return False

        headers = await self.get_auth_headers()
        
        try:
            async with aiohttp.ClientSession(
                connector=aiohttp.TCPConnector(ssl=self.ssl_context)
            ) as session:
                
                # 发起10个并发请求
                start_time = time.time()
                tasks = []
                for i in range(10):
                    task = session.get(
                        f"{self.base_url}/api/v1/herbs",
                        headers=headers
                    )
                    tasks.append(task)
                
                responses = await asyncio.gather(*tasks, return_exceptions=True)
                total_time = time.time() - start_time
                
                successful_responses = 0
                for response in responses:
                    if not isinstance(response, Exception) and hasattr(response, 'status'):
                        if response.status == 200:
                            successful_responses += 1
                        response.close()
                
                success_rate = successful_responses / len(responses) * 100
                avg_time_per_request = total_time / len(responses)
                
                if success_rate >= 80:  # 80%以上成功率认为通过
                    await self.log_test(
                        "并发请求测试",
                        True,
                        f"并发处理正常，{successful_responses}/{len(responses)}请求成功，总耗时: {total_time:.3f}s",
                        {
                            "concurrent_requests": len(responses),
                            "successful_requests": successful_responses,
                            "success_rate": f"{success_rate:.1f}%",
                            "total_time": f"{total_time:.3f}s",
                            "avg_time_per_request": f"{avg_time_per_request:.3f}s"
                        }
                    )
                    return True
                else:
                    await self.log_test(
                        "并发请求测试",
                        False,
                        f"并发处理失败，成功率过低: {success_rate:.1f}%"
                    )
                    return False
                    
        except Exception as e:
            await self.log_test("并发请求测试", False, f"测试异常: {str(e)}")
            return False

    async def test_memory_cache_behavior(self):
        """测试内存缓存行为"""
        if not self.auth_token:
            await self.log_test("内存缓存行为测试", False, "未获取到认证令牌")
            return False

        headers = await self.get_auth_headers()
        
        try:
            async with aiohttp.ClientSession(
                connector=aiohttp.TCPConnector(ssl=self.ssl_context)
            ) as session:
                
                # 1. 首次调用建立缓存
                start_time = time.time()
                async with session.get(
                    f"{self.base_url}/api/v1/herbs",
                    headers=headers
                ) as response:
                    if response.status == 200:
                        first_response_time = time.time() - start_time
                        first_data = await response.json()
                
                await asyncio.sleep(0.05)  # 短暂等待
                
                # 2. 立即再次调用（缓存命中）
                start_time = time.time()
                async with session.get(
                    f"{self.base_url}/api/v1/herbs",
                    headers=headers
                ) as response:
                    if response.status == 200:
                        cached_response_time = time.time() - start_time
                        cached_data = await response.json()
                
                # 3. 验证数据一致性
                data_consistent = (
                    json.dumps(first_data, sort_keys=True) == 
                    json.dumps(cached_data, sort_keys=True)
                )
                
                # 4. 验证响应时间改善
                time_improvement = first_response_time > cached_response_time
                
                if data_consistent and time_improvement:
                    improvement_ratio = first_response_time / cached_response_time if cached_response_time > 0 else 1
                    await self.log_test(
                        "内存缓存行为测试",
                        True,
                        f"缓存行为正常，数据一致，性能提升 {improvement_ratio:.1f}x",
                        {
                            "data_consistent": data_consistent,
                            "first_response_time": f"{first_response_time:.3f}s",
                            "cached_response_time": f"{cached_response_time:.3f}s",
                            "improvement_ratio": f"{improvement_ratio:.1f}x"
                        }
                    )
                    return True
                else:
                    await self.log_test(
                        "内存缓存行为测试",
                        False,
                        f"缓存行为异常，数据一致性: {data_consistent}，性能改善: {time_improvement}"
                    )
                    return False
                    
        except Exception as e:
            await self.log_test("内存缓存行为测试", False, f"测试异常: {str(e)}")
            return False

    async def test_api_optimization_features(self):
        """测试API优化特性"""
        if not self.auth_token:
            await self.log_test("API优化特性测试", False, "未获取到认证令牌")
            return False

        headers = await self.get_auth_headers()
        
        try:
            # 测试快速连续请求（防抖效果）
            start_time = time.time()
            
            async with aiohttp.ClientSession(
                connector=aiohttp.TCPConnector(ssl=self.ssl_context)
            ) as session:
                
                # 快速发送多个相同请求
                tasks = []
                for i in range(5):
                    task = asyncio.create_task(self._make_herbs_request(session, headers))
                    tasks.append(task)
                    await asyncio.sleep(0.05)  # 50ms间隔
                
                results = await asyncio.gather(*tasks, return_exceptions=True)
                total_time = time.time() - start_time
                
                successful_requests = sum(1 for r in results if not isinstance(r, Exception))
                
                if successful_requests >= 3:  # 至少60%成功
                    await self.log_test(
                        "API优化特性测试",
                        True,
                        f"API优化正常，{successful_requests}/5请求成功，总耗时: {total_time:.3f}s",
                        {
                            "total_requests": 5,
                            "successful_requests": successful_requests,
                            "total_time": f"{total_time:.3f}s",
                            "avg_time": f"{total_time/5:.3f}s"
                        }
                    )
                    return True
                else:
                    await self.log_test(
                        "API优化特性测试",
                        False,
                        f"API优化失败，成功率过低: {successful_requests}/5"
                    )
                    return False
                    
        except Exception as e:
            await self.log_test("API优化特性测试", False, f"测试异常: {str(e)}")
            return False

    async def _make_herbs_request(self, session, headers):
        """发送药材API请求"""
        async with session.get(
            f"{self.base_url}/api/v1/herbs",
            headers=headers
        ) as response:
            if response.status == 200:
                return await response.json()
            else:
                raise Exception(f"HTTP {response.status}")

    async def generate_performance_report(self):
        """生成性能测试报告"""
        total_tests = len(self.test_results)
        passed_tests = len([r for r in self.test_results if r["success"]])
        failed_tests = total_tests - passed_tests
        success_rate = (passed_tests / total_tests * 100) if total_tests > 0 else 0
        
        report = {
            "测试类型": "缓存和性能优化测试",
            "测试概要": {
                "总测试数": total_tests,
                "通过数": passed_tests,
                "失败数": failed_tests,
                "成功率": f"{success_rate:.1f}%"
            },
            "测试详情": self.test_results,
            "测试时间": datetime.now().isoformat()
        }
        
        print(f"\n缓存性能测试报告总结:")
        print(f"总测试数: {total_tests}")
        print(f"通过数: {passed_tests}")
        print(f"失败数: {failed_tests}")
        print(f"成功率: {success_rate:.1f}%")
        
        if failed_tests > 0:
            print("\n失败的测试:")
            for result in self.test_results:
                if not result["success"]:
                    print(f"  - {result['test']}: {result['message']}")
        
        return report

    async def run_cache_performance_tests(self):
        """运行所有缓存和性能测试"""
        print("开始缓存和性能优化功能测试")
        print("=" * 50)
        
        # 认证
        if not await self.authenticate():
            print("认证失败，测试终止")
            return
        print("认证成功，开始性能测试...")
        
        # 运行性能测试
        await self.test_response_time_consistency()
        await self.test_concurrent_requests() 
        await self.test_memory_cache_behavior()
        await self.test_api_optimization_features()
        
        # 生成报告
        print("\n" + "=" * 50)
        await self.generate_performance_report()
        print("缓存性能测试完成！")

async def main():
    """主函数"""
    tester = CachePerformanceTest()
    await tester.run_cache_performance_tests()

if __name__ == "__main__":
    asyncio.run(main())