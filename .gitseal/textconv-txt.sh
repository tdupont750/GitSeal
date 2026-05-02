#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/_lib.sh"

# The textconv filter is used by git diff to produce a human-readable
# representation of a blob. Git passes the path to a temporary copy of the
# file as the first argument. We decrypt and print to stdout so the diff
# shows plaintext rather than ciphertext.
readonly SEAL_FILE="${1:-/dev/stdin}"
if [[ "$SEAL_FILE" != "/dev/stdin" ]] && has_seal_mark "$SEAL_FILE" "$SEAL_MARK_TXT"; then
  # Strip the marker, write ciphertext to a temp file, then decrypt.
  # ---
  # Temp file for the raw ciphertext.
  readonly SEAL_TEMP="$(mktemp)"
  # Always clean up on exit.
  trap 'rm -f "$SEAL_TEMP"' EXIT
  # Strip the 8-byte marker.
  dd if="$SEAL_FILE" bs=1 skip=8 status=none >"$SEAL_TEMP"
  # Attempt decrypt; fall back to raw blob on failure.
  "$CHARSEAL" decrypt "$SEAL_PASS" "$SEAL_TEMP" 2>/dev/null \
    || cat "$SEAL_FILE"
else
  # No marker — either plaintext or stdin fallback; emit as-is.
  if [[ "$SEAL_FILE" == "/dev/stdin" ]]; then
    cat
  else
    cat "$SEAL_FILE"
  fi
fi
