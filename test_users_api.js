const https = require('https');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

console.log('🧪 UltraThink v2.0 Phase 10: API功能测试');
console.log('🔍 测试Users模块API接口...');

async function testUsersAPI() {
  const results = [];

  // 1. 测试获取用户列表
  console.log('\n📋 测试1: 获取用户列表');
  try {
    const listResult = await apiRequest('GET', '/api/v1/users');
    if (listResult.success) {
      console.log('✅ 获取用户列表成功');
      const count = listResult.data ? (listResult.data.length || 0) : 0;
      console.log('数据:', count + '个用户');
    } else {
      console.log('❌ 获取用户列表失败:', listResult.error);
    }
    results.push({test: '获取用户列表', result: listResult.success ? '成功' : '失败'});
  } catch (error) {
    console.log('❌ 获取用户列表异常:', error.message);
    results.push({test: '获取用户列表', result: '异常'});
  }

  // 2. 测试创建新用户
  console.log('\n➕ 测试2: 创建新用户');
  const testUser = {
    username: 'test_user_' + Date.now(),
    realName: '测试用户',
    role: 'Doctor', 
    isActive: true
  };
  
  try {
    const createResult = await apiRequest('POST', '/api/v1/users', testUser);
    if (createResult.success) {
      console.log('✅ 创建用户成功');
      console.log('用户ID:', createResult.data && createResult.data.id);
      results.push({test: '创建用户', result: '成功', userId: createResult.data && createResult.data.id});
    } else {
      console.log('❌ 创建用户失败:', createResult.error);
      results.push({test: '创建用户', result: '失败'});
    }
  } catch (error) {
    console.log('❌ 创建用户异常:', error.message);
    results.push({test: '创建用户', result: '异常'});
  }

  // 3. 如果创建成功，测试获取用户详情
  const createSuccess = results.find(r => r.test === '创建用户' && r.result === '成功');
  if (createSuccess && createSuccess.userId) {
    console.log('\n🔍 测试3: 获取用户详情');
    try {
      const getResult = await apiRequest('GET', '/api/v1/users/' + createSuccess.userId);
      if (getResult.success) {
        console.log('✅ 获取用户详情成功');
        console.log('用户名:', getResult.data && getResult.data.username);
      } else {
        console.log('❌ 获取用户详情失败:', getResult.error);
      }
      results.push({test: '获取用户详情', result: getResult.success ? '成功' : '失败'});
    } catch (error) {
      console.log('❌ 获取用户详情异常:', error.message);
      results.push({test: '获取用户详情', result: '异常'});
    }
  }

  // 总结测试结果
  console.log('\n📊 Users模块API测试结果汇总:');
  results.forEach((result, index) => {
    const status = result.result === '成功' ? '✅' : '❌';
    console.log((index + 1) + '. ' + result.test + ': ' + status + ' ' + result.result);
  });

  const successCount = results.filter(r => r.result === '成功').length;
  const totalCount = results.length;
  console.log('\n🎯 总体结果: ' + successCount + '/' + totalCount + ' 测试成功');
  
  return results;
}

// 通用API请求函数
function apiRequest(method, path, data = null) {
  return new Promise((resolve) => {
    const postData = data ? JSON.stringify(data) : null;
    const options = {
      hostname: 'localhost',
      port: 7001,
      path: path,
      method: method,
      headers: {
        'Content-Type': 'application/json'
      }
    };

    if (postData) {
      options.headers['Content-Length'] = Buffer.byteLength(postData);
    }

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
            error: result ? null : 'HTTP ' + res.statusCode + ': ' + body
          });
        } catch (e) {
          resolve({
            success: res.statusCode >= 200 && res.statusCode < 300,
            statusCode: res.statusCode,
            data: body,
            error: body || 'HTTP ' + res.statusCode
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

testUsersAPI();