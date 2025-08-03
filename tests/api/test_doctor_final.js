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

async function getDoctorList(token) {
  return new Promise((resolve, reject) => {
    const options = {
      hostname: 'localhost',
      port: 7001,
      path: '/api/v1/doctors',
      method: 'GET',
      headers: {
        'Authorization': 'Bearer ' + token
      }
    };

    const req = https.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => body += chunk);
      res.on('end', () => {
        console.log('现有医生列表:');
        try {
          const result = JSON.parse(body);
          if (result.success && result.data && result.data.items) {
            result.data.items.forEach(doctor => {
              console.log(`- ID: ${doctor.id}, UserId: ${doctor.userId}, 姓名: ${doctor.realName}`);
            });
          }
        } catch (e) {
          console.log('解析失败:', body);
        }
        resolve();
      });
    });

    req.on('error', reject);
    req.end();
  });
}

async function createNewUser(token) {
  return new Promise((resolve, reject) => {
    const timestamp = Date.now();
    const newUser = {
      username: `newdoctor_${timestamp}`,
      password: 'Test@123456',
      confirmPassword: 'Test@123456',
      realName: `新医生用户${timestamp}`,
      role: 1, // DiagnosingDoctor
      email: `newdoctor${timestamp}@lybt.com`,
      phoneNumber: `136${timestamp.toString().slice(-8)}`,
      isActive: true
    };

    console.log('\n创建新医生用户...');

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
        console.log('用户创建响应:', body);
        resolve(timestamp);
      });
    });

    req.on('error', reject);
    req.write(postData);
    req.end();
  });
}

async function findNewUser(token, timestamp) {
  return new Promise((resolve, reject) => {
    const options = {
      hostname: 'localhost',
      port: 7001,
      path: `/api/v1/users?keyword=newdoctor_${timestamp}`,
      method: 'GET',
      headers: {
        'Authorization': 'Bearer ' + token
      }
    };

    const req = https.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => body += chunk);
      res.on('end', () => {
        try {
          const result = JSON.parse(body);
          if (result.success && result.data && result.data.items && result.data.items.length > 0) {
            const user = result.data.items[0];
            console.log(`✅ 找到新用户: ${user.id} - ${user.username}`);
            resolve(user.id);
          } else {
            console.log('❌ 未找到新创建的用户');
            resolve(null);
          }
        } catch (e) {
          console.error('解析用户列表失败:', e.message);
          resolve(null);
        }
      });
    });

    req.on('error', reject);
    req.end();
  });
}

async function createDoctorWithUserId(token, userId) {
  return new Promise((resolve, reject) => {
    const timestamp = Date.now();
    const newDoctor = {
      userId: userId,
      gender: 1,
      birthday: '1980-01-01T00:00:00.000Z',
      title: 3, // AttendingPhysician
      licenseNumber: `ZY${timestamp.toString().slice(-6)}`,
      specialty: '中医内科',
      status: 1, // Active
      workStatus: 1, // Clinic
      pinyinCode: 'XYSYS',
      remark: '新创建的测试医生',
      contactNumber: `136${timestamp.toString().slice(-8)}`,
      realName: `新医生${timestamp}`,
      phoneNumber: `136${timestamp.toString().slice(-8)}`,
      email: `newdoctor${timestamp}@lybt.com`,
      age: 43
    };

    console.log('\n发送的医生数据:');
    console.log(JSON.stringify(newDoctor, null, 2));

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
        console.log('\n🎯 医生创建响应状态:', res.statusCode);
        console.log('🎯 医生创建响应内容:', body);
        
        if (res.statusCode === 201) {
          console.log('✅ 医生创建成功！');
        } else {
          console.log('❌ 医生创建失败');
        }
        resolve(res.statusCode === 201);
      });
    });

    req.on('error', reject);
    req.write(postData);
    req.end();
  });
}

async function testDoctorCreation() {
  try {
    const token = await getToken();
    console.log('✅ 获取Token成功\n');

    console.log('步骤1: 检查现有医生列表');
    await getDoctorList(token);

    console.log('\n步骤2: 测试用已有用户创建医生');
    // 使用之前创建的DiagnosingDoctor用户
    const existingUserId = '96188727-95c1-4658-bf1a-5ddcbc71d414';
    const success1 = await createDoctorWithUserId(token, existingUserId);
    
    if (success1) {
      console.log('\n🎉 用现有用户创建医生成功！');
      return;
    }

    console.log('\n步骤3: 创建新的医生用户');
    const timestamp = await createNewUser(token);

    console.log('\n步骤4: 等待1秒后查找新用户');
    await new Promise(resolve => setTimeout(resolve, 1000));
    const userId = await findNewUser(token, timestamp);

    if (userId) {
      console.log('\n步骤5: 使用新用户ID创建医生');
      const success = await createDoctorWithUserId(token, userId);
      
      if (success) {
        console.log('\n🎉 医生创建流程完全成功！');
      } else {
        console.log('\n❌ 医生创建仍然失败');
      }
    } else {
      console.log('\n❌ 无法找到新创建的用户，跳过医生创建');
    }

  } catch (error) {
    console.error('❌ 测试过程中发生错误:', error.message);
  }
}

testDoctorCreation();