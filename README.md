# Agents 自动化调用文档

## 患者管理 Agent

**功能描述**：管理患者基本信息，包括新增、编辑、查询、搜索、启用/禁用、分配医生等操作。该 Agent 封装对 `PatientDetailDto`（患者详情 DTO）的 CRUD 业务调用。
 **触发条件/输入要求**：需要提供包含患者信息的 `PatientDetailDto` 对象（字段如姓名、性别、出生日期等在 DTO 定义中注明）。创建或修改患者时，当前登录用户信息（`operatorId`、`operatorName`）会自动从上下文获取。例如，`PatientDetailDto` 中必填字段有 `Name`、`Gender` 等。
 **调用方式**：可以通过后端接口 `IPatientService.AddAsync/UpdateAsync` 等方法，也可以通过 Web API 路径 `/api/Patients`（对应 `PatientsController`）进行调用。在前端代码中，可使用 Refit 定义的 `IPatientApi` 接口（命名空间 `LYBT.UI.WPF.Apis`）来调用对应的 REST API 方法，如 `AddAsync([Body] PatientDetailDto dto)`（发送 POST 到 `/api/Patients/add`）。调用时需引用 `LYBT.Module.Patients.Dtos` 命名空间中的 `PatientDetailDto` 和 `LYBT.UI.WPF.Apis.IPatientApi`。
 **使用示例**：

```csharp
using LYBT.Module.Patients.Dtos;
using LYBT.UI.WPF.Apis;

// 示例：创建并添加一个新患者
var patientDto = new PatientDetailDto { Name = "张三", Gender = Gender.Male, BirthDate = DateTime.Parse("1990-01-01"), ... };
bool success = await patientApi.AddAsync(patientDto);
```

以上示例调用了 `IPatientApi.AddAsync` 方法向后端 `/api/Patients/add` 提交患者信息。

## 挂号管理 Agent

**功能描述**：管理挂号登记信息，包括查询挂号记录、新增挂号、修改挂号及取消挂号等操作。该 Agent 处理 `RegistrationCreateDto`（新增挂号 DTO）和相关 DTO，完成挂号业务。
 **触发条件/输入要求**：新增挂号时需提供 `RegistrationCreateDto`，其中必填字段包括患者 ID (`PatientId`)、医生 ID (`DoctorId`)、挂号类型 (`RegistrationType`) 等。查询或编辑挂号时需提供挂号记录的主键 `id`。例如，`RegistrationCreateDto` 要求包含有效的 `PatientId` 和 `DoctorId`。
 **调用方式**：可调用 `IRegistrationService.AddAsync/UpdateAsync` 等服务方法，也可调用后端 API `/api/Registration`（由 `RegistrationController` 提供）。在前端，可使用 Refit 定义的 `IRegistrationApi`（在 `LYBT.UI.WPF.Apis` 命名空间中）进行调用。例如，`AddAsync([Body] RegistrationCreateDto dto)` 对应 POST `/api/Registration`。调用时需引入 `LYBT.Module.Registration.Dtos` 命名空间。
 **使用示例**：

```csharp
using LYBT.Module.Registration.Dtos;
using LYBT.UI.WPF.Apis;

// 示例：新增挂号
var regDto = new RegistrationCreateDto { PatientId = "患者GUID", DoctorId = "医生GUID", RegistrationType = "普通" };
var response = await registrationApi.AddAsync(regDto);
```

上述示例通过 `IRegistrationApi.AddAsync` 向 `/api/Registration` 提交新的挂号信息。

## 排队管理 Agent

**功能描述**：管理排队信息，包括列出排队列表、新增排队记录、编辑或取消排队条目等操作。该 Agent 负责对 `QueueingCreateDto`（新增排队 DTO）及其他排队 DTO 的调用。
 **触发条件/输入要求**：新增排队时需提供 `QueueingCreateDto`，必填字段包括患者 ID (`PatientId`)、医生 ID (`DoctorId`)、排队类型 (`QueueType`) 等。编辑或删除排队记录时需提供对应记录的 GUID。
 **调用方式**：可调用 `IQueueingService.AddAsync/UpdateAsync` 等服务方法，或通过后端 API `/api/Queueing`（由 `QueueingController` 提供）进行操作。在前端，可使用 `IQueueingApi` 接口（`LYBT.UI.WPF.Apis` 命名空间）来调用，例如 `AddAsync([Body] QueueingCreateDto dto)` 对应 POST `/api/Queueing`。
 **使用示例**：

