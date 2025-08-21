const https = require('https');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

// 测试结果收集器
const testResults = [];
let token = null;

console.log('🧪 UltraThink v2.0 模块API验证测试');
console.log('===============================================');

// 获取认证Token
async function getAuthToken() {
  return new Promise((resolve, reject) => {
    const data = JSON.stringify({
      username: 'sysadmin',
      password: 'Admin@123456',
      rememberMe: false
    });

    const options = {
      hostname: 'localhost',
      port: 7001,
      path: '/api/v1/auth/login',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(data)
      }
    };

    const req = https.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => body += chunk);
      res.on('end', () => {
        try {
          const result = JSON.parse(body);
          if (result.success && result.data && result.data.token) {
            resolve(result.data.token);
          } else {
            reject(new Error('登录失败: ' + body));
          }
        } catch (e) {
          reject(new Error('解析登录响应失败: ' + e.message));
        }
      });
    });

    req.on('error', reject);
    req.write(data);
    req.end();
  });
}

// 通用API请求函数
async function apiRequest(method, path, token, data = null) {
  return new Promise((resolve) => {
    const postData = data ? JSON.stringify(data) : null;
    const options = {
      hostname: 'localhost',
      port: 7001,
      path: path,
      method: method,
      headers: {
        'Authorization': 'Bearer ' + token,
        ...(postData && { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(postData) })
      }
    };

    const req = https.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => body += chunk);
      res.on('end', () => {
        resolve({
          statusCode: res.statusCode,
          success: res.statusCode >= 200 && res.statusCode < 300,
          body: body,
          headers: res.headers
        });
      });
    });

    req.on('error', (err) => resolve({
      success: false,
      error: err.message,
      statusCode: 0
    }));

    if (postData) req.write(postData);
    req.end();
  });
}

// 获取端点测试数据
function getEndpointTestData(path, method) {
  const testDataMap = {
    'POST /api/v1/auth/login': {
      username: 'sysadmin',
      password: 'Admin@123456',
      rememberMe: false
    },
    'POST /api/v1/auth/refresh': 'dummy_refresh_token',
    'POST /api/v1/auth/validate': 'dummy_token'
  };
  
  return testDataMap[`${method} ${path}`] || null;
}

// 测试模块API
async function testModuleAPI(moduleName, endpoints) {
  console.log(`\n📋 测试模块: ${moduleName}`);
  console.log('-'.repeat(50));

  const moduleResults = [];
  
  for (const endpoint of endpoints) {
    try {
      const testData = getEndpointTestData(endpoint.path, endpoint.method);
      const result = await apiRequest(endpoint.method, endpoint.path, token, testData);
      const status = result.success ? '✅ 成功' : `❌ 失败 (${result.statusCode})`;
      
      console.log(`  ${endpoint.method.padEnd(6)} ${endpoint.path.padEnd(30)} - ${status}`);
      
      moduleResults.push({
        method: endpoint.method,
        path: endpoint.path,
        success: result.success,
        statusCode: result.statusCode,
        error: result.error || null
      });
    } catch (error) {
      console.log(`  ${endpoint.method.padEnd(6)} ${endpoint.path.padEnd(30)} - ❌ 异常: ${error.message}`);
      moduleResults.push({
        method: endpoint.method,
        path: endpoint.path,
        success: false,
        error: error.message
      });
    }
  }

  return moduleResults;
}

