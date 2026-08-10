# PasswordHashGenerator - Password Hash Utility

**Purpose**: Standalone CLI tool to generate password hashes for seeding users.

## Structure

```
PasswordHashGenerator/
├── Program.cs          # Entry point + hash generation logic
├── appsettings.json    # Hash algorithm/config
└── PasswordHashGenerator.csproj
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Hash generation | `Program.cs` | Reads password from CLI arg, outputs hash |
| Config | `appsettings.json` | Algorithm settings |

## CONVENTIONS

- **Standalone tool** — Not referenced by LYBTZYZS.sln main projects; dev utility only
- **Password policy** — Output must match Identity PBKDF2 format (not BCrypt)

## ANTI-PATTERNS

- **Hardcoded passwords in source** — Pass via CLI arg or env var
- **Committing generated hashes to repo** — For dev seed only, not production credentials
