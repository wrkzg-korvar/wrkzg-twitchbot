using FluentAssertions;
using Wrkzg.Api.Endpoints;
using Xunit;

namespace Wrkzg.Api.Tests;

/// <summary>
/// Unit tests for asset filename sanitization.
/// </summary>
public class AssetEndpointsTests
{
    [Fact]
    public void SanitizeFileName_RemovesSpecialCharacters()
    {
        string result = AssetEndpoints.SanitizeFileName("my file (1).mp3");
        result.Should().Be("myfile1.mp3");
    }

    [Fact]
    public void SanitizeFileName_PreservesExtension()
    {
        string result = AssetEndpoints.SanitizeFileName("alert-sound_v2.wav");
        result.Should().Be("alert-sound_v2.wav");
    }

    [Fact]
    public void SanitizeFileName_HandlesEmptyName()
    {
        string result = AssetEndpoints.SanitizeFileName("!!!.png");
        result.Should().StartWith("asset_");
        result.Should().EndWith(".png");
    }

    [Fact]
    public void SanitizeFileName_LowercasesExtension()
    {
        string result = AssetEndpoints.SanitizeFileName("Image.PNG");
        result.Should().EndWith(".png");
    }

    [Fact]
    public void SanitizeFileName_PreservesHyphensAndUnderscores()
    {
        string result = AssetEndpoints.SanitizeFileName("my-cool_sound.ogg");
        result.Should().Be("my-cool_sound.ogg");
    }
}
