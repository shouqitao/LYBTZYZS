#!/bin/bash
# Bash脚本：运行测试并生成覆盖率报告
# 使用方法: ./tests/run-coverage.sh

# 设置颜色
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# 参数
CONFIGURATION=${1:-Release}
OPEN_REPORT=${2:-true}
ENFORCE_THRESHOLDS=${3:-false}

echo -e "${CYAN}======================================"
echo -e " 服务端单元测试覆盖率收集工具"
echo -e "======================================${NC}"
echo ""

# 定义路径
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
OUTPUT_DIR="$ROOT_DIR/BIN/TestResults"
COVERAGE_DIR="$OUTPUT_DIR/coverage"

# 清理旧的测试结果
echo -e "${YELLOW}清理旧的测试结果...${NC}"
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"
mkdir -p "$COVERAGE_DIR"

# 还原NuGet包
echo ""
echo -e "${YELLOW}还原NuGet包...${NC}"
dotnet restore "$ROOT_DIR/LYBT.Server.sln" --nologo

# 构建解决方案
echo ""
echo -e "${YELLOW}构建解决方案...${NC}"
dotnet build "$ROOT_DIR/LYBT.Server.sln" -c "$CONFIGURATION" --no-restore --nologo

if [ $? -ne 0 ]; then
    echo -e "${RED}构建失败！${NC}"
    exit 1
fi

# 运行测试并收集覆盖率
echo ""
echo -e "${YELLOW}运行测试并收集覆盖率...${NC}"
echo -e "这可能需要几分钟时间，请耐心等待..."

TEST_COMMAND="dotnet test \"$ROOT_DIR/LYBT.Server.sln\" \
    -c $CONFIGURATION \
    --no-build \
    --no-restore \
    --collect:\"XPlat Code Coverage\" \
    --results-directory \"$OUTPUT_DIR\" \
    --logger \"trx;LogFileName=test-results.trx\" \
    --logger \"console;verbosity=minimal\""

if [ "$ENFORCE_THRESHOLDS" == "true" ]; then
    TEST_COMMAND="$TEST_COMMAND -p:EnforceCoverageThresholds=true"
fi

eval $TEST_COMMAND
TEST_EXIT_CODE=$?

# 查找覆盖率文件
echo ""
echo -e "${YELLOW}查找覆盖率文件...${NC}"
COVERAGE_FILES=$(find "$OUTPUT_DIR" -name "coverage.cobertura.xml")

if [ -z "$COVERAGE_FILES" ]; then
    echo -e "${RED}未找到覆盖率文件！${NC}"
    exit 1
fi

COVERAGE_COUNT=$(echo "$COVERAGE_FILES" | wc -l)
echo -e "${GREEN}找到 $COVERAGE_COUNT 个覆盖率文件${NC}"

# 检查ReportGenerator工具
echo ""
echo -e "${YELLOW}检查ReportGenerator工具...${NC}"
if ! command -v reportgenerator &> /dev/null; then
    echo -e "${YELLOW}安装ReportGenerator工具...${NC}"
    dotnet tool install -g dotnet-reportgenerator-globaltool
fi

# 生成HTML报告
echo ""
echo -e "${YELLOW}生成覆盖率报告...${NC}"

# 构建报告文件列表
REPORT_FILES=""
for FILE in $COVERAGE_FILES; do
    if [ -n "$REPORT_FILES" ]; then
        REPORT_FILES="$REPORT_FILES;$FILE"
    else
        REPORT_FILES="$FILE"
    fi
done

reportgenerator \
    -reports:"$REPORT_FILES" \
    -targetdir:"$COVERAGE_DIR" \
    -reporttypes:Html\;Cobertura\;JsonSummary\;Badges \
    -title:"LYBT服务端测试覆盖率报告" \
    -tag:"$(date '+%Y-%m-%d %H:%M:%S')" \
    -verbosity:Info

if [ $? -ne 0 ]; then
    echo -e "${RED}报告生成失败！${NC}"
    exit 1
fi

# 显示覆盖率摘要
echo ""
echo -e "${GREEN}======================================"
echo -e " 覆盖率收集完成"
echo -e "======================================${NC}"

# 读取并显示覆盖率摘要
SUMMARY_FILE="$COVERAGE_DIR/Summary.json"
if [ -f "$SUMMARY_FILE" ]; then
    echo ""
    echo -e "${CYAN}覆盖率摘要：${NC}"

    # 使用jq解析JSON（如果可用）
    if command -v jq &> /dev/null; then
        LINE_COVERAGE=$(jq -r '.summary.linecoverage' "$SUMMARY_FILE")
        BRANCH_COVERAGE=$(jq -r '.summary.branchcoverage' "$SUMMARY_FILE")
        METHOD_COVERAGE=$(jq -r '.summary.methodcoverage' "$SUMMARY_FILE")

        echo "  - 行覆盖率: ${LINE_COVERAGE}%"
        echo "  - 分支覆盖率: ${BRANCH_COVERAGE}%"
        echo "  - 方法覆盖率: ${METHOD_COVERAGE}%"

        # 检查是否达到阈值
        if [ "$ENFORCE_THRESHOLDS" == "true" ]; then
            LINE_THRESHOLD=90
            BRANCH_THRESHOLD=80

            if (( $(echo "$LINE_COVERAGE < $LINE_THRESHOLD" | bc -l) )); then
                echo ""
                echo -e "${YELLOW}警告：行覆盖率 (${LINE_COVERAGE}%) 低于阈值 (${LINE_THRESHOLD}%)！${NC}"
            fi

            if (( $(echo "$BRANCH_COVERAGE < $BRANCH_THRESHOLD" | bc -l) )); then
                echo -e "${YELLOW}警告：分支覆盖率 (${BRANCH_COVERAGE}%) 低于阈值 (${BRANCH_THRESHOLD}%)！${NC}"
            fi
        fi
    else
        echo "  提示：安装jq以查看详细的覆盖率统计"
    fi
fi

echo ""
echo -e "${CYAN}报告位置：${NC}"
echo "  HTML报告: $COVERAGE_DIR/index.html"
echo "  Cobertura: $COVERAGE_DIR/Cobertura.xml"
echo "  JSON摘要: $COVERAGE_DIR/Summary.json"

# 打开HTML报告
if [ "$OPEN_REPORT" == "true" ]; then
    echo ""
    echo -e "${YELLOW}正在打开HTML报告...${NC}"

    # 根据操作系统打开报告
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        xdg-open "$COVERAGE_DIR/index.html" 2>/dev/null &
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        open "$COVERAGE_DIR/index.html"
    elif [[ "$OSTYPE" == "cygwin" ]] || [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
        start "$COVERAGE_DIR/index.html"
    fi
fi

# 返回测试结果
if [ $TEST_EXIT_CODE -ne 0 ]; then
    echo ""
    echo -e "${RED}测试失败！请检查测试输出。${NC}"
    exit $TEST_EXIT_CODE
fi

echo ""
echo -e "${GREEN}所有测试通过！${NC}"
exit 0