#!/usr/bin/env bash
# Checks a review file follows the review-pr format: every FINDING has all fields and a final verdict.
set -euo pipefail
file="$1"; status=0
for n in $(grep -o '^FINDING-[0-9]*' "$file" | sed 's/FINDING-//'); do
  block=$(awk -v n="FINDING-$n" '$0==n{f=1;next} /^FINDING-/{f=0} f' "$file")
  for field in severity rule location problem evidence suggestion verdict; do
    echo "$block" | grep -q "^$field:" || { echo "FINDING-$n missing $field"; status=1; }
  done
  echo "$block" | grep -Eq '^verdict: (fixed|dismissed: .+|deferred: .+)$' || { echo "FINDING-$n has no final verdict"; status=1; }
done
exit $status
