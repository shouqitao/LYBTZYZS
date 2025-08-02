const http = require('http');

async function getToken() {
  return new Promise((resolve, reject) => {
    const data = JSON.stringify({
      username: 'sysadmin',
      password: 'Admin@123456',
      rememberMe: false
    });

    const options = {
      hostname: 'localhost',
      port: 5297,
      path: '/api/v1/auth/login',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(data)
      }
    };

    const req = http.request(options, (res) => {
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
          reject(new Error('解析响应失败: ' + e.message));
        }
      });
    });

    req.on('error', reject);
    req.write(data);
    req.end();
  });
}

async function testAPI(method, path, token, data = null) {
  return new Promise((resolve) => {
    const postData = data ? JSON.stringify(data) : null;
    const options = {
      hostname: 'localhost',
      port: 5297,
      path: path,
      method: method,
      headers: {
        'Authorization': 'Bearer ' + token,
        ...(postData && { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(postData) })
      }
    };

    const req = http.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => body += chunk);
      res.on('end', () => {
        resolve({
          statusCode: res.statusCode,
          body: body
        });
      });
    });

    req.on('error', (err) => resolve({
      statusCode: 'ERROR',
      body: err.message
    }));

    if (postData) req.write(postData);
    req.end();
  });
}

async function runTests() {
  try {
    console.log('获取认证Token...');
    const token = await getToken();
    console.log('Token获取成功');

    const tests = [
      { name: 'Users - 获取用户列表', method: 'GET', path: '/api/v1/Users' },
      { name: 'Users - 分页查询', method: 'POST', path: '/api/v1/Users/paged', data: { currentPage: 1, pageSize: 10 } },
      { name: 'Patients - 获取患者列表', method: 'GET', path: '/api/v1/Patients' },
      { name: 'Patients - 分页查询', method: 'POST', path: '/api/v1/Patients/paged', data: { currentPage: 1, pageSize: 10 } },
      { name: 'Herbs - 获取药材列表', method: 'GET', path: '/api/v1/Herbs' },
      { name: 'Herbs - 分页查询', method: 'POST', path: '/api/v1/Herbs/paged', data: { currentPage: 1, pageSize: 10 } }
    ];

    console.log('\n开始API测试...\n');

    for (const test of tests) {
      const result = await testAPI(test.method, test.path, token, test.data);
      const status = result.statusCode === 200 ? '成功' : 
                    result.statusCode >= 400 && result.statusCode < 500 ? '客户端错误' : 
                    result.statusCode >= 500 ? '服务器错误' : '未知';
      
      console.log(`${test.name} - 状态码: ${result.statusCode} (${status})`);
      if (result.statusCode !== 200) {
        console.log(`   响应: ${result.body.substring(0, 200)}...`);
      }
      console.log('');
    }

  } catch (error) {
    console.error('测试失败:', error.message);
  }
}

runTests();