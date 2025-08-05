const https = require('https');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

// 挂号管理API测试脚本
async function testRegistrationAPIs() {
  try {
    // 1. 获取Token
    const token = await getAuthToken();
    console.log('✅ 获得认证Token');

    const testResults = [];

    // 2. 测试分页查询挂号记录
    console.log('\n🧪 测试 1: 分页查询挂号记录');
    const pagedQuery = {
      currentPage: 1,
      pageSize: 10,
      patientName: null,
      status: null,
      department: null
    };
    const pagedResult = await apiRequest('POST', '/api/v1/registration/paged', token, pagedQuery);
    testResults.push({
      api: 'POST /api/v1/registration/paged',
      status: pagedResult.success ? '✅ 成功' : '❌ 失败',
      response: pagedResult.data || pagedResult.error
    });

    // 3. 测试获取挂号列表
    console.log('\n🧪 测试 2: 获取挂号列表');
    const listResult = await apiRequest('GET', '/api/v1/registration', token);
    testResults.push({
      api: 'GET /api/v1/registration',
      status: listResult.success ? '✅ 成功' : '❌ 失败',
      response: listResult.data || listResult.error
    });

    // 4. 测试新增挂号
    console.log('\n🧪 测试 3: 新增挂号');
    
    // 首先需要获取患者和医生ID
    // 这里使用固定的测试ID，实际应该从数据库获取
    const createData = {
      patientId: '11111111-1111-1111-1111-111111111111', // 测试患者ID
      doctorId: '22222222-2222-2222-2222-222222222222',  // 测试医生ID
      department: '中医科',
      registrationType: 0, // Regular
      registrationFee: 10,
      appointmentDate: new Date().toISOString(),
      appointmentTimeSlot: '上午',
      isPaid: false,
      remark: '测试挂号'
    };
    
    const createResult = await apiRequest('POST', '/api/v1/registration', token, createData);
    testResults.push({
      api: 'POST /api/v1/registration',
      status: createResult.success ? '✅ 成功' : '❌ 失败',
      response: createResult.data || createResult.error
    });

    // 5. 如果创建成功，测试其他操作
    if (createResult.success && createResult.data && createResult.data.id) {
      const registrationId = createResult.data.id;
      
      // 测试获取挂号详情
      console.log('\n🧪 测试 4: 获取挂号详情');
      const detailResult = await apiRequest('GET', `/api/v1/registration/${registrationId}`, token);
      testResults.push({
        api: `GET /api/v1/registration/${registrationId}`,
        status: detailResult.success ? '✅ 成功' : '❌ 失败',
        response: detailResult.data || detailResult.error
      });

      // 测试更新挂号
      console.log('\n🧪 测试 5: 更新挂号信息');
      const updateData = {
        id: registrationId,
        registrationType: 1, // Expert
        doctorId: '22222222-2222-2222-2222-222222222222',
        remark: '更新后的备注'
      };
      const updateResult = await apiRequest('PUT', `/api/v1/registration/${registrationId}`, token, updateData);
      testResults.push({
        api: `PUT /api/v1/registration/${registrationId}`,
        status: updateResult.success ? '✅ 成功' : '❌ 失败',
        response: updateResult.data || updateResult.error
      });

      // 测试取消挂号
      console.log('\n🧪 测试 6: 取消挂号');
      const cancelResult = await apiRequest('POST', `/api/v1/registration/${registrationId}/cancel`, token);
      testResults.push({
        api: `POST /api/v1/registration/${registrationId}/cancel`,
        status: cancelResult.success ? '✅ 成功' : '❌ 失败',
        response: cancelResult.data || cancelResult.error
      });
    }

    // 打印测试结果
    console.log('\n📊 挂号管理API测试结果:');
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

testRegistrationAPIs();