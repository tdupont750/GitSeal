#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/_lib.sh"

# The smudge filter runs when git checks out a file (index → working tree).
# We buffer stdin to a temp file so we can inspect the first 8 bytes before
# deciding whether to decrypt. If the marker is absent the file is either
# already plaintext or was staged without the filter — pass it through as-is.
# ---
# Buffer stdin to a temp file.
readonly SEAL_TEMP="$(mktemp)"
# Always clean up on exit.
trap 'rm -f "$SEAL_TEMP"' EXIT
# Drain stdin into the temp file.
cat >"$SEAL_TEMP"
if has_seal_mark "$SEAL_TEMP" "$SEAL_MARK_TXT"; then
  # Pass skip=8 so charseal discards the 8-byte marker before decrypting.
  "$CHARSEAL" decrypt "$SEAL_PASS" "$SEAL_TEMP" 8 2>/dev/null \
    || cat "$SEAL_TEMP"
else
  cat "$SEAL_TEMP"
fi
