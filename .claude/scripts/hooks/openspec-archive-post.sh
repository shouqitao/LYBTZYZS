#!/bin/bash
# OpenSpec Archive Post-Hook
# 在 /openspec:archive 命令完成后触发，提醒Claude执行归档完成处理

# 从stdin读取JSON输入
INPUT=$(cat)

# 检查是否是SlashCommand工具
TOOL_NAME=$(echo "$INPUT" | jq -r '.tool_name // empty')

# 检查命令是否包含openspec:archive
COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // empty')

# 仅当是/openspec:archive命令时输出提醒
if [[ "$TOOL_NAME" == "SlashCommand" && "$COMMAND" == *"openspec:archive"* ]]; then
    echo ""
    echo "=================================================="
    echo "OpenSpec归档完成，请执行归档后处理流程："
    echo ""
    echo "调用skill: lybtzyzs-openspec-archive-finalize"
    echo ""
    echo "该skill将自动完成："
    echo "1. 代码审查"
    echo "2. 提交推送到远程仓库"
    echo "3. 保存Graphiti记忆"
    echo "4. 同步docs系统文档"
    echo "=================================================="
    echo ""
fi

exit 0
