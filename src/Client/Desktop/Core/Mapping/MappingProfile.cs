using AutoMapper;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Desktop.Core.Models.Users;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Models.MedicalCase;
using LYBT.Desktop.Core.Models.Consultation;

namespace LYBT.Desktop.Core.Mapping
{
    /// <summary>
    /// AutoMapper 映射配置文件
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // 患者映射
            CreateMap<PatientDetailDto, PatientInfo>()
                .ForMember(dest => dest.IdNumber, opt => opt.MapFrom(src => src.IDNumber))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

            CreateMap<PatientInfo, PatientDetailDto>()
                .ForMember(dest => dest.IDNumber, opt => opt.MapFrom(src => src.IdNumber));

            // UltraThink四层架构：DTO → Info映射配置
            // 验方映射：FormulaDto → FormulaInfo
            CreateMap<FormulaDto, FormulaInfo>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category ?? "其他"))
                .ForMember(dest => dest.Indications, opt => opt.MapFrom(src => src.Indications))
                .ForMember(dest => dest.DosageInstruction, opt => opt.MapFrom(src => src.DosageInstruction))
                .ForMember(dest => dest.Contraindications, opt => opt.MapFrom(src => src.Contraindications))
                .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.Source))
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedByName))
                // UI状态属性使用默认值
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                // 复杂类型映射
                .ForMember(dest => dest.Herbs, opt => opt.MapFrom(src => src.Herbs));

            // 验方药材项映射：FormulaHerbItemDto → FormulaHerbItem
            CreateMap<FormulaHerbItemDto, FormulaHerbItem>()
                .ForMember(dest => dest.HerbId, opt => opt.MapFrom(src => src.HerbId))
                .ForMember(dest => dest.HerbName, opt => opt.MapFrom(src => src.HerbName))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.Unit))
                .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.ProcessingMethod, opt => opt.MapFrom(src => src.Preparation))
                .ForMember(dest => dest.SpecialInstructions, opt => opt.MapFrom(src => src.Usage));

            // 反向映射：FormulaInfo → FormulaDto (用于创建/更新操作)
            CreateMap<FormulaInfo, FormulaCreateDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Effect, opt => opt.MapFrom(src => src.Effect))
                .ForMember(dest => dest.Usage, opt => opt.MapFrom(src => src.Usage))
                .ForMember(dest => dest.IsShared, opt => opt.MapFrom(src => src.IsShared))
                .ForMember(dest => dest.Instructions, opt => opt.MapFrom(src => src.DosageInstruction))
                .ForMember(dest => dest.Indications, opt => opt.MapFrom(src => src.Indications))
                .ForMember(dest => dest.Contraindications, opt => opt.MapFrom(src => src.Contraindications))
                .ForMember(dest => dest.Preparation, opt => opt.MapFrom(src => src.Source))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            // UltraThink模块化架构：Formula模块新增映射配置
            // FormulaCreateInfo → FormulaCreateDto (用于创建操作)
            CreateMap<FormulaCreateInfo, FormulaCreateDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Indications, opt => opt.MapFrom(src => src.Indications))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.Herbs, opt => opt.MapFrom(src => src.Herbs));

            // FormulaUpdateInfo → FormulaUpdateDto (用于更新操作)
            CreateMap<FormulaUpdateInfo, FormulaUpdateDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Indications, opt => opt.MapFrom(src => src.Indications))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.Herbs, opt => opt.MapFrom(src => src.Herbs))
                .ForMember(dest => dest.Version, opt => opt.MapFrom(src => src.Version));

            // FormulaInfo → FormulaCreateInfo (用于复制等操作)
            CreateMap<FormulaInfo, FormulaCreateInfo>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Indications, opt => opt.MapFrom(src => src.Indications))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.Herbs, opt => opt.MapFrom(src => src.Herbs ?? new List<FormulaHerbItem>()))
                // UI状态属性使用默认值
                .ForMember(dest => dest.IsSubmitting, opt => opt.Ignore())
                .ForMember(dest => dest.ValidationErrors, opt => opt.Ignore());

            // FormulaInfo → FormulaUpdateInfo (用于编辑操作)
            CreateMap<FormulaInfo, FormulaUpdateInfo>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Indications, opt => opt.MapFrom(src => src.Indications))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.Herbs, opt => opt.MapFrom(src => src.Herbs ?? new List<FormulaHerbItem>()))
                // UI状态属性使用默认值
                .ForMember(dest => dest.IsSubmitting, opt => opt.Ignore())
                .ForMember(dest => dest.HasUnsavedChanges, opt => opt.Ignore())
                .ForMember(dest => dest.ValidationErrors, opt => opt.Ignore())
                .ForMember(dest => dest.OriginalData, opt => opt.Ignore());

            // UltraThink四层架构：Users模块 DTO → Info映射配置
            // 用户映射：UserDto → UserInfo
            CreateMap<UserDto, UserInfo>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.RealName, opt => opt.MapFrom(src => src.RealName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.WuBiCode, opt => opt.MapFrom(src => src.WuBiCode))
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar))
                .ForMember(dest => dest.IsOnline, opt => opt.MapFrom(src => src.IsOnline))
                .ForMember(dest => dest.LastLoginTime, opt => opt.MapFrom(src => src.LastLoginTime))
                // UI状态属性使用默认值
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsLoading, opt => opt.Ignore());

            // 反向映射：UserInfo → UserDto (用于创建/更新操作)
            CreateMap<UserInfo, UserCreateDto>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.RealName, opt => opt.MapFrom(src => src.RealName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.WuBiCode, opt => opt.MapFrom(src => src.WuBiCode));

            CreateMap<UserInfo, UserUpdateDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.RealName, opt => opt.MapFrom(src => src.RealName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.WuBiCode, opt => opt.MapFrom(src => src.WuBiCode));

            // UltraThink模块化架构：User模块新增映射配置
            // UserCreateInfo → UserCreateDto (用于创建操作)
            CreateMap<UserCreateInfo, UserCreateDto>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.RealName, opt => opt.MapFrom(src => src.RealName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department))
                .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.Position))
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.WuBiCode, opt => opt.MapFrom(src => src.WuBiCode))
                .ForMember(dest => dest.InitialPassword, opt => opt.MapFrom(src => src.InitialPassword))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.IsActiveImmediately, opt => opt.MapFrom(src => src.IsActiveImmediately));

            // UserUpdateInfo → UserUpdateDto (用于更新操作)
            CreateMap<UserUpdateInfo, UserUpdateDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.RealName, opt => opt.MapFrom(src => src.RealName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department))
                .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.Position))
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.WuBiCode, opt => opt.MapFrom(src => src.WuBiCode))
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.Version, opt => opt.MapFrom(src => src.Version));

            // UserCreateInfo → UserInfo (用于验证)
            CreateMap<UserCreateInfo, UserInfo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.IsOnline, opt => opt.Ignore())
                .ForMember(dest => dest.LastLoginTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsLoading, opt => opt.Ignore());

            // UserUpdateInfo → UserInfo (用于验证)
            CreateMap<UserUpdateInfo, UserInfo>()
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.IsOnline, opt => opt.Ignore())
                .ForMember(dest => dest.LastLoginTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsLoading, opt => opt.Ignore());

            // UltraThink四层架构：Herbs模块 DTO → Info映射配置
            // 中药材映射：HerbDto → HerbInfo
            CreateMap<HerbDto, HerbInfo>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Alias, opt => opt.MapFrom(src => src.Alias))
                .ForMember(dest => dest.Effect, opt => opt.MapFrom(src => src.Effect))
                .ForMember(dest => dest.Nature, opt => opt.MapFrom(src => src.Nature))
                .ForMember(dest => dest.Meridian, opt => opt.MapFrom(src => src.Meridian))
                .ForMember(dest => dest.Origin, opt => opt.MapFrom(src => src.Origin))
                .ForMember(dest => dest.Spec, opt => opt.MapFrom(src => src.Spec))
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.Unit))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => src.Stock))
                .ForMember(dest => dest.Supplier, opt => opt.MapFrom(src => src.Supplier))
                .ForMember(dest => dest.Dosage, opt => opt.MapFrom(src => src.Dosage))
                .ForMember(dest => dest.Contraindication, opt => opt.MapFrom(src => src.Contraindication))
                .ForMember(dest => dest.Precaution, opt => opt.MapFrom(src => src.Precaution))
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.WuBiCode, opt => opt.MapFrom(src => src.WuBiCode))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                // UI状态属性使用默认值
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsLoading, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.Status == CommonStatus.Enabled));

            // 反向映射：HerbInfo → HerbCreateDto (用于创建操作)
            CreateMap<HerbInfo, HerbCreateDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Alias, opt => opt.MapFrom(src => src.Alias))
                .ForMember(dest => dest.Effect, opt => opt.MapFrom(src => src.Effect))
                .ForMember(dest => dest.Nature, opt => opt.MapFrom(src => src.Nature))
                .ForMember(dest => dest.Meridian, opt => opt.MapFrom(src => src.Meridian))
                .ForMember(dest => dest.Origin, opt => opt.MapFrom(src => src.Origin))
                .ForMember(dest => dest.Spec, opt => opt.MapFrom(src => src.Spec))
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.Unit))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => src.Stock))
                .ForMember(dest => dest.Supplier, opt => opt.MapFrom(src => src.Supplier))
                .ForMember(dest => dest.Dosage, opt => opt.MapFrom(src => src.Dosage))
                .ForMember(dest => dest.Contraindication, opt => opt.MapFrom(src => src.Contraindication))
                .ForMember(dest => dest.Precaution, opt => opt.MapFrom(src => src.Precaution))
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.WuBiCode, opt => opt.MapFrom(src => src.WuBiCode))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            // 反向映射：HerbInfo → HerbUpdateDto (用于更新操作)
            CreateMap<HerbInfo, HerbUpdateDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Alias, opt => opt.MapFrom(src => src.Alias))
                .ForMember(dest => dest.Effect, opt => opt.MapFrom(src => src.Effect))
                .ForMember(dest => dest.Nature, opt => opt.MapFrom(src => src.Nature))
                .ForMember(dest => dest.Meridian, opt => opt.MapFrom(src => src.Meridian))
                .ForMember(dest => dest.Origin, opt => opt.MapFrom(src => src.Origin))
                .ForMember(dest => dest.Spec, opt => opt.MapFrom(src => src.Spec))
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.Unit))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => src.Stock))
                .ForMember(dest => dest.Supplier, opt => opt.MapFrom(src => src.Supplier))
                .ForMember(dest => dest.Dosage, opt => opt.MapFrom(src => src.Dosage))
                .ForMember(dest => dest.Contraindication, opt => opt.MapFrom(src => src.Contraindication))
                .ForMember(dest => dest.Precaution, opt => opt.MapFrom(src => src.Precaution))
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.WuBiCode, opt => opt.MapFrom(src => src.WuBiCode))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            // UltraThink模块化架构：Herb模块新增映射配置
            // HerbCreateInfo → HerbCreateDto (用于创建操作)
            CreateMap<HerbCreateInfo, HerbCreateDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Alias, opt => opt.MapFrom(src => src.Alias))
                .ForMember(dest => dest.Effect, opt => opt.MapFrom(src => src.Effect))
                .ForMember(dest => dest.Nature, opt => opt.MapFrom(src => src.Nature))
                .ForMember(dest => dest.Meridian, opt => opt.MapFrom(src => src.Meridian))
                .ForMember(dest => dest.Origin, opt => opt.MapFrom(src => src.Origin))
                .ForMember(dest => dest.Spec, opt => opt.MapFrom(src => src.Spec))
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.Unit))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => src.Stock))
                .ForMember(dest => dest.Supplier, opt => opt.MapFrom(src => src.Supplier))
                .ForMember(dest => dest.Dosage, opt => opt.MapFrom(src => src.Dosage))
                .ForMember(dest => dest.Contraindication, opt => opt.MapFrom(src => src.Contraindication))
                .ForMember(dest => dest.Precaution, opt => opt.MapFrom(src => src.Precaution))
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.WuBiCode, opt => opt.MapFrom(src => src.WuBiCode))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.IsActiveImmediately, opt => opt.MapFrom(src => src.IsActiveImmediately));

            // HerbUpdateInfo → HerbUpdateDto (用于更新操作)
            CreateMap<HerbUpdateInfo, HerbUpdateDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Alias, opt => opt.MapFrom(src => src.Alias))
                .ForMember(dest => dest.Effect, opt => opt.MapFrom(src => src.Effect))
                .ForMember(dest => dest.Nature, opt => opt.MapFrom(src => src.Nature))
                .ForMember(dest => dest.Meridian, opt => opt.MapFrom(src => src.Meridian))
                .ForMember(dest => dest.Origin, opt => opt.MapFrom(src => src.Origin))
                .ForMember(dest => dest.Spec, opt => opt.MapFrom(src => src.Spec))
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.Unit))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => src.Stock))
                .ForMember(dest => dest.Supplier, opt => opt.MapFrom(src => src.Supplier))
                .ForMember(dest => dest.Dosage, opt => opt.MapFrom(src => src.Dosage))
                .ForMember(dest => dest.Contraindication, opt => opt.MapFrom(src => src.Contraindication))
                .ForMember(dest => dest.Precaution, opt => opt.MapFrom(src => src.Precaution))
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.WuBiCode, opt => opt.MapFrom(src => src.WuBiCode))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.Version, opt => opt.MapFrom(src => src.Version));

            // HerbCreateInfo → HerbInfo (用于验证)
            CreateMap<HerbCreateInfo, HerbInfo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsLoading, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.StatusDescription, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperationTime, opt => opt.Ignore())
                .ForMember(dest => dest.OperatorName, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActiveImmediately));

            // HerbUpdateInfo → HerbInfo (用于验证)
            CreateMap<HerbUpdateInfo, HerbInfo>()
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsLoading, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.StatusDescription, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperationTime, opt => opt.Ignore())
                .ForMember(dest => dest.OperatorName, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore());

            // UltraThink四层架构：MedicalCase模块 DTO → Info映射配置
            // 医疗案例映射：MedicalCaseDto → MedicalCaseInfo
            CreateMap<MedicalCaseDto, MedicalCaseInfo>()
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.PatientName ?? string.Empty))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.DoctorName ?? string.Empty))
                .ForMember(dest => dest.PatientAge, opt => opt.MapFrom(src => src.PatientAge))
                .ForMember(dest => dest.PatientGender, opt => opt.MapFrom(src => src.PatientGender))
                .ForMember(dest => dest.Diagnosis, opt => opt.MapFrom(src => src.Diagnosis))
                .ForMember(dest => dest.ChiefComplaint, opt => opt.MapFrom(src => src.ChiefComplaint))
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.CompleteTime, opt => opt.MapFrom(src => src.CompleteTime))
                // UI状态属性使用默认值
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsLoading, opt => opt.Ignore())
                .ForMember(dest => dest.CanEdit, opt => opt.Ignore())
                .ForMember(dest => dest.CanDelete, opt => opt.Ignore())
                .ForMember(dest => dest.ConsultationInfo, opt => opt.Ignore());

            // UltraThink模块化架构：MedicalCase模块新增映射配置
            // MedicalCaseCreateInfo → MedicalCaseCreateDto (用于创建操作)
            CreateMap<MedicalCaseCreateInfo, MedicalCaseCreateDto>()
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId))
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.ChiefComplaint, opt => opt.MapFrom(src => src.ChiefComplaint))
                .ForMember(dest => dest.PresentIllness, opt => opt.MapFrom(src => src.PresentIllness))
                .ForMember(dest => dest.PastHistory, opt => opt.MapFrom(src => src.PastHistory))
                .ForMember(dest => dest.FamilyHistory, opt => opt.MapFrom(src => src.FamilyHistory))
                .ForMember(dest => dest.Allergies, opt => opt.MapFrom(src => src.Allergies))
                .ForMember(dest => dest.InitialDiagnosis, opt => opt.MapFrom(src => src.InitialDiagnosis))
                .ForMember(dest => dest.TreatmentPlan, opt => opt.MapFrom(src => src.TreatmentPlan))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.IsUrgent, opt => opt.MapFrom(src => src.IsUrgent))
                .ForMember(dest => dest.AppointmentTime, opt => opt.MapFrom(src => src.AppointmentTime))
                .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes))
                .ForMember(dest => dest.InternalNotes, opt => opt.MapFrom(src => src.InternalNotes));

            // MedicalCaseUpdateInfo → MedicalCaseUpdateDto (用于更新操作)
            CreateMap<MedicalCaseUpdateInfo, MedicalCaseUpdateDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId))
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.ChiefComplaint, opt => opt.MapFrom(src => src.ChiefComplaint))
                .ForMember(dest => dest.PresentIllness, opt => opt.MapFrom(src => src.PresentIllness))
                .ForMember(dest => dest.PastHistory, opt => opt.MapFrom(src => src.PastHistory))
                .ForMember(dest => dest.FamilyHistory, opt => opt.MapFrom(src => src.FamilyHistory))
                .ForMember(dest => dest.Allergies, opt => opt.MapFrom(src => src.Allergies))
                .ForMember(dest => dest.Diagnosis, opt => opt.MapFrom(src => src.Diagnosis))
                .ForMember(dest => dest.TreatmentPlan, opt => opt.MapFrom(src => src.TreatmentPlan))
                .ForMember(dest => dest.FollowUpPlan, opt => opt.MapFrom(src => src.FollowUpPlan))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.IsUrgent, opt => opt.MapFrom(src => src.IsUrgent))
                .ForMember(dest => dest.AppointmentTime, opt => opt.MapFrom(src => src.AppointmentTime))
                .ForMember(dest => dest.CompleteTime, opt => opt.MapFrom(src => src.CompleteTime))
                .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes))
                .ForMember(dest => dest.InternalNotes, opt => opt.MapFrom(src => src.InternalNotes))
                .ForMember(dest => dest.ModificationReason, opt => opt.MapFrom(src => src.ModificationReason));

            // MedicalCaseCreateInfo → MedicalCaseInfo (用于验证)
            CreateMap<MedicalCaseCreateInfo, MedicalCaseInfo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.PatientName))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.DoctorName))
                .ForMember(dest => dest.Diagnosis, opt => opt.MapFrom(src => src.InitialDiagnosis))
                .ForMember(dest => dest.CompleteTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsLoading, opt => opt.Ignore())
                .ForMember(dest => dest.CanEdit, opt => opt.Ignore())
                .ForMember(dest => dest.CanDelete, opt => opt.Ignore())
                .ForMember(dest => dest.ConsultationInfo, opt => opt.Ignore());

            // MedicalCaseUpdateInfo → MedicalCaseInfo (用于验证)
            CreateMap<MedicalCaseUpdateInfo, MedicalCaseInfo>()
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.PatientName))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.DoctorName))
                .ForMember(dest => dest.PatientAge, opt => opt.Ignore())
                .ForMember(dest => dest.PatientGender, opt => opt.Ignore())
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsLoading, opt => opt.Ignore())
                .ForMember(dest => dest.CanEdit, opt => opt.Ignore())
                .ForMember(dest => dest.CanDelete, opt => opt.Ignore())
                .ForMember(dest => dest.ConsultationInfo, opt => opt.Ignore());

            // UltraThink四层架构：Patients模块 DTO → Info映射配置
            // 患者映射：PatientDto → PatientInfo
            CreateMap<PatientDto, PatientInfo>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.Age))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.AllergyHistory, opt => opt.MapFrom(src => src.AllergyHistory))
                .ForMember(dest => dest.Profession, opt => opt.MapFrom(src => src.Profession))
                .ForMember(dest => dest.MaritalStatus, opt => opt.MapFrom(src => src.MaritalStatus))
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.WuBiCode, opt => opt.MapFrom(src => src.WuBiCode))
                // UI状态属性使用默认值
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsLoading, opt => opt.Ignore());

            // 患者详情映射：PatientDetailDto → PatientInfo
            CreateMap<PatientDetailDto, PatientInfo>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.Age))
                .ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.AllergyHistory, opt => opt.MapFrom(src => src.AllergyHistory))
                .ForMember(dest => dest.MedicalHistory, opt => opt.MapFrom(src => src.MedicalHistory))
                .ForMember(dest => dest.FamilyHistory, opt => opt.MapFrom(src => src.FamilyHistory))
                .ForMember(dest => dest.Profession, opt => opt.MapFrom(src => src.Profession))
                .ForMember(dest => dest.MaritalStatus, opt => opt.MapFrom(src => src.MaritalStatus))
                .ForMember(dest => dest.EmergencyContact, opt => opt.MapFrom(src => src.EmergencyContact))
                .ForMember(dest => dest.EmergencyPhone, opt => opt.MapFrom(src => src.EmergencyPhone))
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                // UI状态属性使用默认值
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsLoading, opt => opt.Ignore());

            // 反向映射：PatientInfo → PatientCreateDto (用于创建操作)
            CreateMap<PatientInfo, PatientCreateDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.Age))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.BirthDate))
                .ForMember(dest => dest.IDNumber, opt => opt.MapFrom(src => src.IDNumber))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.AllergyHistory, opt => opt.MapFrom(src => src.AllergyHistory))
                .ForMember(dest => dest.MedicalHistory, opt => opt.MapFrom(src => src.MedicalHistory))
                .ForMember(dest => dest.FamilyHistory, opt => opt.MapFrom(src => src.FamilyHistory))
                .ForMember(dest => dest.Profession, opt => opt.MapFrom(src => src.Profession))
                .ForMember(dest => dest.MaritalStatus, opt => opt.MapFrom(src => src.MaritalStatus))
                .ForMember(dest => dest.EmergencyContact, opt => opt.MapFrom(src => src.EmergencyContact))
                .ForMember(dest => dest.EmergencyPhone, opt => opt.MapFrom(src => src.EmergencyPhone));

            // 反向映射：PatientInfo → PatientUpdateDto (用于更新操作)
            CreateMap<PatientInfo, PatientUpdateDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.Age))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.BirthDate))
                .ForMember(dest => dest.IDNumber, opt => opt.MapFrom(src => src.IDNumber))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.AllergyHistory, opt => opt.MapFrom(src => src.AllergyHistory))
                .ForMember(dest => dest.MedicalHistory, opt => opt.MapFrom(src => src.MedicalHistory))
                .ForMember(dest => dest.FamilyHistory, opt => opt.MapFrom(src => src.FamilyHistory))
                .ForMember(dest => dest.Profession, opt => opt.MapFrom(src => src.Profession))
                .ForMember(dest => dest.MaritalStatus, opt => opt.MapFrom(src => src.MaritalStatus))
                .ForMember(dest => dest.EmergencyContact, opt => opt.MapFrom(src => src.EmergencyContact))
                .ForMember(dest => dest.EmergencyPhone, opt => opt.MapFrom(src => src.EmergencyPhone));

            // UltraThink模块化架构：Patient模块新增映射配置
            // PatientCreateInfo → PatientCreateDto
            CreateMap<PatientCreateInfo, PatientCreateDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.Age))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.BirthDate))
                .ForMember(dest => dest.IDNumber, opt => opt.MapFrom(src => src.IdCard))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.AllergyHistory, opt => opt.MapFrom(src => src.AllergyHistory))
                .ForMember(dest => dest.MedicalHistory, opt => opt.MapFrom(src => src.MedicalHistory))
                .ForMember(dest => dest.FamilyHistory, opt => opt.MapFrom(src => src.FamilyHistory))
                .ForMember(dest => dest.Profession, opt => opt.MapFrom(src => src.Occupation))
                .ForMember(dest => dest.EmergencyContact, opt => opt.MapFrom(src => src.EmergencyContact))
                .ForMember(dest => dest.EmergencyPhone, opt => opt.MapFrom(src => src.EmergencyPhone))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            // PatientUpdateInfo → PatientUpdateDto
            CreateMap<PatientUpdateInfo, PatientUpdateDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.Age))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.BirthDate))
                .ForMember(dest => dest.IDNumber, opt => opt.MapFrom(src => src.IdCard))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.AllergyHistory, opt => opt.MapFrom(src => src.AllergyHistory))
                .ForMember(dest => dest.MedicalHistory, opt => opt.MapFrom(src => src.MedicalHistory))
                .ForMember(dest => dest.FamilyHistory, opt => opt.MapFrom(src => src.FamilyHistory))
                .ForMember(dest => dest.Profession, opt => opt.MapFrom(src => src.Occupation))
                .ForMember(dest => dest.EmergencyContact, opt => opt.MapFrom(src => src.EmergencyContact))
                .ForMember(dest => dest.EmergencyPhone, opt => opt.MapFrom(src => src.EmergencyPhone))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            // PatientCreateInfo → PatientInfo (用于验证)
            CreateMap<PatientCreateInfo, PatientInfo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.IdCard, opt => opt.MapFrom(src => src.IdCard))
                .ForMember(dest => dest.Occupation, opt => opt.MapFrom(src => src.Occupation));

            // PatientUpdateInfo → PatientInfo (用于验证)
            CreateMap<PatientUpdateInfo, PatientInfo>()
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.IdCard, opt => opt.MapFrom(src => src.IdCard))
                .ForMember(dest => dest.Occupation, opt => opt.MapFrom(src => src.Occupation));

            // UltraThink四层架构：Prescriptions模块 DTO → Info映射配置
            // 处方映射：PrescriptionDto → PrescriptionInfo
            CreateMap<PrescriptionDto, PrescriptionInfo>()
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId))
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.PatientName ?? string.Empty))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.DoctorName ?? string.Empty))
                .ForMember(dest => dest.Diagnosis, opt => opt.MapFrom(src => src.Diagnosis))
                .ForMember(dest => dest.DosageCount, opt => opt.MapFrom(src => src.DosageCount))
                .ForMember(dest => dest.SingleDosePrice, opt => opt.MapFrom(src => src.SingleDosePrice))
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice))
                .ForMember(dest => dest.TotalWeight, opt => opt.MapFrom(src => src.TotalWeight))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Advice, opt => opt.MapFrom(src => src.Advice))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
                // 生成处方编号（基于创建时间和ID）
                .ForMember(dest => dest.PrescriptionNumber, opt => opt.MapFrom(src => $"RX{src.CreateTime:yyyyMMdd}{src.Id.ToString().Substring(0, 4)}"))
                // UI状态属性使用默认值
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsPaid, opt => opt.Ignore())
                .ForMember(dest => dest.IsDispensed, opt => opt.Ignore())
                .ForMember(dest => dest.CanEdit, opt => opt.Ignore())
                .ForMember(dest => dest.CanVoid, opt => opt.Ignore());

            // 处方详情映射：PrescriptionDetailDto → PrescriptionInfo
            CreateMap<PrescriptionDetailDto, PrescriptionInfo>()
                .IncludeBase<PrescriptionDto, PrescriptionInfo>()
                .ForMember(dest => dest.PrescriptionNumber, opt => opt.MapFrom(src => src.PrescriptionNo ?? string.Empty))
                .ForMember(dest => dest.Usage, opt => opt.MapFrom(src => src.Usage))
                .ForMember(dest => dest.DosageForm, opt => opt.MapFrom(src => "汤剂")) // 默认剂型
                .ForMember(dest => dest.Discount, opt => opt.MapFrom(src => src.Discount))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            // 处方项目映射：PrescriptionItemDto → PrescriptionItemInfo
            CreateMap<PrescriptionItemDto, PrescriptionItemInfo>()
                .ForMember(dest => dest.HerbId, opt => opt.MapFrom(src => src.HerbId))
                .ForMember(dest => dest.HerbName, opt => opt.MapFrom(src => src.HerbName))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.Unit))
                .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice))
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice))
                .ForMember(dest => dest.TotalWeight, opt => opt.MapFrom(src => src.TotalWeight))
                .ForMember(dest => dest.Subtotal, opt => opt.MapFrom(src => src.Subtotal))
                .ForMember(dest => dest.Usage, opt => opt.MapFrom(src => src.Usage))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                // UI状态属性使用默认值
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.CanEdit, opt => opt.Ignore());

            // 反向映射：PrescriptionInfo → PrescriptionCreateDto (用于创建操作)
            CreateMap<PrescriptionInfo, PrescriptionCreateDto>()
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId))
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Diagnosis, opt => opt.MapFrom(src => src.Diagnosis))
                .ForMember(dest => dest.DosageCount, opt => opt.MapFrom(src => src.DosageCount))
                .ForMember(dest => dest.Advice, opt => opt.MapFrom(src => src.Advice))
                .ForMember(dest => dest.DosageForm, opt => opt.MapFrom(src => src.DosageForm))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.DosageCount))
                .ForMember(dest => dest.Usage, opt => opt.MapFrom(src => src.Usage))
                .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            // 反向映射：PrescriptionInfo → PrescriptionEditDto (用于编辑操作)
            CreateMap<PrescriptionInfo, PrescriptionEditDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Diagnosis, opt => opt.MapFrom(src => src.Diagnosis))
                .ForMember(dest => dest.DosageCount, opt => opt.MapFrom(src => src.DosageCount))
                .ForMember(dest => dest.Advice, opt => opt.MapFrom(src => src.Advice))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            // 反向映射：PrescriptionItemInfo → PrescriptionItemCreateDto (用于创建处方项目)
            CreateMap<PrescriptionItemInfo, PrescriptionItemCreateDto>()
                .ForMember(dest => dest.HerbId, opt => opt.MapFrom(src => src.HerbId))
                .ForMember(dest => dest.HerbName, opt => opt.MapFrom(src => src.HerbName))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.Unit))
                .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice))
                .ForMember(dest => dest.Subtotal, opt => opt.MapFrom(src => src.Subtotal))
                .ForMember(dest => dest.Usage, opt => opt.MapFrom(src => src.Usage))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Remark));

            // UltraThink模块化架构：Prescriptions模块新增映射配置
            // PrescriptionCreateInfo → PrescriptionCreateDto (用于创建操作)
            CreateMap<PrescriptionCreateInfo, PrescriptionCreateDto>()
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId))
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.MedicalCaseId, opt => opt.MapFrom(src => src.MedicalCaseId))
                .ForMember(dest => dest.Diagnosis, opt => opt.MapFrom(src => src.Diagnosis))
                .ForMember(dest => dest.ChiefComplaint, opt => opt.MapFrom(src => src.ChiefComplaint))
                .ForMember(dest => dest.Symptoms, opt => opt.MapFrom(src => src.Symptoms))
                .ForMember(dest => dest.DosageCount, opt => opt.MapFrom(src => src.DosageCount))
                .ForMember(dest => dest.Usage, opt => opt.MapFrom(src => src.Usage))
                .ForMember(dest => dest.Advice, opt => opt.MapFrom(src => src.Advice))
                .ForMember(dest => dest.DosageForm, opt => opt.MapFrom(src => src.DosageForm))
                .ForMember(dest => dest.Preparation, opt => opt.MapFrom(src => src.Preparation))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.IsUrgent, opt => opt.MapFrom(src => src.IsUrgent))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.InternalNotes, opt => opt.MapFrom(src => src.InternalNotes))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            // PrescriptionUpdateInfo → PrescriptionUpdateDto (用于更新操作)
            CreateMap<PrescriptionUpdateInfo, PrescriptionUpdateDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId))
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.MedicalCaseId, opt => opt.MapFrom(src => src.MedicalCaseId))
                .ForMember(dest => dest.Diagnosis, opt => opt.MapFrom(src => src.Diagnosis))
                .ForMember(dest => dest.ChiefComplaint, opt => opt.MapFrom(src => src.ChiefComplaint))
                .ForMember(dest => dest.Symptoms, opt => opt.MapFrom(src => src.Symptoms))
                .ForMember(dest => dest.DosageCount, opt => opt.MapFrom(src => src.DosageCount))
                .ForMember(dest => dest.SingleDosePrice, opt => opt.MapFrom(src => src.SingleDosePrice))
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice))
                .ForMember(dest => dest.TotalWeight, opt => opt.MapFrom(src => src.TotalWeight))
                .ForMember(dest => dest.Usage, opt => opt.MapFrom(src => src.Usage))
                .ForMember(dest => dest.Advice, opt => opt.MapFrom(src => src.Advice))
                .ForMember(dest => dest.DosageForm, opt => opt.MapFrom(src => src.DosageForm))
                .ForMember(dest => dest.Preparation, opt => opt.MapFrom(src => src.Preparation))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.IsUrgent, opt => opt.MapFrom(src => src.IsUrgent))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.InternalNotes, opt => opt.MapFrom(src => src.InternalNotes))
                .ForMember(dest => dest.ModificationReason, opt => opt.MapFrom(src => src.ModificationReason))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            // PrescriptionCreateInfo → PrescriptionInfo (用于验证)
            CreateMap<PrescriptionCreateInfo, PrescriptionInfo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.PatientName))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.DoctorName))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.PrescriptionNumber, opt => opt.Ignore())
                .ForMember(dest => dest.Discount, opt => opt.Ignore())
                .ForMember(dest => dest.IsPaid, opt => opt.Ignore())
                .ForMember(dest => dest.IsDispensed, opt => opt.Ignore())
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.HerbCount, opt => opt.Ignore())
                .ForMember(dest => dest.PatientInfo, opt => opt.Ignore())
                .ForMember(dest => dest.CanEdit, opt => opt.Ignore())
                .ForMember(dest => dest.CanVoid, opt => opt.Ignore());

            // PrescriptionUpdateInfo → PrescriptionInfo (用于验证)
            CreateMap<PrescriptionUpdateInfo, PrescriptionInfo>()
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.PatientName))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.DoctorName))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.PrescriptionNumber, opt => opt.Ignore())
                .ForMember(dest => dest.Discount, opt => opt.Ignore())
                .ForMember(dest => dest.IsPaid, opt => opt.Ignore())
                .ForMember(dest => dest.IsDispensed, opt => opt.Ignore())
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.HerbCount, opt => opt.Ignore())
                .ForMember(dest => dest.PatientInfo, opt => opt.Ignore())
                .ForMember(dest => dest.CanEdit, opt => opt.Ignore())
                .ForMember(dest => dest.CanVoid, opt => opt.Ignore());

            // PrescriptionItemCreateInfo → PrescriptionItemCreateDto
            CreateMap<PrescriptionItemCreateInfo, PrescriptionItemCreateDto>()
                .ForMember(dest => dest.HerbId, opt => opt.MapFrom(src => src.HerbId))
                .ForMember(dest => dest.HerbName, opt => opt.MapFrom(src => src.HerbName))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.Unit))
                .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice))
                .ForMember(dest => dest.Subtotal, opt => opt.MapFrom(src => src.Subtotal))
                .ForMember(dest => dest.Usage, opt => opt.MapFrom(src => src.Usage))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Remark));

            // PrescriptionItemUpdateInfo → PrescriptionItemUpdateDto
            CreateMap<PrescriptionItemUpdateInfo, PrescriptionItemUpdateDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.HerbId, opt => opt.MapFrom(src => src.HerbId))
                .ForMember(dest => dest.HerbName, opt => opt.MapFrom(src => src.HerbName))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.Unit))
                .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice))
                .ForMember(dest => dest.Subtotal, opt => opt.MapFrom(src => src.Subtotal))
                .ForMember(dest => dest.Usage, opt => opt.MapFrom(src => src.Usage))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Remark));

            // PrescriptionItemCreateInfo → PrescriptionItemInfo (用于验证)
            CreateMap<PrescriptionItemCreateInfo, PrescriptionItemInfo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalWeight, opt => opt.Ignore())
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.CanEdit, opt => opt.Ignore());

            // PrescriptionItemUpdateInfo → PrescriptionItemInfo (用于验证)
            CreateMap<PrescriptionItemUpdateInfo, PrescriptionItemInfo>()
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalWeight, opt => opt.Ignore())
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.CanEdit, opt => opt.Ignore());

            // UltraThink四层架构：MedicalCase模块 DTO → Info映射配置
            // 医疗案例映射：MedicalCaseDto → MedicalCaseInfo
            CreateMap<MedicalCaseDto, MedicalCaseInfo>()
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.PatientName ?? string.Empty))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.DoctorName ?? string.Empty))
                .ForMember(dest => dest.PatientAge, opt => opt.MapFrom(src => src.PatientAge))
                .ForMember(dest => dest.PatientGender, opt => opt.MapFrom(src => src.PatientGender))
                .ForMember(dest => dest.Diagnosis, opt => opt.MapFrom(src => src.Diagnosis))
                .ForMember(dest => dest.ChiefComplaint, opt => opt.MapFrom(src => src.ChiefComplaint))
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.CompleteTime, opt => opt.MapFrom(src => src.CompleteTime))
                // UI状态属性使用默认值
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsLoading, opt => opt.Ignore())
                .ForMember(dest => dest.CanEdit, opt => opt.Ignore())
                .ForMember(dest => dest.CanDelete, opt => opt.Ignore())
                .ForMember(dest => dest.ConsultationInfo, opt => opt.Ignore());

            // 医疗案例详情映射：MedicalCaseDetailDto → MedicalCaseInfo
            CreateMap<MedicalCaseDetailDto, MedicalCaseInfo>()
                .IncludeBase<MedicalCaseDto, MedicalCaseInfo>()
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            // 反向映射：MedicalCaseInfo → MedicalCaseCreateDto (用于创建操作)
            CreateMap<MedicalCaseInfo, MedicalCaseCreateDto>()
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId))
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.ChiefComplaint, opt => opt.MapFrom(src => src.ChiefComplaint))
                .ForMember(dest => dest.Diagnosis, opt => opt.MapFrom(src => src.Diagnosis))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            // 反向映射：MedicalCaseInfo → MedicalCaseUpdateDto (用于更新操作)
            CreateMap<MedicalCaseInfo, MedicalCaseUpdateDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.ChiefComplaint, opt => opt.MapFrom(src => src.ChiefComplaint))
                .ForMember(dest => dest.Diagnosis, opt => opt.MapFrom(src => src.Diagnosis))
                .ForMember(dest => dest.CompleteTime, opt => opt.MapFrom(src => src.CompleteTime))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            #endregion

            #region Consultation模块映射配置

            // UltraThink四层架构：Consultation模块 DTO → Info映射配置
            // 看诊映射：ConsultationDto → ConsultationInfo
            CreateMap<ConsultationDto, ConsultationInfo>()
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.PatientName ?? string.Empty))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.DoctorName ?? string.Empty))
                .ForMember(dest => dest.DiagnosisCatalogName, opt => opt.MapFrom(src => src.DiagnosisCatalogName))
                .ForMember(dest => dest.PatientAge, opt => opt.MapFrom(src => src.PatientAge))
                .ForMember(dest => dest.PatientGender, opt => opt.MapFrom(src => src.PatientGender))
                .ForMember(dest => dest.PatientPhone, opt => opt.MapFrom(src => src.PatientPhone))
                .ForMember(dest => dest.Symptoms, opt => opt.MapFrom(src => src.Symptoms))
                .ForMember(dest => dest.DifferentiationAnalysis, opt => opt.MapFrom(src => src.DifferentiationAnalysis))
                // UI状态属性忽略映射
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsCompleted, opt => opt.Ignore());

            // 看诊详情映射：ConsultationDetailDto → ConsultationInfo
            CreateMap<ConsultationDetailDto, ConsultationInfo>()
                .IncludeBase<ConsultationDto, ConsultationInfo>()
                // 详情特有属性的映射
                .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => src.Status == CommonStatus.Enabled));

            // 反向映射：ConsultationInfo → ConsultationCreateDto (用于创建操作)
            CreateMap<ConsultationInfo, ConsultationCreateDto>()
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId))
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.MedicalCaseId, opt => opt.MapFrom(src => src.MedicalCaseId))
                .ForMember(dest => dest.ChiefComplaint, opt => opt.MapFrom(src => src.ChiefComplaint))
                .ForMember(dest => dest.Symptoms, opt => opt.MapFrom(src => src.Symptoms))
                .ForMember(dest => dest.Inspection, opt => opt.MapFrom(src => src.Inspection))
                .ForMember(dest => dest.AuscultationOlfaction, opt => opt.MapFrom(src => src.AuscultationOlfaction))
                .ForMember(dest => dest.Inquiry, opt => opt.MapFrom(src => src.Inquiry))
                .ForMember(dest => dest.Palpation, opt => opt.MapFrom(src => src.Palpation))
                .ForMember(dest => dest.Diagnosis, opt => opt.MapFrom(src => src.Diagnosis))
                .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))
                .ForMember(dest => dest.WesternDiagnosis, opt => opt.MapFrom(src => src.WesternDiagnosis))
                .ForMember(dest => dest.DifferentiationAnalysis, opt => opt.MapFrom(src => src.DifferentiationAnalysis))
                .ForMember(dest => dest.TreatmentPlan, opt => opt.MapFrom(src => src.TreatmentPlan))
                .ForMember(dest => dest.Temperature, opt => opt.MapFrom(src => src.Temperature))
                .ForMember(dest => dest.SystolicPressure, opt => opt.MapFrom(src => src.SystolicPressure))
                .ForMember(dest => dest.DiastolicPressure, opt => opt.MapFrom(src => src.DiastolicPressure))
                .ForMember(dest => dest.HeartRate, opt => opt.MapFrom(src => src.HeartRate))
                .ForMember(dest => dest.ConsultationTime, opt => opt.MapFrom(src => src.ConsultationTime))
                .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.Duration))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            // 反向映射：ConsultationInfo → ConsultationUpdateDto (用于更新操作)
            CreateMap<ConsultationInfo, ConsultationUpdateDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ChiefComplaint, opt => opt.MapFrom(src => src.ChiefComplaint))
                .ForMember(dest => dest.Symptoms, opt => opt.MapFrom(src => src.Symptoms))
                .ForMember(dest => dest.Inspection, opt => opt.MapFrom(src => src.Inspection))
                .ForMember(dest => dest.AuscultationOlfaction, opt => opt.MapFrom(src => src.AuscultationOlfaction))
                .ForMember(dest => dest.Inquiry, opt => opt.MapFrom(src => src.Inquiry))
                .ForMember(dest => dest.Palpation, opt => opt.MapFrom(src => src.Palpation))
                .ForMember(dest => dest.Diagnosis, opt => opt.MapFrom(src => src.Diagnosis))
                .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))
                .ForMember(dest => dest.WesternDiagnosis, opt => opt.MapFrom(src => src.WesternDiagnosis))
                .ForMember(dest => dest.DifferentiationAnalysis, opt => opt.MapFrom(src => src.DifferentiationAnalysis))
                .ForMember(dest => dest.TreatmentPlan, opt => opt.MapFrom(src => src.TreatmentPlan))
                .ForMember(dest => dest.Temperature, opt => opt.MapFrom(src => src.Temperature))
                .ForMember(dest => dest.SystolicPressure, opt => opt.MapFrom(src => src.SystolicPressure))
                .ForMember(dest => dest.DiastolicPressure, opt => opt.MapFrom(src => src.DiastolicPressure))
                .ForMember(dest => dest.HeartRate, opt => opt.MapFrom(src => src.HeartRate))
                .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.Duration))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            // 看诊操作映射：ConsultationStartInfo → ConsultationStartDto (用于开始看诊操作)
            CreateMap<ConsultationStartInfo, ConsultationStartDto>()
                .ForMember(dest => dest.MedicalCaseId, opt => opt.MapFrom(src => src.MedicalCaseId))
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId))
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.EstimatedDuration, opt => opt.MapFrom(src => src.EstimatedDuration))
                .ForMember(dest => dest.ConsultationType, opt => opt.MapFrom(src => src.ConsultationType))
                .ForMember(dest => dest.InitialComplaint, opt => opt.MapFrom(src => src.InitialComplaint))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            // UltraThink模块化架构：Consultation模块新增映射配置
            // ConsultationCreateInfo → ConsultationStartDto (用于创建看诊操作)
            CreateMap<ConsultationCreateInfo, ConsultationStartDto>()
                .ForMember(dest => dest.MedicalCaseId, opt => opt.MapFrom(src => src.MedicalCaseId))
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId))
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.ConsultationTime, opt => opt.MapFrom(src => src.ConsultationTime))
                .ForMember(dest => dest.EstimatedDuration, opt => opt.MapFrom(src => src.Duration))
                .ForMember(dest => dest.InitialComplaint, opt => opt.MapFrom(src => src.ChiefComplaint))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            // ConsultationCreateInfo → ConsultationCreateDto (用于完整创建操作)
            CreateMap<ConsultationCreateInfo, ConsultationCreateDto>()
                .ForMember(dest => dest.MedicalCaseId, opt => opt.MapFrom(src => src.MedicalCaseId))
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.ChiefComplaint, opt => opt.MapFrom(src => src.ChiefComplaint))
                .ForMember(dest => dest.PresentIllness, opt => opt.MapFrom(src => src.PresentIllness))
                .ForMember(dest => dest.PastHistory, opt => opt.MapFrom(src => src.PastHistory))
                .ForMember(dest => dest.AllergyHistory, opt => opt.MapFrom(src => src.AllergyHistory))
                .ForMember(dest => dest.PhysicalExamination, opt => opt.MapFrom(src => src.PhysicalExamination))
                .ForMember(dest => dest.Inspection, opt => opt.MapFrom(src => src.Inspection))
                .ForMember(dest => dest.AuscultationOlfaction, opt => opt.MapFrom(src => src.AuscultationOlfaction))
                .ForMember(dest => dest.Inquiry, opt => opt.MapFrom(src => src.Inquiry))
                .ForMember(dest => dest.Palpation, opt => opt.MapFrom(src => src.Palpation))
                .ForMember(dest => dest.TongueInspection, opt => opt.MapFrom(src => src.TongueInspection))
                .ForMember(dest => dest.PulseCondition, opt => opt.MapFrom(src => src.PulseCondition))
                .ForMember(dest => dest.Temperature, opt => opt.MapFrom(src => src.Temperature))
                .ForMember(dest => dest.SystolicPressure, opt => opt.MapFrom(src => src.SystolicPressure))
                .ForMember(dest => dest.DiastolicPressure, opt => opt.MapFrom(src => src.DiastolicPressure))
                .ForMember(dest => dest.HeartRate, opt => opt.MapFrom(src => src.HeartRate))
                .ForMember(dest => dest.RespiratoryRate, opt => opt.MapFrom(src => src.RespiratoryRate))
                .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))
                .ForMember(dest => dest.WesternDiagnosis, opt => opt.MapFrom(src => src.WesternDiagnosis))
                .ForMember(dest => dest.Diagnosis, opt => opt.MapFrom(src => src.Diagnosis))
                .ForMember(dest => dest.DiagnosisCatalogId, opt => opt.MapFrom(src => src.DiagnosisCatalogId))
                .ForMember(dest => dest.TreatmentPrinciple, opt => opt.MapFrom(src => src.TreatmentPrinciple))
                .ForMember(dest => dest.MedicalAdvice, opt => opt.MapFrom(src => src.MedicalAdvice))
                .ForMember(dest => dest.ConsultationTime, opt => opt.MapFrom(src => src.ConsultationTime))
                .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.Duration))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            // ConsultationUpdateInfo → ConsultationUpdateDto (用于更新操作)
            CreateMap<ConsultationUpdateInfo, ConsultationUpdateDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.MedicalCaseId, opt => opt.MapFrom(src => src.MedicalCaseId))
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.ChiefComplaint, opt => opt.MapFrom(src => src.ChiefComplaint))
                .ForMember(dest => dest.PresentIllness, opt => opt.MapFrom(src => src.PresentIllness))
                .ForMember(dest => dest.PastHistory, opt => opt.MapFrom(src => src.PastHistory))
                .ForMember(dest => dest.AllergyHistory, opt => opt.MapFrom(src => src.AllergyHistory))
                .ForMember(dest => dest.PhysicalExamination, opt => opt.MapFrom(src => src.PhysicalExamination))
                .ForMember(dest => dest.Inspection, opt => opt.MapFrom(src => src.Inspection))
                .ForMember(dest => dest.AuscultationOlfaction, opt => opt.MapFrom(src => src.AuscultationOlfaction))
                .ForMember(dest => dest.Inquiry, opt => opt.MapFrom(src => src.Inquiry))
                .ForMember(dest => dest.Palpation, opt => opt.MapFrom(src => src.Palpation))
                .ForMember(dest => dest.TongueInspection, opt => opt.MapFrom(src => src.TongueInspection))
                .ForMember(dest => dest.PulseCondition, opt => opt.MapFrom(src => src.PulseCondition))
                .ForMember(dest => dest.Temperature, opt => opt.MapFrom(src => src.Temperature))
                .ForMember(dest => dest.SystolicPressure, opt => opt.MapFrom(src => src.SystolicPressure))
                .ForMember(dest => dest.DiastolicPressure, opt => opt.MapFrom(src => src.DiastolicPressure))
                .ForMember(dest => dest.HeartRate, opt => opt.MapFrom(src => src.HeartRate))
                .ForMember(dest => dest.RespiratoryRate, opt => opt.MapFrom(src => src.RespiratoryRate))
                .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))
                .ForMember(dest => dest.WesternDiagnosis, opt => opt.MapFrom(src => src.WesternDiagnosis))
                .ForMember(dest => dest.Diagnosis, opt => opt.MapFrom(src => src.Diagnosis))
                .ForMember(dest => dest.DiagnosisCatalogId, opt => opt.MapFrom(src => src.DiagnosisCatalogId))
                .ForMember(dest => dest.TreatmentPrinciple, opt => opt.MapFrom(src => src.TreatmentPrinciple))
                .ForMember(dest => dest.MedicalAdvice, opt => opt.MapFrom(src => src.MedicalAdvice))
                .ForMember(dest => dest.ConsultationTime, opt => opt.MapFrom(src => src.ConsultationTime))
                .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.Duration))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.ModificationReason, opt => opt.MapFrom(src => src.ModificationReason));

            // ConsultationCreateInfo → ConsultationInfo (用于验证)
            CreateMap<ConsultationCreateInfo, ConsultationInfo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.PatientName))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.DoctorName))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.DiagnosisCatalogName, opt => opt.Ignore())
                .ForMember(dest => dest.PatientAge, opt => opt.Ignore())
                .ForMember(dest => dest.PatientGender, opt => opt.Ignore())
                .ForMember(dest => dest.PatientPhone, opt => opt.Ignore())
                .ForMember(dest => dest.Symptoms, opt => opt.Ignore())
                .ForMember(dest => dest.DifferentiationAnalysis, opt => opt.Ignore())
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsCompleted, opt => opt.Ignore());

            // ConsultationUpdateInfo → ConsultationInfo (用于验证)
            CreateMap<ConsultationUpdateInfo, ConsultationInfo>()
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.PatientName))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.DoctorName))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.DiagnosisCatalogName, opt => opt.Ignore())
                .ForMember(dest => dest.PatientAge, opt => opt.Ignore())
                .ForMember(dest => dest.PatientGender, opt => opt.Ignore())
                .ForMember(dest => dest.PatientPhone, opt => opt.Ignore())
                .ForMember(dest => dest.Symptoms, opt => opt.Ignore())
                .ForMember(dest => dest.DifferentiationAnalysis, opt => opt.Ignore())
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsCompleted, opt => opt.Ignore());

            #endregion
        }
    }
}