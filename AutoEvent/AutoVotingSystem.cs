using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.Interfaces;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using UserSettings.ServerSpecific;

namespace AutoEvent;

internal sealed class AutoVotingSystem : IDisposable
{
    private const int HeaderId = 6102410;
    private const int YesId = 6102411;
    private const int NoId = 6102412;
    private static readonly string[] PermanentExclusions = { "jail", "escape", "lobby", "vote" };
    private readonly AutoEvent _plugin;
    private readonly Dictionary<string, bool> _votes = new(StringComparer.OrdinalIgnoreCase);
    private SSGroupHeader? _header;
    private SSButton? _yesSetting;
    private SSButton? _noSetting;
    private CoroutineHandle _voteCoroutine;
    private Event? _firstEvent;
    private Event? _escapeEvent;
    private bool _enabled;
    private bool _inLobby;
    private bool _voting;
    private bool _pendingRound;
    private bool _ownsLobbyLock;
    private bool _ownsRoundLock;
    private bool _continueToEscape;
    private bool _firstStopped;
    private bool _settingsRegistered;
    private bool _scpStatsDisableSent;
    private int _roundsStarted;
    private int _generation;
    private int _broadcastGeneration;

    internal AutoVotingSystem(AutoEvent plugin)
    {
        _plugin = plugin;
        ValidateConfig();
        _enabled = Settings.Enabled;
        RegisterSettings();
        Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
        Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
        Exiled.Events.Handlers.Server.RestartingRound += OnRestartingRound;
        Exiled.Events.Handlers.Player.Left += OnPlayerLeft;
        ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSettingValueReceived;
        if (Round.IsLobby)
            OnWaitingForPlayers();
    }

    private AutoVotingConfig Settings => _plugin.Config.AutoVoting;
    internal string Status => $"Авто-ивенты: {(_enabled ? "включены" : "выключены")}; сыграно раундов: {_roundsStarted}; следующий раунд: {_roundsStarted + 1}; голосование: {(_voting ? "идёт" : "нет")}; за: {YesCount}; против: {NoCount}; цепочка: {(_firstEvent != null || _escapeEvent != null || _pendingRound ? "активна" : "нет")}.";
    private int YesCount => _votes.Values.Count(v => v);
    private int NoCount => _votes.Values.Count(v => !v);

    private void ValidateConfig()
    {
        if (_plugin.Config.AutoVoting == null)
        {
            Log.Warn("Конфигурация авто-ивентов отсутствует; используются значения по умолчанию.");
            _plugin.Config.AutoVoting = new AutoVotingConfig();
        }
        if (Settings.RoundInterval < 1)
        {
            Log.Warn("Период авто-ивентов должен быть больше нуля; установлено 4.");
            Settings.RoundInterval = 4;
        }
        if (Settings.VoteDurationSeconds < 1)
        {
            Log.Warn("Длительность голосования должна быть больше нуля; установлено 45 секунд.");
            Settings.VoteDurationSeconds = 45;
        }
        if (Settings.BroadcastIntervalSeconds < 0.1f || float.IsNaN(Settings.BroadcastIntervalSeconds) || float.IsInfinity(Settings.BroadcastIntervalSeconds))
        {
            Log.Warn("Период сообщения должен быть не меньше 0,1 секунды; установлена 1 секунда.");
            Settings.BroadcastIntervalSeconds = 1f;
        }
        if (Settings.BroadcastDurationSeconds < 0.1f || Settings.BroadcastDurationSeconds > 60f ||
            float.IsNaN(Settings.BroadcastDurationSeconds) || float.IsInfinity(Settings.BroadcastDurationSeconds))
        {
            Log.Warn("Длительность сообщения должна быть от 0,1 до 60 секунд; установлено 1,5 секунды.");
            Settings.BroadcastDurationSeconds = 1.5f;
        }
        if (string.IsNullOrWhiteSpace(Settings.VoteBroadcastText))
        {
            Log.Warn("Текст голосования пуст; восстановлен текст по умолчанию.");
            Settings.VoteBroadcastText = new AutoVotingConfig().VoteBroadcastText;
        }
        if (string.IsNullOrWhiteSpace(Settings.VotePassedText)) Settings.VotePassedText = new AutoVotingConfig().VotePassedText;
        if (string.IsNullOrWhiteSpace(Settings.VoteFailedText)) Settings.VoteFailedText = new AutoVotingConfig().VoteFailedText;
        if (string.IsNullOrWhiteSpace(Settings.SettingsGroupName)) Settings.SettingsGroupName = "Авто-ивенты";
        if (string.IsNullOrWhiteSpace(Settings.YesLabel) || Settings.YesLabel == "Да") Settings.YesLabel = new AutoVotingConfig().YesLabel;
        if (string.IsNullOrWhiteSpace(Settings.NoLabel) || Settings.NoLabel == "Нет") Settings.NoLabel = new AutoVotingConfig().NoLabel;
        Settings.VoteBroadcastText = Settings.VoteBroadcastText.Replace(
            "Настройте клавиши в Server Specific Settings.",
            "Нажмите кнопку в Server Specific Settings.");
        if (string.IsNullOrWhiteSpace(Settings.YesButtonText)) Settings.YesButtonText = "Да";
        if (string.IsNullOrWhiteSpace(Settings.NoButtonText)) Settings.NoButtonText = "Нет";
        Settings.ExcludedEvents ??= new List<string>();
    }

