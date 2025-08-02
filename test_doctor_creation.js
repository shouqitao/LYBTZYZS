const https = require('https');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

async function testDoctorCreation() {
  try {
    // 1. 登录获取token
    const loginData = JSON.stringify({
      username: 'sysadmin',
      password: 'Admin@123456',
      rememberMe: false
    });

    const token = await new Promise((resolve, reject) => {
      const options = {
        hostname: 'localhost',
        port: 7001,
        path: '/api/v1/auth/login',
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Content-Length': Buffer.byteLength(loginData)
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
              reject(new Error('登录失败'));
            }
          } catch (e) {
            reject(e);
          }
        });
      });
      req.on('error', reject);
      req.write(loginData);
      req.end();
    });

    console.log('✅ 获取Token成功');

    // 2. 测试医生创建
    const timestamp = Date.now();
    const doctorData = JSON.stringify({
      userId: '25c87ec6-0add-44f2-b5c1-845f19ff2cac',
      gender: 1,
      birthday: '1980-01-01T00:00:00.000Z',
      title: 3,
      licenseNumber: `ZY${timestamp.toString().slice(-6)}`,
      idNumber: '110105198001011234',
      specialty: '中医内科',
      status: 1,
      workStatus: 1,
      pinyinCode: 'ZSYS',
      remark: '测试医生',
      contactNumber: `137${timestamp.toString().slice(-8)}`
    });

    const result = await new Promise((resolve, reject) => {
      const options = {
        hostname: 'localhost',
        port: 7001,
        path: '/api/v1/doctors',
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer ' + token,
          'Content-Length': Buffer.byteLength(doctorData)
        }
      };

      const req = https.request(options, (res) => {
        let body = '';
        res.on('data', (chunk) => body += chunk);
        res.on('end', () => {
          console.log('医生创建响应状态:', res.statusCode);
          console.log('响应内容:', body);
          try {
            const parsed = JSON.parse(body);
            console.log('解析后的响应:', JSON.stringify(parsed, null, 2));
          } catch (e) {
            console.log('无法解析JSON响应');
          }
          resolve({status: res.statusCode, body});
        });
      });
      req.on('error', reject);
      req.write(doctorData);
      req.end();
    });

  } catch (error) {
    console.error('❌ 测试失败:', error.message);
  }
}

testDoctorCreation();