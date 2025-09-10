# Controller层返回类型修复方案

## 修复原则

基于方法的返回类型，使用对应的泛型版本：

### 返回类型映射

1. `ActionResult<ApiResponse<T>>` → 使用 `ValidationFail<T>()`, `HandleException<T>()` 等
2. `ActionResult<PagedApiResponse<T>>` → 使用 `ValidationFail<PagedApiResponse<T>>()`
3. `ActionResult<ApiResponse>` → 使用非泛型版本

## PatientsController修复清单

### QuickCreate方法 (Line 42)
- 返回类型: `ActionResult<ApiResponse<PatientDetailDto>>`
- 需要修复:
  - `ValidateModel()` → `ValidateModel<PatientDetailDto>()`
  - `HandleException()` → `HandleException<PatientDetailDto>()`

### ToggleStatus方法 (Line 86)
- 返回类型: `ActionResult<ApiResponse>`
- 需要修复: 无需修改，使用非泛型版本

### GetAll方法 (Line 134)
- 返回类型: `ActionResult<ApiResponse<List<PatientDetailDto>>>`
- 需要修复:
  - `HandleException()` → `HandleException<List<PatientDetailDto>>()`

### GetPaged方法 (Line 161)
- 返回类型: `ActionResult<PagedApiResponse<PatientDetailDto>>`
- 需要修复:
  - `ValidateModel()` → `ValidateModel<PagedApiResponse<PatientDetailDto>>()`
  - `ValidationFail()` → `ValidationFail<PagedApiResponse<PatientDetailDto>>()`
  - `HandleException()` → `HandleException<PagedApiResponse<PatientDetailDto>>()`

### Search方法 (Line 190)
- 返回类型: `ActionResult<ApiResponse<List<PatientDetailDto>>>`
- 需要修复:
  - `HandleException()` → `HandleException<List<PatientDetailDto>>()`

### Export方法 (Line 209)
- 返回类型: `ActionResult<ApiResponse<List<PatientDetailDto>>>`
- 需要修复:
  - `HandleException()` → `HandleException<List<PatientDetailDto>>()`

### GetActivePatients方法 (Line 231)
- 返回类型: `ActionResult<ApiResponse<List<PatientDetailDto>>>`
- 需要修复:
  - `HandleException()` → `HandleException<List<PatientDetailDto>>()`

### FindOrCreate方法 (Line 249)
- 返回类型: `ActionResult<ApiResponse<PatientDetailDto>>`
- 需要修复:
  - `ValidateModel()` → `ValidateModel<PatientDetailDto>()`
  - `ValidationFail()` → `ValidationFail<PatientDetailDto>()`
  - `BusinessFail()` → `BusinessFail<PatientDetailDto>()`
  - `HandleException()` → `HandleException<PatientDetailDto>()`

### GetPatients方法 (Line 279)
- 返回类型: `ActionResult<PagedApiResponse<PatientDetailDto>>`
- 需要修复:
  - `ValidationFail()` → `ValidationFail<PagedApiResponse<PatientDetailDto>>()`
  - `HandleException()` → `HandleException<PagedApiResponse<PatientDetailDto>>()`

### CreatePatient方法 (Line 326)
- 返回类型: `ActionResult<ApiResponse<PatientDetailDto>>`
- 需要修复:
  - `ValidateModel()` → `ValidateModel<PatientDetailDto>()`
  - `ValidationFail()` → `ValidationFail<PatientDetailDto>()`
  - `HandleException()` → `HandleException<PatientDetailDto>()`

### GetPatient方法 (Line 358)
- 返回类型: `ActionResult<ApiResponse<PatientDetailDto>>`
- 需要修复:
  - `ValidateGuid()` → `ValidateGuid<PatientDetailDto>()`
  - `NotFound()` → `NotFound<PatientDetailDto>()`
  - `HandleException()` → `HandleException<PatientDetailDto>()`

### UpdatePatient方法 (Line 384)
- 返回类型: `ActionResult<ApiResponse<PatientDetailDto>>`
- 需要修复:
  - `ValidateGuid()` → `ValidateGuid<PatientDetailDto>()`
  - `ValidateModel()` → `ValidateModel<PatientDetailDto>()`
  - `ValidationFail()` → `ValidationFail<PatientDetailDto>()`
  - `NotFound()` → `NotFound<PatientDetailDto>()`
  - `BusinessFail()` → `BusinessFail<PatientDetailDto>()`
  - `HandleException()` → `HandleException<PatientDetailDto>()`

### DeletePatient方法 (Line 427)
- 返回类型: `ActionResult<ApiResponse>`
- 需要修复: 无需修改，使用非泛型版本

## AuthController修复清单

### Login方法
- 返回类型: `ActionResult<ApiResponse<LoginResponse>>`
- 需要修复:
  - `ValidationFail()` → `ValidationFail<LoginResponse>()`
  - `Unauthorized()` → `Unauthorized<LoginResponse>()`
  - `HandleException()` → `HandleException<LoginResponse>()`
  - `InternalError()` → `InternalError<LoginResponse>()`

### Logout方法
- 返回类型: `ActionResult<ApiResponse>`
- 需要修复: 无需修改，使用非泛型版本