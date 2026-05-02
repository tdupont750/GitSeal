#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/_lib.sh"

# Checkout-time decryption for binary files. Buffer stdin, check for our
# marker, strip it, then pipe the raw ciphertext to openssl for decryption.
# Falls back to the raw encrypted blob if decryption fails.
# ---
# Buffer stdin to a temp file.
readonly SEAL_TEMP="$(mktemp)"
# Always clean up on exit.
trap 'rm -f "$SEAL_TEMP"' EXIT
# Drain stdin into the temp file.
cat >"$SEAL_TEMP"
if has_seal_mark "$SEAL_TEMP" "$SEAL_MARK_BIN"; then
  # Strip the 8-byte marker, decrypt; fall back to raw blob on failure.
  dd if="$SEAL_TEMP" bs=1 skip=8 status=none \
    | openssl enc -d -aes-256-cbc -pbkdf2 -nosalt \
        -pass pass:"$SEAL_PASS" \
        -in /dev/stdin -out /dev/stdout 2>/dev/null \
    || cat "$SEAL_TEMP"
else
  # No marker — relay as-is.
  cat "$SEAL_TEMP"
fi
