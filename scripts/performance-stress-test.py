#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
性能压力测试脚本 - UltraThink重构性能监控架构
自动化执行API性能压力测试，收集性能数据并生成报告
"""

import asyncio
import aiohttp
import time
import json
import argparse
import statistics
import sys
from datetime import datetime
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass, asdict
from typing import List, Dict, Any, Optional
import logging

# 配置日志
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler(f'performance-test-{datetime.now().strftime("%Y%m%d-%H%M%S")}.log'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)

@dataclass
class TestConfig:
    """测试配置"""
    base_url: str = "https://localhost:7001"
    concurrent_users: int = 10
    test_duration: int = 60  # 秒
    ramp_up_time: int = 10   # 秒
    admin_username: str = "sysadmin"
    admin_password: str = "Admin@123456"
    test_scenarios: List[str] = None

@dataclass
class RequestResult:
    """单个请求结果"""
    url: str
    method: str
    status_code: int
    response_time: float
    success: bool
    error: Optional[str] = None
    response_size: int = 0
    timestamp: float = 0

@dataclass
class TestResults:
    """测试结果汇总"""
    start_time: datetime
    end_time: datetime
    total_requests: int
    successful_requests: int
    failed_requests: int
    average_response_time: float
    min_response_time: float
    max_response_time: float
    p50_response_time: float
    p90_response_time: float
    p95_response_time: float
    p99_response_time: float
    requests_per_second: float
    success_rate: float
    errors: Dict[str, int]
    performance_grade: str

class PerformanceStressTester:
    """性能压力测试器"""
    
    def __init__(self, config: TestConfig):
        self.config = config
        self.results: List[RequestResult] = []
        self.session = None
        self.auth_token = None
        self.start_time = None
        self.end_time = None
        
        # 测试场景定义
        self.test_scenarios = {
            'login': {
                'method': 'POST',
                'url': '/api/v1/auth/login',
                'payload': {
                    'username': config.admin_username,
                    'password': config.admin_password,
                    'rememberMe': False
                },
                'weight': 5
            },
            'get_users': {
                'method': 'GET',
                'url': '/api/v1/users',
                'weight': 20
            },
            'get_patients': {
                'method': 'GET',
                'url': '/api/v1/patients',
                'weight': 25
            },
            'get_herbs': {
                'method': 'GET',
                'url': '/api/v1/herbs',
                'weight': 20
            },
            'get_prescriptions': {
                'method': 'GET',
                'url': '/api/v1/prescriptions',
                'weight': 15
            },
            'health_check': {
                'method': 'GET',
                'url': '/api/v1/performance/health-check',
                'weight': 10
            },
            'system_info': {
                'method': 'GET',
                'url': '/api/v1/performance/system/snapshot',
                'weight': 5
            }
        }
        
        # 如果指定了测试场景，只测试指定的场景
        if config.test_scenarios:
            self.test_scenarios = {k: v for k, v in self.test_scenarios.items() 
                                 if k in config.test_scenarios}

    async def authenticate(self) -> bool:
        """身份验证"""
        try:
            auth_data = {
                'username': self.config.admin_username,
                'password': self.config.admin_password,
                'rememberMe': False
            }
            
            async with self.session.post(
                f"{self.config.base_url}/api/v1/auth/login",
                json=auth_data,
                ssl=False
            ) as response:
                if response.status == 200:
                    result = await response.json()
                    if result.get('success') and result.get('data', {}).get('token'):
                        self.auth_token = result['data']['token']
                        # 设置默认认证头
                        self.session.headers.update({
                            'Authorization': f'Bearer {self.auth_token}'
                        })
                        logger.info("Authentication successful")
                        return True
                    else:
                        logger.error(f"Login failed: {result}")
                        return False
                else:
                    logger.error(f"Authentication failed with status: {response.status}")
                    return False
        except Exception as e:
            logger.error(f"Authentication error: {e}")
            return False

    async def execute_request(self, scenario_name: str, scenario: Dict[str, Any]) -> RequestResult:
        """执行单个请求"""
        url = f"{self.config.base_url}{scenario['url']}"
        method = scenario['method']
        payload = scenario.get('payload')
        
        start_time = time.time()
        
        try:
            async with self.session.request(
                method=method,
                url=url,
                json=payload if payload else None,
                ssl=False,
                timeout=aiohttp.ClientTimeout(total=30)
            ) as response:
                response_text = await response.text()
                end_time = time.time()
                response_time = (end_time - start_time) * 1000  # 转换为毫秒
                
                return RequestResult(
                    url=scenario['url'],
                    method=method,
                    status_code=response.status,
                    response_time=response_time,
                    success=200 <= response.status < 400,
                    response_size=len(response_text),
                    timestamp=start_time
                )
                
        except asyncio.TimeoutError:
            return RequestResult(
                url=scenario['url'],
                method=method,
                status_code=0,
                response_time=(time.time() - start_time) * 1000,
                success=False,
                error="Timeout",
                timestamp=start_time
            )
        except Exception as e:
            return RequestResult(
                url=scenario['url'],
                method=method,
                status_code=0,
                response_time=(time.time() - start_time) * 1000,
                success=False,
                error=str(e),
                timestamp=start_time
            )

    async def user_session(self, user_id: int) -> List[RequestResult]:
        """模拟单个用户的测试会话"""
        session_results = []
        session_start = time.time()
        
        # 为每个用户创建独立的会话
        connector = aiohttp.TCPConnector(ssl=False, limit=10)
        async with aiohttp.ClientSession(
            connector=connector,
            timeout=aiohttp.ClientTimeout(total=30),
            headers={'Content-Type': 'application/json'}
        ) as user_session:
            
            # 每个用户都需要独立认证
            auth_data = {
                'username': self.config.admin_username,
                'password': self.config.admin_password,
                'rememberMe': False
            }
            
            try:
                async with user_session.post(
                    f"{self.config.base_url}/api/v1/auth/login",
                    json=auth_data,
                    ssl=False
                ) as response:
                    if response.status == 200:
                        result = await response.json()
                        if result.get('success') and result.get('data', {}).get('token'):
                            token = result['data']['token']
                            user_session.headers.update({'Authorization': f'Bearer {token}'})
                        else:
                            logger.warning(f"User {user_id} authentication failed")
                            return session_results
                    else:
                        logger.warning(f"User {user_id} authentication failed with status: {response.status}")
                        return session_results
            except Exception as e:
                logger.error(f"User {user_id} authentication error: {e}")
                return session_results
            
            # 执行测试请求
            while time.time() - session_start < self.config.test_duration:
                # 根据权重随机选择测试场景
                import random
                scenarios_list = [(name, scenario) for name, scenario in self.test_scenarios.items()]
                weights = [scenario['weight'] for _, scenario in scenarios_list]
                selected_name, selected_scenario = random.choices(scenarios_list, weights=weights)[0]
                
                # 跳过登录场景（已经认证过了）
                if selected_name == 'login':
                    continue
                
                result = await self.execute_request_with_session(user_session, selected_name, selected_scenario)
                session_results.append(result)
                
                # 随机延迟，模拟真实用户行为
                await asyncio.sleep(random.uniform(0.1, 2.0))
        
        logger.info(f"User {user_id} completed {len(session_results)} requests")
        return session_results

    async def execute_request_with_session(self, session: aiohttp.ClientSession, scenario_name: str, scenario: Dict[str, Any]) -> RequestResult:
        """使用指定会话执行请求"""
        url = f"{self.config.base_url}{scenario['url']}"
        method = scenario['method']
        payload = scenario.get('payload')
        
        start_time = time.time()
        
        try:
            async with session.request(
                method=method,
                url=url,
                json=payload if payload else None,
                ssl=False
            ) as response:
                response_text = await response.text()
                end_time = time.time()
                response_time = (end_time - start_time) * 1000
                
                return RequestResult(
                    url=scenario['url'],
                    method=method,
                    status_code=response.status,
                    response_time=response_time,
                    success=200 <= response.status < 400,
                    response_size=len(response_text),
                    timestamp=start_time
                )
                
        except Exception as e:
            return RequestResult(
                url=scenario['url'],
                method=method,
                status_code=0,
                response_time=(time.time() - start_time) * 1000,
                success=False,
                error=str(e),
                timestamp=start_time
            )

    async def run_stress_test(self) -> TestResults:
        """运行压力测试"""
        logger.info(f"Starting stress test with {self.config.concurrent_users} users for {self.config.test_duration} seconds")
        logger.info(f"Test scenarios: {list(self.test_scenarios.keys())}")
        
        self.start_time = datetime.now()
        
        # 创建并发用户任务
        tasks = []
        for user_id in range(self.config.concurrent_users):
            task = asyncio.create_task(self.user_session(user_id))
            tasks.append(task)
            
            # 渐进式启动用户（Ramp-up）
            if user_id < self.config.concurrent_users - 1:
                await asyncio.sleep(self.config.ramp_up_time / self.config.concurrent_users)
        
        # 等待所有用户完成
        logger.info("Waiting for all users to complete...")
        user_results = await asyncio.gather(*tasks)
        
        # 收集所有结果
        for user_result in user_results:
            self.results.extend(user_result)
        
        self.end_time = datetime.now()
        
        # 分析结果
        return self.analyze_results()

    def analyze_results(self) -> TestResults:
        """分析测试结果"""
        if not self.results:
            logger.error("No results to analyze")
            return None
        
        # 基本统计
        successful_results = [r for r in self.results if r.success]
        failed_results = [r for r in self.results if not r.success]
        response_times = [r.response_time for r in self.results]
        
        # 错误统计
        errors = {}
        for result in failed_results:
            error_key = f"{result.status_code}:{result.error}" if result.error else str(result.status_code)
            errors[error_key] = errors.get(error_key, 0) + 1
        
        # 百分位数计算
        response_times.sort()
        
        def percentile(data, percent):
            if not data:
                return 0
            index = int((percent / 100) * len(data))
            if index >= len(data):
                index = len(data) - 1
            return data[index]
        
        # 计算性能等级
        avg_response_time = statistics.mean(response_times)
        success_rate = len(successful_results) / len(self.results) * 100
        performance_grade = self.calculate_performance_grade(avg_response_time, success_rate)
        
        # 计算测试持续时间
        duration = (self.end_time - self.start_time).total_seconds()
        
        return TestResults(
            start_time=self.start_time,
            end_time=self.end_time,
            total_requests=len(self.results),
            successful_requests=len(successful_results),
            failed_requests=len(failed_results),
            average_response_time=avg_response_time,
            min_response_time=min(response_times),
            max_response_time=max(response_times),
            p50_response_time=percentile(response_times, 50),
            p90_response_time=percentile(response_times, 90),
            p95_response_time=percentile(response_times, 95),
            p99_response_time=percentile(response_times, 99),
            requests_per_second=len(self.results) / duration if duration > 0 else 0,
            success_rate=success_rate,
            errors=errors,
            performance_grade=performance_grade
        )

    def calculate_performance_grade(self, avg_response_time: float, success_rate: float) -> str:
        """计算性能等级"""
        if success_rate < 90:
            return "F"
        elif success_rate < 95:
            return "D"
        elif avg_response_time > 2000:
            return "D"
        elif avg_response_time > 1000:
            return "C"
        elif avg_response_time > 500:
            return "B"
        else:
            return "A"

    def generate_report(self, results: TestResults) -> str:
        """生成测试报告"""
        report = f"""
