using System.Numerics;

namespace CharSeal;

/// <summary>
/// PCG32 pseudo-random number generator seeded from a <see langword="long"/>.
/// </summary>
/// <remarks>
/// Uses a single fixed stream (increment = 3). The constructor runs two warm-up
/// steps around the seed injection so the initial state is fully mixed regardless
/// of seed value — a standard PCG seeding ceremony.
/// </remarks>
public struct SeededRng
{
    // Increment must be odd — PCG requirement for a full-period LCG. Stream 1: (1 << 1) | 1 = 3.
    private const ulong Increment = 3UL;

    private ulong _state;

    /// <summary>
    /// Initialises the RNG with <paramref name="seed"/>.
    /// </summary>
    /// <remarks>
    /// Two warm-up rounds bracket the seed injection so that seeds differing
    /// by a single bit produce statistically independent sequences.
    /// </remarks>
    public SeededRng(long seed)
    {
        // Advance from the zero state before injecting the seed.
        NextUInt();
        // Inject seed mid-stream.
        _state += (ulong)seed;
        // Mix the seeded state so the first output is fully dispersed.
        NextUInt();
    }

    /// <summary>
    /// Advances the LCG state and returns the next 32-bit output via the PCG
    /// xorshift-then-rotate output transformation.
    /// </summary>
    private uint NextUInt()
    {
        ulong old = _state;
        // LCG step — standard PCG multiplier.
        _state = old * 6364136223846793005UL + Increment;
        // Xorshift folds 64 bits down to 32.
        uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
        // Top 5 bits select the rotation amount (0–31).
        int rot = (int)(old >> 59);
        // Rotation removes positional bias from the xorshift output.
        return BitOperations.RotateRight(xorshifted, rot);
    }

    /// <summary>
    /// Returns a non-negative random integer in <c>[0, <paramref name="maxExclusive"/>)</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxExclusive"/> is not positive.
    /// </exception>
    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        return (int)(NextUInt() % (uint)maxExclusive);
    }
}
