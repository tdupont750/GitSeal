// CharSeal — encrypts or decrypts a text file in-place using CipherEngine and writes
// the result to stdout.
//
// Usage: CharSeal <encrypt|decrypt> <seed> <filepath> [skip]
//   skip  — number of leading bytes to strip before processing (used by the git smudge
//            filter to remove the 8-byte "Seal_t__" blob marker before decrypting).
using CharSeal;

if (args.Length is < 3 or > 4)
{
    Console.Error.WriteLine("Usage: CharSeal <encrypt|decrypt> <seed> <filepath> [skip]");
    return 1;
}

string mode = args[0];
string seed = args[1];
string path = args[2];
int skip = args.Length == 4 ? int.Parse(args[3]) : 0;

if (mode is not "encrypt" and not "decrypt")
{
    Console.Error.WriteLine($"Error: mode must be 'encrypt' or 'decrypt', got '{mode}'");
    return 1;
}

if (!File.Exists(path))
{
    Console.Error.WriteLine($"Error: file not found: {path}");
    return 1;
}

try
{
    var engine = new CipherEngine(seed);
    // skip strips the leading blob marker bytes (e.g. the 8-byte "Seal_t__" prefix).
    char[] content = File.ReadAllText(path).ToCharArray()[skip..];
    if (mode == "encrypt") engine.Encrypt(content); else engine.Decrypt(content);
    Console.Write(new string(content));
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

return 0;
