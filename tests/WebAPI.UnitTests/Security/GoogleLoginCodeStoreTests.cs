using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using WebAPI.Services;

namespace WebAPI.UnitTests.Security;

public class GoogleLoginCodeStoreTests
{
    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Test]
    public void Code_RequiresMatchingVerifierAndCanOnlyBeRedeemedOnce()
    {
        var store = new GoogleLoginCodeStore(TimeProvider.System);
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var challenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var code = store.Issue("test-token", challenge);
        Assert.That(code, Does.Not.Contain("test-token"));
        Assert.That(store.Redeem(code, new string('x', 43)), Is.Null);
        Assert.That(store.Redeem(code, verifier), Is.EqualTo("test-token"));
        Assert.That(store.Redeem(code, verifier), Is.Null);
    }

    [Test]
    public void Code_ExpiresAfterTwoMinutes()
    {
        var clock = new TestClock();
        var store = new GoogleLoginCodeStore(clock);
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var code = store.Issue("test-token", WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))));
        clock.Now = clock.Now.AddMinutes(2);
        Assert.That(store.Redeem(code, verifier), Is.Null);
    }

    [Test]
    public async Task ConcurrentRedemption_OnlyOneRequestSucceeds()
    {
        var store = new GoogleLoginCodeStore(TimeProvider.System);
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var code = store.Issue("test-token", WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))));
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => store.Redeem(code, verifier))));
        Assert.That(results.Count(value => value != null), Is.EqualTo(1));
    }
}
