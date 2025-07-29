#!/bin/bash

echo "======================================"
echo "初始化 LYBT 中医诊所管理系统数据库"
echo "======================================"
echo

# 定义需要处理的模块列表
MODULES=(
    "Herbs"
    "FormulaTemplates"
    "Prescriptions"
    "Records"
    "DiagnosisTreatment"
    "Billing"
    "Pharmacy"
    "Registration"
    "Queueing"
    "TreatmentRoom"
    "Sync"
    "Diagnostics"
)

# 处理每个模块
for MODULE in "${MODULES[@]}"; do
    echo
    echo "====================================="
    echo "处理模块: LYBT.Module.$MODULE"
    echo "====================================="
    
    # 尝试添加迁移（如果需要）
    echo "检查是否需要添加新迁移..."
    MIGRATION_OUTPUT=$(dotnet ef migrations add UpdateModel --project LYBT.Module.$MODULE --startup-project LYBT.WebAPI 2>&1)
    
    if [[ $MIGRATION_OUTPUT == *"No changes were detected"* ]]; then
        echo "模块 $MODULE: 无需添加新迁移"
    elif [[ $MIGRATION_OUTPUT == *"Done"* ]]; then
        echo "模块 $MODULE: 成功添加迁移"
    else
        echo "模块 $MODULE: 添加迁移时出现问题："
        echo "$MIGRATION_OUTPUT"
    fi
    
    # 更新数据库
    echo "更新 LYBT.Module.$MODULE 数据库..."
    UPDATE_OUTPUT=$(dotnet ef database update --project LYBT.Module.$MODULE --startup-project LYBT.WebAPI 2>&1)
    
    if [[ $UPDATE_OUTPUT == *"Done"* ]]; then
        echo "✓ 模块 $MODULE 数据库更新成功"
    else
        echo "✗ 模块 $MODULE 数据库更新失败："
        echo "$UPDATE_OUTPUT"
        exit 1
    fi
done

echo
echo "======================================"
echo "所有模块数据库初始化完成！"
echo "======================================"
echo