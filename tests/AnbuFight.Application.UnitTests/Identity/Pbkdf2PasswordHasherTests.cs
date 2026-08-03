using AnbuFight.Infrastructure.Identity;

namespace AnbuFight.Application.UnitTests.Identity;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_never_stores_the_password_in_clear_text()
    {
        var hash = _hasher.Hash("Anbu@Fight123");

        hash.ShouldNotContain("Anbu@Fight123");
        hash.Split('.').Length.ShouldBe(3);
    }

    [Fact]
    public void Hash_produces_a_different_result_for_the_same_password()
    {
        // Distinct salts: two students with the same password do not share a hash.
        _hasher.Hash("Anbu@Fight123").ShouldNotBe(_hasher.Hash("Anbu@Fight123"));
    }

    [Fact]
    public void Verify_accepts_the_original_password() =>
        _hasher.Verify("Anbu@Fight123", _hasher.Hash("Anbu@Fight123")).ShouldBeTrue();

    [Fact]
    public void Verify_rejects_a_wrong_password() =>
        _hasher.Verify("wrong-password", _hasher.Hash("Anbu@Fight123")).ShouldBeFalse();

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("abc.def.ghi")]
    public void Verify_returns_false_for_malformed_hashes_instead_of_throwing(string malformedHash) =>
        _hasher.Verify("Anbu@Fight123", malformedHash).ShouldBeFalse();

    [Fact]
    public void DecoyHash_is_well_formed_and_matches_nothing()
    {
        _hasher.DecoyHash.Split('.').Length.ShouldBe(3);
        _hasher.Verify("Anbu@Fight123", _hasher.DecoyHash).ShouldBeFalse();
    }
}
