# UltraThink API响应标准 - 统一数据格式规范

**文档版本**: v1.0  
**创建日期**: 2025-08-17  
**最后更新**: 2025-08-17  
**架构师**: UltraThink AI System  

## 📋 概述

本文档定义了LYBT系统中所有API接口的统一响应格式标准，确保前后端数据交互的一致性和可预测性。

## 🎯 设计原则

### 1. 统一性原则
- 所有业务API使用相同的响应结构
- 系统管理API使用简化但一致的响应格式
- 错误响应遵循统一的错误处理模式

### 2. 可预测性原则
- 响应结构固定，便于前端统一处理
- 成功/失败状态明确标识
- 错误信息结构化且具有可操作性

### 3. 扩展性原则
- 支持未来功能扩展
- 保持向后兼容性
- 允许自定义字段和元数据

## 📊 响应格式分类

### 1. 业务API响应格式

#### 基础响应格式 (ApiResponse<T>)

```typescript
interface ApiResponse<T> {
    success: boolean;           // 操作是否成功
    message: string;            // 响应消息
    data: T | null;            // 业务数据
    errors?: any;              // 错误详情（可选）
    timestamp: string;         // ISO 8601格式时间戳
    requestId: string;         // 请求链路追踪ID
}
```

#### 分页响应格式 (PagedApiResponse<T>)

```typescript
interface PagedApiResponse<T> {
    success: boolean;
    message: string;
    data: {
        items: T[];            // 分页数据项
        totalCount: number;    // 总记录数
        currentPage: number;   // 当前页码
        pageSize: number;      // 每页大小
        totalPages: number;    // 总页数
    };
    timestamp: string;
    requestId: string;
}
```

### 2. 系统管理API响应格式

```typescript
interface SystemResponse {
    success: boolean;
    message: string;
    data?: any;                // 系统数据（可选）
    warning?: boolean;         // 警告标识（可选）
    timestamp: number;         // Unix时间戳
    requestId: string;
}
```

## ✅ 成功响应示例

### 1. 单个数据对象响应

```json
{
    "success": true,
    "message": "查询成功",
    "data": {
        "id": "123e4567-e89b-12d3-a456-426614174000",
        "name": "张三",
        "email": "zhangsan@example.com",
        "createTime": "2025-08-17T10:30:00Z"
    },
    "timestamp": "2025-08-17T10:30:00Z",
    "requestId": "req-123456"
}
```

### 2. 数据列表响应

```json
{
    "success": true,
    "message": "查询成功",
    "data": [
        {
            "id": "123e4567-e89b-12d3-a456-426614174000",
            "name": "张三"
        },
        {
            "id": "987fcdeb-51a2-43d1-9f4b-123456789abc",
            "name": "李四"
        }
    ],
    "timestamp": "2025-08-17T10:30:00Z",
    "requestId": "req-123456"
}
```

### 3. 分页数据响应

```json
{
    "success": true,
    "message": "分页查询成功",
    "data": {
        "items": [
            {
                "id": "123e4567-e89b-12d3-a456-426614174000",
                "name": "张三",
                "email": "zhangsan@example.com"
            }
        ],
        "totalCount": 150,
        "currentPage": 1,
        "pageSize": 20,
        "totalPages": 8
    },
    "timestamp": "2025-08-17T10:30:00Z",
    "requestId": "req-123456"
}
```

### 4. 创建操作响应

```json
{
    "success": true,
    "message": "创建成功",
    "data": {
        "id": "123e4567-e89b-12d3-a456-426614174000",
        "name": "新建用户",
        "createTime": "2025-08-17T10:30:00Z"
    },
    "timestamp": "2025-08-17T10:30:00Z",
    "requestId": "req-123456"
}
```

### 5. 更新操作响应

