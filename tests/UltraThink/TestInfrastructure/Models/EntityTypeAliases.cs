/// <summary>
/// P3-Fix UltraThink测试基础设施实体别名
/// 为保持测试代码兼容性，提供Model后缀的类型别名
/// </summary>

// P3-Fix: 全局类型别名，使得其他命名空间也能访问
global using UserModel = LYBT.Tests.UltraThink.TestInfrastructure.Models.UserModel;
global using PatientModel = LYBT.Tests.UltraThink.TestInfrastructure.Models.PatientModel;
global using HerbModel = LYBT.Tests.UltraThink.TestInfrastructure.Models.HerbModel;
global using FormulaModel = LYBT.Tests.UltraThink.TestInfrastructure.Models.FormulaModel;
global using PrescriptionModel = LYBT.Tests.UltraThink.TestInfrastructure.Models.PrescriptionModel;
global using MedicalCaseModel = LYBT.Tests.UltraThink.TestInfrastructure.Models.MedicalCaseModel;
global using ConsultationModel = LYBT.Tests.UltraThink.TestInfrastructure.Models.ConsultationModel;
global using AuthModel = LYBT.Tests.UltraThink.TestInfrastructure.Models.AuthModel;

using LYBT.Entities.Auth;
using LYBT.Entities.Consultation;
using LYBT.Entities.Formula;
using LYBT.Entities.Herbs;
using LYBT.Entities.MedicalCase;
using LYBT.Entities.Patients;
using LYBT.Entities.Prescriptions;
using LYBT.Entities.Users;

namespace LYBT.Tests.UltraThink.TestInfrastructure.Models
{
    // P3-Fix: 实体类型别名，解决测试项目编译错误
    
    /// <summary>用户实体别名</summary>
    public class UserModel : User { }
    
    /// <summary>患者实体别名</summary>
    public class PatientModel : Patient { }
    
    /// <summary>药材实体别名</summary>
    public class HerbModel : Herb { }
    
    /// <summary>验方实体别名</summary>
    public class FormulaModel : Formula { }
    
    /// <summary>处方实体别名</summary>
    public class PrescriptionModel : Prescription { }
    
    /// <summary>医疗案例实体别名</summary>
    public class MedicalCaseModel : MedicalCase { }
    
    /// <summary>看诊实体别名</summary>
    public class ConsultationModel : Consultation { }
    
    /// <summary>认证实体别名</summary>
    public class AuthModel : AuthSession { }
}