// 主测试函数
async function runAllTests() {
  try {
    // 1. 获取认证Token
    console.log('🔑 获取认证Token...');
    token = await getAuthToken();
    console.log('✅ Token获取成功');
    console.log('🔑 Token预览:', token.substring(0, 50) + '...');

    // 2. 定义所有需要测试的模块和端点
    const moduleTests = [
      {
        name: 'Auth (认证)',
        endpoints: [
          { method: 'POST', path: '/api/v1/auth/login' },
          { method: 'POST', path: '/api/v1/auth/refresh' },
          { method: 'POST', path: '/api/v1/auth/validate' }
        ]
      },
      {
        name: 'Users (用户管理)',
        endpoints: [
          { method: 'GET', path: '/api/v1/users' },
          { method: 'GET', path: '/api/v1/users/profile' }
        ]
      },
      {
        name: 'Patients (患者档案)',
        endpoints: [
          { method: 'GET', path: '/api/v1/patients' },
          { method: 'GET', path: '/api/v1/patients/search' }
        ]
      },
      {
        name: 'Consultation (看诊管理)',
        endpoints: [
          { method: 'GET', path: '/api/v1/consultation' },
          { method: 'GET', path: '/api/v1/consultation/current' }
        ]
      },
      {
        name: 'MedicalCase (医疗案例)',
        endpoints: [
          { method: 'GET', path: '/api/v1/medicalcase' },
          { method: 'GET', path: '/api/v1/medicalcase/recent' }
        ]
      },
      {
        name: 'Prescriptions (处方管理)',
        endpoints: [
          { method: 'GET', path: '/api/v1/prescriptions' },
          { method: 'GET', path: '/api/v1/prescriptions/templates' }
        ]
      },
      {
        name: 'Herbs (中药材管理)',
        endpoints: [
          { method: 'GET', path: '/api/v1/herbs' },
          { method: 'GET', path: '/api/v1/herbs/categories' },
          { method: 'GET', path: '/api/v1/herbs/export-template' }
        ]
      },
      {
        name: 'Formula (验方管理)',
        endpoints: [
          { method: 'GET', path: '/api/v1/formula' },
          { method: 'GET', path: '/api/v1/formula/templates' }
        ]
      }
    ];

    // 3. 运行所有模块测试
    for (const moduleTest of moduleTests) {
      const results = await testModuleAPI(moduleTest.name, moduleTest.endpoints);
      testResults.push({
        module: moduleTest.name,
        results: results
      });
    }

    // 4. 生成测试报告
    generateTestReport();

  } catch (error) {
    console.error('❌ 测试过程中发生错误:', error.message);
  }
}

// 生成测试报告
function generateTestReport() {
  console.log('\n📊 测试报告');
  console.log('===============================================');

  let totalTests = 0;
  let passedTests = 0;
  let failedModules = [];

  testResults.forEach(moduleResult => {
    const moduleTotal = moduleResult.results.length;
    const modulePassed = moduleResult.results.filter(r => r.success).length;
    const moduleFailed = moduleTotal - modulePassed;

    totalTests += moduleTotal;
    passedTests += modulePassed;

    const moduleStatus = moduleFailed === 0 ? '✅' : '❌';
    console.log(`${moduleStatus} ${moduleResult.module}: ${modulePassed}/${moduleTotal} 通过`);

    if (moduleFailed > 0) {
      failedModules.push({
        name: moduleResult.module,
        failed: moduleFailed,
        errors: moduleResult.results.filter(r => !r.success)
      });
    }
  });

  console.log(`\n🎯 总结: ${passedTests}/${totalTests} API端点测试通过`);
  
  if (failedModules.length > 0) {
    console.log('\n❌ 失败的模块详情:');
    failedModules.forEach(module => {
      console.log(`\n  ${module.name}:`);
      module.errors.forEach(error => {
        console.log(`    - ${error.method} ${error.path}: ${error.error || `HTTP ${error.statusCode}`}`);
      });
    });
  } else {
    console.log('\n🎉 所有模块API测试通过！');
  }

  // 计算成功率
  const successRate = ((passedTests / totalTests) * 100).toFixed(1);
  console.log(`\n📈 总体成功率: ${successRate}%`);
  
  if (successRate >= 80) {
    console.log('✅ API可访问性验证通过');
  } else {
    console.log('⚠️ API可访问性需要进一步修复');
  }
}

// 启动测试
runAllTests().then(() => {
  console.log('\n🏁 UltraThink v2.0 模块API验证测试完成');
}).catch(err => {
  console.error('💥 测试执行失败:', err.message);
});