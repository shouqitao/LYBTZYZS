const https = require('https');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

// 医生管理API测试脚本
async function testDoctorAPIs() {
  try {
    console.log('========================================');
    console.log('  医生管理功能完整流程测试');
    console.log('========================================\n');

    // 1. 获取Token
    const token = await getAuthToken();
    console.log('✅ 步骤1: 认证成功，获得Token\n');

    const testResults = [];

    // 2. 测试获取活跃医生列表
    console.log('📋 步骤2: 获取活跃医生列表');
    const activeListResult = await apiRequest('GET', '/api/v1/doctors/active', token);
    testResults.push({
      api: 'GET /api/v1/doctors/active',
      status: activeListResult.success ? '✅ 成功' : '❌ 失败',
      response: activeListResult.data || activeListResult.error
    });
    console.log(`   结果: ${activeListResult.success ? '成功' : '失败'}`);
    if (activeListResult.success && activeListResult.data) {
      console.log(`   - 当前活跃医生数量: ${activeListResult.data.length}\n`);
    }

    // 3. 测试分页查询医生
    console.log('📋 步骤3: 分页查询医生列表');
    const pagedQuery = {
      currentPage: 1,
      pageSize: 10,
      keyword: null,
      title: null,
      status: null
    };
    const pagedResult = await apiRequest('POST', '/api/v1/doctors/paged', token, pagedQuery);
    testResults.push({
      api: 'POST /api/v1/doctors/paged',
      status: pagedResult.success ? '✅ 成功' : '❌ 失败',
      response: pagedResult.data || pagedResult.error
    });
    console.log(`   结果: ${pagedResult.success ? '成功' : '失败'}`);
    if (pagedResult.success && pagedResult.data) {
      console.log(`   - 总记录数: ${pagedResult.data.totalCount}`);
      console.log(`   - 当前页: ${pagedResult.data.currentPage}\n`);
    }

    // 4. 测试新增医生
    console.log('📋 步骤4: 新增医生');
    const createData = {
      realName: '测试医生_' + Date.now(),
      pinYinCode: 'CSYS',
      gender: 0, // Male
      title: 2, // AttendingPhysician
      phoneNumber: '138' + Math.floor(Math.random() * 100000000).toString().padStart(8, '0'),
      specialty: '中医科',
      licenseNumber: 'LIC' + Date.now(),
      status: 0, // Active
      workStatus: 0, // Clinic
      remark: '测试医生备注'
    };
    
    const createResult = await apiRequest('POST', '/api/v1/doctors/add', token, createData);
    testResults.push({
      api: 'POST /api/v1/doctors/add',
      status: createResult.success ? '✅ 成功' : '❌ 失败',
      response: createResult.data || createResult.error
    });
    
    let doctorId = null;
    if (createResult.success && createResult.data && createResult.data.id) {
      doctorId = createResult.data.id;
      console.log(`✅ 医生创建成功: ${createData.realName} (ID: ${doctorId})\n`);
    } else {
      console.log('❌ 创建医生失败:', createResult.error, '\n');
    }

    // 5. 如果创建成功，继续测试其他功能
    if (doctorId) {
      // 测试获取医生详情
      console.log('📋 步骤5: 获取医生详情');
      const detailResult = await apiRequest('GET', `/api/v1/doctors/${doctorId}`, token);
      testResults.push({
        api: `GET /api/v1/doctors/${doctorId}`,
        status: detailResult.success ? '✅ 成功' : '❌ 失败',
        response: detailResult.data || detailResult.error
      });
      if (detailResult.success && detailResult.data) {
        console.log('✅ 医生详情查询成功:');
        console.log(`   - 姓名: ${detailResult.data.realName}`);
        console.log(`   - 科室: ${detailResult.data.specialty}`);
        console.log(`   - 职称: ${detailResult.data.title}`);
        console.log(`   - 状态: ${detailResult.data.status}\n`);
      }

      // 测试更新医生信息
      console.log('📋 步骤6: 更新医生信息');
      const updateData = {
        ...createData,
        id: doctorId,
        realName: createData.realName + '_已更新',
        remark: '更新后的备注信息'
      };
      const updateResult = await apiRequest('PUT', `/api/v1/doctors/${doctorId}`, token, updateData);
      testResults.push({
        api: `PUT /api/v1/doctors/${doctorId}`,
        status: updateResult.success ? '✅ 成功' : '❌ 失败',
        response: updateResult.data || updateResult.error
      });
      console.log(`   结果: ${updateResult.success ? '✅ 医生信息更新成功' : '❌ 更新失败'}\n`);

      // 测试禁用医生
      console.log('📋 步骤7: 禁用医生');
      const disableResult = await apiRequest('PATCH', `/api/v1/doctors/${doctorId}/disable`, token);
      testResults.push({
        api: `PATCH /api/v1/doctors/${doctorId}/disable`,
        status: disableResult.success ? '✅ 成功' : '❌ 失败',
        response: disableResult.data || disableResult.error
      });
      console.log(`   结果: ${disableResult.success ? '✅ 医生已禁用' : '❌ 禁用失败'}\n`);

      // 测试启用医生
      console.log('📋 步骤8: 启用医生');
      const enableResult = await apiRequest('PATCH', `/api/v1/doctors/${doctorId}/enable`, token);
      testResults.push({
        api: `PATCH /api/v1/doctors/${doctorId}/enable`,
        status: enableResult.success ? '✅ 成功' : '❌ 失败',
        response: enableResult.data || enableResult.error
      });
      console.log(`   结果: ${enableResult.success ? '✅ 医生已启用' : '❌ 启用失败'}\n`);

      // 测试切换状态
      console.log('📋 步骤9: 切换医生状态');
      const toggleResult = await apiRequest('PATCH', `/api/v1/doctors/${doctorId}/toggle-status`, token);
      testResults.push({
        api: `PATCH /api/v1/doctors/${doctorId}/toggle-status`,
        status: toggleResult.success ? '✅ 成功' : '❌ 失败',
        response: toggleResult.data || toggleResult.error
      });
      console.log(`   结果: ${toggleResult.success ? '✅ 状态切换成功' : '❌ 切换失败'}\n`);

      // 测试删除医生（软删除）
      console.log('📋 步骤10: 删除医生（软删除）');
      const deleteResult = await apiRequest('DELETE', `/api/v1/doctors/${doctorId}`, token);
      testResults.push({
        api: `DELETE /api/v1/doctors/${doctorId}`,
        status: deleteResult.success ? '✅ 成功' : '❌ 失败',
        response: deleteResult.data || deleteResult.error
      });
      console.log(`   结果: ${deleteResult.success ? '✅ 医生已删除' : '❌ 删除失败'}\n`);
    }

    // 6. 测试搜索功能
    console.log('📋 步骤11: 测试搜索功能');
    const searchResult = await apiRequest('GET', '/api/v1/doctors/search?keyword=测试', token);
    testResults.push({
      api: 'GET /api/v1/doctors/search',
      status: searchResult.success ? '✅ 成功' : '❌ 失败',
      response: searchResult.data || searchResult.error
    });
    if (searchResult.success && searchResult.data) {
      console.log(`   - 搜索到 ${searchResult.data.length} 条记录\n`);
    }

    // 打印测试总结
    console.log('========================================');
    console.log('  测试结果总结');
    console.log('========================================');
    testResults.forEach((result, index) => {
      console.log(`${index + 1}. ${result.api} - ${result.status}`);
      if (!result.status.includes('成功')) {
        console.log(`   错误: ${JSON.stringify(result.response)}`);
      }
    });

    const successCount = testResults.filter(r => r.status.includes('成功')).length;
    const failCount = testResults.filter(r => r.status.includes('失败')).length;
    
    console.log('\n========================================');
    console.log(`  总计: ${testResults.length} 个测试`);
    console.log(`  成功: ${successCount} 个`);
    console.log(`  失败: ${failCount} 个`);
    console.log('========================================');

    if (failCount === 0) {
      console.log('\n✅ 所有医生管理功能测试通过！');
    } else {
      console.log(`\n⚠️ 有 ${failCount} 个测试失败，请检查。`);
    }

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
testDoctorAPIs();