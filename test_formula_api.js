const https = require('https');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

// 验方模板API测试脚本
async function testFormulaTemplateAPIs() {
  try {
    console.log('🧪 开始验方模板API测试...');
    
    // 1. 获取Token
    const token = await getAuthToken();
    console.log('✅ 获得认证Token');

    const testResults = [];

    // 2. 测试获取模板列表
    console.log('\n🧪 测试 1: 获取验方模板列表');
    const listResult = await apiRequest('GET', '/api/v1/FormulaTemplate', token);
    testResults.push({
      api: 'GET /api/v1/FormulaTemplate',
      status: listResult.success ? '✅ 成功' : '❌ 失败',
      statusCode: listResult.statusCode,
      response: listResult.data || listResult.error
    });
    console.log(`状态码: ${listResult.statusCode}, 响应: ${JSON.stringify(listResult.data || listResult.error)}`);

    // 3. 测试新增模板
    console.log('\n🧪 测试 2: 新增验方模板');
    const createData = {
      name: '感冒清热方',
      herbs: [
        { herbId: '11111111-1111-1111-1111-111111111111', herbName: '麻黄', quantity: 6, unit: 'g', usage: '发汗散寒' },
        { herbId: '22222222-2222-2222-2222-222222222222', herbName: '桂枝', quantity: 9, unit: 'g', usage: '温阳化气' }
      ],
      remark: '经典感冒方'
    };
    const createResult = await apiRequest('POST', '/api/v1/FormulaTemplate', token, createData);
    testResults.push({
      api: 'POST /api/v1/FormulaTemplate',
      status: createResult.success ? '✅ 成功' : '❌ 失败',
      statusCode: createResult.statusCode,
      response: createResult.data || createResult.error
    });
    console.log(`状态码: ${createResult.statusCode}, 响应: ${JSON.stringify(createResult.data || createResult.error)}`);

    // 4. 测试导出
    console.log('\n🧪 测试 3: 导出验方模板');
    const exportResult = await apiRequest('POST', '/api/v1/FormulaTemplate/export', token);
    testResults.push({
      api: 'POST /api/v1/FormulaTemplate/export',
      status: exportResult.success ? '✅ 成功' : '❌ 失败',
      statusCode: exportResult.statusCode,
      response: exportResult.data || exportResult.error
    });
    console.log(`状态码: ${exportResult.statusCode}, 响应: ${JSON.stringify(exportResult.data || exportResult.error)}`);

    // 打印测试结果摘要
    console.log('\n📊 验方模板API测试结果摘要:');
    testResults.forEach((result, index) => {
      console.log(`${index + 1}. ${result.api} - ${result.status} (${result.statusCode})`);
    });

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

testFormulaTemplateAPIs();