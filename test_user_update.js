const https = require('https');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

async function testUserDataUpdate() {
  try {
    console.log('开始测试用户数据更新功能...\n');

    // 1. 获取认证Token
    console.log('1. 登录获取认证Token...');
    const token = await getAuthToken();
    console.log('成功获取Token\n');

    // 2. 获取用户列表
    console.log('2. 获取现有用户列表...');
    const usersResponse = await apiRequest('GET', '/api/v1/users', token);
    if (!usersResponse.success) {
      throw new Error('获取用户列表失败: ' + usersResponse.error);
    }
    
    const users = (usersResponse.data && usersResponse.data.data && usersResponse.data.data.items) || [];
    console.log(`获取到 ${users.length} 个用户`);
    
    if (users.length === 0) {
      console.log('没有用户数据，先创建一个测试用户...');
      await createTestUser(token);
      // 重新获取用户列表
      const newUsersResponse = await apiRequest('GET', '/api/v1/users', token);
      if (newUsersResponse.data && newUsersResponse.data.data && newUsersResponse.data.data.items) {
        users.push(...newUsersResponse.data.data.items);
      }
    }

    // 3. 测试更新用户数据
    if (users.length > 0) {
      const testUser = users[0];
      console.log(`\n3. 测试更新用户: ${testUser.userName} (${testUser.realName})`);
      
      console.log('测试用户原始数据:', testUser);
      
      // 准备更新数据 - 使用API期望的字段名
      const updateData = {
        Id: testUser.id,
        Username: testUser.userName || testUser.username,  
        RealName: (testUser.realName || testUser.realname) + '_Updated',
        Email: testUser.email || 'updated@test.com',
        Phone: '13800138000',
        Role: testUser.role,
        IsActive: testUser.isActive !== undefined ? testUser.isActive : true,
        Remark: '测试更新 - ' + new Date().toLocaleString()
      };

      console.log('更新数据:');
      console.log(`   用户名: ${testUser.userName || testUser.username} -> ${updateData.Username}`);
      console.log(`   真实姓名: ${testUser.realName || testUser.realname} -> ${updateData.RealName}`);
      console.log(`   手机号码: ${testUser.phone || '无'} -> ${updateData.Phone}`);
      console.log(`   备注信息: ${testUser.remark || '无'} -> ${updateData.Remark}`);

      // 4. 执行更新操作
      console.log('\n4. 执行用户数据更新...');
      const updateResponse = await apiRequest('PUT', `/api/v1/users/${testUser.id}`, token, updateData);
      
      if (updateResponse.success) {
        console.log('用户数据更新成功!');
        
        // 5. 验证更新结果
        console.log('\n5. 验证更新结果...');
        await new Promise(resolve => setTimeout(resolve, 1000)); // 等待1秒
        
        const verifyResponse = await apiRequest('GET', `/api/v1/users/${testUser.id}`, token);
        if (verifyResponse.success) {
          const updatedUser = verifyResponse.data.data;
          console.log('更新后的用户信息:');
          console.log(`   用户名: ${updatedUser.username || updatedUser.userName}`);
          console.log(`   真实姓名: ${updatedUser.realName || updatedUser.realname}`);
          console.log(`   邮箱: ${updatedUser.email}`);
          console.log(`   手机: ${updatedUser.phoneNumber || updatedUser.phone}`);
          console.log(`   角色: ${updatedUser.role}`);
          console.log(`   状态: ${updatedUser.isActive ? '激活' : '禁用'}`);
          console.log(`   备注: ${updatedUser.remark}`);
          console.log(`   更新时间: ${updatedUser.updateTime}`);
          
          // 验证数据是否正确更新
          const isCorrect = 
            (updatedUser.realName || updatedUser.realname) === updateData.RealName &&
            (updatedUser.phoneNumber || updatedUser.phone) === updateData.Phone;
            
          if (isCorrect) {
            console.log('\n用户数据更新测试成功! 所有字段都正确更新');
          } else {
            console.log('\n数据更新不完整，请检查API实现');
          }
        } else {
          console.log('验证更新结果失败:', verifyResponse.error);
        }
      } else {
        console.log('用户数据更新失败:', updateResponse.error);
        console.log('响应详情:', updateResponse.statusCode, updateResponse.data);
      }
    }

  } catch (error) {
    console.error('测试过程中发生错误:', error.message);
    console.error('错误详情:', error);
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

// 创建测试用户
async function createTestUser(token) {
  const userData = {
    userName: 'testuser_' + Date.now(),
    realName: '测试用户',
    email: 'test@example.com',
    phone: '13900139000',
    role: 'Doctor',
    isActive: true,
    remark: '自动创建的测试用户'
  };

  console.log('创建测试用户:', userData.userName);
  const result = await apiRequest('POST', '/api/v1/users', token, userData);
  
  if (result.success) {
    console.log('测试用户创建成功');
  } else {
    console.log('测试用户创建失败:', result.error);
  }
  
  return result;
}

testUserDataUpdate();