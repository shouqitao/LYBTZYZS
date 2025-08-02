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

async function testPatientCreation() {
  try {
    const token = await getToken();
    console.log('✅ 获取Token成功');

    const timestamp = Date.now();
    const newPatient = {
      name: `测试患者${timestamp}`,
      gender: 1,
      age: 35,
      birthDate: '1990-01-01T00:00:00.000Z',
      phoneNumber: `139${timestamp.toString().slice(-8)}`,
      idNumber: `110101199001011234`, // 使用固定的18位身份证号
      address: '北京市朝阳区测试地址',
      emergencyContact: '紧急联系人',
      emergencyPhone: '13800138000',
      medicalHistory: '无特殊病史',
      allergies: '无过敏史'
    };

    console.log('发送的患者数据:', JSON.stringify(newPatient, null, 2));

    const postData = JSON.stringify(newPatient);
    const options = {
      hostname: 'localhost',
      port: 7001,
      path: '/api/v1/patients',
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
        console.log('患者创建响应状态:', res.statusCode);
        console.log('响应内容:', body);
        
        // 测试医生创建
        testDoctorCreation(token);
      });
    });

    req.on('error', (err) => console.error('患者创建请求错误:', err.message));
    req.write(postData);
    req.end();

  } catch (error) {
    console.error('测试失败:', error.message);
  }
}

async function testDoctorCreation(token) {
  const timestamp = Date.now();
  const newDoctor = {
    userId: '64d9b4c5-26ab-4648-90cb-9234f75924f2', // 使用现有的sysadmin用户ID
    gender: 1, // 使用数字枚举
    birthday: '1980-01-01T00:00:00.000Z',
    title: 3, // AttendingPhysician
    licenseNumber: `ZY${timestamp.toString().slice(-6)}`,
    specialty: '中医内科',
    status: 1, // Active
    workStatus: 1, // Clinic
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
      console.log('响应内容:', body);
    });
  });

  req.on('error', (err) => console.error('医生创建请求错误:', err.message));
  req.write(postData);
  req.end();
}

testPatientCreation();