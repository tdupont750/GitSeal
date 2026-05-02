#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/_lib.sh"

# Git calls this driver as: merge-txt.sh %O %A %B
# $1 = %O = base (ancestor), $2 = %A = ours and output path, $3 = %B = theirs.
# All three are encrypted blobs from the object store (have Crypt_t_ marker).
# Decrypt all three, run git merge-file on the plaintext, then write back to %A:
# • Clean merge: write ENCRYPTED output — git's 'ort' strategy does not re-run
#   the clean filter on merge driver output, so the driver must encrypt itself.
# • Conflict: write PLAINTEXT with conflict markers — git copies %A straight to
#   the working tree; the user resolves and 'git add' re-encrypts via clean.

readonly BASE="${1}"
readonly OURS="${2}"
readonly THEIRS="${3}"
# ---
# Temp files for decrypted content.
readonly TMP_BASE="$(mktemp)"
readonly TMP_OURS="$(mktemp)"
readonly TMP_THEIRS="$(mktemp)"
# Always clean up on exit.
trap 'rm -f "$TMP_BASE" "$TMP_OURS" "$TMP_THEIRS"' EXIT
# ---
# Helper to decrypt a seal-txt blob into a destination file.
# Usage: decrypt_blob <src> <dst>
decrypt_blob() {
  local src="$1" dst="$2"
  if has_seal_mark "$src" "$SEAL_MARK_TXT"; then
    # Pass skip=8 so charseal discards the 8-byte marker before decrypting.
    "$CHARSEAL" decrypt "$SEAL_PASS" "$src" 8 >"$dst" 2>/dev/null \
      || cp "$src" "$dst"
  else
    cp "$src" "$dst"
  fi
}
# Decrypt base. Fall back to raw content if decryption fails.
decrypt_blob "$BASE" "$TMP_BASE"
# Decrypt ours. Fall back to raw content if decryption fails.
decrypt_blob "$OURS" "$TMP_OURS"
# Decrypt theirs. Fall back to raw content if decryption fails.
decrypt_blob "$THEIRS" "$TMP_THEIRS"
# ---
# Run 3-way merge on plaintext. git merge-file modifies TMP_OURS in place.
# Capture exit status manually — set -e would abort on the non-zero conflict
# exit before we can copy the result to %A.
MERGE_STATUS=0
git merge-file "$TMP_OURS" "$TMP_BASE" "$TMP_THEIRS" || MERGE_STATUS=$?
# ---
# Write result back to %A. The behaviour differs by outcome:
# • Clean merge (MERGE_STATUS=0): git's 'ort' strategy does NOT re-run the
#   clean filter on the merge driver's output, so we must write ENCRYPTED
#   content to %A ourselves.  The working tree is then populated via the
#   normal smudge filter, so the user will see plaintext there.
# • Conflict (MERGE_STATUS!=0): git copies %A straight to the working tree
#   for manual resolution, so we must write PLAINTEXT (with conflict markers)
#   to %A.  When the user resolves and runs 'git add', the clean filter
#   re-encrypts automatically.
if [[ "$MERGE_STATUS" -eq 0 ]]; then
  # Encrypt the merged plaintext. Write to a temp file first so that %A is
  # only replaced after a successful encrypt — a partial write to %A would
  # leave a truncated/corrupt blob that smudge cannot recover.
  readonly TMP_ENCRYPTED="$(mktemp)"
  trap 'rm -f "$TMP_BASE" "$TMP_OURS" "$TMP_THEIRS" "$TMP_ENCRYPTED"' EXIT
  printf '%s' "$SEAL_MARK_TXT" >"$TMP_ENCRYPTED"
  "$CHARSEAL" encrypt "$SEAL_PASS" "$TMP_OURS" >>"$TMP_ENCRYPTED"
  cp "$TMP_ENCRYPTED" "$OURS"
else
  # Leave plaintext (with conflict markers) in %A for the user to resolve.
  cp "$TMP_OURS" "$OURS"
fi
exit $MERGE_STATUS
