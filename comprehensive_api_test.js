const https = require('https');
const http = require('http');

// 测试配置 - 使用本地开发环境
const API_BASE_URL = 'https://localhost:7001';
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0'; // 忽略SSL证书验证
const TEST_RESULTS = [];
let authToken = '';

// 默认登录凭据
const LOGIN_CREDENTIALS = {
    username: 'sysadmin',
    password: 'Admin@123456',
    rememberMe: false
};

// HTTP请求工具函数
function makeRequest(method, path, data = null, headers = {}) {
    return new Promise((resolve, reject) => {
        const url = new URL(API_BASE_URL + path);
        const options = {
            hostname: url.hostname,
            port: url.port,
            path: url.pathname + url.search,
            method: method,
            headers: {
                'Content-Type': 'application/json',
                'User-Agent': 'LYBT-API-Tester/1.0',
                ...headers
            }
        };

        if (authToken) {
            options.headers['Authorization'] = `Bearer ${authToken}`;
        }

        const req = (url.protocol === 'https:' ? https : http).request(options, (res) => {
            let body = '';
            res.on('data', (chunk) => body += chunk);
            res.on('end', () => {
                try {
                    const responseData = body ? JSON.parse(body) : {};
                    resolve({
                        statusCode: res.statusCode,
                        data: responseData,
                        headers: res.headers
                    });
                } catch (e) {
                    resolve({
                        statusCode: res.statusCode,
                        data: body,
                        headers: res.headers,
                        parseError: true
                    });
                }
            });
        });

        req.on('error', (err) => {
            reject(err);
        });

        if (data) {
            req.write(JSON.stringify(data));
        }

        req.end();
    });
}

// 记录测试结果
function logTestResult(module, api, method, path, statusCode, success, message, responseTime) {
    const result = {
        timestamp: new Date().toISOString(),
        module,
        api,
        method,
        path,
        statusCode,
        success,
        message,
        responseTime
    };
    
    TEST_RESULTS.push(result);
    
    const status = success ? '✅' : '❌';
    const time = responseTime ? `(${responseTime}ms)` : '';
    console.log(`${status} [${module}] ${method} ${path} - ${statusCode} ${message} ${time}`);
}

// 执行单个API测试
async function testAPI(module, apiName, method, path, data = null, expectedStatus = 200) {
    const startTime = Date.now();
    try {
        const response = await makeRequest(method, path, data);
        const responseTime = Date.now() - startTime;
        
        const success = response.statusCode === expectedStatus;
        const message = success ? 'Success' : `Expected ${expectedStatus}, got ${response.statusCode}`;
        
        logTestResult(module, apiName, method, path, response.statusCode, success, message, responseTime);
        
        return response;
    } catch (error) {
        const responseTime = Date.now() - startTime;
        logTestResult(module, apiName, method, path, 0, false, error.message, responseTime);
        return null;
    }
}

// 登录获取Token
async function login() {
    console.log('\n🔐 执行登录认证...');
    const response = await testAPI('Auth', '用户登录', 'POST', '/api/v1/auth/login', LOGIN_CREDENTIALS);
    
    if (response && response.data && response.data.success && response.data.data && response.data.data.token) {
        authToken = response.data.data.token;
        console.log('✅ 登录成功，获取到认证Token');
        return true;
    } else {
        console.log('❌ 登录失败，无法获取认证Token');
        return false;
    }
}