    private void RegisterSettings()
    {
        var existing = ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>();
        if (existing.Any(s => s.SettingId == HeaderId || s.SettingId == YesId || s.SettingId == NoId))
        {
            Log.Error("Не удалось создать кнопки авто-ивентов: идентификаторы Server Specific Settings уже заняты.");
            _enabled = false;
            return;
        }
        _header = new SSGroupHeader(HeaderId, Settings.SettingsGroupName);
        _yesSetting = new SSButton(YesId, Settings.YesLabel, Settings.YesButtonText, 0f, Settings.YesHint);
        _noSetting = new SSButton(NoId, Settings.NoLabel, Settings.NoButtonText, 0f, Settings.NoHint);
        ServerSpecificSettingsSync.DefinedSettings = existing.Concat(new ServerSpecificSettingBase[] { _header, _yesSetting, _noSetting }).ToArray();
        ServerSpecificSettingsSync.SendToAll();
        _settingsRegistered = true;
    }

    private void OnWaitingForPlayers()
    {
        _inLobby = true;
        RestoreScpStats();
        if (_firstEvent != null || _escapeEvent != null)
            AbortChain();
        TryOpenVote();
    }

    private void TryOpenVote()
    {
        if (!_inLobby || !_enabled || _voting || _pendingRound || !AutoVotingRules.ShouldVote(_roundsStarted, Settings.RoundInterval))
            return;
        if (AutoEvent.EventManager?.CurrentEvent != null || Round.IsLobbyLocked)
        {
            Log.Warn("Голосование авто-ивентов пропущено: в лобби уже запущен ивент или действует чужая блокировка.");
            return;
        }
        _votes.Clear();
        _voting = true;
        Round.IsLobbyLocked = true;
        _ownsLobbyLock = true;
        _voteCoroutine = Timing.RunCoroutine(VoteCountdown());
        Log.Info($"Открыто голосование авто-ивентов перед раундом {_roundsStarted + 1} на {Settings.VoteDurationSeconds} секунд.");
    }

    private System.Collections.Generic.IEnumerator<float> VoteCountdown()
    {
        float remaining = Settings.VoteDurationSeconds;
        while (_voting && remaining > 0f)
        {
            string message = Settings.VoteBroadcastText
                .Replace("{time}", Math.Ceiling(remaining).ToString())
                .Replace("{yes}", YesCount.ToString())
                .Replace("{no}", NoCount.ToString());
            Extensions.Broadcast(message, (ushort)Math.Ceiling(Settings.BroadcastDurationSeconds));
            int broadcastGeneration = ++_broadcastGeneration;
            Timing.CallDelayed(Settings.BroadcastDurationSeconds, () =>
            {
                if (_voting && broadcastGeneration == _broadcastGeneration)
                    Map.ClearBroadcasts();
            });
            float step = Math.Min(Settings.BroadcastIntervalSeconds, remaining);
            yield return Timing.WaitForSeconds(step);
            remaining -= step;
        }
        if (!_voting) yield break;
        bool passed = AutoVotingRules.Passed(_votes);
        Log.Info($"Голосование завершено: за {YesCount}, против {NoCount}; решение: {(passed ? "запустить ивенты" : "обычный раунд")}.");
        _voting = false;
        _broadcastGeneration++;
        _pendingRound = passed;
        ReleaseLobbyLock();
        _votes.Clear();
        Extensions.Broadcast(passed ? Settings.VotePassedText : Settings.VoteFailedText, 5);
    }