```csharp
using LYBT.Module.Queueing.Dtos;
using LYBT.UI.WPF.Apis;

// 示例：新增排队记录
var queueDto = new QueueingCreateDto { PatientId = "患者GUID", DoctorId = "医生GUID", QueueType = "普通" };
var result = await queueingApi.AddAsync(queueDto);
```

示例中调用了 `IQueueingApi.AddAsync` 提交排队信息。

## 诊疗 Agent

**功能描述**：管理诊疗记录，包括查询诊疗列表、新增诊疗、编辑诊疗和取消诊疗等。该 Agent 处理 `DiagnosisTreatmentCreateDto` 等 DTO，用于创建和维护诊疗（就诊）记录。
 **触发条件/输入要求**：新增诊疗时需提供 `DiagnosisTreatmentCreateDto`，关键字段包括患者 ID (`PatientId`)、诊断内容 (`Diagnosis`) 等。其他字段如主诉、现病史、治疗项目列表、草药方等也可填写。
 **调用方式**：调用 `IDiagnosisTreatmentService.AddAsync/UpdateAsync` 等服务，或使用后端 API `/api/DiagnosisTreatment`（由 `DiagnosisTreatmentController` 提供）。前端可使用 `IDiagnosisTreatmentApi` 接口（`LYBT.UI.WPF.Apis` 命名空间）进行调用，例如 `AddAsync([Body] DiagnosisTreatmentCreateDto dto)` 对应 POST `/api/DiagnosisTreatment`。
 **使用示例**：

```csharp
using LYBT.Module.DiagnosisTreatment.Models.Dtos;
using LYBT.UI.WPF.Apis;

// 示例：新增诊疗记录
var diagDto = new DiagnosisTreatmentCreateDto { PatientId = patientId, Diagnosis = "感冒", Treatments = new List<TreatmentItemDto> { ... } };
await diagnosisTreatmentApi.AddAsync(diagDto);
```

示例调用 `IDiagnosisTreatmentApi.AddAsync` 向 `/api/DiagnosisTreatment` 添加诊疗信息。

## 处方 Agent

**功能描述**：管理处方信息，包括查询所有处方、获取处方详情、新增、修改和禁用处方记录。该 Agent 操作 `PrescriptionCreateDto` 等 DTO，用于医生开具和管理处方。
 **触发条件/输入要求**：创建处方需提供 `PrescriptionCreateDto`，必填字段包括患者 ID、医生 ID，以及处方内容和项目列表。例如，`PrescriptionCreateDto` 要包含有效的 `PatientId`、`DoctorId` 和药材明细 `Items`。
 **调用方式**：调用 `IPrescriptionService.CreateAsync/UpdateAsync` 等业务接口，或通过后端 API `/api/Prescriptions`（由 `PrescriptionsController` 提供）进行操作。前端可使用 `HttpClient` 直接调用 REST API：POST 到 `/api/Prescriptions` 并传递 `PrescriptionCreateDto`。后端控制器中对应方法为 `PrescriptionsController.Add`，接受 `PrescriptionCreateDto` 并调用服务。
 **使用示例**：

```csharp
using LYBT.Module.Prescriptions.Dtos;
using System.Net.Http.Json;

// 示例：创建新处方
var presDto = new PrescriptionCreateDto { PatientId = patientId, DoctorId = doctorId, Diagnosis = "头痛", Items = new List<PrescriptionItemCreateDto> { ... } };
var response = await httpClient.PostAsJsonAsync("/api/Prescriptions", presDto);
```

示例中使用 `HttpClient.PostAsJsonAsync` 向 `/api/Prescriptions` 提交处方信息。

## 药房配药 Agent

