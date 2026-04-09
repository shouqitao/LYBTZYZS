# Errors

Command failures and integration errors.

---

## [ERR-20260406-001] Bash export command fails on Windows PowerShell

**Logged**: 2026-04-06T18:00:00Z
**Priority**: high
**Status**: resolved
**Area**: config

### Summary
Bash commands using `export CI=true...` syntax fail on Windows PowerShell with "The term 'export' is not recognized"

### Error
```
export: The term 'export' is not recognized as a name of a cmdlet, function, script file, or executable program.
```

### Context
- Command attempted: `export CI=true DEBIAN_FRONTEND=noninteractive GIT_TERMINAL_PROMPT=0...`
- Environment: Windows PowerShell (not bash)
- The bash tool on Windows PowerShell interprets 'export' as PowerShell command

### Suggested Fix
Use PowerShell syntax for environment variables:
```powershell
$env:GIT_TERMINAL_PROMPT=0; git add ...
```
Or run git commands directly without export prefix.

### Resolution
- **Resolved**: 2026-04-06T19:00:00Z
- **Commit**: 0930dde22
- **Notes**: Added Platform-Specific Rules to AGENTS.md — use git directly without export prefix

### Metadata
- Reproducible: yes
- Related Files: All git operations
- See Also: ERR-20260406-002, ERR-20260406-003

---

## [ERR-20260406-002] Heredoc syntax fails on Windows PowerShell

**Logged**: 2026-04-06T18:00:00Z
**Priority**: high
**Status**: resolved
**Area**: config

### Summary
Heredoc syntax `$(cat <<'EOF')` fails on Windows PowerShell with "Missing file specification after redirection operator"

### Error
```
ParserError: Missing file specification after redirection operator.
```

### Context
- Command attempted: `git commit -m "$(cat <<'EOF'..."`
- Environment: Windows PowerShell (not bash)
- Heredoc syntax is bash-specific, not supported in PowerShell

### Suggested Fix
Use simple string for commit messages on Windows:
```powershell
git commit -m "feat: complete service layer + registration status sync + warning fixes"
```
Or use single-line messages without heredoc.

### Resolution
- **Resolved**: 2026-04-06T19:00:00Z
- **Commit**: 0930dde22
- **Notes**: Added Platform-Specific Rules to AGENTS.md — use simple string messages only

### Metadata
- Reproducible: yes
- Related Files: All git commit operations
- See Also: ERR-20260406-001

---

## [ERR-20260406-003] Edit tool with empty parameters fails

**Logged**: 2026-04-06T18:00:00Z
**Priority**: medium
**Status**: resolved
**Area**: config

### Summary
Edit tool fails when oldString and newString are identical or empty

### Error
```
No changes to apply: oldString and newString are identical.
```

### Context
- Tool: edit
- Issue: Using empty oldString/newString or identical strings
- The edit tool requires different oldString and newString values

### Suggested Fix
Always provide distinct oldString and newString values. Never use empty strings.

### Resolution
- **Resolved**: 2026-04-06T19:00:00Z
- **Commit**: 0930dde22
- **Notes**: Added to Common Pitfalls section — always verify edit parameters are distinct

### Metadata
- Reproducible: yes
- Related Files: N/A
- See Also: None

---

## [ERR-20260408-001] lsp_diagnostics

**Logged**: 2026-04-08T00:00:00+08:00
**Priority**: low
**Status**: pending
**Area**: config

### Summary
No XAML language server is configured, so LSP diagnostics cannot be used for WPF resource files.

### Error
```
Error: No LSP server configured for extension: .xaml
Available servers: typescript, deno, vue, eslint, oxlint, biome, gopls, ruby-lsp, basedpyright, pyright...
```

### Context
- Attempted to verify newly created XAML resource files with `lsp_diagnostics`
- Target path: `src/Client/Desktop/Shell/Resources`

### Suggested Fix
Use an XAML-capable verifier (build/design-time compile, or a configured XAML language server) instead of LSP diagnostics.

### Metadata
- Reproducible: yes
- Related Files: src/Client/Desktop/Shell/Resources/*.xaml

---

## [ERR-20260409-001] CommunityToolkit.Mvvm partial method cannot cross class boundaries

**Logged**: 2026-04-09T09:00:00+08:00
**Priority**: high
**Status**: resolved
**Area**: desktop

### Summary
`partial void OnErrorMessageChanged(string value)` in LoginViewModel fails with CS0759 because the source-generated partial method definition is in the base class CoreViewModelBase, not LoginViewModel.

### Error
```
CS0759: 没有为分部方法"LoginViewModel.OnErrorMessageChanged(string)"的实现声明找到定义声明
```

### Context
- CoreViewModelBase uses `[ObservableProperty] private string _errorMessage` which generates `partial void OnErrorMessageChanged(string)` in CoreViewModelBase
- LoginViewModel (derived class) attempted to implement this partial method, but partial methods cannot cross class boundaries
- Also attempted `[NotifyPropertyChangedFor("HasMessage")]` on CoreViewModelBase, but MVVMTK0015 fires because `HasMessage` is not defined in CoreViewModelBase

### Suggested Fix
Subscribe to `PropertyChanged` event in the derived class constructor:
```csharp
PropertyChanged += (s, e) =>
{
    if (e.PropertyName == nameof(ErrorMessage) || e.PropertyName == nameof(StatusMessage))
        OnPropertyChanged(nameof(HasMessage));
};
```

### Resolution
- **Resolved**: 2026-04-09T09:15:00+08:00
- **Notes**: PropertyChanged event subscription works across inheritance. `[NotifyPropertyChangedFor]` only works when the target property exists in the same class.

### Metadata
- Reproducible: yes
- Related Files: src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs, src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/CoreViewModelBase.cs
- Tags: CommunityToolkit.Mvvm, partial, source-generator, WPF, PropertyChanged