```json
{
    "success": true,
    "message": "更新成功",
    "data": {
        "id": "123e4567-e89b-12d3-a456-426614174000",
        "name": "更新后的用户名",
        "updateTime": "2025-08-17T10:30:00Z"
    },
    "timestamp": "2025-08-17T10:30:00Z",
    "requestId": "req-123456"
}
```

### 6. 删除操作响应

```json
{
    "success": true,
    "message": "删除成功",
    "data": null,
    "timestamp": "2025-08-17T10:30:00Z",
    "requestId": "req-123456"
}
```

### 7. 系统状态响应

```json
{
    "success": true,
    "message": "系统正常",
    "data": {
        "status": "healthy",
        "uptime": "2 days 3 hours",
        "version": "1.0.0"
    },
    "timestamp": 1692261000,
    "requestId": "req-123456"
}
```

## ❌ 错误响应示例

### 1. 业务逻辑错误 (400 Bad Request)

```json
{
    "success": false,
    "message": "用户名已存在",
    "data": null,
    "errors": {
        "code": "DUPLICATE_USERNAME",
        "field": "username",
        "value": "zhangsan"
    },
    "timestamp": "2025-08-17T10:30:00Z",
    "requestId": "req-123456"
}
```

### 2. 参数验证错误 (400 Bad Request)

```json
{
    "success": false,
    "message": "参数验证失败",
    "data": null,
    "errors": {
        "code": "VALIDATION_ERROR",
        "details": [
            {
                "field": "email",
                "message": "邮箱格式不正确"
            },
            {
                "field": "age",
                "message": "年龄必须在18-100之间"
            }
        ]
    },
    "timestamp": "2025-08-17T10:30:00Z",
    "requestId": "req-123456"
}
```

### 3. 资源未找到 (404 Not Found)

```json
{
    "success": false,
    "message": "用户不存在",
    "data": null,
    "errors": {
        "code": "RESOURCE_NOT_FOUND",
        "resourceType": "User",
        "resourceId": "123e4567-e89b-12d3-a456-426614174000"
    },
    "timestamp": "2025-08-17T10:30:00Z",
    "requestId": "req-123456"
}
```

### 4. 权限拒绝 (403 Forbidden)

```json
{
    "success": false,
    "message": "权限不足",
    "data": null,
    "errors": {
        "code": "INSUFFICIENT_PERMISSIONS",
        "required": "Admin",
        "current": "User"
    },
    "timestamp": "2025-08-17T10:30:00Z",
    "requestId": "req-123456"
}
```

### 5. 认证失败 (401 Unauthorized)

```json
{
    "success": false,
    "message": "认证失败",
    "data": null,
    "errors": {
        "code": "AUTHENTICATION_FAILED",
        "reason": "Token已过期"
    },
    "timestamp": "2025-08-17T10:30:00Z",
    "requestId": "req-123456"
}
```

### 6. 服务器内部错误 (500 Internal Server Error)

```json
{
    "success": false,
    "message": "服务器内部错误",
    "data": null,
    "errors": {
        "code": "INTERNAL_SERVER_ERROR",
        "traceId": "trace-123456"
    },
    "timestamp": "2025-08-17T10:30:00Z",
    "requestId": "req-123456"
}
```

### 7. 系统管理错误响应

```json
{
    "success": false,
    "message": "系统监控检查失败",
    "timestamp": 1692261000,
    "requestId": "req-123456"
}
```

## 📏 字段规范

### 1. 必需字段

| 字段 | 类型 | 说明 | 示例 |
|------|------|------|------|
| success | boolean | 操作成功标识 | true/false |
| message | string | 响应消息 | "操作成功" |
| timestamp | string/number | 时间戳 | "2025-08-17T10:30:00Z" |
| requestId | string | 请求追踪ID | "req-123456" |

### 2. 可选字段

| 字段 | 类型 | 说明 | 何时包含 |
|------|------|------|----------|
| data | T | 业务数据 | 成功响应或有数据时 |
| errors | object | 错误详情 | 失败响应时 |
| warning | boolean | 警告标识 | 系统管理API的警告状态 |

