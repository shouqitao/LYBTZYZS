using System;
using System.Collections.Generic;
using Bogus;
using LYBT.Entities.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCase.Tests.Base
{
    /// <summary>
    /// 医疗案例测试数据生成器
    /// </summary>
    public static class MedicalCaseTestDataGenerator
    {
        private static readonly string[] PatientNames = {
            "张三", "李四", "王五", "赵六", "钱七", "孙八",
            "周九", "吴十", "郑十一", "王十二", "李明", "张伟",
            "刘芳", "陈静", "杨帆", "黄雯", "赵敏", "孙丽"
        };

        private static readonly string[] DoctorNames = {
            "陈医生", "李医生", "王医生", "张医生", "赵医生",
            "刘医生", "黄医生", "杨医生", "周医生", "吴医生"
        };

        /// <summary>
        /// 医疗案例数据生成器
        /// </summary>
        public static Faker<LYBT.Entities.MedicalCase.MedicalCase> MedicalCaseGenerator => 
            new Faker<LYBT.Entities.MedicalCase.MedicalCase>("zh_CN")
                .RuleFor(m => m.Id, f => Guid.NewGuid())
                .RuleFor(m => m.PatientId, f => Guid.NewGuid())
                .RuleFor(m => m.PatientName, f => f.PickRandom(PatientNames))
                .RuleFor(m => m.DoctorId, f => Guid.NewGuid())
                .RuleFor(m => m.DoctorName, f => f.PickRandom(DoctorNames))
                .RuleFor(m => m.PrescriptionId, f => f.Random.Bool(0.3f) ? Guid.NewGuid() : (Guid?)null)
                .RuleFor(m => m.ConsultationDate, f => f.Date.Recent(30))
                .RuleFor(m => m.Status, f => f.PickRandom<MedicalCaseStatus>())
                .RuleFor(m => m.Remark, f => f.Lorem.Sentence());

        /// <summary>
        /// 创建测试医疗案例
        /// </summary>
        public static LYBT.Entities.MedicalCase.MedicalCase CreateTestMedicalCase(
            string? patientName = null,
            string? doctorName = null,
            MedicalCaseStatus status = MedicalCaseStatus.Active)
        {
            var medicalCase = MedicalCaseGenerator.Generate();

            if (!string.IsNullOrEmpty(patientName))
                medicalCase.PatientName = patientName;

            if (!string.IsNullOrEmpty(doctorName))
                medicalCase.DoctorName = doctorName;

            medicalCase.Status = status;

            return medicalCase;
        }

        /// <summary>
        /// 批量创建测试医疗案例
        /// </summary>
        public static List<LYBT.Entities.MedicalCase.MedicalCase> CreateTestMedicalCases(int count, MedicalCaseStatus? status = null)
        {
            var generator = MedicalCaseGenerator;

            if (status.HasValue)
                generator = generator.RuleFor(m => m.Status, status.Value);

            // 确保名称唯一性
            var medicalCases = generator.Generate(count);
            for (int i = 0; i < medicalCases.Count; i++)
            {
                medicalCases[i].PatientName = $"{medicalCases[i].PatientName}_{i + 1}";
            }

            return medicalCases;
        }

        /// <summary>
        /// 创建活动状态的医疗案例
        /// </summary>
        public static LYBT.Entities.MedicalCase.MedicalCase CreateActiveMedicalCase()
        {
            return CreateTestMedicalCase(status: MedicalCaseStatus.Active);
        }

        /// <summary>
        /// 创建已关闭的医疗案例
        /// </summary>
        public static LYBT.Entities.MedicalCase.MedicalCase CreateClosedMedicalCase()
        {
            return CreateTestMedicalCase(status: MedicalCaseStatus.Closed);
        }

        /// <summary>
        /// 创建已完成的医疗案例 (兼容性)
        /// </summary>
        [Obsolete("Use CreateClosedMedicalCase() instead. Completed status merged into Closed.")]
        public static LYBT.Entities.MedicalCase.MedicalCase CreateCompletedMedicalCase()
        {
            return CreateTestMedicalCase(status: MedicalCaseStatus.Closed);
        }

        /// <summary>
        /// 创建已取消的医疗案例 (兼容性)
        /// </summary>
        [Obsolete("Use CreateClosedMedicalCase() instead. Cancelled status merged into Closed.")]
        public static LYBT.Entities.MedicalCase.MedicalCase CreateCancelledMedicalCase()
        {
            return CreateTestMedicalCase(status: MedicalCaseStatus.Closed);
        }

        /// <summary>
        /// 创建具有特定患者名称的医疗案例
        /// </summary>
        public static LYBT.Entities.MedicalCase.MedicalCase CreateMedicalCaseWithPatient(string patientName)
        {
            return CreateTestMedicalCase(patientName: patientName);
        }

        /// <summary>
        /// 创建具有特定医生名称的医疗案例
        /// </summary>
        public static LYBT.Entities.MedicalCase.MedicalCase CreateMedicalCaseWithDoctor(string doctorName)
        {
            return CreateTestMedicalCase(doctorName: doctorName);
        }

        /// <summary>
        /// 创建有处方的医疗案例
        /// </summary>
        public static LYBT.Entities.MedicalCase.MedicalCase CreateMedicalCaseWithPrescription()
        {
            var medicalCase = CreateTestMedicalCase();
            medicalCase.PrescriptionId = Guid.NewGuid();
            return medicalCase;
        }

        /// <summary>
        /// 创建无处方的医疗案例
        /// </summary>
        public static LYBT.Entities.MedicalCase.MedicalCase CreateMedicalCaseWithoutPrescription()
        {
            var medicalCase = CreateTestMedicalCase();
            medicalCase.PrescriptionId = null;
            return medicalCase;
        }
    }
}