    private void OnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
    {
        if (!_voting || setting is not SSButton ||
            (setting.SettingId != YesId && setting.SettingId != NoId)) return;
        var player = Player.Get(hub);
        if (player == null || string.IsNullOrWhiteSpace(player.UserId)) return;
        bool yes = setting.SettingId == YesId;
        if (!AutoVotingRules.RecordVote(_votes, player.UserId, yes)) return;
        Log.Debug($"Игрок {player.UserId} проголосовал: {(yes ? "да" : "нет")}. За: {YesCount}; против: {NoCount}.");
    }

    private void OnPlayerLeft(LeftEventArgs ev)
    {
        if (ev.Player?.UserId != null) _votes.Remove(ev.Player.UserId);
    }

    private void OnRoundStarted()
    {
        _inLobby = false;
        _roundsStarted++;
        if (_voting) CancelVote();
        if (!_pendingRound || !_enabled) return;
        _pendingRound = false;
        if (AutoEvent.EventManager?.CurrentEvent != null || Round.IsLocked)
        {
            Log.Warn("Авто-ивенты не запущены: раунд уже заблокирован или другая мини-игра активна.");
            return;
        }
        Round.IsLocked = true;
        _ownsRoundLock = true;
        _continueToEscape = true;
        int generation = ++_generation;
        Timing.CallDelayed(2f, () =>
        {
            if (generation == _generation && _enabled) StartFirstEvent();
        });
    }

    private void StartFirstEvent()
    {
        var manager = AutoEvent.EventManager;
        if (manager == null || manager.CurrentEvent != null) { AbortChain(); return; }
        var exclusions = new HashSet<string>(PermanentExclusions, StringComparer.OrdinalIgnoreCase);
        foreach (string name in Settings.ExcludedEvents) if (!string.IsNullOrWhiteSpace(name)) exclusions.Add(name.Trim());
        var candidates = manager.Events.Where(e =>
            e is not Games.Jail.Plugin && e is not Games.Escape.Plugin &&
            !exclusions.Contains(e.CommandName) && !exclusions.Contains(e.Name) && IsEventReady(e)).ToList();
        if (candidates.Count == 0)
        {
            Log.Error("Авто-ивенты не запущены: после исключений нет подходящих мини-игр.");
            AbortChain();
            return;
        }
        _firstEvent = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        _firstEvent.CleanupFinished += OnFirstCleanup;
        _firstEvent.EventStopped += OnFirstStopped;
        Log.Info($"Запускается автоматическая мини-игра: {_firstEvent.Name}.");
        _scpStatsDisableSent = SendScpStatsCommand("disable");
        try { _firstEvent.StartEvent(); }
        catch (Exception e)
        {
            Log.Error($"Не удалось запустить мини-игру {_firstEvent.Name}: {e}");
            RestoreScpStats();
            AbortChain();
        }
    }

    private static bool SendScpStatsCommand(string action)
    {
        try
        {
            string response = Server.ExecuteCommand($"scpstats {action}");
            Log.Info($"В консоль отправлена команда /scpstats {action}.");
            if (!string.IsNullOrWhiteSpace(response)) Log.Debug($"Ответ SCPStats: {response}");
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Не удалось выполнить команду /scpstats {action}: {e}");
            return false;
        }
    }

    private void RestoreScpStats()
    {
        if (_scpStatsDisableSent && SendScpStatsCommand("enable"))
            _scpStatsDisableSent = false;
    }

    private static bool IsEventReady(Event ev)
    {
        if (ev is not IEventMap map) return true;
        var maps = ev.InternalConfig?.AvailableMaps?.Select(item => item?.Map?.MapName).ToList() ?? new List<string?>();
        if (maps.Count == 0) maps.Add(map.MapInfo?.MapName);
        foreach (string? name in maps.Where(name => !string.IsNullOrWhiteSpace(name) && !string.Equals(name, "none", StringComparison.OrdinalIgnoreCase)))
        {
            if (Extensions.IsExistsMap(name!, out _)) continue;
            Log.Warn($"Мини-игра {ev.Name} исключена из автоматического выбора: схема {name} недоступна в ProjectMER.");
            return false;
        }
        return true;
    }

