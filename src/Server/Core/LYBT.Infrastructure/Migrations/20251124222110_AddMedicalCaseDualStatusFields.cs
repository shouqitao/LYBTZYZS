using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalCaseDualStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 先删除依赖NeedsPrescription列的Filtered Index（如果存在）
            migrationBuilder.Sql(
                @"IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCases_NeedsPrescription_Filtered')
                  DROP INDEX IX_MedicalCases_NeedsPrescription_Filtered ON MedicalCases");

            migrationBuilder.AlterColumn<bool>(
                name: "NeedsPrescription",
                table: "MedicalCases",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            // 重新创建Filtered Index
            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCases_NeedsPrescription_Filtered')
                  CREATE INDEX IX_MedicalCases_NeedsPrescription_Filtered ON MedicalCases (NeedsPrescription) WHERE NeedsPrescription IS NOT NULL");

            // Step 1: 添加新的 CaseStatus 列
            migrationBuilder.AddColumn<int>(
                name: "CaseStatus",
                table: "MedicalCases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Step 2: 将旧 Status 列的字符串数据转换为 CaseStatus 的枚举整数值
            // 旧Status存储格式：nvarchar类型字符串 ('Active', 'Completed', 'Cancelled', 'Draft')
            // 新CaseStatus枚举值：Draft=0, Active=1, Completed=2, Cancelled=3
            migrationBuilder.Sql(
                @"UPDATE MedicalCases
                  SET CaseStatus = CASE Status
                      WHEN 'Draft' THEN 0
                      WHEN 'Active' THEN 1
                      WHEN 'Completed' THEN 2
                      WHEN 'Cancelled' THEN 3
                      ELSE 1  -- 默认为Active
                  END");

            // Step 3: 将 Status 列转换为 CommonStatus 枚举类型（整数）
            // 先添加临时列
            migrationBuilder.AddColumn<int>(
                name: "Status_New",
                table: "MedicalCases",
                type: "int",
                nullable: false,
                defaultValue: 1); // CommonStatus.Enabled = 1

            // 设置所有现有医案为"启用"状态
            migrationBuilder.Sql(
                @"UPDATE MedicalCases
                  SET Status_New = 1"); // CommonStatus.Enabled = 1

            // 删除旧Status列之前，先删除所有依赖的索引
            migrationBuilder.Sql(
                @"IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_MedicalCases_Patient_ActiveOnly')
                  DROP INDEX UX_MedicalCases_Patient_ActiveOnly ON MedicalCases");

            migrationBuilder.Sql(
                @"IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCase_Doctor_Status')
                  DROP INDEX IX_MedicalCase_Doctor_Status ON MedicalCases");

            migrationBuilder.Sql(
                @"IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCase_Status_Date')
                  DROP INDEX IX_MedicalCase_Status_Date ON MedicalCases");

            migrationBuilder.Sql(
                @"IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCases_Status')
                  DROP INDEX IX_MedicalCases_Status ON MedicalCases");

            migrationBuilder.Sql(
                @"IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCases_PatientId_Status')
                  DROP INDEX IX_MedicalCases_PatientId_Status ON MedicalCases");

            // 删除旧Status列
            migrationBuilder.DropColumn(
                name: "Status",
                table: "MedicalCases");

            // 重命名Status_New为Status
            migrationBuilder.RenameColumn(
                name: "Status_New",
                table: "MedicalCases",
                newName: "Status");

            // 重新创建索引（使用CaseStatus替代旧的Status列）
            // 注意：原索引基于旧Status（业务状态），现在使用CaseStatus（业务状态）

            // 清理违反UNIQUE约束的数据：每个患者只保留最新的Active医案，其他设为Completed
            migrationBuilder.Sql(
                @"WITH RankedCases AS (
                      SELECT Id, PatientId, CreatedAt,
                             ROW_NUMBER() OVER (PARTITION BY PatientId ORDER BY CreatedAt DESC) AS RowNum
                      FROM MedicalCases
                      WHERE CaseStatus = 1  -- Active
                  )
                  UPDATE mc
                  SET CaseStatus = 2  -- Completed
                  FROM MedicalCases mc
                  INNER JOIN RankedCases rc ON mc.Id = rc.Id
                  WHERE rc.RowNum > 1");

            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_MedicalCases_Patient_ActiveOnly')
                  CREATE UNIQUE INDEX UX_MedicalCases_Patient_ActiveOnly ON MedicalCases (PatientId) WHERE CaseStatus = 1"); // Active=1

            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCase_Doctor_Status')
                  CREATE INDEX IX_MedicalCase_Doctor_Status ON MedicalCases (DoctorId, CaseStatus)");

            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCase_Status_Date')
                  CREATE INDEX IX_MedicalCase_Status_Date ON MedicalCases (CaseStatus, CreatedAt)");

            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCases_Status')
                  CREATE INDEX IX_MedicalCases_Status ON MedicalCases (CaseStatus)");

            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCases_PatientId_Status')
                  CREATE INDEX IX_MedicalCases_PatientId_Status ON MedicalCases (PatientId, CaseStatus)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除基于CaseStatus的索引
            migrationBuilder.Sql(
                @"IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_MedicalCases_Patient_ActiveOnly')
                  DROP INDEX UX_MedicalCases_Patient_ActiveOnly ON MedicalCases");

            migrationBuilder.Sql(
                @"IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCase_Doctor_Status')
                  DROP INDEX IX_MedicalCase_Doctor_Status ON MedicalCases");

            migrationBuilder.Sql(
                @"IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCase_Status_Date')
                  DROP INDEX IX_MedicalCase_Status_Date ON MedicalCases");

            migrationBuilder.Sql(
                @"IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCases_Status')
                  DROP INDEX IX_MedicalCases_Status ON MedicalCases");

            migrationBuilder.Sql(
                @"IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCases_PatientId_Status')
                  DROP INDEX IX_MedicalCases_PatientId_Status ON MedicalCases");

            // Step 1: 重命名当前Status列为临时列
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "MedicalCases",
                newName: "Status_Old");

            // Step 2: 添加新的Status列（nvarchar类型，恢复为字符串存储）
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "MedicalCases",
                type: "nvarchar(50)",
                nullable: false,
                defaultValue: "Active");

            // Step 3: 将CaseStatus的整数值转换回字符串写入Status列
            migrationBuilder.Sql(
                @"UPDATE MedicalCases
                  SET Status = CASE CaseStatus
                      WHEN 0 THEN 'Draft'
                      WHEN 1 THEN 'Active'
                      WHEN 2 THEN 'Completed'
                      WHEN 3 THEN 'Cancelled'
                      ELSE 'Active'
                  END");

            // Step 4: 删除临时列和CaseStatus列
            migrationBuilder.DropColumn(
                name: "Status_Old",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "CaseStatus",
                table: "MedicalCases");

            // 重新创建原始索引（基于字符串Status列）
            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_MedicalCases_Patient_ActiveOnly')
                  CREATE UNIQUE INDEX UX_MedicalCases_Patient_ActiveOnly ON MedicalCases (PatientId) WHERE Status = 'Active'");

            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCase_Doctor_Status')
                  CREATE INDEX IX_MedicalCase_Doctor_Status ON MedicalCases (DoctorId, Status)");

            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCase_Status_Date')
                  CREATE INDEX IX_MedicalCase_Status_Date ON MedicalCases (Status, CreatedAt)");

            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCases_Status')
                  CREATE INDEX IX_MedicalCases_Status ON MedicalCases (Status)");

            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCases_PatientId_Status')
                  CREATE INDEX IX_MedicalCases_PatientId_Status ON MedicalCases (PatientId, Status)");

            // 删除NeedsPrescription的Filtered Index
            migrationBuilder.Sql(
                @"IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MedicalCases_NeedsPrescription_Filtered')
                  DROP INDEX IX_MedicalCases_NeedsPrescription_Filtered ON MedicalCases");

            migrationBuilder.AlterColumn<bool>(
                name: "NeedsPrescription",
                table: "MedicalCases",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            // 重新创建NeedsPrescription的原始Filtered Index（如果需要）
            // 注意：原始迁移可能没有这个索引，这里先注释掉
            // migrationBuilder.Sql(
            //     @"CREATE INDEX IX_MedicalCases_NeedsPrescription_Filtered ON MedicalCases (NeedsPrescription) WHERE NeedsPrescription = 0");
        }
    }
}
