#!/usr/bin/env bash
set -euo pipefail

# Resolve the directory this script lives in (the gitseal source repo root).
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ── Usage ────────────────────────────────────────────────────────────────────

usage() {
  echo "Usage: $0 <path-to-git-repository>" >&2
  echo "" >&2
  echo "  Installs gitseal filters into the target repository." >&2
  echo "  Existing content in .git/config and .gitattributes is preserved." >&2
  exit 1
}

[[ "${1:-}" == "" ]] && usage
TARGET="$(realpath "$1")"

# ── Validate target ──────────────────────────────────────────────────────────

if [[ ! -d "$TARGET" ]]; then
  echo "Error: '$TARGET' does not exist or is not a directory." >&2
  exit 1
fi

# Initialise a git repository if one is not already present.
if [[ ! -d "$TARGET/.git" ]]; then
  echo "No git repository found at '$TARGET' — initialising one now."
  git -C "$TARGET" init
fi

# ── Copy .gitseal folder ─────────────────────────────────────────────────────

# Copy the entire .gitseal folder (script, binary, and env file) into the
# target repo. An existing .gitseal folder is merged rather than replaced so
# that a pre-existing seal.env passphrase is not accidentally overwritten.
echo "Copying .gitseal to '$TARGET/.gitseal/'..."
mkdir -p "$TARGET/.gitseal"
cp -rn "$SCRIPT_DIR/.gitseal/." "$TARGET/.gitseal/"
chmod +x "$TARGET/.gitseal/"*.sh
chmod +x "$TARGET/.gitseal/charseal"
echo '*' > "$TARGET/.gitseal/.gitignore"

# ── Configure .git/config ────────────────────────────────────────────────────

# Append the seal filter and diff driver sections from git.config.example only
# if they are not already present, so repeated installs remain idempotent.
GIT_CONFIG="$TARGET/.git/config"
if grep -q '\[filter "seal-txt"\]' "$GIT_CONFIG" 2>/dev/null; then
  echo "Filter config already present in '$GIT_CONFIG' — skipping."
else
  echo "Appending filter config to '$GIT_CONFIG'..."
  printf '\n' >> "$GIT_CONFIG"
  cat "$SCRIPT_DIR/config/git.config.example" >> "$GIT_CONFIG"
fi

# If the filter block was already present, git.config.example was not appended
# in full — the merge block may be missing. Check and append it independently.
if grep -q '\[merge "seal-txt"\]' "$GIT_CONFIG" 2>/dev/null; then
  echo "Merge config already present in '$GIT_CONFIG' — skipping."
else
  echo "Appending merge config to '$GIT_CONFIG'..."
  cat >> "$GIT_CONFIG" <<'EOF'

[merge "seal-txt"]
	name = CharSeal text merge
	driver = bash ./.gitseal/merge-txt.sh %O %A %B
EOF
fi

# ── Configure .gitattributes ─────────────────────────────────────────────────

# Append the example gitattributes block only if no seal filter rules are
# already present, so existing encryption rules are not duplicated.
GITATTRIBUTES="$TARGET/.gitattributes"
touch "$GITATTRIBUTES"
if grep -q 'filter=seal-txt' "$GITATTRIBUTES" 2>/dev/null; then
  echo "Gitattributes filter rules already present in '$GITATTRIBUTES' — skipping."
else
  echo "Appending filter rules to '$GITATTRIBUTES'..."
  printf '\n' >> "$GITATTRIBUTES"
  cat "$SCRIPT_DIR/config/.gitattributes.example" >> "$GITATTRIBUTES"
fi

# ── Done ─────────────────────────────────────────────────────────────────────

echo ""
echo "gitseal installed successfully to '$TARGET'."
echo ""
echo "Next steps:"
echo "  1. Set your passphrase in '$TARGET/.gitseal/seal.env'"
echo "  2. Run 'git checkout -f -- .' inside the target repo to apply the filters"
