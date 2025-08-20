const http = require('http');

// UltraThink v2.0 认证功能测试
console.log('🔐 开始UltraThink v2.0 认证功能测试');

function apiRequest(method, path, data = null, token = null) {
    return new Promise((resolve) => {
        const postData = data ? JSON.stringify(data) : null;
        const options = {
            hostname: 'localhost',
            port: 5000,
            path: path,
            method: method,
            headers: {
                'Content-Type': 'application/json',
                ...(token && { 'Authorization': `Bearer ${token}` }),
                ...(postData && { 'Content-Length': Buffer.byteLength(postData) })
            }
        };

        const req = http.request(options, (res) => {
            let body = '';
            res.on('data', (chunk) => body += chunk);
            res.on('end', () => {
                try {
                    resolve({
                        success: res.statusCode >= 200 && res.statusCode < 300,
                        statusCode: res.statusCode,
                        data: body ? JSON.parse(body) : null,
                        error: null
                    });
                } catch (e) {
                    resolve({
                        success: res.statusCode >= 200 && res.statusCode < 300,
                        statusCode: res.statusCode,
                        data: body,
                        error: `解析失败: ${e.message}`
                    });
                }
            });
        });

        req.on('error', (err) => {
            resolve({
                success: false,
                error: `连接错误: ${err.message}`
            });
        });

        if (postData) req.write(postData);
        req.end();
    });
}

async function testAuthentication() {
    console.log('\n📋 步骤1: 测试登录API');
    
    // 使用系统默认管理员账户
    const loginData = {
        username: 'sysadmin',
        password: 'Admin@123456',
        rememberMe: false
    };
    
    const loginResult = await apiRequest('POST', '/api/v1/auth/login', loginData);
    
    if (!loginResult.success) {
        console.log(`❌ 登录失败: ${loginResult.error || loginResult.statusCode}`);
        console.log('原始响应:', loginResult.data);
        return;
    }
    
    console.log('✅ 登录成功!');
    
    // 提取Token
    let token = null;
    if (loginResult.data && loginResult.data.success && loginResult.data.data && loginResult.data.data.token) {
        token = loginResult.data.data.token;
        console.log('✅ Token获取成功');
    } else {
        console.log('❌ Token提取失败');
        console.log('登录响应:', JSON.stringify(loginResult.data, null, 2));
        return;
    }
    
    console.log('\n📋 步骤2: 使用Token测试受保护的API');
    
    // 测试中药材API
    const herbsResult = await apiRequest('GET', '/api/v1/herbs', null, token);
    console.log(`中药材API: ${herbsResult.success ? '✅ 成功' : '❌ 失败'} (状态码: ${herbsResult.statusCode})`);
    
    // 测试用户API
    const usersResult = await apiRequest('GET', '/api/v1/users', null, token);
    console.log(`用户API: ${usersResult.success ? '✅ 成功' : '❌ 失败'} (状态码: ${usersResult.statusCode})`);
    
    console.log('\n🎯 认证测试总结:');
    const tests = [
        { name: '登录功能', success: loginResult.success },
        { name: 'Token提取', success: !!token },
        { name: '中药材API', success: herbsResult.success },
        { name: '用户API', success: usersResult.success }
    ];
    
    tests.forEach((test, index) => {
        console.log(`${index + 1}. ${test.name}: ${test.success ? '✅ 成功' : '❌ 失败'}`);
    });
    
    const successCount = tests.filter(t => t.success).length;
    console.log(`\n📊 成功率: ${successCount}/${tests.length} (${Math.round(successCount/tests.length*100)}%)`);
    
    if (successCount === tests.length) {
        console.log('🎉 UltraThink v2.0 认证系统工作正常!');
    }
}

// 延迟执行
setTimeout(testAuthentication, 2000);