using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace WebAPI.Services;

// A bounded, process-local handoff store for this single-instance application.
public sealed class GoogleLoginCodeStore(TimeProvider clock)
{
    private sealed record Entry(string Token, string Challenge, DateTimeOffset ExpiresAt);
    private readonly Dictionary<string, Entry> _entries = new();
    private readonly object _gate = new();
    public static bool IsValidChallenge(string? value) => value is { Length: 43 }
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    public string Issue(string token, string challenge)
    {
        if (!IsValidChallenge(challenge))
            throw new ArgumentException("Invalid login challenge.", nameof(challenge));
        lock (_gate)
        {
            var now = clock.GetUtcNow();
            foreach (var key in _entries.Where(pair => pair.Value.ExpiresAt <= now).Select(pair => pair.Key).ToArray())
                _entries.Remove(key);
            if (_entries.Count >= 256)
                throw new InvalidOperationException("Too many pending external logins. Please try again later.");
            var code = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            _entries.Add(code, new Entry(token, challenge, now.AddMinutes(2)));
            return code;
        }
    }

    public string? Redeem(string? code, string? verifier)
    {
        if (!IsValidChallenge(code) || !IsValidChallenge(verifier))
            return null;
        var challenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier!)));
        lock (_gate)
        {
            if (!_entries.TryGetValue(code!, out var entry))
                return null;
            if (entry.ExpiresAt <= clock.GetUtcNow())
            {
                _entries.Remove(code!);
                return null;
            }
            if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(challenge), Encoding.ASCII.GetBytes(entry.Challenge)))
                return null;
            _entries.Remove(code!);
            return entry.Token;
        }
    }
}
