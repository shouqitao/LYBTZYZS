const https = require('https');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

async function testUserAPIs() {
  try {
    // 1. 获取Token
    const token = await getAuthToken();
    console.log('✅ 获得认证Token');

    const testResults = [];

    // 2. 测试获取用户列表 (使用正确的POST /paged 端点)
    console.log('\n🧪 测试 1: 获取用户列表 (分页查询)');
    const queryData = {
      currentPage: 1,
      pageSize: 10,
      username: '',
      realName: '',
      role: null,
      isActive: null
    };
    const listResult = await apiRequest('POST', '/api/v1/users/paged', token, queryData);
    testResults.push({
      api: 'POST /api/v1/users/paged',
      status: listResult.success ? '✅ 成功' : '❌ 失败',
      statusCode: listResult.statusCode,
      response: listResult.data || listResult.error
    });

    // 3. 测试新增用户 (使用正确的POST /add 端点和枚举值)
    console.log('\n🧪 测试 2: 新增用户');
    const createData = {
      userName: 'testuser001',
      realName: '测试用户001',
      email: 'test001@example.com',
      role: 1, // DiagnosingDoctor = 1
      isActive: true,
      phoneNumber: '13800138001'
    };
    const createResult = await apiRequest('POST', '/api/v1/users/add', token, createData);
    testResults.push({
      api: 'POST /api/v1/users/add',
      status: createResult.success ? '✅ 成功' : '❌ 失败',
      statusCode: createResult.statusCode,
      response: createResult.data || createResult.error
    });

    let userId = null;
    if (createResult.success && createResult.data && createResult.data.data) {
      userId = createResult.data.data.id;
      console.log('✅ 创建的用户ID:', userId);
    }

    // 4. 重新测试获取用户列表（验证创建结果）
    console.log('\n🧪 测试 3: 重新获取用户列表（验证创建结果）');
    const queryData2 = {
      currentPage: 1,
      pageSize: 10,
      username: '',
      realName: '',
      role: null,
      isActive: null
    };
    const listResult2 = await apiRequest('POST', '/api/v1/users/paged', token, queryData2);
    testResults.push({
      api: 'POST /api/v1/users/paged (验证)',
      status: listResult2.success ? '✅ 成功' : '❌ 失败',
      statusCode: listResult2.statusCode,
      response: listResult2.data || listResult2.error
    });

    // 打印测试结果
    console.log('\n📊 用户模块API测试结果:');
    testResults.forEach((result, index) => {
      console.log(`${index + 1}. ${result.api} - ${result.status} (HTTP ${result.statusCode})`);
      if (!result.status.includes('成功')) {
        console.log(`   错误详情: ${JSON.stringify(result.response, null, 2)}`);
      } else if (result.response && result.response.data) {
        console.log(`   数据预览: ${JSON.stringify(result.response.data, null, 2).substring(0, 200)}...`);
      }
    });

    // 总结
    const successCount = testResults.filter(r => r.status.includes('成功')).length;
    const totalCount = testResults.length;
    console.log(`\n📋 用户API测试总结: ${successCount}/${totalCount} 个接口正常工作`);

    if (successCount === totalCount) {
      console.log('🎉 所有用户API都正常工作！');
    } else {
      console.log('⚠️ 部分用户API存在问题，需要进一步修复');
    }

  } catch (error) {
    console.error('❌ 测试过程中发生错误:', error.message);
  }
}

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
        try {
          const result = res.statusCode >= 200 && res.statusCode < 300;
          resolve({
            success: result,
            statusCode: res.statusCode,
            data: body ? JSON.parse(body) : null,
            error: result ? null : `HTTP ${res.statusCode}: ${body}`
          });
        } catch (e) {
          resolve({
            success: res.statusCode >= 200 && res.statusCode < 300,
            statusCode: res.statusCode,
            data: body,
            error: body || `HTTP ${res.statusCode}`
          });
        }
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

testUserAPIs();