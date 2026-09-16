// ADR-0018 集成事件契约 SSOT 位置说明（P07 合规）
//
// 跨模块领域事件类型（MedicalCaseCompletedEvent / MedicalCaseCancelledEvent）
// 定义于 LYBT.Infrastructure.SharedKernel.Events，而非本目录。
//
// 原因：P07 禁止 Server 模块间相互引用（ArchTests.P07_ServerModules_Should_Not_Reference_Other_ServerModules）。
// 若事件定义在本模块而 Handler 在 Registrations，Registrations 必须 ProjectReference
// MedicalCases 才能编译，将直接违反 P07。跨模块契约与 IXxxCrossModuleService 同层放
// Infrastructure，发布方与消费方均只依赖 Infrastructure。
//
// 本模块（MedicalCases）在 Services/MedicalCaseStateService.cs 中发布事件；
// 消费方 Handler 位于 LYBT.Module.Registrations/Application/EventHandlers/。
