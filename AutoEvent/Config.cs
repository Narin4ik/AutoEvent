using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using PlayerRoles;

namespace AutoEvent;
public class Config : Exiled.API.Interfaces.IConfig
{
    public Config()
    {
        string basePath = Path.Combine(Exiled.API.Features.Paths.Configs, "AutoEvent");
        SchematicsDirectoryPath = Path.Combine(LabApi.Loader.Features.Paths.PathManager.Configs.FullName, "ProjectMER", "Schematics");
        MusicDirectoryPath = Path.Combine(basePath, "Music");
    }

    [Description("Enable/Disable AutoEvent.")]
    public bool IsEnabled { get; set; } = true;

    [Description("Enable/Disable Debug.")]
    public bool Debug { get; set; } = false;
    
    [Description("Enables / Disables Auto-Logging to a debug output file. Enabled by default on debug releases.")]
    public bool AutoLogDebug { get; set; } = false;
    
    [Description("The global volume of plugins (0 - 200, 100 is normal)")]
    public float Volume { get; set; } = 100;

    [Description("Roles that should be ignored during events.")]
    public List<RoleTypeId> IgnoredRoles { get; set; } = new()
    {
        RoleTypeId.Tutorial,
        RoleTypeId.Overwatch,
        RoleTypeId.Filmmaker
    };

    [Description("The players will be set once an event is done. **DO NOT USE A ROLE THAT IS ALSO IN IgnoredRoles**")]
    public RoleTypeId LobbyRole { get; set; } = RoleTypeId.ClassD;
    
    [Description("ProjectMER schematic directory. ProjectMER controls this path; AutoEvent uses its active directory.")]
    public string SchematicsDirectoryPath { get; set; }
    
    [Description("Where the music directory is located. By default it is located in the AutoEvent folder.")]
    public string MusicDirectoryPath { get; set; }

    [Description("Настройки автоматического голосования за мини-игры.")]
    public AutoVotingConfig AutoVoting { get; set; } = new();
}

public class AutoVotingConfig
{
    [Description("Включить автоматические ивенты после загрузки плагина.")]
    public bool Enabled { get; set; } = true;

    [Description("Голосовать перед каждым N-м раундом (4, 8, 12 при значении 4).")]
    public int RoundInterval { get; set; } = 4;

    [Description("Продолжительность голосования в секундах.")]
    public int VoteDurationSeconds { get; set; } = 45;

    [Description("Период обновления сообщения о голосовании в секундах.")]
    public float BroadcastIntervalSeconds { get; set; } = 1f;

    [Description("Продолжительность каждого сообщения о голосовании в секундах.")]
    public float BroadcastDurationSeconds { get; set; } = 1.5f;

    [Description("Текст сообщения. Параметры: {time}, {yes}, {no}.")]
    public string VoteBroadcastText { get; set; } = "<size=28><color=yellow>Авто-ивенты: голосуйте Да или Нет!</color>\nДа: {yes} | Нет: {no} | Осталось: {time} с\nНажмите кнопку в Server Specific Settings.</size>";

    [Description("Сообщение после успешного голосования.")]
    public string VotePassedText { get; set; } = "Голосование завершено: авто-ивенты начнутся в этом раунде.";

    [Description("Сообщение после отклонённого голосования.")]
    public string VoteFailedText { get; set; } = "Голосование завершено: обычный раунд.";

    [Description("Название группы в Server Specific Settings.")]
    public string SettingsGroupName { get; set; } = "Авто-ивенты";

    [Description("Подпись строки голосования «Да».")]
    public string YesLabel { get; set; } = "Проголосовать \"Да\" за проведение ивентов";

    [Description("Подсказка для кнопки согласия.")]
    public string YesHint { get; set; } = "Голосовать за два ивента подряд";

    [Description("Текст кнопки голосования «Да».")]
    public string YesButtonText { get; set; } = "Да";

    [Description("Подпись строки голосования «Нет».")]
    public string NoLabel { get; set; } = "Проголосовать \"Нет\" против проведения ивентов";

    [Description("Подсказка для кнопки отказа.")]
    public string NoHint { get; set; } = "Голосовать за обычный раунд";

    [Description("Текст кнопки голосования «Нет».")]
    public string NoButtonText { get; set; } = "Нет";

    [Description("Дополнительные исключения из случайного выбора (имена команд ивентов). jail, escape, lobby и vote исключены всегда.")]
    public List<string> ExcludedEvents { get; set; } = new();
}