### 3. 时间格式规范

- **业务API**: 使用ISO 8601格式 (`"2025-08-17T10:30:00Z"`)
- **系统API**: 使用Unix时间戳 (`1692261000`)

### 4. ID格式规范

- 使用UUID格式: `"123e4567-e89b-12d3-a456-426614174000"`
- RequestId格式: `"req-"` + 6位随机数字

## 🔧 状态码规范

### HTTP状态码使用规范

| 状态码 | 场景 | 响应示例 |
|--------|------|----------|
| 200 | 操作成功 | `{"success": true, "message": "操作成功"}` |
| 400 | 参数错误/业务逻辑错误 | `{"success": false, "message": "参数无效"}` |
| 401 | 认证失败 | `{"success": false, "message": "认证失败"}` |
| 403 | 权限不足 | `{"success": false, "message": "权限不足"}` |
| 404 | 资源不存在 | `{"success": false, "message": "资源不存在"}` |
| 409 | 冲突（如资源已存在） | `{"success": false, "message": "资源已存在"}` |
| 500 | 服务器内部错误 | `{"success": false, "message": "服务器内部错误"}` |
| 503 | 服务不可用 | `{"success": false, "message": "服务暂不可用"}` |

## 🛠️ 错误代码规范

### 错误代码分类

```typescript
// 通用错误代码
const CommonErrorCodes = {
    VALIDATION_ERROR: "VALIDATION_ERROR",           // 参数验证错误
    RESOURCE_NOT_FOUND: "RESOURCE_NOT_FOUND",      // 资源未找到
    DUPLICATE_RESOURCE: "DUPLICATE_RESOURCE",       // 资源重复
    INSUFFICIENT_PERMISSIONS: "INSUFFICIENT_PERMISSIONS", // 权限不足
    AUTHENTICATION_FAILED: "AUTHENTICATION_FAILED", // 认证失败
    INTERNAL_SERVER_ERROR: "INTERNAL_SERVER_ERROR", // 服务器内部错误
    SERVICE_UNAVAILABLE: "SERVICE_UNAVAILABLE"      // 服务不可用
}

// 业务特定错误代码
const BusinessErrorCodes = {
    USER_NOT_FOUND: "USER_NOT_FOUND",              // 用户不存在
    INVALID_PASSWORD: "INVALID_PASSWORD",           // 密码错误
    PRESCRIPTION_EXPIRED: "PRESCRIPTION_EXPIRED",   // 处方已过期
    HERB_OUT_OF_STOCK: "HERB_OUT_OF_STOCK"         // 药材库存不足
}
```

## 📱 前端集成指南

### 1. TypeScript 类型定义

```typescript
// 基础响应类型
interface ApiResponse<T = any> {
    success: boolean;
    message: string;
    data: T | null;
    errors?: {
        code: string;
        [key: string]: any;
    };
    timestamp: string;
    requestId: string;
}

// 分页响应类型
interface PagedApiResponse<T> {
    success: boolean;
    message: string;
    data: {
        items: T[];
        totalCount: number;
        currentPage: number;
        pageSize: number;
        totalPages: number;
    };
    timestamp: string;
    requestId: string;
}

// 系统响应类型
interface SystemResponse {
    success: boolean;
    message: string;
    data?: any;
    warning?: boolean;
    timestamp: number;
    requestId: string;
}
```

### 2. Axios 响应拦截器示例

```typescript
import axios from 'axios';

// 响应拦截器
axios.interceptors.response.use(
    (response) => {
        const data: ApiResponse = response.data;
        
        // 检查业务成功状态
        if (!data.success) {
            console.error('业务错误:', data.message, data.errors);
            throw new Error(data.message);
        }
        
        return response;
    },
    (error) => {
        // 处理HTTP错误
        if (error.response?.data) {
            const errorData: ApiResponse = error.response.data;
            console.error('API错误:', errorData.message, errorData.errors);
        }
        throw error;
    }
);
```

