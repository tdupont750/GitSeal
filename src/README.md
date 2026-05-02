# CharSeal

A .NET 10 console application that encrypts and decrypts text files using a seeded pseudo-random Caesar cipher over a 95-character printable ASCII ring.

## How It Works

CharSeal shifts each character in the input by a pseudo-random amount derived from a seed string. The substitution ring covers all 95 printable ASCII characters (digits, upper and lower case letters, space, and punctuation). Control characters and newlines are outside the ring and pass through unchanged.

**Ring:** `0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz !"#$%&'()*+,-./:;<=>?@[\]^_`{|}~` (95 characters)

Encryption and decryption are exact inverses: encrypting a file with a seed and then decrypting with the same seed always restores the original content.

### Per-line seeding

Each line is processed independently using a two-phase keystream:

1. **Phase 1** — an RNG seeded with `masterSeed + lineLength` shifts the first ring character on the line.
2. **Phase 2** — after the first ring character is shifted, the RNG is re-seeded with `masterSeed + (firstCharOriginalIndex << 20) + lineLength`. This ties the keystream to the content of the line, so identical lines in different files encrypt identically only when the seed matches.

Characters outside the ring do not advance the RNG. Newlines reset the per-line state.

### Components

**`SeededRng`** — PCG32 pseudo-random number generator. Takes a `long` seed and produces a statistically uniform, deterministic sequence of integers. Implemented as a `struct` to avoid heap allocation on the hot path.

**`CipherEngine`** — Converts a seed string to a `long` hash via polynomial rolling hash, then applies the two-phase per-line keystream to shift each ring character in-place.

**`Program`** — CLI entry point. Reads the file, runs the cipher, and writes the result to stdout.

## Usage

```
charseal <encrypt|decrypt> <seed> <filepath> [skip]
```

Output is written to stdout, leaving the source file unchanged. The optional `skip` argument discards that many bytes from the front of the file before processing — used by the git smudge filter to strip the 8-byte encryption marker.

**Encrypt a file:**
```bash
charseal encrypt "my secret seed" document.txt
```

**Decrypt and save to a new file:**
```bash
charseal decrypt "my secret seed" document.txt > document_decrypted.txt
```

**Round-trip example:**
```bash
echo "Hello World 123!" > message.txt
charseal encrypt "myseed" message.txt > encrypted.txt
charseal decrypt "myseed" encrypted.txt
# → Hello World 123!
```

## Building

**Run (development):**
```bash
dotnet run --project CharSeal.Console/CharSeal.Console.csproj -- encrypt "seed" file.txt
```

**Publish as a self-contained single-file binary:**
```bash
bash publish.sh
```

This publishes a trimmed, self-contained `linux-x64` binary directly into `../.gitseal/charseal`.

## Testing

```bash
dotnet test CharSeal.Tests/
```

All tests operate in memory — no files are read from or written to disk.

## Project Structure

```
src/
├── publish.sh
├── CharSeal.slnx
├── CharSeal.Console/
│   ├── CharSeal.Console.csproj
│   ├── Program.cs          # CLI entry point
│   ├── CipherEngine.cs     # Two-phase per-line Caesar cipher
│   └── SeededRng.cs        # PCG32 RNG (struct)
└── CharSeal.Tests/
    ├── CharSeal.Tests.csproj
    ├── CipherEngineTests.cs
    └── SeededRngTests.cs
```