**功能描述**：管理药房配药单，包括新增药单、编辑和标记配药完成等操作。该 Agent 处理 `PharmacyCreateDto` 等 DTO，用于对处方进行发药处理。
 **触发条件/输入要求**：新增药房单时需提供 `PharmacyCreateDto`，其中 `PrescriptionId`（处方ID）和 `OperatorId`（药房操作员ID）为必填项。可以选择填写配药时间、状态和备注等信息。
 **调用方式**：调用 `IPharmacyService.AddAsync/UpdateAsync` 等接口。前端可使用定义好的服务接口（如 `IPharmacyService`）来调用这些方法。`IPharmacyService.AddAsync(PharmacyCreateDto dto)` 方法将创建新的药房单，参数需引入 `LYBT.Module.Pharmacy.Dtos.PharmacyCreateDto`。
 **使用示例**：

```csharp
using LYBT.Module.Pharmacy.Dtos;
using LYBT.UI.WPF.Interfaces;

// 示例：新增药房配药单
var phDto = new PharmacyCreateDto { PrescriptionId = presId, OperatorId = operatorId };
bool ok = await pharmacyService.AddAsync(phDto);
```

示例调用 `IPharmacyService.AddAsync` 创建新药房单。

## 费用结算 Agent

**功能描述**：管理费用结算，包括查询账单、生成新账单、编辑账单、标记支付、退款等操作。该 Agent 处理 `BillingCreateDto`、`BillingItemDto` 等 DTO，用于患者收费管理。
 **触发条件/输入要求**：生成账单时需提供 `BillingCreateDto`，必填字段包括患者 ID (`PatientId`)、医生 ID (`DoctorId`) 以及账单明细 `Items` 列表。其他字段如总金额、已付金额、支付方式等可选填写。
 **调用方式**：调用 `IBillingService.AddAsync/UpdateAsync` 等服务方法。前端可使用对应业务接口调用，例如 `IBillingService.AddAsync(BillingCreateDto dto)`。详细定义了相关接口和 DTO 字段。
 **使用示例**：

```csharp
using LYBT.Module.Billing.Dtos;
using LYBT.UI.WPF.Interfaces;

// 示例：新增费用结算账单
var billDto = new BillingCreateDto { PatientId = patientId, DoctorId = doctorId, Items = new List<BillingItemDto> {
    new BillingItemDto { Name = "挂号费", UnitPrice = 50, Quantity = 1 }
}, TotalAmount = 50, PaidAmount = 0, Status = BillingStatus.Pending };
bool success = await billingService.AddAsync(billDto);
```

示例调用 `IBillingService.AddAsync` 提交新账单。

## 病历记录 Agent

**功能描述**：管理病历记录，包括查询病历列表、新增病历、编辑和禁用病历，以及共享/撤销共享病历等操作。该 Agent 处理 `RecordCreateDto` 等 DTO，用于记录患者就诊过程。
 **触发条件/输入要求**：创建病历时需提供 `RecordCreateDto`，必填字段包括患者 ID (`PatientId`)、挂号 ID (`RegistrationId`) 和诊断内容 (`Diagnosis`)。其他字段如主诉、现病史、治疗建议、处方 ID 等可选填写。也可指定是否共享以及共享给哪些医生。
 **调用方式**：调用 `IRecordService.AddAsync/UpdateAsync` 等服务方法。需要传入 `RecordCreateDto` 以及当前操作员信息 (`operatorId`、`operatorName`)。例如，`IRecordService.AddAsync(recordDto, operatorId, operatorName)` 会创建新的病历。`RecordCreateDto` 定义在 `LYBT.Module.Records.Dtos` 中。
 **使用示例**：

```csharp
using LYBT.Module.Records.Dtos;
using LYBT.UI.WPF.Interfaces;

// 示例：新增病历记录
var recDto = new RecordCreateDto { PatientId = patientGuid, RegistrationId = regGuid, Diagnosis = "感冒发热", ChiefComplaint = "咳嗽发烧" };
bool ok = await recordService.AddAsync(recDto, operatorId, operatorName);
```

示例调用 `IRecordService.AddAsync` 创建新病历。

## 经验方模板 Agent