### 3. React Hook 示例

```typescript
import { useState, useEffect } from 'react';

function useApiData<T>(url: string) {
    const [data, setData] = useState<T | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const fetchData = async () => {
            try {
                setLoading(true);
                const response = await axios.get<ApiResponse<T>>(url);
                
                if (response.data.success) {
                    setData(response.data.data);
                    setError(null);
                } else {
                    setError(response.data.message);
                }
            } catch (err) {
                setError(err instanceof Error ? err.message : '未知错误');
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, [url]);

    return { data, loading, error };
}
```

## 🧪 测试规范

### 1. 响应格式验证

```typescript
// Jest 测试示例
describe('API响应格式', () => {
    test('成功响应应包含必需字段', async () => {
        const response = await api.get('/users/123');
        
        expect(response.data).toHaveProperty('success', true);
        expect(response.data).toHaveProperty('message');
        expect(response.data).toHaveProperty('data');
        expect(response.data).toHaveProperty('timestamp');
        expect(response.data).toHaveProperty('requestId');
    });

    test('错误响应应包含错误信息', async () => {
        try {
            await api.get('/users/invalid-id');
        } catch (error) {
            const errorData = error.response.data;
            
            expect(errorData).toHaveProperty('success', false);
            expect(errorData).toHaveProperty('message');
            expect(errorData).toHaveProperty('errors');
            expect(errorData.errors).toHaveProperty('code');
        }
    });
});
```

### 2. 分页响应验证

```typescript
test('分页响应格式验证', async () => {
    const response = await api.post('/users/paged', {
        pageIndex: 1,
        pageSize: 20
    });

    expect(response.data.success).toBe(true);
    expect(response.data.data).toHaveProperty('items');
    expect(response.data.data).toHaveProperty('totalCount');
    expect(response.data.data).toHaveProperty('currentPage');
    expect(response.data.data).toHaveProperty('pageSize');
    expect(response.data.data).toHaveProperty('totalPages');
    expect(Array.isArray(response.data.data.items)).toBe(true);
});
```

## 📝 最佳实践

### 1. 消息文案规范

- ✅ **清晰简洁**: "用户创建成功"
- ✅ **面向用户**: "密码格式不正确"
- ❌ **技术细节**: "SQL异常: 违反唯一约束"
- ❌ **英文消息**: "User not found"

### 2. 数据结构设计

- ✅ **扁平化**: 避免过深的嵌套结构
- ✅ **一致性**: 同类型数据使用相同结构
- ✅ **完整性**: 包含前端渲染所需的所有字段

### 3. 性能考虑

- ✅ **按需返回**: 列表接口只返回必要字段
- ✅ **分页支持**: 大数据集必须分页
- ✅ **压缩**: 启用gzip压缩减少传输大小

## 🔍 监控和调试

### 1. RequestId 追踪

每个响应都包含唯一的 `requestId`，用于：
- 前后端日志关联
- 问题排查和调试
- 性能监控分析

### 2. 错误日志记录

```csharp
// 控制器中的错误处理
catch (Exception ex)
{
    var context = new { UserId = GetOperator().operatorId, RequestData = request };
    return HandleException<T>(ex, "创建用户", context);
}
```

### 3. 监控指标

- API响应时间
- 错误率统计
- 各状态码分布
- RequestId追踪链路

---

## 📚 相关文档

- [控制器设计模式](./ultrathink-controller-design-patterns-20250817.md)
- [控制器开发模板](../templates/controller-templates-20250817.md)
- [错误处理最佳实践](../guides/error-handling-best-practices-20250817.md)
- [前端集成指南](../guides/frontend-integration-guide-20250817.md)

---

**维护说明**: 本标准应保持稳定，任何变更都需要考虑向后兼容性和对现有系统的影响。