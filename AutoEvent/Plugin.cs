using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using AutoEvent.API;
using Exiled.API.Features;

namespace AutoEvent;
public class AutoEvent : Plugin<Config>
{
    public override string Name => "AutoEvent";
    public override string Author => "Narin (ex. RisottoMan)";
    public override Version Version => Version.Parse("9.11.6");
    public override Version RequiredExiledVersion => new(9, 14, 2);
    public static string BaseConfigPath { get; set;}
    public static AutoEvent Singleton;
    public static Harmony HarmonyPatch;
    public static EventManager EventManager;
    private EventHandler _eventHandler;
    internal AutoVotingSystem VotingSystem { get; private set; }
    
    public override void OnEnabled()
    {
        if (!Config.IsEnabled) return;

        if (!AppDomain.CurrentDomain.GetAssemblies().Any(x => x.GetName().Name == "ProjectMER"))
        {
            Log.Error("Для AutoEvent нужен ProjectMER 2026.7.6.1 в LabAPI/plugins/global. Установите и загрузите ProjectMER перед включением AutoEvent.");
            return;
        }

        CosturaUtility.Initialize();
        
        BaseConfigPath = Path.Combine(Paths.Configs, "AutoEvent");
        if (!ConfigureSchematicsDirectory()) return;
        
        try
        {
            Singleton = this;
            
            if (Config.IgnoredRoles.Contains(Config.LobbyRole))
            {
                DebugLogger.LogDebug("Роль лобби указана среди игнорируемых ролей. Она удалена из списка, иначе мини-игры не будут работать.", LogLevel.Error, true);
                Config.IgnoredRoles.Remove(Config.LobbyRole);
            }

            FriendlyFireSystem.IsFriendlyFireEnabledByDefault = Server.FriendlyFire;

            var debugLogger = new DebugLogger(Config.AutoLogDebug);
            DebugLogger.Debug = Config.Debug;
            if (DebugLogger.Debug)
            {
                DebugLogger.LogDebug("Режим отладки включён.", LogLevel.Info, true);
            }
            
            try
            {
                HarmonyPatch = new Harmony("autoevent");
                HarmonyPatch.PatchAll();
            }
            catch (Exception e)
            {
                DebugLogger.LogDebug("Не удалось применить патчи Harmony.", LogLevel.Warn, true);
                DebugLogger.LogDebug($"{e}");
            }

            try
            {
                DebugLogger.LogDebug($"Путь конфигурации: {BaseConfigPath}");
                DebugLogger.LogDebug($"Пути ресурсов: \n" +
                                     $"{Config.SchematicsDirectoryPath}\n" +
                                     $"{Config.MusicDirectoryPath}\n");
                CreateDirectoryIfNotExists(BaseConfigPath);
                CreateDirectoryIfNotExists(Config.MusicDirectoryPath);
            }
            catch (Exception e)
            {
                DebugLogger.LogDebug("Ошибка инициализации каталогов.", LogLevel.Warn, true);
                DebugLogger.LogDebug($"{e}");
            }

            _eventHandler = new EventHandler(this);
            EventManager = new EventManager();
            EventManager.RegisterInternalEvents();
            ConfigManager.LoadConfigsAndTranslations();
            VotingSystem = new AutoVotingSystem(this);
            
            DebugLogger.LogDebug("Мини-игры загружены.", LogLevel.Info, true);
        }
        catch (Exception e)
        {
            DebugLogger.LogDebug("Ошибка запуска плагина.", LogLevel.Warn, true);
            DebugLogger.LogDebug($"{e}");
        }
        
        base.OnEnabled();
    }
    
    private static void CreateDirectoryIfNotExists(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        catch (Exception e)
        {
            DebugLogger.LogDebug("Не удалось создать каталог.", LogLevel.Warn, true);
            DebugLogger.LogDebug($"Путь: {path}\n{e}");
        }
    }

    private bool ConfigureSchematicsDirectory()
    {
        var merDirectory = ProjectMER.ProjectMER.SchematicsDir;
        if (string.IsNullOrWhiteSpace(merDirectory))
        {
            Log.Error("ProjectMER найден, но каталог схем не инициализирован. Проверьте журнал загрузки LabAPI.");
            return false;
        }

        if (!string.Equals(Config.SchematicsDirectoryPath, merDirectory, StringComparison.OrdinalIgnoreCase))
            Log.Warn($"AutoEvent использует каталог схем ProjectMER: {merDirectory}. Параметр schematics_directory_path игнорируется.");

        Config.SchematicsDirectoryPath = merDirectory;
        return true;
    }
    
    public override void OnDisabled()
    {
        VotingSystem?.Dispose();
        VotingSystem = null;
        _eventHandler?.UnregisterEvents();
        _eventHandler = null;

        HarmonyPatch?.UnpatchAll();
        EventManager = null;
        Singleton = null;
        
        base.OnDisabled();
    }
}
