using Sweepr.Core.Services;
using Xunit;

namespace Sweepr.Tests;

public class NameCleanerTests
{
    [Theory]
    [InlineData("Audacity 3.6.1", "Audacity")]
    [InlineData("7-Zip 23.01 (x64)", "7-Zip")]
    [InlineData("Mozilla Thunderbird (x64 en-US)", "Mozilla Thunderbird")]
    [InlineData("Malwarebytes version 5.1.7.121", "Malwarebytes")]
    public void Clean_StripsVersionsAndNoise(string input, string expected) =>
        Assert.Equal(expected, NameCleaner.Clean(input));

    [Fact]
    public void Normalize_KeepsOnlyLettersAndDigitsLowercase() =>
        Assert.Equal("7zip", NameCleaner.Normalize("7-Zip"));
}
