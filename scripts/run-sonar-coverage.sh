#!/usr/bin/env bash
set -uo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RESULTS_DIR="${ROOT_DIR}/TestResults"

cleanup_and_pause() {
  local exit_code=$?
  echo
  echo "Script finished with exit code: ${exit_code}"
  read -p "Press Enter to exit..."
  exit "${exit_code}"
}

trap cleanup_and_pause EXIT

rm -rf "${RESULTS_DIR}"

cd "${ROOT_DIR}"
dotnet test budget-tracker-backend.sln \
  --settings coverage.runsettings \
  --collect:"XPlat Code Coverage" \
  --results-directory "${RESULTS_DIR}"

WINDOWS_RESULTS_DIR="$(cygpath -w "${RESULTS_DIR}")"

reportgenerator \
  -reports:"${WINDOWS_RESULTS_DIR}\**\coverage.opencover.xml" \
  -targetdir:"${WINDOWS_RESULTS_DIR}\CoverageReport" \
  -reporttypes:Html

echo "Coverage report: ${WINDOWS_RESULTS_DIR}\CoverageReport\index.html"