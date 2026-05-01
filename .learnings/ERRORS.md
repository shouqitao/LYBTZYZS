# Errors

Command failures and integration errors.

---

## [ERR-20260501-001] Model calls unavailable 'grep' tool in OpenCode

**Logged**: 2026-05-01T09:30:00+08:00
**Priority**: medium
**Status**: resolved
**Area**: config

### Summary
Model repeatedly attempts to call `grep` tool which does not exist in OpenCode environment. OpenCode available tools: bash, read, edit, write, task, websearch, todowrite, skill, compress, lsp_*, ast_grep_search, session_*, background_*, filesystem_*, skill_mcp, look_at.

### Error
```
Model tried to call unavailable tool 'grep'. Available tools: invalid, question, bash, read, edit, write, task, websearch, todowrite, skill, compress, lsp_goto_definition, lsp_find_references, lsp_symbols, lsp_diagnostics, lsp_prepare_rename, lsp_rename, ast_grep_search, ast_grep_replace, session_list, session_read, session_search, session_info, background_output, background_cancel, look_at, skill_mcp, filesystem_read_file, filesystem_read_text_file, filesystem_read_media_file, filesystem_read_multiple_files, filesystem_write_file, filesystem_edit_file, filesystem_create_directory, filesystem_list_directory, filesystem_list_directory_with_sizes, filesystem_directory_tree, filesystem_move_file, filesystem_search_files, filesystem_get_file_info, filesystem_list_allowed_directories, websearch_web_search_exa, grep_app_searchGitHub, context7_resolve-library-id, context7_query-docs.
```

### Context
- Model inherits Claude Code behavior and tries to use `grep` for content search
- In OpenCode, use `ast_grep_search` for AST-aware code search
- For simple text search in files, use `bash` with `Select-String` (PowerShell) or `ast_grep_search`
- The system prompt mentions "NEVER use the following tools" but model still tries grep

### Suggested Fix
1. Use `ast_grep_search` for code pattern search (supports 25 languages)
2. Use `bash` with `Select-String` for simple text search in PowerShell
3. Use `filesystem_search_files` for file name search
4. Never call `grep` directly - it's not an available tool

### Resolution
- **Resolved**: 2026-05-01T09:30:00+08:00
- **Notes**: Added ERR-20260501-001 to ERRORS.md. Model should use ast_grep_search (code) or bash+Select-String (text)

### Metadata
- Reproducible: yes
- Related Files: AGENTS.md, system prompt
- Tags: tool-mapping, opencode, grep, ast_grep


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


