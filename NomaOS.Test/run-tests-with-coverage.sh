#!/usr/bin/env bash
#
# Runs the unit/integration suite with coverage and enforces the gate.
#
#   ./NomaOS.Test/run-tests-with-coverage.sh [--threshold 95] [--filter <xunit filter>]
#
# Produces, under NomaOS.Test/TestResults/coverage/:
#   coverage.cobertura.xml   consumed by this gate and by CI
#   coverage.opencover.xml   consumed by SonarQube (sonar.cs.opencover.reportsPaths)
#   coverage.info            LCOV, for editors / Codecov (optional consumer)
#   coverage-summary.md      markdown table, appended to $GITHUB_STEP_SUMMARY in CI
#
# Exits 1 when a test fails OR when line/branch coverage of the owned modules
# is below the threshold. There is no "warn and continue" mode on purpose: a
# coverage gate that does not fail the build is a dashboard, not a gate.
#
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "${SCRIPT_DIR}/.." && pwd)"

PROJECT="${SCRIPT_DIR}/NomaOS.Test.csproj"
RUNSETTINGS="${SCRIPT_DIR}/coverlet.runsettings"
RESULTS_DIR="${SCRIPT_DIR}/TestResults"
OUTPUT_DIR="${RESULTS_DIR}/coverage"

THRESHOLD="${COVERAGE_THRESHOLD:-95}"
CONFIGURATION="${CONFIGURATION:-Release}"
FILTER=""
VERBOSITY="${VERBOSITY:-minimal}"

RED=''; GREEN=''; YELLOW=''; BOLD=''; RESET=''
if [[ -t 1 ]]; then
  RED=$'\033[31m'; GREEN=$'\033[32m'; YELLOW=$'\033[33m'; BOLD=$'\033[1m'; RESET=$'\033[0m'
fi

info() { printf '%s==>%s %s\n' "${BOLD}" "${RESET}" "$*"; }
ok()   { printf '%s  ok%s %s\n' "${GREEN}" "${RESET}" "$*"; }
warn() { printf '%s  !!%s %s\n' "${YELLOW}" "${RESET}" "$*" >&2; }
die()  { printf '%s  xx%s %s\n' "${RED}" "${RESET}" "$*" >&2; exit 1; }

usage() {
  sed -n '2,20p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
  exit 0
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --threshold) THRESHOLD="${2:?--threshold needs a value}"; shift 2 ;;
    --filter)    FILTER="${2:?--filter needs a value}"; shift 2 ;;
    --config)    CONFIGURATION="${2:?--config needs a value}"; shift 2 ;;
    -h|--help)   usage ;;
    *)           die "unknown argument: $1 (try --help)" ;;
  esac
done

command -v dotnet >/dev/null 2>&1 || die "dotnet SDK not found on PATH"

# ---------------------------------------------------------------- run tests --
rm -rf "${RESULTS_DIR}"
mkdir -p "${OUTPUT_DIR}"

declare -a test_args=(
  test "${PROJECT}"
  --configuration "${CONFIGURATION}"
  --verbosity "${VERBOSITY}"
  --logger "console;verbosity=${VERBOSITY}"
  --logger "trx;LogFileName=test-results.trx"
  --results-directory "${RESULTS_DIR}"
  --collect:"XPlat Code Coverage"
  --settings "${RUNSETTINGS}"
)
[[ -n "${FILTER}" ]] && test_args+=(--filter "${FILTER}")

info "dotnet test (${CONFIGURATION}) with coverage"
set +e
(cd "${REPO_ROOT}" && dotnet "${test_args[@]}")
TEST_EXIT=$?
set -e

# ------------------------------------------------------- collect the reports --
# The collector writes into a per-run GUID directory; flatten it so CI and the
# gate always look at the same two paths.
COBERTURA_SRC="$(find "${RESULTS_DIR}" -name 'coverage.cobertura.xml' -print -quit 2>/dev/null || true)"
LCOV_SRC="$(find "${RESULTS_DIR}" -name 'coverage.info' -print -quit 2>/dev/null || true)"
OPENCOVER_SRC="$(find "${RESULTS_DIR}" -name 'coverage.opencover.xml' -print -quit 2>/dev/null || true)"

