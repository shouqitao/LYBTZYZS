const http = require('http');
const https = require('https');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

// API测试配置
const API_BASE = 'http://localhost:5297/api/v1';
const TEST_TIMEOUT = 30000; // 30秒超时

// 测试数据
const testData = {
    login: {
        username: 'sysadmin',
        password: 'Admin@123456',
        rememberMe: false
    },
    user: {
        userName: `test_${Date.now()}`,
        realName: '测试用户',
        email: 'test@example.com',
        role: 1, // DiagnosingDoctor
        isActive: true,
        phoneNumber: '13800000001'
    },
    patient: {
        name: '测试患者' + Date.now(),
        gender: 1,
        age: 30,
        idCard: '12345678901234' + Math.floor(Math.random() * 10000).toString().padStart(4, '0'),
        phoneNumber: '138' + Math.floor(Math.random() * 100000000).toString().padStart(8, '0'),
        address: '测试地址'
    },
    herb: {
        name: '测试药材',
        pinyin: 'CSCYC',
        wuBi: '',
        origin: '测试产地',
        spec: '10',
        unit: 'g',
        price: 10.50,
        stock: 100,
        batchNo: 'TEST001',
        expireDate: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString(),
        effect: '测试功效',
        remark: '测试备注'
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

// 通用HTTP请求函数
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
                    body: body ? JSON.parse(body) : null
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
    if (response.status === 200 && response.body.success) {
        console.log('✅ 登录成功');
        return response.body.data.token;
    } else {
        throw new Error(`登录失败: ${JSON.stringify(response.body)}`);
    }
}

// 测试结果汇总
const testResults = {
    passed: 0,
    failed: 0,
    details: []
};

// 记录测试结果
function recordTest(module, api, method, route, success, message) {
    const result = {
        module,
        api,
        method,
        route,
        success,
        message,
        timestamp: new Date().toISOString()
    };
    testResults.details.push(result);
    if (success) testResults.passed++;
    else testResults.failed++;
    
    console.log(`${success ? '✅' : '❌'} [${module}] ${method} ${route} - ${message}`);
}

// 测试用户模块
async function testUserModule(token) {
    console.log('\n📌 测试用户模块');
    
    // 1. 测试新增用户 (新路由)
    try {
        const response = await apiRequest('POST', '/users/add', token, testData.user);
        recordTest('用户模块', '新增用户', 'POST', '/users/add', 
            response.status === 200, `状态码: ${response.status}`);
    } catch (error) {
        recordTest('用户模块', '新增用户', 'POST', '/users/add', false, error.message);
    }

    // 2. 测试分页查询
    try {
        const response = await apiRequest('POST', '/users/paged', token, { currentPage: 1, pageSize: 10 });
        recordTest('用户模块', '分页查询', 'POST', '/users/paged', 
            response.status === 200, `状态码: ${response.status}`);
        
        // 保存第一个用户ID用于后续测试
        if (response.body?.data?.items?.length > 0) {
            const userId = response.body.data.items[0].id;
            
            // 3. 测试启用用户 (新PATCH方法)
            try {
                const enableResponse = await apiRequest('PATCH', `/users/${userId}/enable`, token);
                recordTest('用户模块', '启用用户', 'PATCH', `/users/{id}/enable`, 
                    enableResponse.status === 200, `状态码: ${enableResponse.status}`);
            } catch (error) {
                recordTest('用户模块', '启用用户', 'PATCH', '/users/{id}/enable', false, error.message);
            }

            // 4. 测试禁用用户 (新PATCH方法)
            try {
                const disableResponse = await apiRequest('PATCH', `/users/${userId}/disable`, token);
                recordTest('用户模块', '禁用用户', 'PATCH', `/users/{id}/disable`, 
                    disableResponse.status === 200, `状态码: ${disableResponse.status}`);
            } catch (error) {
                recordTest('用户模块', '禁用用户', 'PATCH', '/users/{id}/disable', false, error.message);
            }
        }
    } catch (error) {
        recordTest('用户模块', '分页查询', 'POST', '/users/paged', false, error.message);
    }
}

// 测试患者模块
async function testPatientModule(token) {
    console.log('\n📌 测试患者模块');
    
    // 1. 测试新增患者 (新路由)
    try {
        const response = await apiRequest('POST', '/patients/add', token, testData.patient);
        recordTest('患者模块', '新增患者', 'POST', '/patients/add', 
            response.status === 200, `状态码: ${response.status}`);
    } catch (error) {
        recordTest('患者模块', '新增患者', 'POST', '/patients/add', false, error.message);
    }

    // 2. 测试分页查询
    try {
        const response = await apiRequest('POST', '/patients/paged', token, { currentPage: 1, pageSize: 10 });
        recordTest('患者模块', '分页查询', 'POST', '/patients/paged', 
            response.status === 200, `状态码: ${response.status}`);
        
        // 保存第一个患者ID用于后续测试
        if (response.body?.data?.items?.length > 0) {
            const patientId = response.body.data.items[0].id;
            
            // 3. 测试更新患者
            try {
                const updateData = { ...testData.patient, name: '更新的患者' };
                const updateResponse = await apiRequest('PUT', `/patients/${patientId}`, token, updateData);
                recordTest('患者模块', '更新患者', 'PUT', `/patients/{id}`, 
                    updateResponse.status === 200, `状态码: ${updateResponse.status}`);
            } catch (error) {
                recordTest('患者模块', '更新患者', 'PUT', '/patients/{id}', false, error.message);
            }
        }
    } catch (error) {
        recordTest('患者模块', '分页查询', 'POST', '/patients/paged', false, error.message);
    }
}

// 测试药材模块
async function testHerbModule(token) {
    console.log('\n📌 测试药材模块');
    
    // 1. 测试新增药材 (新路由)
    try {
        const response = await apiRequest('POST', '/herbs/add', token, testData.herb);
        recordTest('药材模块', '新增药材', 'POST', '/herbs/add', 
            response.status === 200, `状态码: ${response.status}`);
    } catch (error) {
        recordTest('药材模块', '新增药材', 'POST', '/herbs/add', false, error.message);
    }

    // 2. 测试分页查询
    try {
        const response = await apiRequest('POST', '/herbs/paged', token, { currentPage: 1, pageSize: 10 });
        recordTest('药材模块', '分页查询', 'POST', '/herbs/paged', 
            response.status === 200, `状态码: ${response.status}`);
        
        // 保存第一个药材ID用于后续测试
        if (response.body?.data?.items?.length > 0) {
            const herbId = response.body.data.items[0].id;
            
            // 3. 测试更新药材 (新路由)
            try {
                const updateData = { ...testData.herb, name: '更新的药材' };
                const updateResponse = await apiRequest('PUT', `/herbs/${herbId}`, token, updateData);
                recordTest('药材模块', '更新药材', 'PUT', `/herbs/{id}`, 
                    updateResponse.status === 200, `状态码: ${updateResponse.status}`);
            } catch (error) {
                recordTest('药材模块', '更新药材', 'PUT', '/herbs/{id}', false, error.message);
            }

            // 4. 测试启用药材 (新增)
            try {
                const enableResponse = await apiRequest('PATCH', `/herbs/${herbId}/enable`, token);
                recordTest('药材模块', '启用药材', 'PATCH', `/herbs/{id}/enable`, 
                    enableResponse.status === 200, `状态码: ${enableResponse.status}`);
            } catch (error) {
                recordTest('药材模块', '启用药材', 'PATCH', '/herbs/{id}/enable', false, error.message);
            }

            // 5. 测试禁用药材 (新增)
            try {
                const disableResponse = await apiRequest('PATCH', `/herbs/${herbId}/disable`, token);
                recordTest('药材模块', '禁用药材', 'PATCH', `/herbs/{id}/disable`, 
                    disableResponse.status === 200, `状态码: ${disableResponse.status}`);
            } catch (error) {
                recordTest('药材模块', '禁用药材', 'PATCH', '/herbs/{id}/disable', false, error.message);
            }

            // 6. 验证DELETE方法已移除
            try {
                const deleteResponse = await apiRequest('DELETE', `/herbs/${herbId}`, token);
                recordTest('药材模块', 'DELETE已移除', 'DELETE', `/herbs/{id}`, 
                    deleteResponse.status === 404 || deleteResponse.status === 405, 
                    `预期404/405，实际: ${deleteResponse.status}`);
            } catch (error) {
                recordTest('药材模块', 'DELETE已移除', 'DELETE', '/herbs/{id}', true, '请求失败符合预期');
            }
        }
    } catch (error) {
        recordTest('药材模块', '分页查询', 'POST', '/herbs/paged', false, error.message);
    }
}

// 测试医生模块
async function testDoctorModule(token) {
    console.log('\n📌 测试医生模块');
    
    // 先创建一个用户作为医生
    try {
        const doctorUser = {
            ...testData.user,
            userName: `doctor_${Date.now()}`,
            realName: '测试医生',
            role: 1 // DiagnosingDoctor
        };
        const userResponse = await apiRequest('POST', '/users/add', token, doctorUser);
        if (userResponse.status === 200 && userResponse.body.success) {
            // 获取用户列表找到刚创建的用户
            const listResponse = await apiRequest('POST', '/users/paged', token, { 
                currentPage: 1, 
                pageSize: 100,
                keyword: doctorUser.userName 
            });
            
            if (listResponse.body?.data?.items?.length > 0) {
                const userId = listResponse.body.data.items[0].id;
                testData.doctor.userId = userId;
                
                // 1. 测试新增医生 (新路由)
                try {
                    const response = await apiRequest('POST', '/doctors/add', token, testData.doctor);
                    recordTest('医生模块', '新增医生', 'POST', '/doctors/add', 
                        response.status === 200, `状态码: ${response.status}`);
                } catch (error) {
                    recordTest('医生模块', '新增医生', 'POST', '/doctors/add', false, error.message);
                }
            }
        }
    } catch (error) {
        console.error('创建医生用户失败:', error.message);
    }

    // 2. 测试分页查询
    try {
        const response = await apiRequest('POST', '/doctors/paged', token, { currentPage: 1, pageSize: 10 });
        recordTest('医生模块', '分页查询', 'POST', '/doctors/paged', 
            response.status === 200, `状态码: ${response.status}`);
        
        // 保存第一个医生ID用于后续测试
        if (response.body?.data?.items?.length > 0) {
            const doctorId = response.body.data.items[0].id;
            
            // 3. 测试更新医生 (新路由)
            try {
                const updateData = { ...testData.doctor, department: '更新的科室' };
                const updateResponse = await apiRequest('PUT', `/doctors/${doctorId}`, token, updateData);
                recordTest('医生模块', '更新医生', 'PUT', `/doctors/{id}`, 
                    updateResponse.status === 200, `状态码: ${updateResponse.status}`);
            } catch (error) {
                recordTest('医生模块', '更新医生', 'PUT', '/doctors/{id}', false, error.message);
            }
        }
    } catch (error) {
        recordTest('医生模块', '分页查询', 'POST', '/doctors/paged', false, error.message);
    }
}

// 打印测试报告
function printTestReport() {
    console.log('\n' + '='.repeat(60));
    console.log('📊 API路由测试报告');
    console.log('='.repeat(60));
    console.log(`总测试数: ${testResults.passed + testResults.failed}`);
    console.log(`✅ 通过: ${testResults.passed}`);
    console.log(`❌ 失败: ${testResults.failed}`);
    console.log(`成功率: ${((testResults.passed / (testResults.passed + testResults.failed)) * 100).toFixed(2)}%`);
    
    if (testResults.failed > 0) {
        console.log('\n失败的测试:');
        testResults.details.filter(r => !r.success).forEach(r => {
            console.log(`- [${r.module}] ${r.method} ${r.route}: ${r.message}`);
        });
    }
    
    console.log('\n详细结果已保存到: test_results.json');
    
    // 保存详细结果
    const fs = require('fs');
    fs.writeFileSync('test_results.json', JSON.stringify(testResults, null, 2));
}

// 主测试函数
async function runTests() {
    console.log('🚀 开始API路由测试');
    console.log('API地址:', API_BASE);
    console.log('测试时间:', new Date().toLocaleString());
    console.log('=' .repeat(60));
    
    try {
        // 获取认证Token
        const token = await getAuthToken();
        
        // 测试各模块
        await testUserModule(token);
        await testPatientModule(token);
        await testHerbModule(token);
        await testDoctorModule(token);
        
        // 打印测试报告
        printTestReport();
        
    } catch (error) {
        console.error('❌ 测试失败:', error.message);
    }
}

// 检查服务是否已启动
async function waitForServer() {
    console.log('⏳ 等待WebAPI服务启动...');
    for (let i = 0; i < 30; i++) {
        try {
            await apiRequest('GET', '/health');
            console.log('✅ WebAPI服务已就绪');
            return true;
        } catch (error) {
            process.stdout.write('.');
            await new Promise(resolve => setTimeout(resolve, 1000));
        }
    }
    throw new Error('WebAPI服务启动超时');
}

// 执行测试
(async () => {
    try {
        await waitForServer();
        await runTests();
    } catch (error) {
        console.error('❌ 无法连接到WebAPI服务:', error.message);
        console.log('请确保WebAPI服务正在运行在 http://localhost:5297');
    }
})();