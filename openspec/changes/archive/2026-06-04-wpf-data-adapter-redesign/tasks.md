## 1. IApiClient Interface Definition

- [x] 1.1 Create IApiClient interface in LYBT.Desktop.Contracts with per-module sub-interfaces (IAuthApi, IPatientApi, IHerbApi, IFormulaApi, IMedicalCaseApi, IUserApi, IRegistrationApi)
- [x] 1.2 Define IAuthApi sub-interface methods (LoginAsync, RefreshTokenAsync, AutoLoginAsync)
- [x] 1.3 Define IPatientApi sub-interface methods (GetListAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync, ImportAsync, ExportAsync, GetByIdNumberAsync)
- [x] 1.4 Define IHerbApi sub-interface methods (GetListAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync, GetCategoriesAsync)
- [x] 1.5 Define IFormulaApi sub-interface methods (GetListAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync, CloneAsync, GetCategoriesAsync)
- [x] 1.6 Define IMedicalCaseApi sub-interface methods (GetListAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync, GetPendingAsync, GetByStatusAsync)
- [x] 1.7 Define IUserApi sub-interface methods (GetListAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync, ChangePasswordAsync)
- [x] 1.8 Define IRegistrationApi sub-interface methods (GetListAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync)
- [x] 1.9 Create ApiClientOptions configuration class for HttpClient settings (TimeoutSeconds, RetryCount, BaseUrl overrides)

## 2. Refit Implementation (Remote Mode)

- [x] 2.1 Create RefitApiClient implementing IApiClient, using Refit.RestService.For<T>() for each sub-interface
- [x] 2.2 Migrate existing Refit interface attributes from Contracts/IApi/ to new IApiClient sub-interfaces
- [x] 2.3 Add Refit-specific settings (JsonSerializerSettings, AuthorizationHeaderValueGetter) to RefitApiClient configuration
- [x] 2.4 Create RefitApiClient registration extension method for DI (AddRefitApiClient)

## 3. HttpClient Wrapper Implementation (LocalWebAPI Mode)

- [x] 3.1 Create HttpClientApiClient implementing IApiClient, using IHttpClientFactory + System.Text.Json
- [x] 3.2 Implement base HTTP helper methods (GetAsync<T>, PostAsync<T>, PutAsync<T>, DeleteAsync) with JSON serialization
- [x] 3.3 Implement IAuthApi in HttpClientApiClient with local JWT auth endpoints
- [x] 3.4 Implement IPatientApi in HttpClientApiClient
- [x] 3.5 Implement IHerbApi in HttpClientApiClient
- [x] 3.6 Implement IFormulaApi in HttpClientApiClient
- [x] 3.7 Implement IMedicalCaseApi in HttpClientApiClient
- [x] 3.8 Implement IUserApi in HttpClientApiClient
- [x] 3.9 Implement IRegistrationApi in HttpClientApiClient
- [x] 3.10 Create HttpClientApiClient registration extension method for DI (AddHttpClientApiClient), resolving LocalWebApiHost.Port for BaseAddress

## 4. Unified Auth DelegatingHandler

- [x] 4.1 Create AuthDelegatingHandler extending DelegatingHandler, injecting ITokenService for JWT token retrieval
- [x] 4.2 Implement automatic Bearer token attachment in SendAsync
- [x] 4.3 Implement 401 response handling: attempt token refresh, then trigger re-login on failure
- [x] 4.4 Register AuthDelegatingHandler in IHttpClientFactory pipeline for both Remote and LocalWebAPI modes
- [x] 4.5 Remove manual Authorization header code from all Repository implementations

## 5. IHttpClientFactory Integration

- [x] 5.1 Add Microsoft.Extensions.Http NuGet package to LYBT.Desktop.Foundation
- [x] 5.2 Configure Named HttpClient for Remote mode with BaseAddress from appsettings.json
- [x] 5.3 Configure Named HttpClient for LocalWebAPI mode with dynamic BaseAddress (127.0.0.1:{port})
- [x] 5.4 Configure global timeout and retry policy via Polly extensions
- [x] 5.5 Wire AuthDelegatingHandler into both HttpClient configurations

## 6. Unified Error Handling

- [x] 6.1 Create ApiErrorHandler utility that maps Refit ApiException and HttpRequestException to ServiceResult<T>
- [x] 6.2 Implement HTTP status code → Chinese error message mapping (reuse ClientErrorMessageMapper)
- [x] 6.3 Integrate ApiErrorHandler into both RefitApiClient and HttpClientApiClient
- [x] 6.4 Remove duplicate error handling code from individual Repository implementations

## 7. Repository Layer Refactoring

- [x] 7.1 Refactor Remote PatientRepository to depend on IApiClient.Patients instead of Refit IPatientApi directly
- [x] 7.2 Refactor Remote HerbRepository to depend on IApiClient.Herbs
- [x] 7.3 Refactor Remote FormulaRepository to depend on IApiClient.Formulas
- [x] 7.4 Refactor Remote MedicalCaseRepository to depend on IApiClient.MedicalCases
- [x] 7.5 Refactor Remote UserRepository to depend on IApiClient.Users
- [x] 7.6 Refactor Remote RegistrationRepository to depend on IApiClient.Registrations
- [x] 7.7 Refactor LocalWebAPI HttpPatientRepository to depend on IApiClient.Patients instead of raw HttpClient
- [x] 7.8 Refactor LocalWebAPI HttpHerbRepository to depend on IApiClient.Herbs
- [x] 7.9 Refactor LocalWebAPI HttpFormulaRepository to depend on IApiClient.Formulas
- [x] 7.10 Refactor LocalWebAPI HttpMedicalCaseRepository to depend on IApiClient.MedicalCases
- [x] 7.11 Refactor LocalWebAPI HttpUserRepository to depend on IApiClient.Users
- [x] 7.12 Refactor LocalWebAPI HttpRegistrationRepository to depend on IApiClient.Registrations

## 8. DI Registration & Configuration

- [x] 8.1 Add ApiMode configuration section to appsettings.json
- [x] 8.2 Refactor DataSourceRegistrationExtensions to register IApiClient based on ApiMode config value
- [x] 8.3 Create AddUnifiedApiClient extension method combining HttpClientFactory + AuthHandler + ApiClient registration
- [x] 8.4 Update StartupPipeline or App.xaml.cs to call AddUnifiedApiClient during initialization
- [x] 8.5 Ensure LocalWebApiHost.Port is available before HttpClientApiClient registration

## 9. Cleanup & Removal

- [x] 9.1 Remove old standalone Refit interface files (IApi/*) from LYBT.Desktop.Contracts after migration confirmed
- [x] 9.2 Remove manual HttpClient instantiation from all HttpXxxRepository constructors
- [x] 9.3 Remove scattered JWT header injection code from all Repository files
- [x] 9.4 Remove duplicate error handling code from Repository files
- [x] 9.5 Update XML documentation on all changed public APIs

## 10. Testing

- [x] 10.1 Write unit tests for RefitApiClient with mocked HttpMessageHandler
- [x] 10.2 Write unit tests for HttpClientApiClient with mocked HttpMessageHandler
- [x] 10.3 Write unit tests for AuthDelegatingHandler (token injection + 401 refresh flow)
- [x] 10.4 Write unit tests for ApiErrorHandler mapping all HTTP status codes
- [x] 10.5 Write integration tests for Remote mode flow (IApiClient → Mocked Server)
- [x] 10.6 Write integration tests for LocalWebAPI mode flow (IApiClient → LocalWebAPI)
- [x] 10.7 Run existing test suite (LYBT.Tests.Desktop) to verify no regressions
