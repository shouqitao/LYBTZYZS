const http = require('http');

// 测试配置
const API_BASE_URL = 'http://192.168.190.243:5000';
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
                'User-Agent': 'LYBT-API-Tester/2.0',
                ...headers
            }
        };

        if (authToken) {
            options.headers['Authorization'] = `Bearer ${authToken}`;
        }

        const req = http.request(options, (res) => {
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
    const response = await testAPI('Auth', '用户登录', 'POST', '/api/v1/Auth/login', LOGIN_CREDENTIALS);
    
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
    
    // 测试Token刷新
    await testAPI('Auth', '刷新Token', 'POST', '/api/v1/Auth/RefreshToken');
    
    // 测试修改密码
    await testAPI('Auth', '修改密码', 'PUT', '/api/v1/Auth/password', {
        oldPassword: 'WrongPassword',
        newPassword: 'NewPassword123',
        confirmPassword: 'NewPassword123'
    }, 400); // 预期失败
    
    // 测试登出
    await testAPI('Auth', '用户登出', 'POST', '/api/v1/Auth/logout');
}

// 测试用户管理模块
async function testUsersModule() {
    console.log('\n👥 测试用户管理模块 (Users)...');
    
    // 获取用户列表
    await testAPI('Users', '获取用户列表', 'GET', '/api/v1/Users');
    
    // 分页查询用户
    await testAPI('Users', '分页查询用户', 'GET', '/api/v1/Users/paged?page=1&pageSize=10');
    
    // 获取角色列表
    await testAPI('Users', '获取角色列表', 'GET', '/api/v1/Users/getRoles');
    
    // 获取活跃用户
    await testAPI('Users', '获取活跃用户', 'GET', '/api/v1/Users/active');
    
    // 创建测试用户
    const newUser = {
        userName: `testuser_${Date.now()}`,
        realName: '测试用户',
        email: 'test@lybt.com',
        phoneNumber: '13800138000',
        role: 4, // Receptionist
        isActive: true
    };
    const createResponse = await testAPI('Users', '创建用户', 'POST', '/api/v1/Users/add', newUser, 201);
    
    // 如果创建成功，测试其他操作
    if (createResponse && createResponse.data && createResponse.data.success) {
        const userId = createResponse.data.data.id;
        
        // 获取用户详情
        await testAPI('Users', '获取用户详情', 'GET', `/api/v1/Users/getById/${userId}`);
        
        // 更新用户
        const updateData = { ...newUser, realName: '更新的测试用户' };
        await testAPI('Users', '更新用户', 'PUT', '/api/v1/Users/update', { id: userId, ...updateData });
        
        // 禁用用户
        await testAPI('Users', '禁用用户', 'PUT', `/api/v1/Users/${userId}/disable`);
        
        // 启用用户
        await testAPI('Users', '启用用户', 'PUT', `/api/v1/Users/${userId}/enable`);
    }
}

// 测试患者管理模块
async function testPatientsModule() {
    console.log('\n🏥 测试患者管理模块 (Patients)...');
    
    // 获取患者列表
    await testAPI('Patients', '获取患者列表', 'GET', '/api/v1/Patients');
    
    // 分页查询患者
    await testAPI('Patients', '分页查询患者', 'GET', '/api/v1/Patients/paged?page=1&pageSize=10');
    
    // 获取活跃患者
    await testAPI('Patients', '获取活跃患者', 'GET', '/api/v1/Patients/active');
    
    // 搜索患者
    await testAPI('Patients', '搜索患者', 'GET', '/api/v1/Patients/search?keyword=测试');
    
    // 创建测试患者
    const newPatient = {
        name: '测试患者',
        gender: 1, // 男性
        birthDate: '1990-01-01',
        phoneNumber: '13900139000',
        idNumber: '110101199001011234',
        address: '北京市朝阳区测试地址',
        emergencyContact: '紧急联系人',
        emergencyPhone: '13800138000'
    };
    const createResponse = await testAPI('Patients', '创建患者', 'POST', '/api/v1/Patients/add', newPatient, 201);
    
    // 如果创建成功，测试其他操作
    if (createResponse && createResponse.data && createResponse.data.success) {
        const patientId = createResponse.data.data.id;
        
        // 获取患者详情
        await testAPI('Patients', '获取患者详情', 'GET', `/api/v1/Patients/${patientId}`);
        
        // 获取患者病历
        await testAPI('Patients', '获取患者病历', 'GET', `/api/v1/Patients/${patientId}/records`);
        
        // 禁用患者
        await testAPI('Patients', '禁用患者', 'PUT', `/api/v1/Patients/${patientId}/disable`);
        
        // 启用患者
        await testAPI('Patients', '启用患者', 'PUT', `/api/v1/Patients/${patientId}/enable`);
    }
}

// 测试医生管理模块
async function testDoctorsModule() {
    console.log('\n👨‍⚕️ 测试医生管理模块 (Doctors)...');
    
    // 分页查询医生
    await testAPI('Doctors', '分页查询医生', 'GET', '/api/v1/Doctors/paged?page=1&pageSize=10');
    
    // 获取活跃医生
    await testAPI('Doctors', '获取活跃医生', 'GET', '/api/v1/Doctors/active');
    
    // 搜索医生
    await testAPI('Doctors', '搜索医生', 'GET', '/api/v1/Doctors/search?keyword=医生');
    
    // 获取医生角色
    await testAPI('Doctors', '获取医生角色', 'GET', '/api/v1/Doctors/roles');
    
    // 创建测试医生
    const newDoctor = {
        name: '测试医生',
        gender: 1,
        title: 2, // 主治医师
        specialties: '中医内科',
        phoneNumber: '13700137000',
        email: 'doctor@lybt.com',
        introduction: '专业的中医医生'
    };
    const createResponse = await testAPI('Doctors', '创建医生', 'POST', '/api/v1/Doctors/add', newDoctor, 201);
    
    // 如果创建成功，测试其他操作
    if (createResponse && createResponse.data && createResponse.data.success) {
        const doctorId = createResponse.data.data.id;
        
        // 获取医生详情
        await testAPI('Doctors', '获取医生详情', 'GET', `/api/v1/Doctors/${doctorId}`);
        
        // 禁用医生
        await testAPI('Doctors', '禁用医生', 'PUT', `/api/v1/Doctors/${doctorId}/disable`);
        
        // 启用医生
        await testAPI('Doctors', '启用医生', 'PUT', `/api/v1/Doctors/${doctorId}/enable`);
    }
}

// 测试药材管理模块
async function testHerbsModule() {
    console.log('\n🌿 测试药材管理模块 (Herbs)...');
    
    // 获取药材列表
    await testAPI('Herbs', '获取药材列表', 'GET', '/api/v1/Herbs');
    
    // 分页查询药材
    await testAPI('Herbs', '分页查询药材', 'GET', '/api/v1/Herbs/paged?page=1&pageSize=10');
    
    // 获取可用药材
    await testAPI('Herbs', '获取可用药材', 'GET', '/api/v1/Herbs/available');
    
    // 获取即将过期药材
    await testAPI('Herbs', '获取即将过期药材', 'GET', '/api/v1/Herbs/expiring');
    
    // 获取缺货药材
    await testAPI('Herbs', '获取缺货药材', 'GET', '/api/v1/Herbs/out-of-stock');
    
    // 获取药材统计
    await testAPI('Herbs', '获取药材统计', 'GET', '/api/v1/Herbs/statistics');
    
    // 创建测试药材
    const newHerb = {
        name: '测试药材',
        category: '清热解毒',
        specification: '500g/袋',
        unit: 'g',
        price: 25.50,
        stock: 100,
        status: 1, // 可用
        description: '用于测试的药材'
    };
    const createResponse = await testAPI('Herbs', '创建药材', 'POST', '/api/v1/Herbs/add', newHerb, 201);
    
    // 如果创建成功，测试其他操作
    if (createResponse && createResponse.data && createResponse.data.success) {
        const herbId = createResponse.data.data.id;
        
        // 获取药材详情
        await testAPI('Herbs', '获取药材详情', 'GET', `/api/v1/Herbs/${herbId}`);
        
        // 批量状态更新
        await testAPI('Herbs', '批量状态更新', 'PATCH', '/api/v1/Herbs/batch-status', {
            ids: [herbId],
            status: 1,
            isEnabled: true,
            reason: '测试批量更新'
        });
        
        // 禁用药材
        await testAPI('Herbs', '禁用药材', 'PUT', `/api/v1/Herbs/${herbId}/disable`);
        
        // 启用药材
        await testAPI('Herbs', '启用药材', 'PUT', `/api/v1/Herbs/${herbId}/enable`);
    }
}

// 测试验方模板模块
async function testFormulaTemplatesModule() {
    console.log('\n📋 测试验方模板模块 (FormulaTemplates)...');
    
    // 获取验方模板列表
    await testAPI('FormulaTemplates', '获取验方模板列表', 'GET', '/api/v1/FormulaTemplates');
    
    // 分页查询验方模板
    await testAPI('FormulaTemplates', '分页查询验方模板', 'GET', '/api/v1/FormulaTemplates/paged?page=1&pageSize=10');
    
    // 创建测试验方模板
    const newTemplate = {
        name: '测试验方',
        herbs: [
            { name: '当归', dosage: 10, unit: 'g' },
            { name: '白芍', dosage: 15, unit: 'g' }
        ],
        instructions: '水煎服，日一剂',
        remark: '测试用验方模板'
    };
    const createResponse = await testAPI('FormulaTemplates', '创建验方模板', 'POST', '/api/v1/FormulaTemplates', newTemplate, 201);
    
    // 如果创建成功，测试其他操作
    if (createResponse && createResponse.data && createResponse.data.success) {
        const templateId = createResponse.data.data.id;
        
        // 获取验方模板详情
        await testAPI('FormulaTemplates', '获取验方模板详情', 'GET', `/api/v1/FormulaTemplates/${templateId}`);
        
        // 更新验方模板
        const updateData = { ...newTemplate, name: '更新的测试验方' };
        await testAPI('FormulaTemplates', '更新验方模板', 'PUT', `/api/v1/FormulaTemplates/${templateId}`, updateData);
    }
}

// 测试其他业务模块
async function testOtherModules() {
    console.log('\n🔧 测试其他业务模块...');
    
    // 挂号模块
    await testAPI('Registration', '获取挂号列表', 'GET', '/api/v1/Registration');
    await testAPI('Registration', '分页查询挂号', 'GET', '/api/v1/Registration/paged?page=1&pageSize=10');
    
    // 诊疗模块
    await testAPI('DiagnosisTreatment', '获取诊疗记录', 'GET', '/api/v1/DiagnosisTreatment');
    await testAPI('DiagnosisTreatment', '分页查询诊疗记录', 'GET', '/api/v1/DiagnosisTreatment/paged?page=1&pageSize=10');
    
    // 处方模块
    await testAPI('Prescriptions', '获取处方列表', 'GET', '/api/v1/Prescriptions');
    await testAPI('Prescriptions', '分页查询处方', 'GET', '/api/v1/Prescriptions/paged?page=1&pageSize=10');
    
    // 药房模块
    await testAPI('Pharmacy', '获取药房信息', 'GET', '/api/v1/Pharmacy');
    await testAPI('Pharmacy', '分页查询药房', 'GET', '/api/v1/Pharmacy/paged?page=1&pageSize=10');
    await testAPI('Pharmacy', '获取待配药', 'GET', '/api/v1/Pharmacy/waiting');
    
    // 计费模块
    await testAPI('Billing', '获取计费记录', 'GET', '/api/v1/Billing');
    await testAPI('Billing', '分页查询计费', 'GET', '/api/v1/Billing/paged?page=1&pageSize=10');
    await testAPI('Billing', '获取可退费项目', 'GET', '/api/v1/Billing/refundable');
    
    // 病历模块
    await testAPI('Records', '获取病历列表', 'GET', '/api/v1/Records');
    await testAPI('Records', '分页查询病历', 'GET', '/api/v1/Records/paged?page=1&pageSize=10');
    
    // 排队模块
    await testAPI('Queueing', '获取排队信息', 'GET', '/api/v1/Queueing');
    await testAPI('Queueing', '分页查询排队', 'GET', '/api/v1/Queueing/paged?page=1&pageSize=10');
    
    // 治疗室模块
    await testAPI('TreatmentRoom', '获取治疗室列表', 'GET', '/api/v1/TreatmentRoom');
    await testAPI('TreatmentRoom', '分页查询治疗室', 'GET', '/api/v1/TreatmentRoom/paged?page=1&pageSize=10');
    
    // 同步模块
    await testAPI('Sync', '获取连接状态', 'GET', '/api/v1/Sync/connection-status');
    await testAPI('Sync', '获取同步日志', 'GET', '/api/v1/Sync/logs');
    await testAPI('Sync', '获取同步任务', 'GET', '/api/v1/Sync/tasks');
    
    // 健康检查
    await testAPI('Health', '基础健康检查', 'GET', '/api/Health');
    await testAPI('Health', '数据库健康检查', 'GET', '/api/Health/database');
    await testAPI('Health', '详细健康检查', 'GET', '/api/Health/detailed');
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
            moduleStats[result.module] = { total: 0, success: 0, failed: 0, apis: [] };
        }
        moduleStats[result.module].total++;
        moduleStats[result.module].apis.push({
            name: result.api,
            method: result.method,
            path: result.path,
            success: result.success,
            statusCode: result.statusCode,
            responseTime: result.responseTime
        });
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
async function runCorrectedTest() {
    console.log('🚀 开始凌隐宝堂中医诊所诊疗系统 API 修正版全面测试');
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
        console.log('📊 凌隐宝堂中医诊所诊疗系统 API 测试报告 (修正版)');
        console.log('='.repeat(80));
        console.log(`🕐 测试时间: ${new Date(report.summary.testTime).toLocaleString()}`);
        console.log(`🌐 测试地址: ${report.summary.apiBaseUrl}`);
        console.log(`📈 总体结果: ${report.summary.successTests}/${report.summary.totalTests} 成功 (${report.summary.successRate})`);
        console.log(`⚡ 平均响应时间: ${report.summary.avgResponseTime}`);
        console.log(`📊 响应时间范围: ${report.summary.minResponseTime} - ${report.summary.maxResponseTime}`);
        
        console.log('\n📋 模块测试统计:');
        Object.entries(report.moduleStats).forEach(([module, stats]) => {
            const moduleSuccessRate = ((stats.success / stats.total) * 100).toFixed(1);
            const status = stats.failed === 0 ? '✅' : stats.success > 0 ? '⚠️' : '❌';
            console.log(`${status} ${module}: ${stats.success}/${stats.total} (${moduleSuccessRate}%)`);
        });
        
        if (report.summary.failedTests > 0) {
            console.log('\n❌ 失败的测试:');
            report.detailResults.filter(r => !r.success).forEach(result => {
                console.log(`   [${result.module}] ${result.method} ${result.path} - ${result.statusCode} ${result.message}`);
            });
        }
        
        console.log('\n✨ 测试完成!');
        
        // 保存详细报告到文件
        require('fs').writeFileSync(
            'D:\\source\\repos\\LYBTZYZS\\CORRECTED_API_TEST_REPORT.json', 
            JSON.stringify(report, null, 2), 
            'utf8'
        );
        console.log('📄 详细报告已保存到: CORRECTED_API_TEST_REPORT.json');
        
    } catch (error) {
        console.error('❌ 测试过程中发生错误:', error.message);
    }
}

// 启动测试
runCorrectedTest();