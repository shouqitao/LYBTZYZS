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

