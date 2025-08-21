const https = require('https');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

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

// 测试带Token的API请求
async function testWithToken(path, token) {
  return new Promise((resolve) => {
    const options = {
      hostname: 'localhost',
      port: 7001,
      path: path,
      method: 'GET',
      headers: {
        'Authorization': 'Bearer ' + token,
        'Content-Type': 'application/json'
      }
    };

    console.log(`🧪 测试: GET ${path}`);
    console.log(`🔑 使用Token: ${token.substring(0, 50)}...`);

    const req = https.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => body += chunk);
      res.on('end', () => {
        console.log(`📊 响应状态: ${res.statusCode}`);
        console.log(`📄 响应内容: ${body.substring(0, 200)}${body.length > 200 ? '...' : ''}`);
        resolve({ statusCode: res.statusCode, body });
      });
    });

    req.on('error', (err) => {
      console.log(`❌ 请求错误: ${err.message}`);
      resolve({ error: err.message });
    });

    req.end();
  });
}

async function main() {
  try {
    console.log('🔐 UltraThink v2.0 Token认证测试');
    console.log('=====================================');

    // 1. 获取Token
    const token = await getAuthToken();
    console.log('✅ 登录成功，获取Token');
    
    // 2. 测试需要认证的端点
    console.log('\\n📋 测试需要认证的API端点:');
    
    await testWithToken('/api/v1/users', token);
    console.log('');
    await testWithToken('/api/v1/herbs', token);
    console.log('');
    await testWithToken('/api/v1/prescriptions', token);

  } catch (error) {
    console.error('❌ 测试失败:', error.message);
  }
}

main();