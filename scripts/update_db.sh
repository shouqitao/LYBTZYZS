#!/bin/bash
# Apply EF Core migration for the LYBT database

set -euo pipefail

# Update database to migration UpdateIdFieldsToGuid
cd "$(dirname "$0")/.."
dotnet ef database update 20250615155700_UpdateIdFieldsToGuid
