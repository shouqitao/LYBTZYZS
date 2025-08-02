const http = require('http');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

// API配置
const API_BASE = 'http://localhost:5297/api/v1';

// 测试数据
const testData = {
    login: {
        username: 'sysadmin',
        password: 'Admin@123456',
        rememberMe: false
    }
};

// HTTP请求函数
async function apiRequest(method, path, token = null, data = null) {
    return new Promise((resolve, reject) => {
        const postData = data ? JSON.stringify(data) : null;
        const options = {
            hostname: 'localhost',
            port: 5297,
            path: `/api/v1${path}`,
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
                resolve({
                    status: res.statusCode,
                    headers: res.headers,
                    body: body
                });
            });
        });

        req.on('error', reject);
        if (postData) req.write(postData);
        req.end();
    });
}

// 获取认证Token
async function getAuthToken() {
    console.log('🔐 正在登录获取Token...');
    const response = await apiRequest('POST', '/auth/login', null, testData.login);
    if (response.status === 200) {
        const data = JSON.parse(response.body);
        if (data.success) {
            console.log('✅ 登录成功');
            return data.data.token;
        }
    }
    throw new Error(`登录失败: ${response.body}`);
}

// 测试患者模块
async function testPatient(token, patientData, testName) {
    console.log(`\n📌 测试: ${testName}`);
    console.log('发送数据:', JSON.stringify(patientData, null, 2));
    
    const response = await apiRequest('POST', '/patients/add', token, patientData);
    console.log(`响应状态码: ${response.status}`);
    console.log('响应内容:', response.body);
    
    if (response.status === 200) {
        console.log('✅ 患者新增成功！');
    } else if (response.status === 400) {
        console.log('❌ 患者新增失败 - 400错误');
        try {
            const errorData = JSON.parse(response.body);
            if (errorData.errors) {
                console.log('验证错误:', JSON.stringify(errorData.errors, null, 2));
            }
        } catch (e) {}
    }
}

// 主函数
async function main() {
    console.log('🚀 开始调试患者模块400错误');
    console.log('API地址:', API_BASE);
    console.log('=' .repeat(60));
    
    try {
        const token = await getAuthToken();
        
        // 测试1: test_fixed_400_errors.js中的数据（成功）
        const workingData = {
            name: '测试患者',
            gender: 1,
            age: 30,
            idCard: '123456789012345678',
            phoneNumber: '13800000002',
            address: '测试地址'
        };
        await testPatient(token, workingData, '使用成功的数据格式');
        
        // 测试2: test_api_routes.js中的数据（可能失败）
        const failingData = {
            name: '测试患者',
            gender: 1,
            age: 30,
            idCard: '123456789012345678',
            phoneNumber: '13800000002',
            address: '测试地址'
        };
        await testPatient(token, failingData, '使用API路由测试的数据格式');
        
        // 测试3: 查看是否是重复数据问题
        const uniqueData = {
            name: '唯一测试患者' + Date.now(),
            gender: 1,
            age: 30,
            idCard: '98765432101234567' + Math.floor(Math.random() * 10),
            phoneNumber: '138' + Math.floor(Math.random() * 100000000).toString().padStart(8, '0'),
            address: '测试地址'
        };
        await testPatient(token, uniqueData, '使用唯一数据');
        
        console.log('\n' + '='.repeat(60));
        console.log('✅ 调试完成');
        
    } catch (error) {
        console.error('❌ 测试失败:', error.message);
    }
}

main();