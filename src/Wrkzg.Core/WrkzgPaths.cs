using System;
using System.IO;

namespace Wrkzg.Core;

/// <summary>
/// Central path resolution for all Wrkzg data directories.
/// </summary>
public static class WrkzgPaths
{
    public static string DataDirectory
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Wrkzg");
        }
    }

    public static string AssetsDirectory => Path.Combine(DataDirectory, "assets");
    public static string SoundsDirectory => Path.Combine(AssetsDirectory, "sounds");
    public static string ImagesDirectory => Path.Combine(AssetsDirectory, "images");
    public static string CustomOverlaysDirectory => Path.Combine(AssetsDirectory, "custom-overlays");

    /// <summary>
    /// Ensures all asset directories exist. Call once at app startup.
    /// </summary>
    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(SoundsDirectory);
        Directory.CreateDirectory(ImagesDirectory);
        Directory.CreateDirectory(CustomOverlaysDirectory);
    }
}
