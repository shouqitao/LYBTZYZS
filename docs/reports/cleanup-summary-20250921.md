# Cleanup Summary — 2025-09-21

- Purpose: archive historical docs/scripts/outputs to keep the root tidy and prevent stale info from interfering with development.
- Archive root: `docs/_archive/2025-09-21/`
- Index file: `docs/_archive/2025-09-21/ARCHIVE_INDEX.md`

## Key Actions
- Moved historical docs and notes to `legacy/`, `reports/`, and `notes/` under archive.
- Consolidated ad‑hoc scripts:
  - `scriptshealth-check.ps1` -> `scripts/health-check.ps1`
  - `update_client_readmes_detailed.py` -> `docs/_archive/2025-09-21/scripts/`
- Archived generated outputs and temp folders to `outputs/`:
  - `out/`, `outwebapi-fx/`, `outwebapi-self/`, `publish/`, `artifacts/`, root `obj/`, `TestResults/`
- Archived misc user or temp files: `LYBT.All.slnLaunch.user`, `temp_auth.json`, `nul`
- Preserved all dot‑prefixed tool config folders: `.claude/`, `.github/`, `.ai/`, `.serena/` (unchanged)
- Source (`src/`), tests (`tests/`), tools (`tools/`), scripts (`scripts/`), and BIN policy remained unchanged.

## Notes
- No source or test files were modified.
- .gitignore already covers common outputs (out/**, BIN/**, obj/**, TestResults/**, artifacts/). No change required.
- See the index for full source→destination mapping.

