const https = require('https');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

console.log('🎯 UltraThink v2.0 错误修复验证 - API端点测试');
console.log('===============================================');

async function testFixedAPIs() {
  try {
    const testResults = [];

    // 1. 测试健康检查
    console.log('1️⃣ 测试健康检查端点...');
    const healthResult = await testEndpoint('GET', '/api/v1/health');
    testResults.push({
      endpoint: '/api/v1/health',
      status: healthResult.success ? '✅ 正常' : '❌ 异常',
      code: healthResult.statusCode
    });

    // 2. 测试认证端点
    console.log('\n2️⃣ 测试认证登录端点...');
    const token = await getAuthToken();
    testResults.push({
      endpoint: '/api/v1/auth/login',
      status: token ? '✅ 正常' : '❌ 异常',
      note: token ? '成功获取Token' : '登录失败'
    });

    if (token) {
      // 3. 测试Users模块
      console.log('\n3️⃣ 测试Users模块API...');
      const usersResult = await testEndpoint('GET', '/api/v1/users?pageIndex=1&pageSize=5', token);
      testResults.push({
        endpoint: '/api/v1/users',
        status: usersResult.success ? '✅ 正常' : '❌ 异常',
        code: usersResult.statusCode
      });

      // 4. 测试Herbs模块
      console.log('\n4️⃣ 测试Herbs模块API...');
      const herbsResult = await testEndpoint('GET', '/api/v1/herbs?pageIndex=1&pageSize=5', token);
      testResults.push({
        endpoint: '/api/v1/herbs',
        status: herbsResult.success ? '✅ 正常' : '❌ 异常',
        code: herbsResult.statusCode
      });
    }

    // 最终结果报告
    console.log('\n🎉 UltraThink v2.0 错误修复验证完成');
    console.log('===============================================');
    console.log('📊 测试结果汇总:');
    
    testResults.forEach((result, index) => {
      console.log(`   ${index + 1}. ${result.endpoint}: ${result.status}`);
      if (result.code) console.log(`      状态码: ${result.code}`);
      if (result.note) console.log(`      备注: ${result.note}`);
    });

    const successCount = testResults.filter(r => r.status.includes('✅')).length;
    console.log(`\n✨ 总体评估: ${successCount}/${testResults.length} 个端点正常工作`);

  } catch (error) {
    console.error('❌ API测试过程中发生错误:', error.message);
  }
}

// 获取认证Token
async function getAuthToken() {
  try {
    const data = JSON.stringify({
      username: 'sysadmin',
      password: 'Admin@123456',
      rememberMe: false
    });

    const result = await testEndpoint('POST', '/api/v1/auth/login', null, data);
    
    if (result.success && result.data && result.data.success && result.data.data && result.data.data.token) {
      return result.data.data.token;
    }
    return null;
  } catch (error) {
    return null;
  }
}

// 通用API测试函数
async function testEndpoint(method, path, token = null, data = null) {
  return new Promise((resolve) => {
    const postData = data;
    const options = {
      hostname: 'localhost',
      port: 7001,
      path: path,
      method: method,
      headers: {
        ...(token && { 'Authorization': 'Bearer ' + token }),
        ...(postData && { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(postData) })
      }
    };

    const req = https.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => body += chunk);
      res.on('end', () => {
        const success = res.statusCode >= 200 && res.statusCode < 300;
        let parsedData;
        try {
          parsedData = body ? JSON.parse(body) : null;
        } catch (e) {
          parsedData = body;
        }
        
        resolve({
          success: success,
          statusCode: res.statusCode,
          data: parsedData
        });
      });
    });

    req.on('error', (err) => resolve({
      success: false,
      error: err.message
    }));

    if (postData) req.write(postData);
    req.end();
  });
}

testFixedAPIs();