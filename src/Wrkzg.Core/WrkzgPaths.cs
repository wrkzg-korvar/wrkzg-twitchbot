using System;
using System.IO;

namespace Wrkzg.Core;

/// <summary>
/// Central path resolution for all Wrkzg data directories.
/// </summary>
public static class WrkzgPaths
{
    /// <summary>Gets the root data directory for Wrkzg under the user's application data folder.</summary>
    public static string DataDirectory
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Wrkzg");
        }
    }

    /// <summary>Gets the path to the assets directory containing sounds, images, and custom overlays.</summary>
    public static string AssetsDirectory => Path.Combine(DataDirectory, "assets");

    /// <summary>Gets the path to the directory storing alert and notification sound files.</summary>
    public static string SoundsDirectory => Path.Combine(AssetsDirectory, "sounds");

    /// <summary>Gets the path to the directory storing uploaded image assets.</summary>
    public static string ImagesDirectory => Path.Combine(AssetsDirectory, "images");

    /// <summary>Gets the path to the directory storing user-created custom overlay HTML files.</summary>
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
