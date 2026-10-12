#!/usr/bin/env bash

set -u

ROOT="${1:-qa}"
FAILED=0
TOTAL=0

while IFS= read -r -d '' file; do
  TOTAL=$((TOTAL + 1))

  echo "============================================================"
  echo "Checking: $file"
  echo "============================================================"

  if ! bash -n "$file"; then
    echo "❌ FAILED: Bash syntax"
    FAILED=$((FAILED + 1))
    continue
  fi

  if grep -n '^[[:space:]]*```' "$file"; then
    echo "❌ FAILED: Markdown fence detected"
    FAILED=$((FAILED + 1))
    continue
  fi

  if command -v shellcheck >/dev/null 2>&1; then
    if ! shellcheck "$file"; then
      echo "❌ FAILED: ShellCheck"
      FAILED=$((FAILED + 1))
      continue
    fi
  fi

  echo "✅ PASSED"
done < <(find "$ROOT" -type f -name '*.sh' -print0)

echo ""
echo "============================================================"
echo "QA SCRIPT VALIDATION SUMMARY"
echo "============================================================"
echo "Scripts checked : $TOTAL"
echo "Scripts failed  : $FAILED"
echo "Scripts passed  : $((TOTAL - FAILED))"
echo "============================================================"

if (( FAILED > 0 )); then
  exit 1
fi

echo "ALL QA SCRIPTS PASSED"