if [[ -z "${COBERTURA_SRC}" ]]; then
  [[ ${TEST_EXIT} -ne 0 ]] && die "tests failed (exit ${TEST_EXIT}) and produced no coverage report"
  die "no coverage.cobertura.xml was produced — is coverlet.collector referenced?"
fi

COBERTURA="${OUTPUT_DIR}/coverage.cobertura.xml"
cp "${COBERTURA_SRC}" "${COBERTURA}"
[[ -n "${LCOV_SRC}" ]] && cp "${LCOV_SRC}" "${OUTPUT_DIR}/coverage.info"
[[ -n "${OPENCOVER_SRC}" ]] && cp "${OPENCOVER_SRC}" "${OUTPUT_DIR}/coverage.opencover.xml"

# --------------------------------------------------------------- parse rates --
# Cobertura keeps the totals as attributes on the root <coverage> element.
attr() {
  sed -n 's/.*<coverage[^>]*[[:space:]]'"$1"'="\([0-9.eE+-]*\)".*/\1/p' "${COBERTURA}" | head -n 1
}

LINE_RATE="$(attr 'line-rate')"
BRANCH_RATE="$(attr 'branch-rate')"

[[ -n "${LINE_RATE}" ]]   || die "could not read line-rate from ${COBERTURA}"
[[ -n "${BRANCH_RATE}" ]] || die "could not read branch-rate from ${COBERTURA}"

pct() { awk -v rate="$1" 'BEGIN { printf "%.2f", rate * 100 }'; }

LINE_PCT="$(pct "${LINE_RATE}")"
BRANCH_PCT="$(pct "${BRANCH_RATE}")"

# ------------------------------------------------------------ markdown table --
SUMMARY="${OUTPUT_DIR}/coverage-summary.md"
{
  printf '## Coverage — observability & health modules\n\n'
  printf '| Metric | Coverage | Threshold | Result |\n'
  printf '| --- | ---: | ---: | :---: |\n'
  for metric in "Line:${LINE_PCT}" "Branch:${BRANCH_PCT}"; do
    name="${metric%%:*}"
    value="${metric##*:}"
    verdict="$(awk -v v="${value}" -v t="${THRESHOLD}" 'BEGIN { print (v + 0 >= t + 0) ? "pass" : "FAIL" }')"
    printf '| %s | %s%% | %s%% | %s |\n' "${name}" "${value}" "${THRESHOLD}" "${verdict}"
  done
  printf '\n### Per class\n\n'
  printf '| Class | Line | Branch |\n'
  printf '| --- | ---: | ---: |\n'
  # One row per <class>, deduplicated (partial classes appear more than once).
  tr '>' '>\n' < "${COBERTURA}" \
    | sed -n 's/.*<class name="\([^"]*\)" filename="[^"]*" line-rate="\([0-9.eE+-]*\)" branch-rate="\([0-9.eE+-]*\)".*/\1|\2|\3/p' \
    | sort -u \
    | awk -F'|' '{ printf "| `%s` | %.2f%% | %.2f%% |\n", $1, $2 * 100, $3 * 100 }'
  printf '\nReport: `%s`\n' "${COBERTURA#"${REPO_ROOT}"/}"
} > "${SUMMARY}"

if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
  cat "${SUMMARY}" >> "${GITHUB_STEP_SUMMARY}"
fi

cat "${SUMMARY}"

# ------------------------------------------------------------------- verdict --
if [[ ${TEST_EXIT} -ne 0 ]]; then
  die "tests failed (dotnet test exited ${TEST_EXIT})"
fi
ok "all tests passed"

BELOW="$(awk -v l="${LINE_PCT}" -v b="${BRANCH_PCT}" -v t="${THRESHOLD}" \
  'BEGIN { print (l + 0 < t + 0 || b + 0 < t + 0) ? "yes" : "no" }')"

if [[ "${BELOW}" == "yes" ]]; then
  warn "line ${LINE_PCT}% / branch ${BRANCH_PCT}% (threshold ${THRESHOLD}%)"
  die "coverage gate failed"
fi

ok "coverage gate passed — line ${LINE_PCT}%, branch ${BRANCH_PCT}% (threshold ${THRESHOLD}%)"