    private void OnFirstStopped(string name) => _firstStopped = true;

    private void OnFirstCleanup(string name)
    {
        if (_firstEvent != null)
        {
            _firstEvent.CleanupFinished -= OnFirstCleanup;
            _firstEvent.EventStopped -= OnFirstStopped;
            _firstEvent = null;
        }
        int generation = _generation;
        Timing.CallDelayed(0.1f, () =>
        {
            if (generation != _generation) return;
            if (!_continueToEscape || _firstStopped || !_enabled || AutoEvent.EventManager?.CurrentEvent != null)
            {
                AbortChain();
                return;
            }
            StartEscape();
        });
    }

    private void StartEscape()
    {
        var manager = AutoEvent.EventManager;
        _escapeEvent = manager?.Events.OfType<Games.Escape.Plugin>().FirstOrDefault();
        if (_escapeEvent == null)
        {
            Log.Error("Авто-ивенты: мини-игра Atomic Escape (escape) не найдена.");
            AbortChain();
            return;
        }
        _escapeEvent.CleanupFinished += OnEscapeCleanup;
        Log.Info("Первая мини-игра завершена; запускается Atomic Escape.");
        try { _escapeEvent.StartEvent(); }
        catch (Exception e) { Log.Error($"Не удалось запустить Atomic Escape: {e}"); AbortChain(); }
    }

    private void OnEscapeCleanup(string name)
    {
        if (_escapeEvent != null) _escapeEvent.CleanupFinished -= OnEscapeCleanup;
        _escapeEvent = null;
        Log.Info("Atomic Escape завершён; блокировка раунда снята.");
        AbortChain();
    }

    private void OnRestartingRound()
    {
        _inLobby = false;
        CancelVote();
        AbortChain();
    }

    private void CancelVote()
    {
        if (_voting)
        {
            _voting = false;
            _broadcastGeneration++;
            if (_voteCoroutine.IsRunning) Timing.KillCoroutines(_voteCoroutine);
            Log.Info("Голосование авто-ивентов отменено.");
        }
        _pendingRound = false;
        _votes.Clear();
        ReleaseLobbyLock();
    }

    private void ReleaseLobbyLock()
    {
        if (!_ownsLobbyLock) return;
        Round.IsLobbyLocked = false;
        _ownsLobbyLock = false;
    }

    private void AbortChain()
    {
        _generation++;
        _continueToEscape = false;
        _firstStopped = false;
        if (_firstEvent != null)
        {
            _firstEvent.CleanupFinished -= OnFirstCleanup;
            _firstEvent.EventStopped -= OnFirstStopped;
            _firstEvent = null;
        }
        if (_escapeEvent != null)
        {
            _escapeEvent.CleanupFinished -= OnEscapeCleanup;
            _escapeEvent = null;
        }
        if (_ownsRoundLock)
        {
            Round.IsLocked = false;
            _ownsRoundLock = false;
        }
    }

    internal void CancelForManualEvent()
    {
        CancelVote();
        if (_firstEvent == null && _escapeEvent == null) AbortChain();
    }

    internal void Reset()
    {
        CancelVote();
        _roundsStarted = 0;
        Log.Info("Счётчик раундов авто-ивентов сброшен.");
    }

    internal void SetEnabled(bool enabled)
    {
        if (enabled && !_settingsRegistered)
        {
            Log.Error("Авто-ивенты нельзя включить: кнопки голосования не зарегистрированы.");
            return;
        }
        _enabled = enabled;
        if (!enabled)
        {
            CancelVote();
            _continueToEscape = false;
            if (_firstEvent == null && _escapeEvent == null) AbortChain();
        }
        else TryOpenVote();
        Log.Info($"Авто-ивенты {(enabled ? "включены" : "выключены")} через Remote Admin.");
    }

    public void Dispose()
    {
        Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
        Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
        Exiled.Events.Handlers.Server.RestartingRound -= OnRestartingRound;
        Exiled.Events.Handlers.Player.Left -= OnPlayerLeft;
        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSettingValueReceived;
        CancelVote();
        AbortChain();
        RestoreScpStats();
        var existing = ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>();
        ServerSpecificSettingsSync.DefinedSettings = existing.Where(s => s != _header && s != _yesSetting && s != _noSetting).ToArray();
        ServerSpecificSettingsSync.SendToAll();
    }
}