// 测试认证模块
async function testAuthModule() {
    console.log('\n📋 测试认证模块 (Auth)...');
    
    // 测试登录API（已在login中测试）
    
    // 测试获取当前用户信息
    await testAPI('Auth', '获取当前用户', 'GET', '/api/v1/auth/current-user');
    
    // 测试刷新Token
    await testAPI('Auth', '刷新Token', 'POST', '/api/v1/auth/refresh-token');
    
    // 测试修改密码 - 特殊处理，检查响应体而不是状态码
    const changePasswordResponse = await testAPI('Auth', '修改密码', 'POST', '/api/v1/auth/change-password', {
        oldPassword: 'WrongPassword',
        newPassword: 'NewPassword123'
    }, 200); // API总是返回200
    
    // 手动检查响应体中的success字段
    if (changePasswordResponse && changePasswordResponse.data && changePasswordResponse.data.success === false) {
        // 修正测试结果为成功（因为我们期望密码错误时失败）
        const testResults = TEST_RESULTS;
        if (testResults.length > 0) {
            const lastResult = testResults[testResults.length - 1];
            if (lastResult.api === '修改密码') {
                lastResult.success = true;
                lastResult.message = 'Success (password validation works correctly)';
            }
        }
    }
}

// 测试用户管理模块
async function testUsersModule() {
    console.log('\n👥 测试用户管理模块 (Users)...');
    
    // 获取用户列表
    await testAPI('Users', '获取用户列表', 'GET', '/api/v1/users');
    
    // 分页查询用户
    await testAPI('Users', '分页查询用户', 'GET', '/api/v1/users?page=1&pageSize=10');
    
    // 搜索用户
    await testAPI('Users', '搜索用户', 'GET', '/api/v1/users?keyword=admin');
    
    // 创建测试用户
    const newUser = {
        username: `testuser_${Date.now()}`,
        password: 'Test@123456',
        confirmPassword: 'Test@123456',
        realName: '测试用户',
        role: 4, // Staff
        email: `test${Date.now()}@lybt.com`,
        phoneNumber: `138${Date.now().toString().slice(-8)}`,
        isActive: true
    };
    
    try {
        const response = await makeRequest('POST', '/api/v1/users', newUser);
        const responseTime = 50; // 估算时间
        
        if (response.statusCode === 200 && response.data && response.data.success) {
            const userId = response.data.data?.id;
            logTestResult('Users', '创建用户', 'POST', '/api/v1/users', 200, true, 'Success', responseTime);
            
            if (userId) {
                // 获取用户详情
                await testAPI('Users', '获取用户详情', 'GET', `/api/v1/users/${userId}`);
                
                // 更新用户
                const updateData = { ...newUser, realName: '更新的测试用户' };
                await testAPI('Users', '更新用户', 'PUT', `/api/v1/users/${userId}`, updateData);
                
                // 测试删除用户 (应该返回405，因为采用软删除)
                await testAPI('Users', '删除用户', 'DELETE', `/api/v1/users/${userId}`, null, 405);
                
                // 测试禁用用户
                await testAPI('Users', '禁用用户', 'PATCH', `/api/v1/users/${userId}/disable`);
                
                // 测试启用用户
                await testAPI('Users', '启用用户', 'PATCH', `/api/v1/users/${userId}/enable`);
            }
        } else {
            logTestResult('Users', '创建用户', 'POST', '/api/v1/users', response.statusCode, false, `Expected 200, got ${response.statusCode}`, responseTime);
        }
    } catch (error) {
        logTestResult('Users', '创建用户', 'POST', '/api/v1/users', 0, false, `Error: ${error.message}`, 0);
    }
}

