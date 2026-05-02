namespace CharSeal.Tests;

public class CipherEngineTests
{
    [Fact]
    public void EncryptThenDecryptReturnsOriginal()
    {
        var engine = new CipherEngine("myseed");
        char[] input = "Hello World! abc XYZ 0123".ToCharArray();
        char[] original = (char[])input.Clone();
        engine.Encrypt(input);
        engine.Decrypt(input);
        Assert.Equal(original, input);
    }

    [Fact]
    public void MultilineRoundTrip()
    {
        var engine = new CipherEngine("myseed");
        char[] input = "line one\nline two\nline three".ToCharArray();
        char[] original = (char[])input.Clone();
        engine.Encrypt(input);
        engine.Decrypt(input);
        Assert.Equal(original, input);
    }

    [Fact]
    public void IdenticalLinesEncryptIdentically()
    {
        var engine = new CipherEngine("myseed");
        char[] input = "hello\nhello".ToCharArray();
        engine.Encrypt(input);
        Assert.Equal(input[0], input[6]);
        Assert.Equal(input[1], input[7]);
        Assert.Equal(input[2], input[8]);
        Assert.Equal(input[3], input[9]);
        Assert.Equal(input[4], input[10]);
    }

    [Fact]
    public void EmptySeedRoundTrip()
    {
        var engine = new CipherEngine("");
        char[] input = "Hello World".ToCharArray();
        char[] original = (char[])input.Clone();
        engine.Encrypt(input);
        engine.Decrypt(input);
        Assert.Equal(original, input);
    }

    [Fact]
    public void SameSeedProducesSameCiphertext()
    {
        char[] input1 = "abc123".ToCharArray();
        char[] input2 = "abc123".ToCharArray();
        new CipherEngine("hello").Encrypt(input1);
        new CipherEngine("hello").Encrypt(input2);
        Assert.Equal(input1, input2);
    }

    [Fact]
    public void DifferentSeedsProduceDifferentCiphertext()
    {
        char[] input1 = "abcdefgh12345678".ToCharArray();
        char[] input2 = "abcdefgh12345678".ToCharArray();
        new CipherEngine("seed1").Encrypt(input1);
        new CipherEngine("seed2").Encrypt(input2);
        Assert.NotEqual(input1, input2);
    }

    [Fact]
    public void NonRingCharactersPassThrough()
    {
        var engine = new CipherEngine("anyseed");
        char[] input = "\t\n\r\x01\x1F\x7F".ToCharArray();
        char[] original = (char[])input.Clone();
        engine.Encrypt(input);
        Assert.Equal(original, input);
    }

    [Fact]
    public void SpaceIsTransformed()
    {
        var engine = new CipherEngine("anyseed");
        char[] input = " ".ToCharArray();
        engine.Encrypt(input);
        Assert.NotEqual(' ', input[0]);
        Assert.True(CipherEngine.Ring.Contains(input[0]));
    }

    [Fact]
    public void SpaceEncryptedOutputIsInRing()
    {
        var engine = new CipherEngine("spaceseed");
        char[] input = "a b c".ToCharArray();
        engine.Encrypt(input);
        foreach (char c in input)
            Assert.True(CipherEngine.Ring.Contains(c), $"Encrypted char '{c}' is not in the ring");
    }

    [Fact]
    public void EmptyInputIsNoOp()
    {
        var engine = new CipherEngine("seed");
        char[] empty = Array.Empty<char>();
        engine.Encrypt(empty);
        Assert.Empty(empty);
        engine.Decrypt(empty);
        Assert.Empty(empty);
    }

    [Theory]
    [InlineData('0')]
    [InlineData('9')]
    [InlineData('A')]
    [InlineData('Z')]
    [InlineData('a')]
    [InlineData('z')]
    [InlineData(' ')]
    [InlineData('!')]
    [InlineData('~')]
    public void BoundaryCharactersWrapCorrectlyRoundTrip(char c)
    {
        var engine = new CipherEngine("wrap-test");
        char[] input = new[] { c };
        engine.Encrypt(input);

        Assert.True(CipherEngine.Ring.Contains(input[0]),
            $"Encrypted char '{input[0]}' is not in the ring");

        engine.Decrypt(input);
        Assert.Equal(c, input[0]);
    }

    [Fact]
    public void SpecialCharactersRoundTrip()
    {
        var engine = new CipherEngine("specseed");
        char[] input = "Hello, World! (2024) - test@example.com: foo/bar?x=1&y=2".ToCharArray();
        char[] original = (char[])input.Clone();
        engine.Encrypt(input);
        engine.Decrypt(input);
        Assert.Equal(original, input);
    }

    [Fact]
    public void SpecialCharactersEncryptedOutputIsInRing()
    {
        var engine = new CipherEngine("specseed");
        char[] input = "!@#$%^&*()_+-=[]{}|;':\",./<>?".ToCharArray();
        engine.Encrypt(input);
        foreach (char c in input)
            Assert.True(CipherEngine.Ring.Contains(c), $"Encrypted char '{c}' is not in the ring");
    }

    [Theory]
    [InlineData(' ')]
    [InlineData('!')]
    public void RngAdvancedForRingCharacter(char prefix)
    {
        var engine1 = new CipherEngine("seed");
        var engine2 = new CipherEngine("seed");
        char[] withPrefix    = new[] { prefix, 'a', 'b', 'c' };
        char[] withoutPrefix = "abc".ToCharArray();
        engine1.Encrypt(withPrefix);
        engine2.Encrypt(withoutPrefix);
        Assert.NotEqual(withPrefix[1], withoutPrefix[0]);
        Assert.NotEqual(withPrefix[2], withoutPrefix[1]);
        Assert.NotEqual(withPrefix[3], withoutPrefix[2]);
    }

    [Theory]
    [InlineData('\t')]
    [InlineData('\n')]
    public void RngNotAdvancedForNonRingCharacter(char nonRing)
    {
        var engine1 = new CipherEngine("seed");
        var engine2 = new CipherEngine("seed");
        // For newline: place it before abc (resets line state, abc starts fresh line)
        // For tab: place it before abc, abc at end has no non-ring interference
        char[] withNonRing;
        char[] withoutNonRing;
        if (nonRing == '\n')
        {
            withNonRing    = new[] { '\n', 'a', 'b', 'c' };
            withoutNonRing = "abc".ToCharArray();
        }
        else
        {
            withNonRing    = new[] { nonRing, 'a', 'b', 'c' };
            withoutNonRing = new[] { 'a', 'b', 'c', nonRing };
        }
        engine1.Encrypt(withNonRing);
        engine2.Encrypt(withoutNonRing);
        Assert.Equal(withNonRing[1], withoutNonRing[0]);
        Assert.Equal(withNonRing[2], withoutNonRing[1]);
        Assert.Equal(withNonRing[3], withoutNonRing[2]);
    }
}
