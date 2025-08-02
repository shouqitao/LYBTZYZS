const http = require('http');

const API_BASE_URL = 'http://localhost:5001';
const LOGIN_CREDENTIALS = {
    username: 'sysadmin',
    password: 'Admin@123456',
    rememberMe: false
};

class APITester {
    constructor() {
        this.token = null;
        this.testResults = [];
    }

    async request(method, path, body = null, useAuth = true) {
        return new Promise((resolve) => {
            const url = new URL(path, API_BASE_URL);
            const postData = body ? JSON.stringify(body) : null;
            
            const options = {
                hostname: url.hostname,
                port: url.port,
                path: url.pathname + url.search,
                method: method,
                headers: {
                    'Content-Type': 'application/json',
                    ...(useAuth && this.token && { 'Authorization': `Bearer ${this.token}` }),
                    ...(postData && { 'Content-Length': Buffer.byteLength(postData) })
                }
            };

            const req = http.request(options, (res) => {
                let responseBody = '';
                res.on('data', (chunk) => responseBody += chunk);
                res.on('end', () => {
                    try {
                        const data = responseBody ? JSON.parse(responseBody) : null;
                        resolve({
                            success: res.statusCode >= 200 && res.statusCode < 300,
                            statusCode: res.statusCode,
                            data: data,
                            error: res.statusCode >= 400 ? responseBody : null
                        });
                    } catch (e) {
                        resolve({
                            success: res.statusCode >= 200 && res.statusCode < 300,
                            statusCode: res.statusCode,
                            data: responseBody,
                            error: res.statusCode >= 400 ? responseBody : null
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

    async login() {
        console.log('🔐 正在获取认证Token...');
        const result = await this.request('POST', '/api/v1/auth/login', LOGIN_CREDENTIALS, false);
        
        if (result.success && result.data && result.data.data && result.data.data.token) {
            this.token = result.data.data.token;
            console.log('✅ 认证成功');
            return true;
        } else {
            console.error('❌ 认证失败:', result.error);
            return false;
        }
    }

    logTest(name, success, details) {
        const status = success ? '✅' : '❌';
        console.log(`${status} ${name}`);
        if (!success && details) {
            console.log(`   错误: ${details}`);
        }
        this.testResults.push({ name, success, details });
    }

    async testBasicEndpoints() {
        console.log('\n📋 测试基础接口...');

        // 测试密码修改 (使用PATCH方法)
        const passwordChangeData = {
            oldPassword: 'Admin@123456',
            newPassword: 'NewPassword@123',
            confirmPassword: 'NewPassword@123'
        };
        const passwordResult = await this.request('PATCH', '/api/v1/auth/password', passwordChangeData);
        this.logTest('PATCH /api/v1/auth/password', passwordResult.success, passwordResult.error);

        // 如果密码修改成功，需要重新登录
        if (passwordResult.success) {
            // 用新密码登录
            const newLoginResult = await this.request('POST', '/api/v1/auth/login', {
                ...LOGIN_CREDENTIALS,
                password: 'NewPassword@123'
            }, false);
            
            if (newLoginResult.success) {
                this.token = newLoginResult.data.data.token;
                
                // 再改回原密码
                const revertPasswordData = {
                    oldPassword: 'NewPassword@123',
                    newPassword: 'Admin@123456',
                    confirmPassword: 'Admin@123456'
                };
                await this.request('PATCH', '/api/v1/auth/password', revertPasswordData);
                
                // 重新用原密码登录
                await this.login();
            }
        }

        // 测试Token刷新
        const refreshResult = await this.request('POST', '/api/v1/auth/RefreshToken');
        this.logTest('POST /api/v1/auth/RefreshToken', refreshResult.success, refreshResult.error);
    }

    async testUsersModule() {
        console.log('\n👥 测试用户模块...');

        // 测试用户分页查询 (使用POST方法)
        const userPagedQuery = {
            currentPage: 1,
            pageSize: 10,
            username: null,
            realName: null,
            role: null,
            isActive: true,
            includeInactive: false
        };
        const usersPagedResult = await this.request('POST', '/api/v1/users/paged', userPagedQuery);
        this.logTest('POST /api/v1/users/paged', usersPagedResult.success, usersPagedResult.error);

        // 测试RESTful获取用户列表
        const usersListResult = await this.request('GET', '/api/v1/users?page=1&pageSize=10');
        this.logTest('GET /api/v1/users', usersListResult.success, usersListResult.error);

        // 测试创建用户 (提供完整必需字段)
        const randomId = Math.floor(Math.random() * 10000);
        const newUserData = {
            username: `testuser${randomId}`,
            password: 'TestPassword@123',
            confirmPassword: 'TestPassword@123',
            realName: '测试用户',
            role: 2, // DiagnosingDoctor
            email: 'test@example.com',
            phoneNumber: '13800138000',
            department: '内科',
            position: '医生',
            isActive: true,
            remark: '系统测试用户'
        };
        const createUserResult = await this.request('POST', '/api/v1/users', newUserData);
        this.logTest('POST /api/v1/users (创建用户)', createUserResult.success, createUserResult.error);

        // 测试获取角色列表
        const rolesResult = await this.request('GET', '/api/v1/users/getRoles');
        this.logTest('GET /api/v1/users/getRoles', rolesResult.success, rolesResult.error);

        // 测试获取活跃用户
        const activeUsersResult = await this.request('GET', '/api/v1/users/active');
        this.logTest('GET /api/v1/users/active', activeUsersResult.success, activeUsersResult.error);
    }

    async testPatientsModule() {
        console.log('\n🏥 测试患者模块...');

        // 测试患者分页查询 (使用POST方法)
        const patientPagedQuery = {
            currentPage: 1,
            pageSize: 10,
            name: null,
            phoneNumber: null,
            idCard: null,
            isActive: true
        };
        const patientsPagedResult = await this.request('POST', '/api/v1/patients/paged', patientPagedQuery);
        this.logTest('POST /api/v1/patients/paged', patientsPagedResult.success, patientsPagedResult.error);

        // 测试快速创建患者 (提供必需字段)
        const quickPatientData = {
            name: '测试患者',
            phoneNumber: '13900139000',
            gender: 1, // 男
            birthDate: '1990-01-01',
            idCard: '110101199001011234',
            address: '北京市朝阳区',
            isActive: true
        };
        const createPatientResult = await this.request('POST', '/api/v1/patients/quick', quickPatientData);
        this.logTest('POST /api/v1/patients/quick', createPatientResult.success, createPatientResult.error);

        // 测试获取患者列表
        const patientsListResult = await this.request('GET', '/api/v1/patients?page=1&pageSize=10');
        this.logTest('GET /api/v1/patients', patientsListResult.success, patientsListResult.error);
    }

    async testDoctorsModule() {
        console.log('\n👨‍⚕️ 测试医生模块...');

        // 测试医生分页查询 (使用POST方法)
        const doctorPagedQuery = {
            currentPage: 1,
            pageSize: 10,
            name: null,
            department: null,
            specialization: null,
            isActive: true
        };
        const doctorsPagedResult = await this.request('POST', '/api/v1/doctors/paged', doctorPagedQuery);
        this.logTest('POST /api/v1/doctors/paged', doctorsPagedResult.success, doctorsPagedResult.error);

        // 测试获取医生列表
        const doctorsListResult = await this.request('GET', '/api/v1/doctors?page=1&pageSize=10');
        this.logTest('GET /api/v1/doctors', doctorsListResult.success, doctorsListResult.error);

        // 测试获取活跃医生
        const activeDoctorsResult = await this.request('GET', '/api/v1/doctors/active');
        this.logTest('GET /api/v1/doctors/active', activeDoctorsResult.success, activeDoctorsResult.error);
    }

    async testHerbsModule() {
        console.log('\n🌿 测试中药材模块...');

        // 测试中药材分页查询 (使用POST方法)
        const herbPagedQuery = {
            currentPage: 1,
            pageSize: 10,
            name: null,
            category: null,
            isActive: true
        };
        const herbsPagedResult = await this.request('POST', '/api/v1/herbs/paged', herbPagedQuery);
        this.logTest('POST /api/v1/herbs/paged', herbsPagedResult.success, herbsPagedResult.error);

        // 测试获取中药材列表
        const herbsListResult = await this.request('GET', '/api/v1/herbs?page=1&pageSize=10');
        this.logTest('GET /api/v1/herbs', herbsListResult.success, herbsListResult.error);

        // 测试获取活跃中药材
        const activeHerbsResult = await this.request('GET', '/api/v1/herbs/active');
        this.logTest('GET /api/v1/herbs/active', activeHerbsResult.success, activeHerbsResult.error);
    }

    async testFormulaTemplatesModule() {
        console.log('\n📜 测试验方模板模块...');

        // 测试验方模板分页查询 (使用POST方法)
        const formulaPagedQuery = {
            currentPage: 1,
            pageSize: 10,
            name: null,
            category: null,
            isActive: true
        };
        const formulasPagedResult = await this.request('POST', '/api/v1/formulatemplates/paged', formulaPagedQuery);
        this.logTest('POST /api/v1/formulatemplates/paged', formulasPagedResult.success, formulasPagedResult.error);

        // 测试获取验方模板列表
        const formulasListResult = await this.request('GET', '/api/v1/formulatemplates?page=1&pageSize=10');
        this.logTest('GET /api/v1/formulatemplates', formulasListResult.success, formulasListResult.error);
    }

    async runAllTests() {
        console.log('🚀 开始凌隐宝堂中医诊所诊疗系统API修复验证测试\n');
        console.log('=' * 60);

        // 登录
        if (!await this.login()) {
            console.error('❌ 无法获取认证，测试终止');
            return;
        }

        // 运行所有测试
        await this.testBasicEndpoints();
        await this.testUsersModule();
        await this.testPatientsModule();
        await this.testDoctorsModule();
        await this.testHerbsModule();
        await this.testFormulaTemplatesModule();

        // 生成测试报告
        this.generateReport();
    }

    generateReport() {
        console.log('\n' + '=' * 60);
        console.log('📊 API修复验证测试报告');
        console.log('=' * 60);

        const totalTests = this.testResults.length;
        const passedTests = this.testResults.filter(r => r.success).length;
        const failedTests = totalTests - passedTests;
        const passRate = totalTests > 0 ? ((passedTests / totalTests) * 100).toFixed(1) : '0.0';

        console.log(`总测试数: ${totalTests}`);
        console.log(`通过: ${passedTests}`);
        console.log(`失败: ${failedTests}`);
        console.log(`通过率: ${passRate}%`);

        console.log('\n失败的测试:');
        const failedResults = this.testResults.filter(r => !r.success);
        if (failedResults.length === 0) {
            console.log('🎉 所有测试都通过了!');
        } else {
            failedResults.forEach((result, index) => {
                console.log(`${index + 1}. ${result.name}`);
                if (result.details) {
                    console.log(`   错误详情: ${result.details}`);
                }
            });
        }

        console.log('\n✅ 修复验证测试完成!');
    }
}

// 运行测试
const tester = new APITester();
tester.runAllTests().catch(console.error);