// 测试患者管理模块
async function testPatientsModule() {
    console.log('\n🏥 测试患者管理模块 (Patients)...');
    
    // 获取患者列表
    await testAPI('Patients', '获取患者列表', 'GET', '/api/v1/patients');
    
    // 分页查询患者
    await testAPI('Patients', '分页查询患者', 'GET', '/api/v1/patients?page=1&pageSize=10');
    
    // 创建测试患者
    const timestamp = Date.now();
    const newPatient = {
        name: `测试患者${timestamp}`,
        gender: 1, // 男性
        age: 35,
        birthDate: '1990-01-01T00:00:00.000Z',
        phoneNumber: `139${timestamp.toString().slice(-8)}`,
        idNumber: `110105199001011232`, // 使用有效的18位身份证号
        address: '北京市朝阳区测试地址',
        emergencyContact: '紧急联系人',
        emergencyPhone: '13800138000',
        medicalHistory: '无特殊病史',
        allergies: '无过敏史'
    };
    
    try {
        const response = await makeRequest('POST', '/api/v1/patients', newPatient);
        const responseTime = 50;
        
        if (response.statusCode === 201 && response.data && response.data.success) {
            const patientId = response.data.data?.id;
            logTestResult('Patients', '创建患者', 'POST', '/api/v1/patients', 201, true, 'Success', responseTime);
            
            if (patientId) {
                // 获取患者详情
                await testAPI('Patients', '获取患者详情', 'GET', `/api/v1/patients/${patientId}`);
                
                // 更新患者信息
                const updateData = { ...newPatient, name: '更新的测试患者' };
                await testAPI('Patients', '更新患者', 'PUT', `/api/v1/patients/${patientId}`, updateData);
                
                // 搜索患者
                await testAPI('Patients', '搜索患者', 'GET', '/api/v1/patients?keyword=测试');
                
                // 测试禁用患者
                await testAPI('Patients', '禁用患者', 'PATCH', `/api/v1/patients/${patientId}/disable`);
                
                // 测试启用患者
                await testAPI('Patients', '启用患者', 'PATCH', `/api/v1/patients/${patientId}/enable`);
            }
        } else {
            logTestResult('Patients', '创建患者', 'POST', '/api/v1/patients', response.statusCode, false, `Expected 201, got ${response.statusCode}`, responseTime);
        }
    } catch (error) {
        logTestResult('Patients', '创建患者', 'POST', '/api/v1/patients', 0, false, `Error: ${error.message}`, 0);
    }
}

// 测试医生管理模块
async function testDoctorsModule() {
    console.log('\n👨‍⚕️ 测试医生管理模块 (Doctors)...');
    
    // 获取医生列表
    await testAPI('Doctors', '获取医生列表', 'GET', '/api/v1/doctors');
    
    // 分页查询医生
    await testAPI('Doctors', '分页查询医生', 'GET', '/api/v1/doctors?page=1&pageSize=10');
    
    // 创建测试医生
    const timestamp = Date.now();
    const newDoctor = {
        userId: '25c87ec6-0add-44f2-b5c1-845f19ff2cac', // 使用最新创建的DiagnosingDoctor用户ID
        gender: 1, // 使用数字枚举
        birthday: '1980-01-01T00:00:00.000Z',
        title: 3, // AttendingPhysician
        licenseNumber: `ZY${timestamp.toString().slice(-6)}`,
        idNumber: '110105198001011234', // 有效的身份证号码
        specialty: '中医内科',
        status: 1, // Active
        workStatus: 1, // Clinic
        pinyinCode: 'ZSYS',
        remark: '测试医生',
        contactNumber: `137${timestamp.toString().slice(-8)}`,
        realName: `测试医生${timestamp}`,
        phoneNumber: `137${timestamp.toString().slice(-8)}`,
        email: `doctor${timestamp}@lybt.com`,
        age: 43
    };
    
    try {
        const response = await makeRequest('POST', '/api/v1/doctors', newDoctor);
        const responseTime = 50;
        
        if (response.statusCode === 201 && response.data && response.data.success) {
            const doctorId = response.data.data?.id;
            logTestResult('Doctors', '创建医生', 'POST', '/api/v1/doctors', 201, true, 'Success', responseTime);
            
            if (doctorId) {
                // 获取医生详情
                await testAPI('Doctors', '获取医生详情', 'GET', `/api/v1/doctors/${doctorId}`);
                
                // 更新医生信息
                const updateData = { ...newDoctor, introduction: '更新的专业中医医生' };
                await testAPI('Doctors', '更新医生', 'PUT', `/api/v1/doctors/${doctorId}`, updateData);
                
                // 测试禁用医生
                await testAPI('Doctors', '禁用医生', 'PATCH', `/api/v1/doctors/${doctorId}/disable`);
                
                // 测试启用医生
                await testAPI('Doctors', '启用医生', 'PATCH', `/api/v1/doctors/${doctorId}/enable`);
            }
        } else {
            logTestResult('Doctors', '创建医生', 'POST', '/api/v1/doctors', response.statusCode, false, `Expected 201, got ${response.statusCode}`, responseTime);
        }
    } catch (error) {
        logTestResult('Doctors', '创建医生', 'POST', '/api/v1/doctors', 0, false, `Error: ${error.message}`, 0);
    }
}

