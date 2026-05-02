# Shared constants and functions for all seal filter/merge-driver scripts.
# Source this file at the top of each mode script — do not execute directly.
#
# Requires: $0 must resolve to a path inside .gitseal/ so that seal.env
# can be found in the same directory.

# Load SEAL_PASS from seal.env, which lives alongside this script in .gitseal/.
# The path is resolved relative to the invoking script ($0) so the filter works
# regardless of which directory git invokes it from.
source "$(dirname "$0")/seal.env"
readonly SEAL_PASS

# Absolute-ish path to the CharSeal binary. Git always runs filter commands
# from the repository root, so a root-relative path works reliably here.
readonly CHARSEAL='./.gitseal/charseal'

# Every encrypted blob is prefixed with an 8-byte ASCII marker before it is
# stored in the index. Distinct markers let smudge detect which filter produced
# a blob. Files that lack a marker are passed through unchanged, which makes
# filters safe to apply to already-plaintext content and allows graceful
# recovery if the passphrase is unavailable.
# ---
# Text (CharSeal) blobs — exactly 8 bytes.
readonly SEAL_MARK_TXT='Crypt_t_'
# Binary (OpenSSL) blobs — exactly 8 bytes.
readonly SEAL_MARK_BIN='Crypt___'

# Returns true (exit 0) if the first 8 bytes of the given file match the given
# marker exactly. Usage: has_seal_mark <file> <mark>
has_seal_mark() {
  head -c 8 "$1" 2>/dev/null | grep -q "^$2$"
}
