using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Bloxstrap.Models.BloxstrapRPC;

namespace Bloxstrap.Integrations;

public static class WallpaperController
{
    private static string? _originalWallpaper;

    private static bool _wallpaperApps = false;
    private static readonly List<string> _closedWallpaperApps = new();
    private static readonly string[] WallpaperProcesses =
    {
        "wallpaper32",
        "wallpaper64",
        "Lively",
        "LivelyUI",
        "Livelywpf",
    };

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

                RestoreWallpaperApps();

                _originalWallpaper = null;
                return;
            }

            CloseWallpaperApps();

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

    private static void CloseWallpaperApps()
    {
        if (_wallpaperApps)
            return;

        _wallpaperApps = true;

        foreach (string procName in WallpaperProcesses)
        {
            foreach (Process proc in Process.GetProcessesByName(procName))
            {
                try
                {
                    string? exe = null;

                    try
                    {
                        exe = proc.MainModule?.FileName;
                    }
                    catch { }

                    if (!string.IsNullOrWhiteSpace(exe))
                        _closedWallpaperApps.Add(exe);

                    App.Logger.WriteLine(
                        "WallpaperController", $"Closing wallpaper app: {proc.ProcessName}"
                    );

                    proc.CloseMainWindow();

                    if (!proc.WaitForExit(3000))
                        proc.Kill();
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(
                        "WallpaperController", $"Failed to close wallpaper app: {ex}"
                    );
                }
            }
        }
    }

    private static void RestoreWallpaperApps()
    {
        foreach (string exe in _closedWallpaperApps)
        {
            try
            {
                if (File.Exists(exe))
                {
                    Process.Start(exe);

                    App.Logger.WriteLine(
                        "WallpaperController", $"Failed to restart wallpaper app: {exe}"
                    );
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(
                    "WallpaperController", $"Failed to restart wallpaper app: {ex}"
                );
            }
        }

        _closedWallpaperApps.Clear();
        _wallpaperApps = false;
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