**功能描述**：管理经验方（中药配方）模板，包括查询模板列表、新增、编辑和禁用模板等。该 Agent 处理 `FormulaTemplateCreateDto` 等 DTO，用于维护常用草药方剂。
 **触发条件/输入要求**：新增模板时需提供 `FormulaTemplateCreateDto`，必填字段是模板名称 (`Name`)，以及药材列表 `Herbs`。可选填写模板备注。
 **调用方式**：调用 `IFormulaTemplateService.AddAsync/UpdateAsync` 等服务方法。将 `FormulaTemplateCreateDto` 作为参数传入即可。需要引用命名空间 `LYBT.Module.FormulaTemplates.Dtos`。
 **使用示例**：

```csharp
using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.UI.WPF.Interfaces;

// 示例：新增经验方模板
var templateDto = new FormulaTemplateCreateDto { Name = "清热解毒方", Herbs = new List<HerbDto> {
    new HerbDto { Id = herbId1, Name = "金银花", Quantity = 10, Unit = "克" },
    // ...
} };
bool added = await formulaService.AddAsync(templateDto);
```

示例调用 `IFormulaTemplateService.AddAsync` 添加新的中药方模板。

## 系统设置 Agent

**功能描述**：管理系统全局设置，包括读取和更新全局参数（如默认隐私设置、同步模式等）。该 Agent 主要操作 `GlobalSettingsDto`，控制系统运行参数。
 **触发条件/输入要求**：读取设置时无需输入。更新设置时需提供完整的 `GlobalSettingsDto` 对象（如 `DefaultRecordSharing`、`SyncMode` 等字段）。
 **调用方式**：通过后端 API `/api/GlobalSettings`（由 `GlobalSettingsController` 提供）进行调用。GET 请求返回当前 `GlobalSettingsDto`，PUT 请求传递 `GlobalSettingsDto` 以保存设置。在代码中可使用 `IGlobalSettingsService.GetAsync()` 和 `IGlobalSettingsService.SaveAsync(GlobalSettingsDto dto)` 接口调用。
 **使用示例**：

```csharp
using LYBT.Module.Settings.Dtos;
using LYBT.UI.WPF.Interfaces;

// 示例：获取并修改全局设置
var settings = await settingsService.GetAsync();
settings.DefaultRecordSharing = "Public";
bool saved = await settingsService.SaveAsync(settings);
```

示例中 `GetAsync()` 获取当前设置，`SaveAsync(dto)` 更新设置。

## 日志 Agent

**功能描述**：记录和查询系统操作日志，支持任何业务模块记录操作，以及按条件分页查询日志。该 Agent 使用 `LogDto` 传输日志信息，用于保存用户操作历史。
 **触发条件/输入要求**：写入日志时需提供 `LogDto` 对象，其中包含日志类型 (`LogType`)、对象类型 (`ObjectType`)、操作对象 ID、操作类型 (`ActionType`)、操作人信息 (`OperatorId`/`OperatorName`)、操作内容 `Content` 等。查询日志时需提供包含查询条件的 `LogQueryDto`（如时间范围、关键词、用户角色等）。
 **调用方式**：调用 `ILogService.AddLogAsync(LogDto dto)` 方法将日志写入；调用 `ILogService.GetLogsAsync(LogQueryDto query)` 进行分页查询。需要引用 `LYBT.Module.Logs.Dtos.LogDto` 和 `LYBT.Module.Logs.Interfaces.ILogService`。
 **使用示例**：

```csharp
using LYBT.Module.Logs.Dtos;
using LYBT.Common.Enums.Logs;
using LYBT.UI.WPF.Interfaces;

// 示例：写入一条操作日志
var logDto = new LogDto {
    LogType = LogType.Operation, ObjectType = ObjectType.Patient,
    ObjectId = patientId, OperatorId = userId, OperatorName = "张三",
    ActionType = ActionType.Edit, Content = "修改患者信息", OldValue = "{...}", NewValue = "{...}"
};
Guid logId = await logService.AddLogAsync(logDto);
```

示例调用 `ILogService.AddLogAsync` 写入日志记录，包括日志类型、操作对象和操作人等信息。
