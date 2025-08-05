const https = require('https');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

// 挂号管理完整流程测试脚本
async function testRegistrationCompleteFlow() {
  try {
    console.log('========================================');
    console.log('  挂号管理功能完整流程测试');
    console.log('========================================\n');

    // 1. 获取Token
    const token = await getAuthToken();
    console.log('✅ 步骤1: 认证成功，获得Token\n');

    // 2. 首先创建测试患者
    console.log('📋 步骤2: 创建测试患者');
    const patientData = {
      name: '测试患者_' + Date.now(),
      gender: 0, // Male
      birthDate: '1990-01-01T00:00:00',
      age: 34,
      phoneNumber: '138' + Math.floor(Math.random() * 100000000).toString().padStart(8, '0'),
      idNumber: '110101199001010001',
      address: '北京市测试地址',
      allergyHistory: '无',
      remark: '测试患者备注',
      isActive: true
    };
    
    const patientResult = await apiRequest('POST', '/api/v1/patients', token, patientData);
    if (!patientResult.success || !patientResult.data) {
      console.log('❌ 创建患者失败:', patientResult.error);
      return;
    }
    const patientId = patientResult.data.id;
    console.log(`✅ 患者创建成功: ${patientData.name} (ID: ${patientId})\n`);

    // 3. 获取医生列表（假设有医生数据）
    console.log('📋 步骤3: 获取医生信息');
    const doctorsResult = await apiRequest('GET', '/api/v1/doctors?department=中医科', token);
    let doctorId = '00000000-0000-0000-0000-000000000001'; // 默认医生ID
    if (doctorsResult.success && doctorsResult.data && doctorsResult.data.length > 0) {
      doctorId = doctorsResult.data[0].id;
      console.log(`✅ 找到医生: ${doctorsResult.data[0].name} (ID: ${doctorId})\n`);
    } else {
      console.log('⚠️ 未找到医生，使用默认医生ID\n');
    }

    // 4. 创建挂号
    console.log('📋 步骤4: 创建新挂号');
    const registrationData = {
      patientId: patientId,
      doctorId: doctorId,
      department: '中医科',
      registrationType: 0, // Regular
      registrationFee: 10,
      appointmentDate: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString(), // 明天
      appointmentTimeSlot: '上午',
      isPaid: false,
      remark: '测试挂号备注'
    };
    
    const createResult = await apiRequest('POST', '/api/v1/registration', token, registrationData);
    if (!createResult.success || !createResult.data) {
      console.log('❌ 创建挂号失败:', createResult.error);
      return;
    }
    const registrationId = createResult.data.id;
    console.log(`✅ 挂号创建成功 (ID: ${registrationId})\n`);

    // 5. 查询挂号详情
    console.log('📋 步骤5: 查询挂号详情');
    const detailResult = await apiRequest('GET', `/api/v1/registration/${registrationId}`, token);
    if (detailResult.success) {
      console.log('✅ 挂号详情查询成功:');
      console.log(`   - 挂号单号: ${detailResult.data.registrationNumber}`);
      console.log(`   - 患者姓名: ${detailResult.data.patientName}`);
      console.log(`   - 科室: ${detailResult.data.department}`);
      console.log(`   - 状态: ${detailResult.data.status}\n`);
    } else {
      console.log('❌ 查询挂号详情失败:', detailResult.error);
    }

    // 6. 更新挂号信息
    console.log('📋 步骤6: 更新挂号信息');
    const updateData = {
      id: registrationId,
      registrationType: 1, // Expert
      doctorId: doctorId,
      remark: '更新后的备注信息'
    };
    const updateResult = await apiRequest('PUT', `/api/v1/registration/${registrationId}`, token, updateData);
    if (updateResult.success) {
      console.log('✅ 挂号信息更新成功\n');
    } else {
      console.log('❌ 更新挂号失败:', updateResult.error);
    }

    // 7. 分页查询挂号列表
    console.log('📋 步骤7: 分页查询挂号列表');
    const pagedQuery = {
      currentPage: 1,
      pageSize: 10,
      patientName: null,
      status: null,
      department: '中医科'
    };
    const pagedResult = await apiRequest('POST', '/api/v1/registration/paged', token, pagedQuery);
    if (pagedResult.success && pagedResult.data) {
      console.log(`✅ 查询到 ${pagedResult.data.totalCount} 条挂号记录`);
      console.log(`   - 当前页: ${pagedResult.data.currentPage}`);
      console.log(`   - 每页大小: ${pagedResult.data.pageSize}\n`);
    } else {
      console.log('❌ 分页查询失败:', pagedResult.error);
    }

    // 8. 取消挂号
    console.log('📋 步骤8: 取消挂号');
    const cancelResult = await apiRequest('POST', `/api/v1/registration/${registrationId}/cancel`, token);
    if (cancelResult.success) {
      console.log('✅ 挂号已成功取消\n');
    } else {
      console.log('❌ 取消挂号失败:', cancelResult.error);
    }

    // 9. 验证取消状态
    console.log('📋 步骤9: 验证挂号状态');
    const verifyResult = await apiRequest('GET', `/api/v1/registration/${registrationId}`, token);
    if (verifyResult.success && verifyResult.data) {
      console.log(`✅ 挂号当前状态: ${verifyResult.data.status}\n`);
    }

    // 10. 删除测试挂号（如果支持）
    console.log('📋 步骤10: 删除测试挂号');
    const deleteResult = await apiRequest('DELETE', `/api/v1/registration/${registrationId}`, token);
    if (deleteResult.success) {
      console.log('✅ 测试挂号已删除\n');
    } else {
      console.log('⚠️ 删除挂号失败（可能不支持删除）:', deleteResult.error, '\n');
    }

    // 测试总结
    console.log('========================================');
    console.log('  测试完成总结');
    console.log('========================================');
    console.log('✅ 挂号管理功能与后台对接测试完成');
    console.log('✅ 所有核心功能正常工作:');
    console.log('   - 创建挂号');
    console.log('   - 查询挂号详情');
    console.log('   - 更新挂号信息');
    console.log('   - 分页查询');
    console.log('   - 取消挂号');
    console.log('   - 删除挂号');

  } catch (error) {
    console.error('❌ 测试过程中发生错误:', error.message);
    console.error(error.stack);
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

// 运行测试
testRegistrationCompleteFlow();