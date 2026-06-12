# docs/ File Renaming Plan — Numbered Prefix Sorting

## Naming Convention
- Format: `NN- descriptive-name.md` (2-digit prefix + hyphen + lowercase-hyphenated name)
- README.md stays unchanged (GitHub auto-displays)
- Archive/ and decisions/ subdirectories not renamed
- All cross-references updated after renaming

## 01-product/ (8 files)
| New Name | Old Name |
|----------|----------|
| README.md | README.md (keep) |
| 01-vision.md | vision.md |
| 02-personas.md | personas.md |
| 03-jtbd.md | jtbd.md |
| 04-user-roles.md | user-roles.md |
| 05-feature-list.md | feature-list.md |
| 06-clinical-workflow.md | clinical-workflow.md |
| 07-glossary.md | glossary.md |

## 02-requirements/ (22 files)
| New Name | Old Name |
|----------|----------|
| README.md | README.md (keep) |
| 01-prd.md | prd.md |
| 02-auth.md | auth.md |
| 03-users.md | users.md |
| 04-patients.md | patients.md |
| 05-herbs.md | herbs.md |
| 06-formulas.md | formulas.md |
| 07-medical-cases.md | medical-cases.md |
| 08-registration.md | registration.md |
| 09-printing.md | printing.md |
| 10-sync.md | sync.md |
| 11-configuration.md | configuration.md |
| 12-desktop-shell.md | desktop-shell.md |
| 13-error-handling.md | error-handling.md |
| 14-logging.md | logging.md |
| 15-health-diagnostics.md | health-diagnostics.md |
| 16-card-reader.md | card-reader.md |
| 17-nfr.md | nfr.md |
| 18-ui-patterns.md | ui-patterns.md |
| 19-user-story-map.md | user-story-map.md |
| 20-roadmap.md | roadmap.md |
| 21-role-permission-matrix.md | role-permission-matrix.md |

## 03-architecture/ (12 entries)
| New Name | Old Name |
|----------|----------|
| README.md | README.md (keep) |
| 01-system-overview.md | system-overview.md |
| 02-desktop.md | desktop.md |
| 03-server.md | server.md |
| 04-data-model.md | data-model.md |
| 05-dual-mode.md | dual-mode.md |
| 06-error-handling.md | error-handling-architecture.md |
| 07-configuration.md | configuration.md |
| 08-shared.md | shared.md |
| localwebapi/ | localwebapi/ (keep dir) |
| decisions/ | decisions/ (keep dir) |
| archive/ | archive/ (keep dir) |

## 04-api-reference/ (13 files)
| New Name | Old Name |
|----------|----------|
| README.md | README.md (keep) |
| 01-auth.md | auth.md |
| 02-users.md | users.md |
| 03-patients.md | patients.md |
| 04-herbs.md | herbs.md |
| 05-formulas.md | formulas.md |
| 06-medical-cases.md | medical-cases.md |
| 07-registrations.md | registrations.md |
| 08-printing.md | printing.md |
| 09-sync.md | sync.md |
| 10-configuration.md | configuration.md |
| 11-health.md | health.md |
| 12-diagnostics.md | diagnostics.md |

## 05-development/ (14 entries)
| New Name | Old Name |
|----------|----------|
| README.md | README.md (keep) |
| 01-setup.md | setup.md |
| 02-workflow.md | workflow.md |
| 03-code-standards.md | code-standards.md |
| 04-patterns.md | patterns.md |
| 05-testing.md | testing.md |
| 06-security-password-management.md | security-password-management.md |
| 07-openspec-tracking-guide.md | openspec-tracking-guide.md |
| 08-configuration-migration-guide.md | configuration-migration-guide.md |
| 09-performance-baseline.md | performance-baseline.md |
| 10-uat-test-plan.md | uat-test-plan.md |
| 11-postman-vs-dotnet-testing.md | postman-vs-dotnet-testing-strategy.md |
| standards/ | standards/ (keep dir) |
| archive/ | archive/ (keep dir) |

## 06-operations/ (9 entries)
| New Name | Old Name |
|----------|----------|
| README.md | README.md (keep) |
| 01-deployment.md | deployment.md |
| 02-configuration.md | configuration.md |
| 03-webapi-deployment-summary.md | webapi-deployment-summary.md |
| 04-windows-deployment.md | WINDOWS-DEPLOYMENT.md |
| 05-development-environment-spec.md | development-environment-spec.md |
| 06-api-tests.md | LYBTZYZS_API_Tests.md |
| archive/ | archive/ (keep dir) |

## 07-concepts/ (38 entries)
| New Name | Old Name |
|----------|----------|
| README.md | README.md (keep) |
| 01-dual-mode-architecture.md | dual-mode-architecture.md |
| 02-embedded-kestrel.md | embedded-kestrel-architecture.md |
| 03-single-window-architecture.md | single-window-architecture.md |
| 04-workspace-modes.md | workspace-modes.md |
| 05-clinical-vs-management-mode.md | clinical-vs-management-mode.md |
| 06-clinical-workflow.md | clinical-workflow.md |
| 07-authentication.md | authentication.md |
| 08-authorization-policies.md | authorization-policies.md |
| 09-password-management.md | password-management-strategy.md |
| 10-sensitive-data.md | sensitive-data-classification.md |
| 11-error-handling.md | error-handling.md |
| 12-exception-hierarchy.md | exception-hierarchy.md |
| 13-api-response-envelope.md | api-response-envelope.md |
| 14-feature-toggles.md | feature-toggles.md |
| 15-caching-strategy.md | caching-strategy.md |
| 16-herb-cache-strategy.md | herb-cache-strategy.md |
| 17-memory-cache-management.md | memory-cache-management.md |
| 18-mvvm-prism.md | mvvm-prism.md |
| 19-mapperly.md | mapperly.md |
| 20-startup-pipeline.md | startup-pipeline.md |
| 21-edit-mode-state-machine.md | edit-mode-state-machine.md |
| 22-menu-visibility-matrix.md | menu-visibility-matrix.md |
| 23-cross-module-communication.md | cross-module-communication.md |
| 24-testing-strategy.md | testing-strategy.md |
| 25-zero-mock-strategy.md | zero-mock-strategy.md |
| 26-print-protection.md | print-protection.md |
| 27-pinyin-search.md | pinyin-search-implementation.md |
| 28-formula-validation-workflow.md | formula-validation-workflow.md |
| 29-prescription-completeness-checker.md | prescription-completeness-checker.md |
| 30-patient-status-lifecycle.md | patient-status-lifecycle.md |
| 31-medical-case-locking-rules.md | medical-case-locking-rules.md |
| 32-registration-lifecycle.md | registration-lifecycle.md |
| 33-sync-conflict-resolution.md | sync-conflict-resolution.md |
| 34-batch-operation-pattern.md | batch-operation-pattern.md |
| 35-validator-architecture.md | validator-architecture.md |
| development/ | development/ (keep dir) |
| modules/ | modules/ (keep dir) |

## 07-concepts/modules/ (keep names, no renumbering)
These are already consistently named: auth-module.md, medical-case-module.md, etc.

## 07-concepts/development/ (keep names, no renumbering)
Already consistent: terminology.md, common-pitfalls.md, build-and-run.md, naming-conventions.md
