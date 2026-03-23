#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RESULTS_DIR="${ROOT_DIR}/TestResults"

rm -rf "${RESULTS_DIR}"

cd "${ROOT_DIR}"
dotnet test budget-tracker-backend.sln \
  --settings coverage.runsettings \
  --collect:"XPlat Code Coverage" \
  --results-directory "${RESULTS_DIR}"

find "${RESULTS_DIR}" -type f -name 'coverage.opencover.xml' -print
