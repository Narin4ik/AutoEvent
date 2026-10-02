using System;
using System.Linq;
using System.Reflection;
using Exiled.API.Features;

namespace AutoEvent.API;
public class FriendlyFireSystem
{
    private static readonly PropertyInfo CedModAdminDisabled = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(x => x.GetName().Name?.StartsWith("CedMod", StringComparison.OrdinalIgnoreCase) == true)
        ?.GetType("CedMod.FriendlyFireAutoban")
        ?.GetProperty("AdminDisabled", BindingFlags.Public | BindingFlags.Static);
    public static bool CedModIsPresent { get; private set; }
    public static bool IsFriendlyFireEnabledByDefault { get; set; }
    public static bool FriendlyFireAutoBanDefaultEnabled { get; set; }
    static FriendlyFireSystem()
    {
        CedModIsPresent = false;
        initializeFFSettings();
        FriendlyFireAutoBanDefaultEnabled = IsFriendlyFireEnabledByDefault;
    }
    private static void initializeFFSettings()
    {
        if (CedModAdminDisabled != null)
        {
            DebugLogger.LogDebug("CedMod обнаружен.");
            CedModIsPresent = true;
        }
        else
            DebugLogger.LogDebug("CedMod не обнаружен.");
    }

    public static bool FriendlyFireDetectorIsDisabled
    {
        get
        {
            try
            {
                // if cedmod detector is not paused - false
                if (CedModIsPresent)
                {
                    if (!_cedmodFFAutobanIsDisabled())
                    {
                        return false;
                    }
                }

                // if basegame detector is not paused - false
                return FriendlyFireConfig.PauseDetector;
                // Both MUST be off to be considered "paused".
            }
            catch
            {
                return false;
            }
        }
    }

    private static bool _cedmodFFAutobanIsDisabled()
    {
        return CedModAdminDisabled != null && (bool)CedModAdminDisabled.GetValue(null);
    }

    private static void _cedmodFFDisable()
    {
        CedModAdminDisabled?.SetValue(null, true);
    }

    private static void _cedmodFFEnable()
    {
        CedModAdminDisabled?.SetValue(null, false);
    }

    public static void EnableFriendlyFireDetector()
    {
        DebugLogger.LogDebug("Включается детектор урона по союзникам.");
        try
        {
            FriendlyFireConfig.PauseDetector = false;

            if (CedModIsPresent)
            {
                _cedmodFFEnable();
            }
        }
        catch { }
    }

    public static void DisableFriendlyFireDetector()
    {
        try
        {
            DebugLogger.LogDebug("Выключается детектор урона по союзникам.");
            FriendlyFireConfig.PauseDetector = true;

            if (CedModIsPresent)
            {
                _cedmodFFDisable();
            }
        }
        catch { }
    }

    public static void EnableFriendlyFire()
    {
        DebugLogger.LogDebug("Включается урон по союзникам.");
        
        Server.FriendlyFire = true;
    }

    public static void DisableFriendlyFire()
    {
        DebugLogger.LogDebug("Выключается урон по союзникам.");

        Server.FriendlyFire = false;
    }

    public static void RestoreFriendlyFire()
    {
        DebugLogger.LogDebug("Восстанавливаются настройки урона по союзникам и детектора.");
        Server.FriendlyFire = IsFriendlyFireEnabledByDefault;

        return; //03.05.2025 fix console errors
        
        if (FriendlyFireAutoBanDefaultEnabled && FriendlyFireDetectorIsDisabled)
        {
            EnableFriendlyFireDetector();
        }

        if (!FriendlyFireAutoBanDefaultEnabled && !FriendlyFireDetectorIsDisabled)
        {
            DisableFriendlyFireDetector();
        }
    }
}
