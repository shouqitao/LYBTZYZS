const https = require('https');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

async function simpleUserUpdateTest() {
  try {
    console.log('简单用户更新测试...\n');

    // 1. 获取Token
    const token = await getAuthToken();
    console.log('✅ 获取Token成功\n');

    // 2. 创建新用户
    console.log('📝 创建测试用户...');
    const newUserData = {
      Username: 'testuser_' + Date.now(),
      RealName: '测试用户',
      Email: 'test@example.com',
      Phone: '13900000000',
      Role: 'Doctor',
      IsActive: true,
      Remark: '测试创建'
    };

    const createResponse = await apiRequest('POST', '/api/v1/users', token, newUserData);
    console.log('创建用户响应:', createResponse);
    
    if (!createResponse.success) {
      console.log('❌ 创建用户失败:', createResponse.error);
      return;
    }

    const createdUserId = createResponse.data && createResponse.data.data ? createResponse.data.data.id : createResponse.data.id;
    console.log('✅ 用户创建成功，ID:', createdUserId);

    // 3. 立即查询验证创建
    const getUserResponse = await apiRequest('GET', `/api/v1/users/${createdUserId}`, token);
    if (getUserResponse.success) {
      const user = getUserResponse.data.data;
      console.log('📊 创建后的用户信息:');
      console.log(`   用户名: ${user.username}`);
      console.log(`   真实姓名: ${user.realName}`);
      console.log(`   邮箱: ${user.email}`);
      console.log(`   手机: ${user.phoneNumber}`);
      console.log(`   角色: ${user.role}`);
      console.log(`   备注: ${user.remark}`);
    }

    // 4. 更新用户数据
    console.log('\n🔄 测试更新用户数据...');
    const updateData = {
      Id: createdUserId,
      Username: newUserData.Username,
      RealName: '测试用户_已更新',
      Email: 'updated@example.com',
      Phone: '13800000000',
      Role: 'Doctor',
      IsActive: true,
      Remark: '测试更新完成'
    };

    const updateResponse = await apiRequest('PUT', `/api/v1/users/${createdUserId}`, token, updateData);
    console.log('更新响应状态:', updateResponse.statusCode);
    console.log('更新响应内容:', updateResponse.data);

    if (updateResponse.success) {
      console.log('✅ 更新请求成功');
      
      // 5. 再次查询验证更新
      console.log('\n🔍 验证更新结果...');
      await new Promise(resolve => setTimeout(resolve, 1000)); // 等待1秒
      
      const verifyResponse = await apiRequest('GET', `/api/v1/users/${createdUserId}`, token);
      if (verifyResponse.success) {
        const updatedUser = verifyResponse.data.data;
        console.log('📊 更新后的用户信息:');
        console.log(`   用户名: ${updatedUser.username}`);
        console.log(`   真实姓名: ${updatedUser.realName}`);
        console.log(`   邮箱: ${updatedUser.email}`);
        console.log(`   手机: ${updatedUser.phoneNumber}`);
        console.log(`   角色: ${updatedUser.role}`);
        console.log(`   备注: ${updatedUser.remark}`);
        
        // 比较更新结果
        console.log('\n📋 更新结果对比:');
        console.log(`   真实姓名: ${updatedUser.realName === updateData.RealName ? '✅' : '❌'} (期望: ${updateData.RealName})`);
        console.log(`   邮箱: ${updatedUser.email === updateData.Email ? '✅' : '❌'} (期望: ${updateData.Email})`);
        console.log(`   手机: ${updatedUser.phoneNumber === updateData.Phone ? '✅' : '❌'} (期望: ${updateData.Phone})`);
        console.log(`   备注: ${updatedUser.remark === updateData.Remark ? '✅' : '❌'} (期望: ${updateData.Remark})`);

        if (updatedUser.realName === updateData.RealName && 
            updatedUser.phoneNumber === updateData.Phone &&
            updatedUser.email === updateData.Email) {
          console.log('\n🎉 用户数据更新测试完全成功！');
        } else {
          console.log('\n⚠️ 用户数据更新不完整，API可能存在实现问题');
        }
      }
    } else {
      console.log('❌ 更新请求失败:', updateResponse.error);
    }

    // 6. 清理测试数据
    console.log('\n🧹 清理测试数据...');
    const deleteResponse = await apiRequest('DELETE', `/api/v1/users/${createdUserId}`, token);
    if (deleteResponse.success) {
      console.log('✅ 测试用户已删除');
    } else {
      console.log('⚠️ 测试用户删除失败，请手动清理:', createdUserId);
    }

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

simpleUserUpdateTest();