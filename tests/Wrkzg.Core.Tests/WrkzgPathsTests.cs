using FluentAssertions;
using Wrkzg.Core;
using Xunit;

namespace Wrkzg.Core.Tests;

public class WrkzgPathsTests
{
    [Fact]
    public void DataDirectory_IsNotEmpty()
    {
        WrkzgPaths.DataDirectory.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AssetsDirectory_ContainsDataDirectory()
    {
        WrkzgPaths.AssetsDirectory.Should().StartWith(WrkzgPaths.DataDirectory);
    }

    [Fact]
    public void SoundsDirectory_IsUnderAssets()
    {
        WrkzgPaths.SoundsDirectory.Should().StartWith(WrkzgPaths.AssetsDirectory);
        WrkzgPaths.SoundsDirectory.Should().EndWith("sounds");
    }

    [Fact]
    public void ImagesDirectory_IsUnderAssets()
    {
        WrkzgPaths.ImagesDirectory.Should().StartWith(WrkzgPaths.AssetsDirectory);
        WrkzgPaths.ImagesDirectory.Should().EndWith("images");
    }
}
