const https = require('https');
const http = require('http');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

const ports = [
    { protocol: 'https', port: 7001 },
    { protocol: 'https', port: 7157 },
    { protocol: 'http', port: 5297 },
    { protocol: 'https', port: 5001 },
    { protocol: 'http', port: 5000 }
];

async function checkPort(protocol, port) {
    return new Promise((resolve) => {
        const client = protocol === 'https' ? https : http;
        const req = client.get(`${protocol}://localhost:${port}/api/health`, (res) => {
            resolve({ port, protocol, status: res.statusCode, success: true });
        });
        req.on('error', () => {
            resolve({ port, protocol, status: null, success: false });
        });
        req.setTimeout(2000, () => {
            req.destroy();
            resolve({ port, protocol, status: null, success: false });
        });
    });
}

async function findActivePort() {
    console.log('🔍 检查WebAPI运行端口...\n');
    
    for (const { protocol, port } of ports) {
        const result = await checkPort(protocol, port);
        if (result.success) {
            console.log(`✅ ${protocol}://localhost:${port} - 状态码: ${result.status}`);
            console.log(`\n🎯 WebAPI正在运行在: ${protocol}://localhost:${port}`);
            return;
        } else {
            console.log(`❌ ${protocol}://localhost:${port} - 无响应`);
        }
    }
    
    console.log('\n❌ 未找到运行中的WebAPI服务');
}

findActivePort();