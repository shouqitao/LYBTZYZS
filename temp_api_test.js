const http = require('http');

// UltraThink v2.0 API功能测试脚本
console.log('🧪 开始UltraThink v2.0 API功能测试');

// 测试配置
const apiBase = 'http://localhost:5000/api/v1';
const testResults = [];

// 通用API请求函数
function apiRequest(method, path, data = null) {
    return new Promise((resolve) => {
        const postData = data ? JSON.stringify(data) : null;
        const options = {
            hostname: 'localhost',
            port: 5000,
            path: path,
            method: method,
            headers: {
                'Content-Type': 'application/json',
                ...(postData && { 'Content-Length': Buffer.byteLength(postData) })
            }
        };

        const req = http.request(options, (res) => {
            let body = '';
            res.on('data', (chunk) => body += chunk);
            res.on('end', () => {
                try {
                    const result = {
                        success: res.statusCode >= 200 && res.statusCode < 300,
                        statusCode: res.statusCode,
                        data: body ? JSON.parse(body) : null,
                        error: null
                    };
                    resolve(result);
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

// 执行测试
async function runTests() {
    console.log('\n📋 测试1: 健康检查端点');
    const healthResult = await apiRequest('GET', '/api/v1/health');
    testResults.push({
        test: 'Health Check',
        status: healthResult.success ? '✅ 成功' : '❌ 失败',
        details: healthResult.error || `状态码: ${healthResult.statusCode}`
    });

    console.log('\n📋 测试2: 中药材管理API');
    const herbsResult = await apiRequest('GET', '/api/v1/herbs');
    testResults.push({
        test: 'Herbs API',
        status: herbsResult.success ? '✅ 成功' : '❌ 失败',
        details: herbsResult.error || `状态码: ${herbsResult.statusCode}`
    });

    console.log('\n📋 测试3: 用户管理API');
    const usersResult = await apiRequest('GET', '/api/v1/users');
    testResults.push({
        test: 'Users API',
        status: usersResult.success ? '✅ 成功' : '❌ 失败',
        details: usersResult.error || `状态码: ${usersResult.statusCode}`
    });

    console.log('\n📋 测试4: Swagger文档');
    const swaggerResult = await apiRequest('GET', '/swagger/v1/swagger.json');
    testResults.push({
        test: 'Swagger Docs',
        status: swaggerResult.success ? '✅ 成功' : '❌ 失败',
        details: swaggerResult.error || `状态码: ${swaggerResult.statusCode}`
    });

    // 输出测试结果
    console.log('\n🎯 UltraThink v2.0 API测试结果:');
    console.log('=' .repeat(50));
    testResults.forEach((result, index) => {
        console.log(`${index + 1}. ${result.test} - ${result.status}`);
        if (!result.status.includes('成功')) {
            console.log(`   错误: ${result.details}`);
        }
    });
    
    const successCount = testResults.filter(r => r.status.includes('成功')).length;
    console.log('\n📊 测试总结:');
    console.log(`成功: ${successCount}/${testResults.length} 个测试`);
    
    if (successCount === testResults.length) {
        console.log('🎉 所有API测试通过! UltraThink v2.0架构重构成功!');
    } else {
        console.log('⚠️  部分测试失败，可能是服务启动问题');
    }
}

// 延迟执行，给服务启动时间
setTimeout(runTests, 3000);