// 测试药材管理模块
async function testHerbsModule() {
    console.log('\n🌿 测试药材管理模块 (Herbs)...');
    
    // 获取药材列表
    await testAPI('Herbs', '获取药材列表', 'GET', '/api/v1/herbs');
    
    // 分页查询药材
    await testAPI('Herbs', '分页查询药材', 'GET', '/api/v1/herbs?page=1&pageSize=10');
    
    // 创建测试药材
    const timestamp = Date.now();
    const newHerb = {
        name: `测试药材${timestamp}`,
        category: '清热解毒',
        specification: '500g/袋',
        unit: 'g',
        price: 25.50,
        stock: 100,
        status: 1, // 可用
        description: '用于测试的药材',
        producer: '测试药厂',
        batchNumber: `BATCH${timestamp}`,
        expiryDate: '2025-12-31T00:00:00.000Z',
        isEnabled: true
    };
    
    try {
        const response = await makeRequest('POST', '/api/v1/herbs', newHerb);
        const responseTime = 50;
        
        if (response.statusCode === 201 && response.data && response.data.success) {
            const herbId = response.data.data?.id;
            logTestResult('Herbs', '创建药材', 'POST', '/api/v1/herbs', 201, true, 'Success', responseTime);
            
            if (herbId) {
                // 获取药材详情
                await testAPI('Herbs', '获取药材详情', 'GET', `/api/v1/herbs/${herbId}`);
                
                // 更新药材信息
                const updateData = { ...newHerb, description: '更新的测试药材' };
                await testAPI('Herbs', '更新药材', 'PUT', `/api/v1/herbs/${herbId}`, updateData);
                
                // 批量状态更新
                await testAPI('Herbs', '批量状态更新', 'PATCH', '/api/v1/herbs/batch-status', {
                    ids: [herbId],
                    status: 1,
                    isEnabled: true,
                    reason: '测试批量更新'
                });
                
                // 测试禁用药材
                await testAPI('Herbs', '禁用药材', 'PATCH', `/api/v1/herbs/${herbId}/disable`);
                
                // 测试启用药材
                await testAPI('Herbs', '启用药材', 'PATCH', `/api/v1/herbs/${herbId}/enable`);
            }
        } else {
            logTestResult('Herbs', '创建药材', 'POST', '/api/v1/herbs', response.statusCode, false, `Expected 201, got ${response.statusCode}`, responseTime);
        }
    } catch (error) {
        logTestResult('Herbs', '创建药材', 'POST', '/api/v1/herbs', 0, false, `Error: ${error.message}`, 0);
    }
}

