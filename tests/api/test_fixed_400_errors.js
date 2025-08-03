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
    },
    patient: {
        name: '测试患者',
        gender: 1,
        age: 30,
        idCard: '123456789012345678',
        phoneNumber: '13800000002',
        address: '测试地址'
    },
    doctor: {
        userId: null, // 将在测试中设置
        title: 3, // AttendingPhysician = 3 (主治医师)
        department: '中医科',
        specialty: '内科',
        licenseNumber: 'TEST001',
        biography: '测试医生简介'
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
async function testPatient(token) {
    console.log('\n📌 测试患者模块新增接口（修复后）');
    console.log('发送数据:', JSON.stringify(testData.patient, null, 2));
    
    const response = await apiRequest('POST', '/patients/add', token, testData.patient);
    console.log(`\n响应状态码: ${response.status}`);
    console.log('响应内容:', response.body);
    
    if (response.status === 200) {
        console.log('✅ 患者新增成功！');
    } else {
        console.log('❌ 患者新增失败');
    }
}

// 测试医生模块
async function testDoctor(token) {
    console.log('\n📌 测试医生模块新增接口（修复后）');
    
    // 先创建一个用户
    const doctorUser = {
        userName: `doctor_fixed_${Date.now()}`,
        realName: '测试医生修复',
        email: 'testdoctorfix@example.com',
        role: 1, // DiagnosingDoctor
        isActive: true,
        phoneNumber: '13800138004'
    };
    
    console.log('1. 先创建医生用户...');
    const userResponse = await apiRequest('POST', '/users/add', token, doctorUser);
    if (userResponse.status !== 200) {
        console.log('创建用户失败:', userResponse.body);
        return;
    }
    
    // 获取创建的用户ID
    const listResponse = await apiRequest('POST', '/users/paged', token, {
        currentPage: 1,
        pageSize: 100,
        keyword: doctorUser.userName
    });
    
    if (listResponse.status === 200) {
        const listData = JSON.parse(listResponse.body);
        if (listData.data?.items?.length > 0) {
            const userId = listData.data.items[0].id;
            testData.doctor.userId = userId;
            console.log('✅ 用户创建成功，ID:', userId);
            
            console.log('\n2. 创建医生档案...');
            console.log('发送数据:', JSON.stringify(testData.doctor, null, 2));
            
            const response = await apiRequest('POST', '/doctors/add', token, testData.doctor);
            console.log(`\n响应状态码: ${response.status}`);
            console.log('响应内容:', response.body);
            
            if (response.status === 200) {
                console.log('✅ 医生新增成功！');
            } else {
                console.log('❌ 医生新增失败');
            }
        }
    }
}

// 主函数
async function main() {
    console.log('🚀 开始测试修复后的API');
    console.log('API地址:', API_BASE);
    console.log('=' .repeat(60));
    
    try {
        const token = await getAuthToken();
        
        // 等待一秒确保AutoMapper配置已重新加载
        console.log('\n⏳ 等待服务重新加载配置...');
        await new Promise(resolve => setTimeout(resolve, 1000));
        
        await testPatient(token);
        await testDoctor(token);
        
        console.log('\n' + '='.repeat(60));
        console.log('✅ 测试完成');
        
    } catch (error) {
        console.error('❌ 测试失败:', error.message);
    }
}

main();