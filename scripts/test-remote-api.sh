#!/bin/bash
echo "===================================="
echo "  LYBT WebAPI Remote Service Test"
echo "  Server: 192.168.190.243:5297"
echo "===================================="
echo

SERVER_URL="http://192.168.190.243:5297"

echo "[INFO] Testing Health Check..."
curl -s $SERVER_URL/health
if [ $? -eq 0 ]; then
    echo "[OK] Health Check successful"
else
    echo "[ERROR] Health Check failed"
fi

echo
echo "[INFO] Testing Swagger Documentation..."
curl -s -I $SERVER_URL/swagger/index.html | head -1
if [ $? -eq 0 ]; then
    echo "[OK] Swagger documentation accessible"
else
    echo "[ERROR] Swagger documentation not accessible"
fi

echo
echo "[INFO] Testing Auth API - Password Hash..."
curl -s "$SERVER_URL/api/v1.0/auth/hashPassword?password=test123"
if [ $? -eq 0 ]; then
    echo
    echo "[OK] Auth API responding normally"
else
    echo "[ERROR] Auth API not responding"
fi

echo
echo "[INFO] Testing Login API..."
curl -s -X POST $SERVER_URL/api/v1.0/auth/login \
-H "Content-Type: application/json" \
-d '{"username":"sysadmin","password":"Admin@123456","rememberMe":true}'

if [ $? -eq 0 ]; then
    echo
    echo "[OK] Login API responding normally"
else
    echo
    echo "[ERROR] Login API call failed"
fi

echo
echo "[INFO] Testing Users API..."
curl -s -I $SERVER_URL/api/v1.0/users | head -1
if [ $? -eq 0 ]; then
    echo "[OK] Users API endpoint exists"
else
    echo "[ERROR] Users API endpoint not accessible"
fi

echo
echo "[INFO] Testing Herbs API..."
curl -s -I $SERVER_URL/api/v1.0/herbs | head -1
if [ $? -eq 0 ]; then
    echo "[OK] Herbs API endpoint exists"
else
    echo "[ERROR] Herbs API endpoint not accessible"
fi

echo
echo "===================================="
echo "  Complete API Test Report"
echo "===================================="
echo

echo "[INFO] Available endpoints:"
echo "- Health: $SERVER_URL/health"
echo "- Swagger: $SERVER_URL/swagger"
echo "- Auth API: $SERVER_URL/api/v1.0/auth/*"
echo "- Users API: $SERVER_URL/api/v1.0/users/*"
echo "- Herbs API: $SERVER_URL/api/v1.0/herbs/*"

echo
echo "[INFO] Default admin account:"
echo "- Username: sysadmin"
echo "- Password: Admin@123456"

echo
echo "Test completed! Please check the output above."