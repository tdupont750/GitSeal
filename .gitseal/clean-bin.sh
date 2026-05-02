#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/_lib.sh"

# Stage-time encryption for binary files. OpenSSL reads from stdin directly
# so no temp file is needed. SEAL_MARK_BIN is written first so smudge-bin
# can detect our blobs.
printf '%s' "$SEAL_MARK_BIN"
exec openssl enc -aes-256-cbc -pbkdf2 -nosalt \
     -pass pass:"$SEAL_PASS" \
     -in /dev/stdin -out /dev/stdout
