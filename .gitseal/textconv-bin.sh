#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/_lib.sh"

# Diff-time decryption for binary files. Because the decrypted content is
# still binary, git diff output may not be human-readable, but at least it
# will reflect the actual file content rather than the ciphertext.
readonly SEAL_FILE="${1:-/dev/stdin}"
if [[ "$SEAL_FILE" != "/dev/stdin" ]] && has_seal_mark "$SEAL_FILE" "$SEAL_MARK_BIN"; then
  # Strip the 8-byte marker, decrypt; fall back to raw blob on failure.
  dd if="$SEAL_FILE" bs=1 skip=8 status=none \
    | openssl enc -d -aes-256-cbc -pbkdf2 -nosalt \
        -pass pass:"$SEAL_PASS" \
        -in /dev/stdin -out /dev/stdout 2>/dev/null \
    || cat "$SEAL_FILE"
else
  # No marker — either plaintext or stdin fallback; emit as-is.
  if [[ "$SEAL_FILE" == "/dev/stdin" ]]; then
    cat
  else
    cat "$SEAL_FILE"
  fi
fi
