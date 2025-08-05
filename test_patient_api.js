const https = require('https');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

// 患者管理API测试脚本
async function testPatientAPIs() {
  try {
    // 1. 获取Token
    const token = await getAuthToken();
    console.log('✅ 获得认证Token');

    const testResults = [];

    // 2. 测试获取患者列表
    console.log('\n🧪 测试 1: 获取患者列表');
    const listResult = await apiRequest('GET', '/api/v1/patients?page=1&pageSize=10', token);
    testResults.push({
      api: 'GET /api/v1/patients',
      status: listResult.success ? '✅ 成功' : '❌ 失败',
      response: listResult.data || listResult.error
    });

    // 3. 测试新增患者
    console.log('\n🧪 测试 2: 新增患者');
    const createData = {
      name: '测试患者' + Date.now(),
      gender: 0, // Male
      birthDate: '1990-01-01T00:00:00',
      age: 34,
      phoneNumber: '13800138000',
      idNumber: '110101199001010001',
      address: '北京市朝阳区测试街道',
      allergyHistory: '无',
      remark: '紧急联系人：张三，紧急电话：13900139000\n既往病史：无',
      isActive: true
    };
    const createResult = await apiRequest('POST', '/api/v1/patients', token, createData);
    testResults.push({
      api: 'POST /api/v1/patients',
      status: createResult.success ? '✅ 成功' : '❌ 失败',
      response: createResult.data || createResult.error
    });

    // 4. 如果创建成功，测试更新患者
    if (createResult.success && createResult.data && createResult.data.id) {
      console.log('\n🧪 测试 3: 更新患者信息');
      const patientId = createResult.data.id;
      const updateData = {
        ...createResult.data,
        name: '更新后的患者名',
        address: '更新后的地址'
      };
      const updateResult = await apiRequest('PUT', `/api/v1/patients/${patientId}`, token, updateData);
      testResults.push({
        api: `PUT /api/v1/patients/${patientId}`,
        status: updateResult.success ? '✅ 成功' : '❌ 失败',
        response: updateResult.data || updateResult.error
      });

      // 5. 测试获取患者详情
      console.log('\n🧪 测试 4: 获取患者详情');
      const detailResult = await apiRequest('GET', `/api/v1/patients/${patientId}`, token);
      testResults.push({
        api: `GET /api/v1/patients/${patientId}`,
        status: detailResult.success ? '✅ 成功' : '❌ 失败',
        response: detailResult.data || detailResult.error
      });

      // 6. 测试禁用患者（软删除）
      console.log('\n🧪 测试 5: 禁用患者');
      const disableResult = await apiRequest('PATCH', `/api/v1/patients/${patientId}/disable`, token);
      testResults.push({
        api: `PATCH /api/v1/patients/${patientId}/disable`,
        status: disableResult.success ? '✅ 成功' : '❌ 失败',
        response: disableResult.data || disableResult.error
      });
    }

    // 打印测试结果
    console.log('\n📊 患者管理API测试结果:');
    testResults.forEach((result, index) => {
      console.log(`${index + 1}. ${result.api} - ${result.status}`);
      if (!result.status.includes('成功')) {
        console.log(`   错误: ${JSON.stringify(result.response)}`);
      }
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

testPatientAPIs();