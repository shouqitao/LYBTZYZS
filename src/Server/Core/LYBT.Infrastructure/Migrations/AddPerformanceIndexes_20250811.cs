using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations {

    /// <summary>
    /// 添加性能优化索引 - UltraThink重构数据库优化
    /// 基于CQRS查询模式的索引优化策略
    /// </summary>
    public partial class AddPerformanceIndexes_20250811 : Migration {

        protected override void Up(MigrationBuilder migrationBuilder) {
            // ===========================================
            // 用户表 (Users) 性能索引
            // ===========================================

            // 1. 用户名查询索引 - GetUserByUsernameQuery
            // 单字段唯一索引，支持精确查询
            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName_Unique",
                table: "Users",
                column: "UserName",
                unique: true);

            // 2. 邮箱查询索引 - 支持邮箱登录和验证
            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_Unique",
                table: "Users",
                column: "Email",
                unique: true);

            // 3. 复合查询索引 - GetUsersPagedQuery (角色 + 状态过滤)
            // 支持角色和活跃状态的组合查询
            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_IsActive_CreatedAt",
                table: "Users",
                columns: new[] { "Role", "IsActive", "CreatedAt" });

            // 4. 搜索优化索引 - SearchUsersQuery
            // 支持真实姓名的模糊搜索
            migrationBuilder.CreateIndex(
                name: "IX_Users_RealName_IsActive",
                table: "Users",
                columns: new[] { "RealName", "IsActive" });

            // 5. 日期范围查询索引 - GetUsersPagedQuery (日期过滤)
            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt_Role",
                table: "Users",
                columns: new[] { "CreatedAt", "Role" });

            // 6. 统计查询索引 - GetUserStatisticsQuery
            // 支持角色统计和活跃用户统计
            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive_Role_CreatedAt",
                table: "Users",
                columns: new[] { "IsActive", "Role", "CreatedAt" });

            // ===========================================
            // 患者表 (Patients) 性能索引
            // ===========================================

            // 1. 患者姓名查询索引
            migrationBuilder.CreateIndex(
                name: "IX_Patients_Name_IsActive",
                table: "Patients",
                columns: new[] { "Name", "IsActive" });

            // 2. 电话号码查询索引
            migrationBuilder.CreateIndex(
                name: "IX_Patients_PhoneNumber",
                table: "Patients",
                column: "PhoneNumber");

            // 3. 身份证号码查询索引
            migrationBuilder.CreateIndex(
                name: "IX_Patients_IdCardNumber",
                table: "Patients",
                column: "IdCardNumber");

            // 4. 患者搜索复合索引
            migrationBuilder.CreateIndex(
                name: "IX_Patients_Name_PhoneNumber_CreatedAt",
                table: "Patients",
                columns: new[] { "Name", "PhoneNumber", "CreatedAt" });

            // ===========================================
            // 中药材表 (Herbs) 性能索引
            // ===========================================

            // 1. 中药材名称查询索引
            migrationBuilder.CreateIndex(
                name: "IX_Herbs_Name_IsEnabled",
                table: "Herbs",
                columns: new[] { "Name", "IsEnabled" });

            // 2. 中药材类别查询索引
            migrationBuilder.CreateIndex(
                name: "IX_Herbs_Category_IsEnabled_CreatedAt",
                table: "Herbs",
                columns: new[] { "Category", "IsEnabled", "CreatedAt" });

            // 3. 库存状态查询索引
            migrationBuilder.CreateIndex(
                name: "IX_Herbs_Stock_Price_IsEnabled",
                table: "Herbs",
                columns: new[] { "Stock", "Price", "IsEnabled" });

            // ===========================================
            // 处方表 (Prescriptions) 性能索引
            // ===========================================

            // 1. 患者处方查询索引 - 最常用的查询
            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId_CreatedAt",
                table: "Prescriptions",
                columns: new[] { "PatientId", "CreatedAt" });

            // 2. 医生处方查询索引
            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_DoctorId_CreatedAt",
                table: "Prescriptions",
                columns: new[] { "DoctorId", "CreatedAt" });

            // 3. 处方状态查询索引
            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_Status_CreatedAt",
                table: "Prescriptions",
                columns: new[] { "Status", "CreatedAt" });

            // 4. 处方日期范围查询索引
            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_CreatedAt_Status_PatientId",
                table: "Prescriptions",
                columns: new[] { "CreatedAt", "Status", "PatientId" });

            // ===========================================
            // 验方模板表 (FormulaTemplates) 性能索引
            // ===========================================

            // 1. 验方名称查询索引
            migrationBuilder.CreateIndex(
                name: "IX_FormulaTemplates_Name_IsActive",
                table: "FormulaTemplates",
                columns: new[] { "Name", "IsActive" });

            // 2. 验方创建者查询索引
            migrationBuilder.CreateIndex(
                name: "IX_FormulaTemplates_CreatedBy_IsActive_CreatedAt",
                table: "FormulaTemplates",
                columns: new[] { "CreatedBy", "IsActive", "CreatedAt" });

            // ===========================================
            // 看诊记录表 (Consultations) 性能索引
            // ===========================================

            // 1. 患者看诊记录查询索引
            migrationBuilder.CreateIndex(
                name: "IX_Consultations_PatientId_ConsultationDate",
                table: "Consultations",
                columns: new[] { "PatientId", "ConsultationDate" });

            // 2. 医生看诊记录查询索引
            migrationBuilder.CreateIndex(
                name: "IX_Consultations_DoctorId_ConsultationDate",
                table: "Consultations",
                columns: new[] { "DoctorId", "ConsultationDate" });

            // 3. 看诊状态查询索引
            migrationBuilder.CreateIndex(
                name: "IX_Consultations_Status_ConsultationDate",
                table: "Consultations",
                columns: new[] { "Status", "ConsultationDate" });

            // ===========================================
            // 医疗案例表 (MedicalCases) 性能索引
            // ===========================================

            // 1. 患者医疗案例查询索引
            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_PatientId_CreatedAt",
                table: "MedicalCases",
                columns: new[] { "PatientId", "CreatedAt" });

            // 2. 医生医疗案例查询索引
            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_DoctorId_CreatedAt",
                table: "MedicalCases",
                columns: new[] { "DoctorId", "CreatedAt" });

            // 3. 案例状态查询索引
            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_Status_CreatedAt",
                table: "MedicalCases",
                columns: new[] { "Status", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder) {
            // ===========================================
            // 删除用户表索引
            // ===========================================
            migrationBuilder.DropIndex(name: "IX_Users_UserName_Unique", table: "Users");
            migrationBuilder.DropIndex(name: "IX_Users_Email_Unique", table: "Users");
            migrationBuilder.DropIndex(name: "IX_Users_Role_IsActive_CreatedAt", table: "Users");
            migrationBuilder.DropIndex(name: "IX_Users_RealName_IsActive", table: "Users");
            migrationBuilder.DropIndex(name: "IX_Users_CreatedAt_Role", table: "Users");
            migrationBuilder.DropIndex(name: "IX_Users_IsActive_Role_CreatedAt", table: "Users");

            // ===========================================
            // 删除患者表索引
            // ===========================================
            migrationBuilder.DropIndex(name: "IX_Patients_Name_IsActive", table: "Patients");
            migrationBuilder.DropIndex(name: "IX_Patients_PhoneNumber", table: "Patients");
            migrationBuilder.DropIndex(name: "IX_Patients_IdCardNumber", table: "Patients");
            migrationBuilder.DropIndex(name: "IX_Patients_Name_PhoneNumber_CreatedAt", table: "Patients");

            // ===========================================
            // 删除中药材表索引
            // ===========================================
            migrationBuilder.DropIndex(name: "IX_Herbs_Name_IsEnabled", table: "Herbs");
            migrationBuilder.DropIndex(name: "IX_Herbs_Category_IsEnabled_CreatedAt", table: "Herbs");
            migrationBuilder.DropIndex(name: "IX_Herbs_Stock_Price_IsEnabled", table: "Herbs");

            // ===========================================
            // 删除处方表索引
            // ===========================================
            migrationBuilder.DropIndex(name: "IX_Prescriptions_PatientId_CreatedAt", table: "Prescriptions");
            migrationBuilder.DropIndex(name: "IX_Prescriptions_DoctorId_CreatedAt", table: "Prescriptions");
            migrationBuilder.DropIndex(name: "IX_Prescriptions_Status_CreatedAt", table: "Prescriptions");
            migrationBuilder.DropIndex(name: "IX_Prescriptions_CreatedAt_Status_PatientId", table: "Prescriptions");

            // ===========================================
            // 删除验方模板表索引
            // ===========================================
            migrationBuilder.DropIndex(name: "IX_FormulaTemplates_Name_IsActive", table: "FormulaTemplates");
            migrationBuilder.DropIndex(name: "IX_FormulaTemplates_CreatedBy_IsActive_CreatedAt", table: "FormulaTemplates");

            // ===========================================
            // 删除看诊记录表索引
            // ===========================================
            migrationBuilder.DropIndex(name: "IX_Consultations_PatientId_ConsultationDate", table: "Consultations");
            migrationBuilder.DropIndex(name: "IX_Consultations_DoctorId_ConsultationDate", table: "Consultations");
            migrationBuilder.DropIndex(name: "IX_Consultations_Status_ConsultationDate", table: "Consultations");

            // ===========================================
            // 删除医疗案例表索引
            // ===========================================
            migrationBuilder.DropIndex(name: "IX_MedicalCases_PatientId_CreatedAt", table: "MedicalCases");
            migrationBuilder.DropIndex(name: "IX_MedicalCases_DoctorId_CreatedAt", table: "MedicalCases");
            migrationBuilder.DropIndex(name: "IX_MedicalCases_Status_CreatedAt", table: "MedicalCases");
        }
    }
}
