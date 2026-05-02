using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("CharSeal.Tests")]

namespace CharSeal;

/// <summary>
/// Encrypts and decrypts char arrays using a seeded pseudo-random Caesar cipher
/// over a fixed ring of 95 printable ASCII characters.
/// </summary>
/// <remarks>
/// Each line is processed independently. The keystream for a line is derived in
/// two phases, both seeded from the master seed:
/// <list type="number">
///   <item>An initial RNG seeded with <c>masterSeed + lineLength</c> shifts the
///         first ring character on the line.</item>
///   <item>A second RNG seeded with <c>masterSeed + (firstCharOriginalIndex * lineLength)</c>
///         shifts every subsequent ring character on the line.</item>
/// </list>
/// Characters not in the ring (e.g. control characters, newlines) are passed
/// through unchanged and do not advance the RNG.
/// Newlines act as line delimiters and reset the per-line state.
/// </remarks>
public class CipherEngine
{
    /// <summary>All characters eligible for substitution, in ring order.</summary>
    internal const string Ring = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";

    /// <summary>Number of characters in the ring — used as the shift modulus.</summary>
    private static readonly int RingSize = Ring.Length;

    /// <summary>Reverse lookup: character → its index in <see cref="Ring"/>, or -1 if not in ring.</summary>
    private static readonly int[] RingLookup = BuildLookup();

    /// <summary>
    /// Builds the 128-element reverse-lookup table for the ring.
    /// Entries outside the ring are initialised to -1 so callers can use a
    /// single bounds check rather than a dictionary lookup on the hot path.
    /// </summary>
    private static int[] BuildLookup()
    {
        // 128 covers the full 7-bit ASCII range.
        var t = new int[128];
        // -1 signals "not in ring" for the fast bounds check in Transform.
        Array.Fill(t, -1);
        for (int i = 0; i < Ring.Length; i++)
            t[Ring[i]] = i;
        return t;
    }

    /// <summary>Master seed derived from the passphrase via polynomial rolling hash.</summary>
    private readonly long _seed;

    /// <summary>
    /// Initialises the engine with the given seed string.
    /// The seed is hashed to a <see langword="long"/> via polynomial rolling hash.
    /// </summary>
    public CipherEngine(string seed)
    {
        long hash = 0;
        foreach (char c in seed)
            // unchecked: intentional wrap-around on overflow.
            unchecked { hash = hash * 31 + c; }
        _seed = hash;
    }

    /// <summary>Encrypts <paramref name="input"/> in-place.</summary>
    public void Encrypt(char[] input) => Transform(input, encrypt: true);

    /// <summary>Decrypts <paramref name="input"/> in-place.</summary>
    public void Decrypt(char[] input) => Transform(input, encrypt: false);

    /// <summary>
    /// Core transform. Iterates over the input line by line, applying a two-phase
    /// keystream to every ring character on each line.
    /// </summary>
    private void Transform(char[] input, bool encrypt)
    {
        SeededRng rng = default;
        bool rngReady = false;
        bool isFirstChar = true;
        int length = 0;

        for (int i = 0; i < input.Length; i++)
        {
            // Newline resets per-line state; the character itself is not transformed.
            if (input[i] == '\n')
            {
                rngReady = false;
                isFirstChar = true;
                length = 0;
                continue;
            }

            // Phase 1: on the first character of a line, measure the line length and
            // seed the initial RNG with masterSeed + lineLength.
            if (!rngReady)
            {
                var remaining = input.AsSpan(i);
                int nl = remaining.IndexOf('\n');
                length = nl < 0 ? remaining.Length : nl;
                rng = new SeededRng(_seed + length);
                rngReady = true;
            }

            // Chars above 127 are never in the ring; the lookup table only covers ASCII.
            int ringIndex = input[i] < 128 ? RingLookup[input[i]] : -1;
            // Pass non-ring characters through unchanged.
            if (ringIndex < 0)
                continue;

            int shift = rng.Next(RingSize);
            int newIndex = encrypt
                ? (ringIndex + shift) % RingSize
                // +RingSize keeps the result non-negative before %.
                : (ringIndex - shift + RingSize) % RingSize;
            input[i] = Ring[newIndex];

            // Phase 2: after the first ring character is processed, re-seed the RNG
            // using masterSeed + (firstCharOriginalIndex << 20) + lineLength. Both encrypt
            // and decrypt converge on the same value because encrypt uses the original
            // index (ringIndex, pre-shift) and decrypt recovers it via newIndex.
            if (isFirstChar)
            {
                isFirstChar = false;
                // << 20 separates the two seed dimensions: ring position (0–94) occupies
                // bits 20+, line length (0–…) occupies bits 0–19, preventing collisions
                // between inputs that share the same (position + length) sum.
                rng = new SeededRng(_seed + ((long)(encrypt ? ringIndex : newIndex) << 20) + length);
            }
        }
    }
}