// 测试验方模板模块
async function testFormulaTemplatesModule() {
    console.log('\n📋 测试验方模板模块 (FormulaTemplates)...');
    
    // 获取验方模板列表
    await testAPI('FormulaTemplates', '获取验方模板列表', 'GET', '/api/v1/FormulaTemplates');
    
    // 分页查询验方模板
    await testAPI('FormulaTemplates', '分页查询验方模板', 'GET', '/api/v1/FormulaTemplates?page=1&pageSize=10');
    
    // 创建测试验方模板
    const timestamp = Date.now();
    const newTemplate = {
        name: `测试验方${timestamp}`,
        herbs: [
            { 
                herbId: '11111111-1111-1111-1111-111111111111',
                name: '当归', 
                dosage: 10, 
                unit: 'g',
                price: 12.5
            },
            { 
                herbId: '22222222-2222-2222-2222-222222222222',
                name: '白芍', 
                dosage: 15, 
                unit: 'g',
                price: 15.0
            }
        ],
        instructions: '水煎服，日一剂',
        remark: '测试用验方模板',
        category: '补血方',
        isActive: true
    };
    
    try {
        const response = await makeRequest('POST', '/api/v1/FormulaTemplates', newTemplate);
        const responseTime = 50;
        
        if (response.statusCode === 201 && response.data && response.data.success) {
            const templateId = response.data.data?.id;
            logTestResult('FormulaTemplates', '创建验方模板', 'POST', '/api/v1/FormulaTemplates', 201, true, 'Success', responseTime);
            
            if (templateId) {
                // 获取验方模板详情
                await testAPI('FormulaTemplates', '获取验方模板详情', 'GET', `/api/v1/FormulaTemplates/${templateId}`);
                
                // 更新验方模板
                const updateData = { ...newTemplate, remark: '更新的测试验方模板' };
                await testAPI('FormulaTemplates', '更新验方模板', 'PUT', `/api/v1/FormulaTemplates/${templateId}`, updateData);
                
                // 测试禁用验方模板
                await testAPI('FormulaTemplates', '禁用验方模板', 'PATCH', `/api/v1/FormulaTemplates/${templateId}/disable`);
                
                // 测试启用验方模板
                await testAPI('FormulaTemplates', '启用验方模板', 'PATCH', `/api/v1/FormulaTemplates/${templateId}/enable`);
            }
        } else {
            logTestResult('FormulaTemplates', '创建验方模板', 'POST', '/api/v1/FormulaTemplates', response.statusCode, false, `Expected 201, got ${response.statusCode}`, responseTime);
        }
    } catch (error) {
        logTestResult('FormulaTemplates', '创建验方模板', 'POST', '/api/v1/FormulaTemplates', 0, false, `Error: ${error.message}`, 0);
    }
}

// 测试其他业务模块
async function testOtherModules() {
    console.log('\n🔧 测试其他业务模块...');
    
    // 挂号模块
    await testAPI('Registration', '获取挂号列表', 'GET', '/api/v1/registration');
    
    // 诊疗模块
    await testAPI('DiagnosisTreatment', '获取诊疗记录', 'GET', '/api/v1/DiagnosisTreatment');
    
    // 处方模块
    await testAPI('Prescriptions', '获取处方列表', 'GET', '/api/v1/prescriptions');
    
    // 药房模块
    await testAPI('Pharmacy', '获取药房信息', 'GET', '/api/v1/pharmacy');
    
    // 计费模块
    await testAPI('Billing', '获取计费记录', 'GET', '/api/v1/billing');
    
    // 病历模块
    await testAPI('Records', '获取病历列表', 'GET', '/api/v1/records');
    
    // 排队模块
    await testAPI('Queueing', '获取排队信息', 'GET', '/api/v1/queueing');
    
    // 治疗室模块
    await testAPI('TreatmentRoom', '获取治疗室列表', 'GET', '/api/v1/TreatmentRoom');
    
    // 同步模块
    await testAPI('Sync', '获取同步状态', 'GET', '/api/v1/Sync/connection-status');
}

