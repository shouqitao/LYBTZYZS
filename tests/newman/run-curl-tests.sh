#!/bin/bash
# LYBT WebAPI 集成测试 - curl 版本

BASE_URL="http://192.168.190.246:5000"
PASS=0
FAIL=0
TOKEN=""
PATIENT_ID=""
HERB_ID=""

echo "============================================"
echo "  LYBT WebAPI 集成测试"
echo "  服务: $BASE_URL"
echo "============================================"

test_api() {
    local desc="$1"
    local method="$2"
    local url="$3"
    local data="$4"
    local expect_status="$5"
    
    if [ "$method" = "POST" ] && [ -n "$data" ]; then
        RESP=$(curl -s -w "\n%{http_code}" -X POST "$url" -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" -d "$data")
    elif [ "$method" = "DELETE" ]; then
        RESP=$(curl -s -w "\n%{http_code}" -X DELETE "$url" -H "Authorization: Bearer $TOKEN")
    else
        RESP=$(curl -s -w "\n%{http_code}" -X GET "$url" -H "Authorization: Bearer $TOKEN")
    fi
    
    STATUS=$(echo "$RESP" | tail -1)
    BODY=$(echo "$RESP" | sed '$d')
    
    if [ "$STATUS" = "$expect_status" ]; then
        echo "  ✅ $desc (HTTP $STATUS)"
        PASS=$((PASS+1))
    else
        echo "  ❌ $desc (期望 $expect_status, 实际 $STATUS)"
        echo "     $BODY"
        FAIL=$((FAIL+1))
    fi
}

# --- 1. Health ---
echo ""
echo "[1/9] Health"
test_api "GET /health" GET "$BASE_URL/health" "" "200"
test_api "GET /health/ping" GET "$BASE_URL/health/ping" "" "200"

# --- 2. Auth ---
echo ""
echo "[2/9] Auth"
LOGIN_RESP=$(curl -s -X POST "$BASE_URL/api/v1/auth/login" -H "Content-Type: application/json" -d '{"username":"sysadmin","password":"SysAdmin@2026a!"}')
TOKEN=$(echo "$LOGIN_RESP" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['token'])" 2>/dev/null)

if [ -n "$TOKEN" ]; then
    echo "  ✅ POST /auth/login (获取Token)"
    PASS=$((PASS+1))
else
    echo "  ❌ POST /auth/login (登录失败)"
    echo "     $LOGIN_RESP"
    FAIL=$((FAIL+1))
    echo "  测试终止: 无法获取Token"
    exit 1
fi

test_api "GET /auth/validate" GET "$BASE_URL/api/v1/auth/validate" "" "200"

# --- 3. Users ---
echo ""
echo "[3/9] Users"
test_api "GET /users (列表)" GET "$BASE_URL/api/v1/users?page=1&pageSize=5" "" "200"
test_api "GET /users/current" GET "$BASE_URL/api/v1/users/current" "" "200"

# --- 4. Patients ---
echo ""
echo "[4/9] Patients"
PATIENT_RESP=$(curl -s -X POST "$BASE_URL/api/v1/patients" -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" -d '{"name":"集成测试患者","gender":1,"birthDate":"1990-01-01","phoneNumber":"13900139000","idNumber":"110101199001019999"}')
PATIENT_ID=$(echo "$PATIENT_RESP" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['id'])" 2>/dev/null)

if [ -n "$PATIENT_ID" ]; then
    echo "  ✅ POST /patients (创建患者)"
    PASS=$((PASS+1))
else
    echo "  ❌ POST /patients"
    echo "     $PATIENT_RESP"
    FAIL=$((FAIL+1))
fi

test_api "GET /patients (列表)" GET "$BASE_URL/api/v1/patients?page=1&pageSize=5" "" "200"
test_api "GET /patients/{id} (详情)" GET "$BASE_URL/api/v1/patients/$PATIENT_ID" "" "200"
test_api "GET /patients/{id}/check-reference" GET "$BASE_URL/api/v1/patients/$PATIENT_ID/check-reference" "" "200"

# --- 5. Herbs ---
echo ""
echo "[5/9] Herbs"
HERB_RESP=$(curl -s -X POST "$BASE_URL/api/v1/herbs" -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" -d '{"name":"集成测试药材-金银花","category":"清热解毒药","effect":"清热解毒","price":35.0}')
HERB_ID=$(echo "$HERB_RESP" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['id'])" 2>/dev/null)

if [ -n "$HERB_ID" ]; then
    echo "  ✅ POST /herbs (创建药材)"
    PASS=$((PASS+1))
else
    echo "  ❌ POST /herbs"
    echo "     $HERB_RESP"
    FAIL=$((FAIL+1))
fi

test_api "GET /herbs (列表)" GET "$BASE_URL/api/v1/herbs?page=1&pageSize=5" "" "200"
test_api "GET /herbs/{id}/check-reference" GET "$BASE_URL/api/v1/herbs/$HERB_ID/check-reference" "" "200"
test_api "POST /herbs/{id}/toggle-status" POST "$BASE_URL/api/v1/herbs/$HERB_ID/toggle-status" "" "200"

# --- 6. Registrations ---
echo ""
echo "[6/9] Registrations"
REG_RESP=$(curl -s -X POST "$BASE_URL/api/v1/registrations" -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" -d "{\"patientId\":\"$PATIENT_ID\",\"patientName\":\"集成测试患者\",\"doctorId\":\"$(echo "$LOGIN_RESP" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['user']['id'])" 2>/dev/null)\",\"doctorName\":\"系统运维\",\"source\":\"Doctor\",\"remark\":\"集成测试\"}")
REG_ID=$(echo "$REG_RESP" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['id'])" 2>/dev/null)

if [ -n "$REG_ID" ]; then
    echo "  ✅ POST /registrations (创建挂号)"
    PASS=$((PASS+1))
else
    echo "  ❌ POST /registrations"
    echo "     $REG_RESP"
    FAIL=$((FAIL+1))
fi

test_api "GET /registrations (列表)" GET "$BASE_URL/api/v1/registrations?page=1&pageSize=5" "" "200"
test_api "GET /registrations/queue (队列)" GET "$BASE_URL/api/v1/registrations/queue" "" "200"

# --- 7. Reports ---
echo ""
echo "[7/9] Reports"
test_api "GET /reports/daily/income" GET "$BASE_URL/api/v1/reports/daily/income" "" "200"
test_api "GET /reports/daily/consultations" GET "$BASE_URL/api/v1/reports/daily/consultations" "" "200"
test_api "GET /reports/daily/herbs" GET "$BASE_URL/api/v1/reports/daily/herbs" "" "200"

# --- 8. Configuration ---
echo ""
echo "[8/9] Configuration"
test_api "GET /configuration" GET "$BASE_URL/api/v1/configuration" "" "200"

# --- 9. Cleanup ---
echo ""
echo "[9/9] Cleanup"
if [ -n "$HERB_ID" ]; then
    test_api "DELETE /herbs/{id}" DELETE "$BASE_URL/api/v1/herbs/$HERB_ID" "" "200"
fi
if [ -n "$PATIENT_ID" ]; then
    test_api "DELETE /patients/{id}" DELETE "$BASE_URL/api/v1/patients/$PATIENT_ID" "" "200"
fi

# --- Summary ---
TOTAL=$((PASS+FAIL))
echo ""
echo "============================================"
echo "  测试结果: $PASS 通过 / $FAIL 失败 / $TOTAL 总计"
echo "============================================"

if [ $FAIL -eq 0 ]; then
    echo "  ✅ 全部通过!"
    exit 0
else
    echo "  ❌ 存在失败"
    exit 1
fi
