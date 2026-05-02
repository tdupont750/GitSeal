# GitSeal

GitSeal transparently encrypts and decrypts files in your repository.

- It is ***not*** cryptographically safe enough to secure content in a public repository.
- It is ***good enough*** to keep the big tech bots from crawling your private repository.

Why make GitSeal? It was fun, I want my life to be repository driven, and [git-crypt](https://github.com/AGWA/git-crypt) (an unquestionably superior project) hasn't been updated for quite a while.

**Note:** The CharSeal executable is compiled for `linux-x64`, so be sure to update the publish script if want to run on another platform or OS.

![Clause](imgs/GitSeal_small.png)

## Setup

### 1. Install

Use `install.sh` to set up the filters in any target repository:

```bash
bash install.sh <path-to-repo>
```

This copies `.gitseal/` into the target, appends the filter and merge config to `.git/config`, and appends encryption rules to `.gitattributes`. All steps are idempotent.

### 2. Set Passphrase

Restore your previous passphrase, or generate a new one:

```bash
openssl rand -hex 32
```

Set that passphrase in  `.gitseal/seal.env`

```bash
SEAL_PASS='<passphrase>'
```

***Save your passphrase somewhere safe!!!***

### 3. Re-checkout to encrypt

```bash
# Re-checkout the working tree from the index (triggers smudge)
git checkout -- .
# If anything looks stuck:
git checkout -f -- .
# Or discard all local changes:
git reset --hard
```

## How it works

Each filter prefixes every encrypted blob with an 8-byte marker so the smudge filter can detect whether a file is already encrypted before attempting decryption. Files without the matching marker are passed through unchanged.

| Filter     | Marker     | Cipher                                                     | Used for              |
| ---------- | ---------- | ---------------------------------------------------------- | --------------------- |
| `seal-txt` | `Crypt_t_` | CharSeal — seeded Caesar over 95-char printable ASCII ring | Known text extensions |
| `seal-bin` | `Crypt___` | OpenSSL AES-256-CBC (PBKDF2, no salt)                      | Everything else       |

### Merge driver

The `seal-txt` merge driver (`merge-txt.sh`) handles three-way merges of encrypted text files:

- **Clean merge** — decrypts all three blobs (base, ours, theirs), runs `git merge-file`, re-encrypts the result, and writes it back. Git indexes the encrypted output directly.
- **Conflict** — writes the plaintext conflict markers to the working tree so they are human-readable. The file must be re-encrypted manually (or via `git add`) after resolving.

### CharSeal binary

CharSeal is a dotnet project that implements symmetric line by line (mediocre) encryption for clear text files.

```bash
bash src/publish.sh
```

This publishes a trimmed, self-contained `linux-x64` binary directly into `.gitseal/charseal`. The binary is built with `InvariantGlobalization` enabled, so it has no dependency on ICU or any system globalization library.

