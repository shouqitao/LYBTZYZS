# Learnings

Corrections, insights, and knowledge gaps captured during development.

**Categories**: correction | insight | knowledge_gap | best_practice

---

## [LRN-20260406-001] Self-improvement skill not activated proactively

**Logged**: 2026-04-06T18:00:00Z
**Priority**: high
**Status**: resolved
**Area**: config

### Summary
Same bash command errors keep recurring because self-improvement skill isn't activated at session start, and .learnings files aren't checked before operations

### Details
- Self-improvement skill is available but not activated automatically
- .learnings files are empty despite repeated failures
- No mechanism to check for known error patterns before executing commands
- Errors in ERRORS.md (ERR-20260406-001, ERR-20260406-002, ERR-20260406-003) would have prevented repeated failures

### Root Cause
1. **No proactive activation**: Self-improvement skill should be loaded at session start
2. **No pre-check**: Before executing git commands, check ERRORS.md for known patterns
3. **No feedback loop**: Errors aren't logged immediately after occurrence

### Suggested Action
1. Activate self-improvement skill at session start
2. Check .learnings/ERRORS.md before executing git/bash commands
3. Log errors immediately after they occur
4. Use PowerShell syntax on Windows (not bash syntax)

### Resolution
- **Resolved**: 2026-04-06T19:00:00Z
- **Commit**: 0930dde22
- **Notes**: Added "Session Start (MANDATORY)" section to AGENTS.md with:
  1. Read .learnings/ERRORS.md before executing commands
  2. Read .learnings/LEARNINGS.md for project conventions
  3. Platform-Specific Rules section for Windows PowerShell

### Metadata
- Source: user_feedback
- Related Files: AGENTS.md, .learnings/ERRORS.md
- Tags: bash, powershell, windows, git, environment-variables
- See Also: ERR-20260406-001, ERR-20260406-002, ERR-20260406-003

---

