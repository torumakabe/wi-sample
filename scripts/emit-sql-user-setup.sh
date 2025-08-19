#!/usr/bin/env bash
set -euo pipefail

# Emit a ready-to-paste T-SQL for creating the EXTERNAL USER (Service Principal)
# for Azure SQL Database, using values from azd env/infra outputs when possible.

echo "========================================"
echo "Emit paste-ready SQL for Azure SQL user"
echo "========================================"

get_from_azd() {
  local key="$1"
  if command -v azd >/dev/null 2>&1; then
    azd env get-values 2>/dev/null | awk -F= -v k="$key" '$1==k {print $2}' | tr -d '\r' || true
  fi
}

# Prefer already-set envs; otherwise, try azd env; minimal sane defaults last.
AZURE_RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-$(get_from_azd AZURE_RESOURCE_GROUP)}"
SQL_SERVER_FQDN="${SQL_SERVER_FQDN:-$(get_from_azd SQL_SERVER_FQDN)}"
SQL_DATABASE_NAME="${SQL_DATABASE_NAME:-$(get_from_azd SQL_DATABASE_NAME)}"
FRONTEND_SERVICE_PRINCIPAL_NAME="${FRONTEND_SERVICE_PRINCIPAL_NAME:-$(get_from_azd FRONTEND_SERVICE_PRINCIPAL_NAME)}"
AZURE_ENV_NAME="${AZURE_ENV_NAME:-$(get_from_azd AZURE_ENV_NAME)}"

# Derive server name from FQDN if set
SQL_SERVER_NAME="${SQL_SERVER_NAME:-}"
if [[ -z "${SQL_SERVER_NAME}" && -n "${SQL_SERVER_FQDN:-}" ]]; then
  SQL_SERVER_NAME="${SQL_SERVER_FQDN%%.*}"
fi

# Fallbacks for readability (do not block emission)
[[ -z "${FRONTEND_SERVICE_PRINCIPAL_NAME:-}" && -n "${AZURE_ENV_NAME:-}" ]] && FRONTEND_SERVICE_PRINCIPAL_NAME="wi-sample-frontend-${AZURE_ENV_NAME}"

if [[ -z "${SQL_DATABASE_NAME:-}" ]]; then
  echo "[WARN] SQL_DATABASE_NAME not found in env or azd env. The SQL will be emitted without DB context."
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUT_DIR="${SCRIPT_DIR}/../tmp/sql"
mkdir -p "$OUT_DIR"
OUT_FILE="$OUT_DIR/create-user-${SQL_DATABASE_NAME:-unknown}.sql"

cat > "$OUT_FILE" <<SQL
-- Paste this in Azure Portal > SQL Database (${SQL_DATABASE_NAME:-<your-db>}) > Query editor (preview)
-- Server: ${SQL_SERVER_NAME:-<your-server>} (${SQL_SERVER_FQDN:-<your-server>.database.windows.net})
-- Resource Group: ${AZURE_RESOURCE_GROUP:-<your-rg>}
-- Creates EXTERNAL USER for the Service Principal and grants minimal read permissions
SET NOCOUNT ON;

IF EXISTS (SELECT * FROM sys.database_principals WHERE name = N'${FRONTEND_SERVICE_PRINCIPAL_NAME:-<SP_DISPLAY_NAME>}')
    DROP USER [${FRONTEND_SERVICE_PRINCIPAL_NAME:-<SP_DISPLAY_NAME>}];

CREATE USER [${FRONTEND_SERVICE_PRINCIPAL_NAME:-<SP_DISPLAY_NAME>}] FROM EXTERNAL PROVIDER;

ALTER ROLE db_datareader ADD MEMBER [${FRONTEND_SERVICE_PRINCIPAL_NAME:-<SP_DISPLAY_NAME>}];
GRANT VIEW DATABASE STATE TO [${FRONTEND_SERVICE_PRINCIPAL_NAME:-<SP_DISPLAY_NAME>}];
GRANT VIEW DEFINITION TO [${FRONTEND_SERVICE_PRINCIPAL_NAME:-<SP_DISPLAY_NAME>}];

-- Diagnostics
SELECT name, type_desc, authentication_type_desc
FROM sys.database_principals
WHERE type IN ('E')
ORDER BY name;
SQL

echo "Generated SQL file: $OUT_FILE"
echo
echo "----- Paste-ready SQL begin -----"
cat "$OUT_FILE"
echo "----- Paste-ready SQL end -------"
echo
echo "Next: Open Azure Portal > SQL Database (${SQL_DATABASE_NAME:-<your-db>}) > Query editor (preview) and run the SQL above as Entra ID admin."
