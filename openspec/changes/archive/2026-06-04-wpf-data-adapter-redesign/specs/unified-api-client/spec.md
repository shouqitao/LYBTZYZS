## ADDED Requirements

### Requirement: Unified API Client Abstraction
The system SHALL provide a unified `IApiClient` interface that abstracts HTTP communication for both Remote (WebAPI) and LocalWebAPI modes, exposing typed methods per business domain module.

#### Scenario: Remote mode uses Refit API client
- **WHEN** `ApiMode` configuration is set to `Remote`
- **THEN** DI container SHALL resolve `IApiClient` as a Refit-generated implementation targeting the configured WebAPI base URL

#### Scenario: LocalWebAPI mode uses HttpClient wrapper
- **WHEN** `ApiMode` configuration is set to `LocalWebAPI`
- **THEN** DI container SHALL resolve `IApiClient` as a managed HttpClient implementation targeting `http://127.0.0.1:{port}` with the port dynamically resolved from `LocalWebApiHost`

### Requirement: IHttpClientFactory Integration
The system SHALL use `IHttpClientFactory` to manage HttpClient lifecycle for both modes, ensuring connection pooling, timeout configuration, and retry policy are centrally managed.

#### Scenario: Timeout configuration applied globally
- **WHEN** any API call is made through `IApiClient`
- **THEN** the HttpClient SHALL enforce the timeout configured in `appsettings.json` under `HttpClient:TimeoutSeconds`

#### Scenario: Retry policy applied to transient failures
- **WHEN** an HTTP call fails with `HttpRequestException` or `TimeoutException`
- **THEN** the system SHALL automatically retry up to 3 times with exponential backoff (1s, 2s, 4s)

### Requirement: Unified Authentication DelegatingHandler
The system SHALL inject JWT Bearer tokens into all outgoing HTTP requests through a shared `DelegatingHandler` pipeline, eliminating manual header management in individual Repository implementations.

#### Scenario: Access token attached to every request
- **WHEN** a user is authenticated and any API call is made
- **THEN** the `Authorization: Bearer {token}` header SHALL be automatically attached to the request

#### Scenario: Expired token triggers refresh or re-login
- **WHEN** an API call returns HTTP 401 Unauthorized
- **THEN** the DelegatingHandler SHALL attempt token refresh via `ITokenRefreshService`, and if refresh fails, trigger re-login flow

### Requirement: Unified Error Handling Pipeline
The system SHALL map all HTTP errors (Refit `ApiException` and `HttpRequestException`) through a single error handling pipeline, returning `ServiceResult<T>` with user-friendly Chinese error messages.

#### Scenario: HTTP 500 maps to ServiceResult failure
- **WHEN** any API call returns HTTP 500
- **THEN** the system SHALL return `ServiceResult<T>.Failure("服务器内部错误")`

#### Scenario: HTTP 401 maps to unauthorized result
- **WHEN** any API call returns HTTP 401
- **THEN** the system SHALL return `ServiceResult<T>.Failure("登录已过期，请重新登录")` and trigger the DelegatingHandler auth flow

### Requirement: Repository Layer Uses IApiClient Exclusively
All Repository implementations in the WPF client SHALL depend on `IApiClient` instead of directly depending on Refit interfaces or raw HttpClient, ensuring mode-independent data access.

#### Scenario: Remote PatientRepository delegates to IApiClient
- **WHEN** `PatientRepository.GetListAsync()` is called in Remote mode
- **THEN** the operation SHALL delegate to `IApiClient.Patients.GetListAsync()` without any mode-specific logic in the Repository

#### Scenario: LocalWebAPI HttpPatientRepository delegates to IApiClient
- **WHEN** `HttpPatientRepository.GetListAsync()` is called in LocalWebAPI mode
- **THEN** the operation SHALL delegate to `IApiClient.Patients.GetListAsync()` without any raw HttpClient usage

### Requirement: Config-Driven Mode Switching
The system SHALL determine the active API mode from `appsettings.json` configuration at startup, with DI registration resolving the correct `IApiClient` implementation without runtime conditional logic.

#### Scenario: Mode configured via appsettings.json
- **WHEN** `appsettings.json` contains `"ApiMode": "LocalWebAPI"`
- **THEN** DI container SHALL register the LocalWebAPI `IApiClient` implementation

#### Scenario: Mode switch requires application restart
- **WHEN** a user changes `ApiMode` in configuration
- **THEN** the application SHALL require a restart for the new mode to take effect