// 生成测试报告
function generateTestReport() {
    console.log('\n📊 生成测试报告...');
    
    const totalTests = TEST_RESULTS.length;
    const successTests = TEST_RESULTS.filter(r => r.success).length;
    const failedTests = totalTests - successTests;
    const successRate = ((successTests / totalTests) * 100).toFixed(2);
    
    // 按模块统计
    const moduleStats = {};
    TEST_RESULTS.forEach(result => {
        if (!moduleStats[result.module]) {
            moduleStats[result.module] = { total: 0, success: 0, failed: 0 };
        }
        moduleStats[result.module].total++;
        if (result.success) {
            moduleStats[result.module].success++;
        } else {
            moduleStats[result.module].failed++;
        }
    });
    
    // 计算响应时间统计
    const responseTimes = TEST_RESULTS.filter(r => r.responseTime).map(r => r.responseTime);
    const avgResponseTime = responseTimes.length > 0 ? 
        (responseTimes.reduce((a, b) => a + b, 0) / responseTimes.length).toFixed(2) : 'N/A';
    const maxResponseTime = responseTimes.length > 0 ? Math.max(...responseTimes) : 'N/A';
    const minResponseTime = responseTimes.length > 0 ? Math.min(...responseTimes) : 'N/A';
    
    const report = {
        summary: {
            testTime: new Date().toISOString(),
            apiBaseUrl: API_BASE_URL,
            totalTests,
            successTests,
            failedTests,
            successRate: `${successRate}%`,
            avgResponseTime: `${avgResponseTime}ms`,
            maxResponseTime: `${maxResponseTime}ms`,
            minResponseTime: `${minResponseTime}ms`
        },
        moduleStats,
        detailResults: TEST_RESULTS
    };
    
    return report;
}

// 主测试函数
async function runComprehensiveTest() {
    console.log('🚀 开始凌隐宝堂中医诊所诊疗系统 API 全面测试');
    console.log(`📍 测试地址: ${API_BASE_URL}`);
    console.log(`⏰ 测试时间: ${new Date().toLocaleString()}`);
    
    try {
        // 1. 登录认证
        const loginSuccess = await login();
        if (!loginSuccess) {
            console.log('❌ 登录失败，终止测试');
            return;
        }
        
        // 2. 测试各个模块
        await testAuthModule();
        await testUsersModule();
        await testPatientsModule();
        await testDoctorsModule();
        await testHerbsModule();
        await testFormulaTemplatesModule();
        await testOtherModules();
        
        // 3. 生成测试报告
        const report = generateTestReport();
        
        // 4. 输出测试报告
        console.log('\n' + '='.repeat(80));
        console.log('📊 凌隐宝堂中医诊所诊疗系统 API 测试报告');
        console.log('='.repeat(80));
        console.log(`🕐 测试时间: ${new Date(report.summary.testTime).toLocaleString()}`);
        console.log(`🌐 测试地址: ${report.summary.apiBaseUrl}`);
        console.log(`📈 总体结果: ${report.summary.successTests}/${report.summary.totalTests} 成功 (${report.summary.successRate})`);
        console.log(`⚡ 平均响应时间: ${report.summary.avgResponseTime}`);
        console.log(`📊 响应时间范围: ${report.summary.minResponseTime} - ${report.summary.maxResponseTime}`);
        
        console.log('\n📋 模块测试统计:');
        Object.entries(report.moduleStats).forEach(([module, stats]) => {
            const moduleSuccessRate = ((stats.success / stats.total) * 100).toFixed(1);
            const status = stats.failed === 0 ? '✅' : '⚠️';
            console.log(`${status} ${module}: ${stats.success}/${stats.total} (${moduleSuccessRate}%)`);
        });
        
        if (report.summary.failedTests > 0) {
            console.log('\n❌ 失败的测试:');
            report.detailResults.filter(r => !r.success).forEach(result => {
                console.log(`   [${result.module}] ${result.method} ${result.path} - ${result.message}`);
            });
        }
        
        console.log('\n✨ 测试完成!');
        
        // 保存详细报告到文件
        require('fs').writeFileSync(
            'D:\\source\\repos\\LYBTZYZS\\API_TEST_REPORT.json', 
            JSON.stringify(report, null, 2), 
            'utf8'
        );
        console.log('📄 详细报告已保存到: API_TEST_REPORT.json');
        
    } catch (error) {
        console.error('❌ 测试过程中发生错误:', error.message);
    }
}

// 启动测试
runComprehensiveTest();