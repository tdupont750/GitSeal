#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/_lib.sh"

# The clean filter runs when git stages a file (working tree → index).
# CharSeal requires a file path rather than stdin, so we buffer all of
# stdin to a temp file first, then encrypt it.
# Output order: SEAL_MARK_TXT prefix, then the ciphertext from CharSeal.
# ---
# Buffer stdin to a temp file.
readonly SEAL_TEMP="$(mktemp)"
# Always clean up on exit.
trap 'rm -f "$SEAL_TEMP"' EXIT
# Drain stdin into the temp file.
cat >"$SEAL_TEMP"
# Write the 8-byte marker prefix first.
printf '%s' "$SEAL_MARK_TXT"
# Encrypt and emit ciphertext.
"$CHARSEAL" encrypt "$SEAL_PASS" "$SEAL_TEMP"
