const http = require('http');

async function testLogin(password) {
  return new Promise((resolve, reject) => {
    const data = JSON.stringify({
      username: 'sysadmin',
      password: password,
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
        console.log(`密码 "${password}" - 状态码: ${res.statusCode}`);
        console.log(`响应: ${body.substring(0, 200)}`);
        console.log('---');
        resolve();
      });
    });

    req.on('error', reject);
    req.write(data);
    req.end();
  });
}

async function runTests() {
  const passwords = ['Admin@123456', 'admin', 'sysadmin', '123456', 'password'];
  
  for (const pwd of passwords) {
    try {
      await testLogin(pwd);
    } catch (error) {
      console.error(`测试密码 ${pwd} 失败:`, error.message);
    }
  }
}

runTests();