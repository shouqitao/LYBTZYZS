/**
 * LYBT医疗系统 - 测试数据生成脚本
 * 用于创建用户、患者等测试数据
 */

const BASE_URL = 'http://localhost:5297/api/v1';
let authToken = '';

// 测试用户数据
const testUsers = [
    {
        userName: "doctor01",
        realName: "张医生",
        role: 1, // DiagnosingDoctor
        roles: [1],
        isActive: true,
        email: "doctor01@lybt.com",
        phoneNumber: "13800138001"
    },
    {
        userName: "doctor02", 
        realName: "李医生",
        role: 1,
        roles: [1],
        isActive: true,
        email: "doctor02@lybt.com",
        phoneNumber: "13800138002"
    },
    {
        userName: "nurse01",
        realName: "王护士",
        role: 0, // Staff
        roles: [0],
        isActive: true,
        email: "nurse01@lybt.com",
        phoneNumber: "13800138003"
    },
    {
        userName: "cashier01",
        realName: "赵收费员",
        role: 2, // CashierStaff
        roles: [2],
        isActive: true,
        email: "cashier01@lybt.com",
        phoneNumber: "13800138004"
    },
    {
        userName: "pharmacist01",
        realName: "陈药剂师",
        role: 3, // PharmacyStaff
        roles: [3],
        isActive: true,
        email: "pharmacist01@lybt.com",
        phoneNumber: "13800138005"
    },
    {
        userName: "therapist01",
        realName: "孙理疗师",
        role: 4, // PhysiotherapyStaff
        roles: [4],
        isActive: true,
        email: "therapist01@lybt.com",
        phoneNumber: "13800138006"
    }
];

// 测试患者数据
const testPatients = [
    {
        name: "张三",
        gender: "Male",
        birthDate: "1985-03-15",
        idCard: "320102198503151234",
        phoneNumber: "13900139001",
        address: "北京市朝阳区建国路88号",
        emergencyContact: "李四",
        emergencyContactPhone: "13900139002"
    },
    {
        name: "李四",
        gender: "Female", 
        birthDate: "1990-07-22",
        idCard: "320102199007221234",
        phoneNumber: "13900139003",
        address: "上海市浦东新区陆家嘴街道",
        emergencyContact: "王五",
        emergencyContactPhone: "13900139004"
    },
    {
        name: "王五",
        gender: "Male",
        birthDate: "1978-12-08",
        idCard: "320102197812081234",
        phoneNumber: "13900139005",
        address: "广州市天河区珠江新城",
        emergencyContact: "赵六",
        emergencyContactPhone: "13900139006"
    },
    {
        name: "赵六",
        gender: "Female",
        birthDate: "1995-05-30",
        idCard: "320102199505301234",
        phoneNumber: "13900139007",
        address: "深圳市南山区科技园",
        emergencyContact: "孙七",
        emergencyContactPhone: "13900139008"
    },
    {
        name: "孙七",
        gender: "Male",
        birthDate: "1982-11-18",
        idCard: "320102198211181234",
        phoneNumber: "13900139009",
        address: "杭州市西湖区文三路",
        emergencyContact: "周八",
        emergencyContactPhone: "13900139010"
    }
];

// 登录获取token
async function login() {
    console.log('🔐 正在登录系统...');
    
    const response = await fetch(`${BASE_URL}/Auth/login`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            username: 'sysadmin',
            password: 'Admin@123456',
            rememberMe: true,
            loginType: 'Password'
        })
    });

    const result = await response.json();
    
    if (result.success && result.data.token) {
        authToken = result.data.token;
        console.log('✅ 登录成功，获取到Token');
        return true;
    } else {
        console.error('❌ 登录失败:', result.message);
        return false;
    }
}

// 创建用户
async function createUsers() {
    console.log('\n👥 开始创建测试用户...');
    
    for (const user of testUsers) {
        try {
            const response = await fetch(`${BASE_URL}/Users/add`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${authToken}`
                },
                body: JSON.stringify(user)
            });

            const result = await response.json();
            
            if (result.success) {
                console.log(`✅ 用户创建成功: ${user.realName} (${user.userName})`);
            } else {
                console.log(`⚠️  用户创建失败: ${user.realName} - ${result.message}`);
            }
        } catch (error) {
            console.error(`❌ 创建用户出错: ${user.realName}`, error.message);
        }
        
        // 避免请求过快
        await new Promise(resolve => setTimeout(resolve, 100));
    }
}

// 创建患者
async function createPatients() {
    console.log('\n🏥 开始创建测试患者...');
    
    for (const patient of testPatients) {
        try {
            const response = await fetch(`${BASE_URL}/Patients`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${authToken}`
                },
                body: JSON.stringify(patient)
            });

            if (response.ok) {
                const result = await response.json();
                console.log(`✅ 患者创建成功: ${patient.name}`);
            } else {
                const errorText = await response.text();
                console.log(`⚠️  患者创建失败: ${patient.name} - ${errorText}`);
            }
        } catch (error) {
            console.error(`❌ 创建患者出错: ${patient.name}`, error.message);
        }
        
        // 避免请求过快
        await new Promise(resolve => setTimeout(resolve, 100));
    }
}

// 验证创建的数据
async function verifyData() {
    console.log('\n🔍 验证创建的数据...');
    
    try {
        // 查询用户
        const usersResponse = await fetch(`${BASE_URL}/Users/search?pageIndex=1&pageSize=20`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });
        
        if (usersResponse.ok) {
            const usersResult = await usersResponse.json();
            console.log(`📊 用户数据: 共 ${usersResult.total} 个用户`);
        }

        // 查询患者
        const patientsResponse = await fetch(`${BASE_URL}/Patients/paged?pageIndex=1&pageSize=20`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });
        
        if (patientsResponse.ok) {
            const patientsResult = await patientsResponse.json();
            console.log(`📊 患者数据: 共 ${patientsResult.data?.total || 0} 个患者`);
        }
        
    } catch (error) {
        console.error('❌ 验证数据出错:', error.message);
    }
}

// 主函数
async function main() {
    console.log('🚀 LYBT医疗系统测试数据生成器');
    console.log('=====================================');
    
    // 1. 登录
    const loginSuccess = await login();
    if (!loginSuccess) {
        console.error('❌ 无法登录，退出程序');
        return;
    }
    
    // 2. 创建用户
    await createUsers();
    
    // 3. 创建患者
    await createPatients();
    
    // 4. 验证数据
    await verifyData();
    
    console.log('\n🎉 测试数据生成完成！');
    console.log('=====================================');
    console.log('📋 生成的测试账户:');
    console.log('  医生账户: doctor01, doctor02 (初始密码请查看系统配置)');
    console.log('  护士账户: nurse01');
    console.log('  收费员账户: cashier01'); 
    console.log('  药剂师账户: pharmacist01');
    console.log('  理疗师账户: therapist01');
    console.log('📋 生成的测试患者: 张三, 李四, 王五, 赵六, 孙七');
}

// 执行
main().catch(console.error);