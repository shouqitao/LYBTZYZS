const https = require('https');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

async function getToken() {
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
            reject(new Error('登录失败'));
          }
        } catch (e) {
          reject(e);
        }
      });
    });

    req.on('error', reject);
    req.write(data);
    req.end();
  });
}

async function createUser(token) {
  return new Promise((resolve, reject) => {
    const timestamp = Date.now();
    const newUser = {
      username: `doctoruser_${timestamp}`,
      password: 'Test@123456',
      confirmPassword: 'Test@123456',
      realName: `医生用户${timestamp}`,
      role: 3, // Doctor
      email: `doctoruser${timestamp}@lybt.com`,
      phoneNumber: `138${timestamp.toString().slice(-8)}`,
      isActive: true
    };

    const postData = JSON.stringify(newUser);
    const options = {
      hostname: 'localhost',
      port: 7001,
      path: '/api/v1/users',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + token,
        'Content-Length': Buffer.byteLength(postData)
      }
    };

    const req = https.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => body += chunk);
      res.on('end', () => {
        console.log('用户创建响应状态:', res.statusCode);
        console.log('用户创建响应内容:', body);
        try {
          const result = JSON.parse(body);
          if (result.success && result.data && result.data.id) {
            resolve(result.data.id);
          } else {
            reject(new Error('用户创建失败'));
          }
        } catch (e) {
          reject(e);
        }
      });
    });

    req.on('error', reject);
    req.write(postData);
    req.end();
  });
}

async function createDoctor(token, userId) {
  return new Promise((resolve, reject) => {
    const timestamp = Date.now();
    const newDoctor = {
      userId: userId,
      gender: 1,
      birthday: '1980-01-01T00:00:00.000Z',
      title: 3, // AttendingPhysician
      licenseNumber: `ZY${timestamp.toString().slice(-6)}`,
      specialty: '中医内科',
      status: 1,
      workStatus: 1,
      pinyinCode: 'ZSYS',
      remark: '测试医生',
      contactNumber: `137${timestamp.toString().slice(-8)}`,
      realName: `测试医生${timestamp}`,
      phoneNumber: `137${timestamp.toString().slice(-8)}`,
      email: `doctor${timestamp}@lybt.com`,
      age: 43
    };

    console.log('\n发送的医生数据:', JSON.stringify(newDoctor, null, 2));

    const postData = JSON.stringify(newDoctor);
    const options = {
      hostname: 'localhost',
      port: 7001,
      path: '/api/v1/doctors',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + token,
        'Content-Length': Buffer.byteLength(postData)
      }
    };

    const req = https.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => body += chunk);
      res.on('end', () => {
        console.log('\n医生创建响应状态:', res.statusCode);
        console.log('医生创建响应内容:', body);
        resolve();
      });
    });

    req.on('error', reject);
    req.write(postData);
    req.end();
  });
}

async function testDoctorFlow() {
  try {
    const token = await getToken();
    console.log('✅ 获取Token成功');

    console.log('\n步骤1: 创建用户...');
    const userId = await createUser(token);
    console.log('✅ 用户创建成功，ID:', userId);

    console.log('\n步骤2: 创建医生...');
    await createDoctor(token, userId);
    console.log('✅ 医生创建流程完成');

  } catch (error) {
    console.error('❌ 测试失败:', error.message);
  }
}

testDoctorFlow();