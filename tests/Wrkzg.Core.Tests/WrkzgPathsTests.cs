using FluentAssertions;
using Wrkzg.Core;
using Xunit;

namespace Wrkzg.Core.Tests;

/// <summary>Tests for the WrkzgPaths static path configuration.</summary>
public class WrkzgPathsTests
{
    /// <summary>Verifies that the data directory path is not null or whitespace.</summary>
    [Fact]
    public void DataDirectory_IsNotEmpty()
    {
        WrkzgPaths.DataDirectory.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>Verifies that the assets directory is under the data directory.</summary>
    [Fact]
    public void AssetsDirectory_ContainsDataDirectory()
    {
        WrkzgPaths.AssetsDirectory.Should().StartWith(WrkzgPaths.DataDirectory);
    }

    /// <summary>Verifies that the sounds directory is under the assets directory.</summary>
    [Fact]
    public void SoundsDirectory_IsUnderAssets()
    {
        WrkzgPaths.SoundsDirectory.Should().StartWith(WrkzgPaths.AssetsDirectory);
        WrkzgPaths.SoundsDirectory.Should().EndWith("sounds");
    }

    /// <summary>Verifies that the images directory is under the assets directory.</summary>
    [Fact]
    public void ImagesDirectory_IsUnderAssets()
    {
        WrkzgPaths.ImagesDirectory.Should().StartWith(WrkzgPaths.AssetsDirectory);
        WrkzgPaths.ImagesDirectory.Should().EndWith("images");
    }
}
