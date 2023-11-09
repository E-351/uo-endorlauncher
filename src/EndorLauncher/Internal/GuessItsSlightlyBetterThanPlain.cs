using System;
using System.Buffers;
using System.Globalization;
using System.Text;
using Avalonia.Controls.Documents;

namespace EndorLauncher.Internal;

public static class GuessItsSlightlyBetterThanPlain
{
    private static readonly string _key = Environment.MachineName;
    
    public static string Encrypt(string plain)
    {
        ArgumentNullException.ThrowIfNull(plain);

        var builder = new StringBuilder(2 + plain.Length * 2);
        builder.Append("1-");
        for (var i = 0; i < plain.Length; i++)
        {
            builder.Append($"{(byte)(plain[i] ^ (byte)_key[i % _key.Length]):X2}");
        }
        return builder.ToString();
    }

    public static string Decrypt(string encrypted)
    {
        if (encrypted.Length <= 2 || encrypted[0] != '1' || encrypted[1] != '-')
        {
            throw new FormatException("Failed to decrypt password");
        }

        return string.Create((encrypted.Length - 2) / 2, encrypted, (decryptedBuffer, state) =>
        {
            var encryptedBuffer = state.AsSpan()[2..];
            
            for (var i = 0; i < decryptedBuffer.Length; i += 1)
            {
                var c = byte.Parse(encryptedBuffer.Slice(i * 2, 2), NumberStyles.AllowHexSpecifier);
            
                decryptedBuffer[i] = (char)(c ^ _key[i % _key.Length]);
            }
        });
    }
}