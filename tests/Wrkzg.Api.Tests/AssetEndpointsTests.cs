using FluentAssertions;
using Wrkzg.Api.Endpoints;
using Xunit;

namespace Wrkzg.Api.Tests;

/// <summary>
/// Unit tests for asset filename sanitization.
/// </summary>
public class AssetEndpointsTests
{
    /// <summary>Verifies that special characters such as spaces and parentheses are stripped from filenames.</summary>
    [Fact]
    public void SanitizeFileName_RemovesSpecialCharacters()
    {
        string result = AssetEndpoints.SanitizeFileName("my file (1).mp3");
        result.Should().Be("myfile1.mp3");
    }

    /// <summary>Verifies that the file extension is preserved after sanitization.</summary>
    [Fact]
    public void SanitizeFileName_PreservesExtension()
    {
        string result = AssetEndpoints.SanitizeFileName("alert-sound_v2.wav");
        result.Should().Be("alert-sound_v2.wav");
    }

    /// <summary>Verifies that a filename with only special characters falls back to a generated name.</summary>
    [Fact]
    public void SanitizeFileName_HandlesEmptyName()
    {
        string result = AssetEndpoints.SanitizeFileName("!!!.png");
        result.Should().StartWith("asset_");
        result.Should().EndWith(".png");
    }

    /// <summary>Verifies that the file extension is lowercased after sanitization.</summary>
    [Fact]
    public void SanitizeFileName_LowercasesExtension()
    {
        string result = AssetEndpoints.SanitizeFileName("Image.PNG");
        result.Should().EndWith(".png");
    }

    /// <summary>Verifies that hyphens and underscores are kept intact during sanitization.</summary>
    [Fact]
    public void SanitizeFileName_PreservesHyphensAndUnderscores()
    {
        string result = AssetEndpoints.SanitizeFileName("my-cool_sound.ogg");
        result.Should().Be("my-cool_sound.ogg");
    }
}