=== 性能压力测试报告 ===
测试时间: {results.start_time.strftime('%Y-%m-%d %H:%M:%S')} - {results.end_time.strftime('%H:%M:%S')}
测试配置: {self.config.concurrent_users}个并发用户，持续{self.config.test_duration}秒

📊 请求统计:
- 总请求数: {results.total_requests}
- 成功请求: {results.successful_requests}
- 失败请求: {results.failed_requests}
- 成功率: {results.success_rate:.2f}%
- 请求/秒: {results.requests_per_second:.2f}

⏱️ 响应时间 (ms):
- 平均: {results.average_response_time:.2f}
- 最小: {results.min_response_time:.2f}
- 最大: {results.max_response_time:.2f}
- P50: {results.p50_response_time:.2f}
- P90: {results.p90_response_time:.2f}
- P95: {results.p95_response_time:.2f}
- P99: {results.p99_response_time:.2f}

🎯 性能等级: {results.performance_grade}

❌ 错误统计:
"""
        
        if results.errors:
            for error, count in results.errors.items():
                report += f"- {error}: {count}次\n"
        else:
            report += "- 无错误\n"
        
        # 性能建议
        report += "\n💡 性能建议:\n"
        if results.performance_grade in ["D", "F"]:
            report += "- ⚠️  系统性能严重不足，需要立即优化\n"
            report += "- 检查数据库查询性能\n"
            report += "- 考虑增加服务器资源\n"
        elif results.performance_grade == "C":
            report += "- ⚠️  性能有待改善\n"
            report += "- 检查慢查询和高CPU使用率操作\n"
        elif results.performance_grade == "B":
            report += "- ✅ 性能良好，可进行小幅优化\n"
        else:
            report += "- ✅ 性能优秀！\n"
        
        return report

async def main():
    """主函数"""
    parser = argparse.ArgumentParser(description="LYBT系统性能压力测试")
    parser.add_argument("--url", default="https://localhost:7001", help="API基础URL")
    parser.add_argument("--users", type=int, default=10, help="并发用户数")
    parser.add_argument("--duration", type=int, default=60, help="测试持续时间（秒）")
    parser.add_argument("--rampup", type=int, default=10, help="用户启动时间（秒）")
    parser.add_argument("--username", default="sysadmin", help="管理员用户名")
    parser.add_argument("--password", default="Admin@123456", help="管理员密码")
    parser.add_argument("--scenarios", nargs="+", help="指定测试场景")
    parser.add_argument("--output", help="报告输出文件")
    
    args = parser.parse_args()
    
    config = TestConfig(
        base_url=args.url,
        concurrent_users=args.users,
        test_duration=args.duration,
        ramp_up_time=args.rampup,
        admin_username=args.username,
        admin_password=args.password,
        test_scenarios=args.scenarios
    )
    
    tester = PerformanceStressTester(config)
    
    try:
        logger.info("Starting performance stress test...")
        results = await tester.run_stress_test()
        
        if results:
            report = tester.generate_report(results)
            print(report)
            
            # 保存报告到文件
            if args.output:
                with open(args.output, 'w', encoding='utf-8') as f:
                    f.write(report)
                    # 同时保存JSON格式的详细数据
                    json_output = args.output.replace('.txt', '.json')
                    with open(json_output, 'w', encoding='utf-8') as jf:
                        json.dump(asdict(results), jf, ensure_ascii=False, indent=2, default=str)
                logger.info(f"Report saved to {args.output} and {json_output}")
            
            # 根据性能等级设置退出码
            exit_code = 0 if results.performance_grade in ["A", "B"] else 1
            sys.exit(exit_code)
        else:
            logger.error("Failed to get test results")
            sys.exit(1)
            
    except KeyboardInterrupt:
        logger.info("Test interrupted by user")
        sys.exit(1)
    except Exception as e:
        logger.error(f"Test failed: {e}")
        sys.exit(1)

if __name__ == "__main__":
    asyncio.run(main())