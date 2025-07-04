#!/bin/bash
# Apply EF Core migration for the LYBT database

set -euo pipefail

# Update database to migration AddDoctorInfoRequest
cd "$(dirname "$0")/.."
dotnet ef database update 20250704145214_AddDoctorInfoRequest
