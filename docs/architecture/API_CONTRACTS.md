# API Contract Specification

## Table of Contents

1. [Overview](#overview)
2. [API Design Principles](#api-design-principles)
3. [URL Routing Standards](#url-routing-standards)
4. [HTTP Method Standards](#http-method-standards)
5. [Request Standards](#request-standards)
6. [Response Standards](#response-standards)
7. [Authentication and Authorization](#authentication-and-authorization)
8. [Error Handling](#error-handling)
9. [API Versioning](#api-versioning)
10. [Business Module APIs](#business-module-apis)
11. [Testing Guide](#testing-guide)

## Overview

This document defines the API contract specifications for the LYBT Traditional Chinese Medicine Clinic Management System. All API design and implementation should follow the conventions in this document to ensure interface consistency, usability, and maintainability.

### Basic Information

- **Base URL**: `https://localhost:7001/api/v1`
- **Protocol**: HTTPS (mandatory in production)
- **Data Format**: JSON
- **Character Encoding**: UTF-8
- **API Version**: v1.0

## API Design Principles

### RESTful Principles

1. **Resource-Oriented**: URLs represent resources, use nouns not verbs
2. **Uniform Interface**: Use standard HTTP methods to represent operations
3. **Stateless**: Each request contains all necessary information
4. **Layered System**: Client doesn't need to know if directly connected to server
5. **Cacheable**: Responses should clearly identify if cacheable

### Naming Conventions

- **URL Paths**: Lowercase letters, use hyphens to separate words
- **Query Parameters**: camelCase
- **JSON Properties**: camelCase
- **Controller Names**: Plural form (e.g., patients, doctors)

## URL Routing Standards

### Base Routing Pattern

```
/api/v{version}/[controller]
```

### Standard Route Examples

| Operation       | HTTP Method | URL Pattern             | Description                  |
| --------------- | ----------- | ----------------------- | ---------------------------- |
| Get List        | GET         | `/api/v1/patients`      | Get patient list             |
| Get Details     | GET         | `/api/v1/patients/{id}` | Get specific patient         |
| Create Resource | POST        | `/api/v1/patients`      | Create new patient           |
| Update Resource | PUT         | `/api/v1/patients/{id}` | Update patient info          |
| Delete Resource | DELETE      | `/api/v1/patients/{id}` | Delete patient (soft delete) |

### Business Route Examples

| Operation     | HTTP Method | URL Pattern                      | Description            |
| ------------- | ----------- | -------------------------------- | ---------------------- |
| Paged Query   | POST        | `/api/v1/patients/paged`         | Paged patient query    |
| Batch Enable  | POST        | `/api/v1/patients/batch-enable`  | Batch enable patients  |
| Batch Disable | POST        | `/api/v1/patients/batch-disable` | Batch disable patients |
| Search        | GET         | `/api/v1/patients/search`        | Search patients        |
| Import        | POST        | `/api/v1/patients/import`        | Import patient data    |
| Export        | GET         | `/api/v1/patients/export`        | Export patient data    |

## HTTP Method Standards

### Method Semantics

| Method | Semantics       | Idempotent | Safe | Request Body | Response Body |
| ------ | --------------- | ---------- | ---- | ------------ | ------------- |
| GET    | Get resource    | Yes        | Yes  | No           | Yes           |
| POST   | Create resource | No         | No   | Yes          | Yes           |
| PUT    | Full update     | Yes        | No   | Yes          | Yes           |
| PATCH  | Partial update  | Yes        | No   | Yes          | Yes           |
| DELETE | Delete resource | Yes        | No   | No           | No            |

### Usage Standards

1. **GET**: Only for retrieving data, should have no side effects
2. **POST**: For creating new resources or non-idempotent operations
3. **PUT**: For complete resource replacement
4. **PATCH**: For partial resource updates
5. **DELETE**: For deleting resources (this system uses soft delete)

## Request Standards

### Request Headers

Required headers:

```http
Content-Type: application/json
Accept: application/json
Authorization: Bearer {token}
```

Optional headers:

```http
Accept-Language: zh-CN
X-Request-ID: {uuid}
```

### Request Parameters

#### Query Parameters

For filtering, sorting, pagination:

```
GET /api/v1/patients?pageNumber=1&pageSize=20&searchTerm=zhang&orderBy=name
```

#### Path Parameters

For identifying specific resources:

```
GET /api/v1/patients/{id}
PUT /api/v1/patients/{id}
```

#### Request Body

Use JSON format when creating or updating resources:

```json
{
  "name": "Zhang San",
  "idNumber": "110101199001011234",
  "phoneNumber": "13800138000",
  "gender": 1,
  "birthDate": "1990-01-01"
}
```

### Pagination Request

Unified pagination request format:

```json
{
  "pageNumber": 1,
  "pageSize": 20,
  "searchTerm": "zhang",
  "orderBy": "name",
  "isDescending": false,
  "filters": {
    "isActive": true,
    "gender": 1
  }
}
```

## Response Standards

### Success Response

#### Single Resource

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Zhang San",
  "phoneNumber": "13800138000",
  "createdAt": "2024-01-01T08:00:00Z"
}
```

#### Resource List

```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Zhang San"
  },
  {
    "id": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
    "name": "Li Si"
  }
]
```

#### Paginated Response

```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Zhang San"
    }
  ],
  "totalCount": 100,
  "currentPage": 1,
  "pageSize": 20,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

#### Operation Response

```json
{
  "message": "Operation successful",
  "affectedRows": 5
}
```

### Response Status Codes

| Status Code | Meaning               | Use Case                                   |
| ----------- | --------------------- | ------------------------------------------ |
| 200         | OK                    | Successfully retrieved or updated resource |
| 201         | Created               | Successfully created resource              |
| 204         | No Content            | Successfully deleted resource              |
| 400         | Bad Request           | Request parameter error                    |
| 401         | Unauthorized          | Not authenticated                          |
| 403         | Forbidden             | No permission                              |
| 404         | Not Found             | Resource does not exist                    |
| 409         | Conflict              | Resource conflict                          |
| 422         | Unprocessable Entity  | Business logic error                       |
| 500         | Internal Server Error | Server internal error                      |

## Authentication and Authorization

### JWT Authentication

#### Login Request

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "username": "sysadmin",
  "password": "Admin@123456",
  "rememberMe": false
}
```

#### Login Response

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 28800,
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "username": "sysadmin",
    "realName": "System Administrator",
    "role": "Admin"
  }
}
```

### Request Authentication

All requests requiring authentication must include JWT Token in the request header:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Token Configuration

- **Default Expiration**: 8 hours (480 minutes)
- **Remember Me Expiration**: 30 days (43200 minutes)
- **Clock Skew**: 5 minutes (300 seconds)

## Error Handling

### Error Response Format

Using RFC 7807 Problem Details standard:

```json
{
  "type": "https://example.com/probs/out-of-credit",
  "title": "Insufficient balance",
  "status": 403,
  "detail": "Your current balance is 30, but this operation requires 50.",
  "instance": "/account/12345/withdraw"
}
```

### Validation Errors

```json
{
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "name": ["Name cannot be empty", "Name cannot exceed 50 characters"],
    "phoneNumber": ["Invalid phone number format"]
  }
}
```

### Business Errors

```json
{
  "title": "Business error",
  "status": 422,
  "detail": "This patient already has an incomplete registration"
}
```

## API Versioning

### Versioning Strategy

- **Version Location**: In URL path (/api/v1/...)
- **Version Format**: v{major}
- **Backward Compatibility**: Minor version updates maintain backward compatibility
- **Deprecation Notice**: 3 months advance notice for API deprecation

### Version Negotiation

Supports multiple version passing methods:

1. **URL Path** (recommended):
   
   ```
   /api/v1/patients
   ```

2. **Query Parameter**:
   
   ```
   /api/patients?api-version=1.0
   ```

3. **Request Header**:
   
   ```
   X-API-Version: 1.0
   ```

## Business Module APIs

### 1. Authentication Module (Auth)

#### Login

```http
POST /api/v1/auth/login
```

Request body:

```json
{
  "username": "string",
  "password": "string",
  "rememberMe": false
}
```

#### Logout

```http
POST /api/v1/auth/logout
```

#### Refresh Token

```http
POST /api/v1/auth/refresh
```

#### Change Password

```http
POST /api/v1/auth/change-password
```

Request body:

```json
{
  "oldPassword": "string",
  "newPassword": "string"
}
```

### 2. Patient Management (Patients)

#### Get Patient List

```http
GET /api/v1/patients?pageNumber=1&pageSize=20
```

#### Get Patient Details

```http
GET /api/v1/patients/{id}
```

#### Create Patient

```http
POST /api/v1/patients
```

Request body:

```json
{
  "name": "Zhang San",
  "idNumber": "110101199001011234",
  "phoneNumber": "13800138000",
  "gender": 1,
  "birthDate": "1990-01-01",
  "address": "Beijing Chaoyang District",
  "emergencyContact": "Li Si",
  "emergencyPhone": "13900139000"
}
```

#### Update Patient

```http
PUT /api/v1/patients/{id}
```

#### Delete Patient

```http
DELETE /api/v1/patients/{id}
```

### 3. Registration Management (Registration)

#### Create Registration

```http
POST /api/v1/registrations
```

Request body:

```json
{
  "patientId": "550e8400-e29b-41d4-a716-446655440000",
  "doctorId": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
  "registrationType": 1,
  "appointmentTime": "2024-01-20T09:00:00",
  "remark": "First visit"
}
```

#### Cancel Registration

```http
POST /api/v1/registrations/{id}/cancel
```

Request body:

```json
{
  "reason": "Patient has urgent matters"
}
```

#### Complete Visit

```http
POST /api/v1/registrations/{id}/complete
```

### 4. Prescription Management (Prescriptions)

#### Create Prescription

```http
POST /api/v1/prescriptions
```

Request body:

```json
{
  "patientId": "550e8400-e29b-41d4-a716-446655440000",
  "recordId": "7ba7b810-9dad-11d1-80b4-00c04fd430c8",
  "items": [
    {
      "herbId": "8ba7b810-9dad-11d1-80b4-00c04fd430c8",
      "dosage": 10,
      "unit": "g",
      "usage": "Decoct with water"
    }
  ],
  "totalDays": 7,
  "dailyTimes": 2,
  "instructions": "Take after meals"
}
```

#### Approve Prescription

```http
POST /api/v1/prescriptions/{id}/approve
```

Request body:

```json
{
  "approved": true,
  "comment": "Approved"
}
```

### 5. Herb Management (Herbs)

#### Get Herb List

```http
GET /api/v1/herbs?category=1&isActive=true
```

#### Update Stock

```http
PATCH /api/v1/herbs/{id}/stock
```

Request body:

```json
{
  "quantity": 100,
  "operation": "add",
  "reason": "Purchase stock"
}
```

#### Update Price

```http
PATCH /api/v1/herbs/{id}/price
```

Request body:

```json
{
  "newPrice": 25.50,
  "effectiveDate": "2024-02-01"
}
```

### 6. Billing Settlement (Billing)

#### Generate Bill

```http
POST /api/v1/billing/generate
```

Request body:

```json
{
  "registrationId": "550e8400-e29b-41d4-a716-446655440000",
  "prescriptionId": "6ba7b810-9dad-11d1-80b4-00c04fd430c8"
}
```

#### Pay Bill

```http
POST /api/v1/billing/{id}/pay
```

Request body:

```json
{
  "paymentMethod": "cash",
  "amount": 280.50,
  "remark": "Cash payment"
}
```

#### Refund

```http
POST /api/v1/billing/{id}/refund
```

Request body:

```json
{
  "refundAmount": 100.00,
  "reason": "Some herbs out of stock"
}
```

## Testing Guide

### Using Swagger UI

1. Visit `https://localhost:7001/swagger`
2. Click "Authorize" button
3. Enter JWT Token (format: Bearer {token})
4. Select API endpoint to test
5. Fill in request parameters
6. Click "Execute" to send request

### Using Postman

#### Environment Configuration

```json
{
  "baseUrl": "https://localhost:7001/api/v1",
  "token": "{{jwt_token}}"
}
```

#### Authentication Configuration

1. Select "Bearer Token" in Authorization tab
2. Set Token value to `{{token}}`

#### Test Script Example

Login and save token:

```javascript
// Tests tab
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Save token", function () {
    var jsonData = pm.response.json();
    pm.environment.set("token", jsonData.token);
});
```

### Using cURL

#### Login Request

```bash
curl -X POST https://localhost:7001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"sysadmin","password":"Admin@123456"}'
```

#### Authenticated Request

```bash
curl -X GET https://localhost:7001/api/v1/patients \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Using REST Client (VS Code)

Create `.http` file:

```http
@baseUrl = https://localhost:7001/api/v1
@token = eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

### Login
POST {{baseUrl}}/auth/login
Content-Type: application/json

{
  "username": "sysadmin",
  "password": "Admin@123456"
}

### Get Patient List
GET {{baseUrl}}/patients
Authorization: Bearer {{token}}
```

## Best Practices

### Request Design

1. **Idempotency**: PUT, DELETE, PATCH operations should be idempotent
2. **Resource Location**: Use ID rather than other attributes to locate resources
3. **Batch Operations**: Provide batch interfaces to reduce request count
4. **Query Optimization**: Support field filtering to reduce data transfer

### Response Design

1. **Minimization**: Return only necessary data
2. **Consistency**: Same type of resources return same fields
3. **Time Format**: Use ISO 8601 format uniformly
4. **Null Handling**: Clearly distinguish between null and empty strings

### Error Handling

1. **Detailed Information**: Provide sufficient error information for debugging
2. **Error Codes**: Use consistent error code system
3. **Internationalization**: Support multi-language error messages
4. **Security**: Don't expose sensitive system information

### Performance Optimization

1. **Pagination**: Large datasets must be paginated
2. **Caching**: Use HTTP cache headers appropriately
3. **Compression**: Enable GZIP compression
4. **Asynchronous**: Use asynchronous mode for long operations

### Security Standards

1. **HTTPS**: Mandatory HTTPS in production
2. **Authentication**: All sensitive operations require authentication
3. **Authorization**: Implement fine-grained permission control
4. **Validation**: Strictly validate all input parameters
5. **Auditing**: Log all important operations

## Version History

| Version | Date       | Description     |
| ------- | ---------- | --------------- |
| 1.0     | 2024-01-01 | Initial version |

## Contact Information

- **API Support**: api-support@lybt.com
- **Technical Documentation**: https://docs.lybt.com/api
- **Issue Feedback**: https://github.com/lybt/api/issues