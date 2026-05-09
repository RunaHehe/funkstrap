using Microsoft.Win32;
using System.Runtime.InteropServices;
using Bloxstrap.Models.BloxstrapRPC;

namespace Bloxstrap.Integrations;

public static class WallpaperController
{
    private static string? _originalWallpaper;

    public static void SetWallpaper(WallpaperMessage data)
    {
        try
        {
            if (_originalWallpaper == null)
                _originalWallpaper = GetCurrentWallpaper();

            if (data.Reset == true)
            {
                if (!string.IsNullOrEmpty(_originalWallpaper))
                    ApplyWallpaper(_originalWallpaper, "Fill");

                _originalWallpaper = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(data.Asset))
                return;

            string wallpapersPath = Path.Combine(
                Watcher.robloxPath!,
                "content",
                "bloxstrap",
                "wallpapers"
            );

            string fileName = Path.GetFileName(data.Asset);

            string fullPath = Path.Combine(wallpapersPath, fileName);

            if (!File.Exists(fullPath))
            {
                App.Logger.WriteLine(
                    "WallpaperController",
                    $"Wallpaper does not exist: {fullPath}"
                );

                return;
            }

            ApplyWallpaper(fullPath, data.Style ?? "Fill");
        }
        catch (Exception ex)
        {
            App.Logger.WriteLine(
                "WallpaperController",
                $"Failed to set wallpaper: {ex}"
            );
        }
    }

    private static void ApplyWallpaper(string path, string style)
    {
        App.Logger.WriteLine(
            "WallpaperController",
            $"Applying wallpaper: {path} | style-{style}"
        );

        SetWallpaperStyle(style);

        bool result = SystemParametersInfo(
            SPI_SETDESKWALLPAPER,
            0,
            path,
            SPIF_UPDATEINIFILE | SPIF_SENDCHANGE
        );

        if (!result)
        {
            App.Logger.WriteLine(
                "WallpaperController",
                $"SystemParametersInfo FAILED YOU IDIOT!!! {Marshal.GetLastWin32Error()} | path={path}"
            );
        }
    }

    private static string GetCurrentWallpaper()
    {
        const int SPI_GETDESKWALLPAPER = 0x0073;
        const int MAX_PATH = 260;

        var buffer = new System.Text.StringBuilder(MAX_PATH);

        SystemParametersInfo(
            SPI_GETDESKWALLPAPER,
            MAX_PATH,
            buffer,
            0
        );

        return buffer.ToString();
    }

    private static void SetWallpaperStyle(string style)
    {
        using RegistryKey? key =
            Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Desktop",
                true
            );

        switch (style)
        {
            case "Fill":
                key?.SetValue("WallpaperStyle", "10");
                key?.SetValue("TileWallpaper", "0");
                break;

            case "Fit":
                key?.SetValue("WallpaperStyle", "6");
                key?.SetValue("TileWallpaper", "0");
                break;

            case "Stretch":
                key?.SetValue("WallpaperStyle", "2");
                key?.SetValue("TileWallpaper", "0");
                break;

            case "Tile":
                key?.SetValue("WallpaperStyle", "0");
                key?.SetValue("TileWallpaper", "1");
                break;

            case "Center":
                key?.SetValue("WallpaperStyle", "0");
                key?.SetValue("TileWallpaper", "0");
                break;

            case "Span":
                key?.SetValue("WallpaperStyle", "22");
                key?.SetValue("TileWallpaper", "0");
                break;
        }
    }

    private const int SPI_SETDESKWALLPAPER = 20;
    private const int SPI_GETDESKWALLPAPER = 0x0073;
    private const int SPIF_UPDATEINIFILE = 0x01;
    private const int SPIF_SENDCHANGE = 0x02;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool SystemParametersInfo(
        int uAction,
        int uParam,
        string lpvParam,
        int fuWinIni
    );

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool SystemParametersInfo(
        int uAction,
        int uParam,
        System.Text.StringBuilder lpvParam,
        int fuWinIni
    );
}