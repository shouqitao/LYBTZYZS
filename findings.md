# Findings & Decisions
<!-- 
  WHAT: Knowledge base for LYBTZYZS Service layer implementation.
  WHY: Store gap analysis results and implementation decisions.
  WHEN: Updated after gap analysis completion.
-->

## Requirements
<!-- 
  WHAT: Requirements from gap analysis and frontend-completion.md
-->
- Implement IUserService with all 14 IUserApi methods
- Implement IHerbService with all 13 IHerbApi methods  
- Implement IRegistrationService with all 6 IRegistrationApi methods
- Enhance IFormulaService (add 10 missing methods)
- Enhance IPatientService (add 2 missing methods)
- Follow 3-layer architecture: Controller → Service → Repository → DbContext
- Add CancellationToken to all Service methods (T6-2)
- Enable unit testing of business logic

## Research Findings
<!-- 
  WHAT: Key discoveries from codebase analysis
-->
- Existing Services follow RemoteXxxService pattern (RemotePatientService, RemoteFormulaService)
- Services are in src/Client/Desktop/Modules/*/Services/ directory
- Interfaces are in src/Client/Desktop/Modules/*/Interfaces/ directory
- All Services inject IApiClient and use Refit APIs
- MedicalCase uses multiple interfaces (IMedicalCaseQueryService + IMedicalCaseCommandService + IMedicalCaseLifecycleService)
- ViewModels currently bypass Service layer in User/Herb/Registration modules

## Technical Decisions
<!-- 
  WHAT: Architecture and implementation choices
-->
| Decision | Rationale |
|----------|-----------|
| Follow RemoteXxxService pattern | Consistent with existing codebase (RemotePatientService) |
| Add CancellationToken to all methods | Required by T6-2 and allows async cancellation |
| Implement all API methods | Full coverage prevents future gaps |
| Use existing IApiClient injection | Matches current DI pattern |
| Keep existing interfaces for Formula/Patient | Enhance rather than replace to avoid breaking changes |

## Issues Encountered
<!-- 
  WHAT: Problems identified during gap analysis
-->
| Issue | Resolution |
|-------|------------|
| User module no Service layer | Will implement IUserService + RemoteUserService |
| Herb module no Service layer | Will implement IHerbService + RemoteHerbService |
| Registration module no Service layer | Will implement IRegistrationService + RemoteRegistrationService |
| Formula missing 10 methods | Will add to existing IFormulaService |
| Patient missing 2 methods | Will add to existing IPatientService |

## Resources
<!-- 
  WHAT: Key file paths and references
-->
- API interfaces: src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/
- Existing Services: src/Client/Desktop/Modules/*/Services/
- Existing Interfaces: src/Client/Desktop/Modules/*/Interfaces/
- ViewModels to update: src/Client/Desktop/Modules/*/ViewModels/
- Frontend plan: .sisyphus/plans/frontend-completion.md

## Visual/Browser Findings
<!-- 
  WHAT: N/A - gap analysis was code-based
-->
- N/A

---
*Findings captured from comprehensive API vs Service gap analysis*