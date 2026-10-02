# AutoEvent 9.11.6 for EXILED 9.14.2

This is an independent compatibility build of the archived AutoEvent source. It requires ExMod EXILED 9.14.2, LabAPI 1.1.7 and ProjectMER 2026.7.6.1. The build was checked against SCP:SL dedicated server build ID 24893701. Server loading and gameplay have not been tested.

## Install

1. Install [ExMod EXILED v9.14.2](https://github.com/ExMod-Team/EXILED/releases/tag/v9.14.2) using the included `Exiled-9.14.2.tar.gz` or the official release instructions. Use a dedicated server with LabAPI 1.1.7.
2. Put [ProjectMER 2026.7.6.1](https://github.com/Michal78900/ProjectMER/releases/tag/2026.7.6.1) (`ProjectMER.dll`, included in the install archive) in `LabAPI/plugins/global/`. Put the included Harmony 2.3.6 `0Harmony.dll` in `LabAPI/dependencies/global/`.
3. Put `AutoEvent.dll` in `EXILED/Plugins/`.
4. Copy the contents of this package's `Schematics/` into `LabAPI/configs/ProjectMER/Schematics/`, keeping each schematic's own folder and files together. ProjectMER owns this directory; AutoEvent no longer redirects it to the EXILED configuration directory.
5. Copy the contents of `Music/` into `EXILED/Configs/AutoEvent/Music/`. On some hosts EXILED stores `Configs` under the service account's application data directory; use the path reported in the server log.
6. Grant `ev.*` to the administrator role in `EXILED/Configs/permissions.yml`, then restart the server.

The `schematics_directory_path` setting from older AutoEvent configurations is retained for compatibility, but the running plugin uses ProjectMER's active schematic directory and logs a warning if the values differ. Move existing custom schematics there. AutoEvent does not delete old folders.

After restart, check the server log for both plugins and use `ev list` and `ev run <name>` to check a map based event. These runtime checks were not performed for this release.

## Автоматические ивенты

Система включена по умолчанию. В лобби перед каждым четвёртым раундом (4, 8, 12...) она блокирует начало раунда на 45 секунд и проводит голосование. Сообщение обновляется раз в секунду; настройки сообщения находятся в разделе `auto_voting` основного конфига EXILED. Игроки открывают Server Specific Settings, находят группу «Авто-ивенты» и нажимают кнопки «Да» или «Нет». Строки подписаны «Проголосовать "Да" за проведение ивентов» и «Проголосовать "Нет" против проведения ивентов». Повторное нажатие сохраняет прежний голос; противоположная кнопка меняет его. Голос вышедшего игрока удаляется.

Если голосов «Да» больше, при начале раунда запускается случайная мини-игра, а после её полной очистки — Atomic Escape. Тюрьма, Atomic Escape, служебные ивенты и мини-игры с недоступными схемами исключены из первого выбора. Если подходящих мини-игр нет, блокировка раунда снимается и причина записывается в консоль. После Atomic Escape блокировка раунда снимается. При ничьей или отсутствии голосов начинается обычный раунд.

Непосредственно перед запуском первой автоматической мини-игры AutoEvent отправляет в серверную консоль `scpstats disable`. В следующем `WaitingForPlayers` он один раз отправляет `scpstats enable`. При выгрузке плагина до следующего ожидания AutoEvent также пытается выполнить `scpstats enable`. Для этого на сервере должна быть доступна команда SCPStats; её работу нужно проверить на сервере.

Управление через Remote Admin: `ev auto status`, `ev auto reset`, `ev auto on`, `ev auto off`. Выдайте право `ev.auto` администраторам (или используйте `ev.*`). `reset` обнуляет счётчик начатых раундов. `off` отменяет текущее голосование и продолжение автоматической цепочки, но не прерывает уже запущенную мини-игру. Команды действуют до следующей перезагрузки плагина; значение `auto_voting.enabled` определяет состояние после загрузки.

Параметры `auto_voting`: `enabled`, `round_interval`, `vote_duration_seconds`, `broadcast_interval_seconds`, `broadcast_duration_seconds`, `vote_broadcast_text` (подстановки `{time}`, `{yes}`, `{no}`), `vote_passed_text`, `vote_failed_text`, `settings_group_name`, `yes_label`, `yes_button_text`, `yes_hint`, `no_label`, `no_button_text`, `no_hint`, `excluded_events`. Старые параметры `suggested_yes_key` и `suggested_no_key` больше не используются: их можно удалить из существующего конфига. Старые стандартные подписи `yes_label: Да` и `no_label: Нет` автоматически заменяются новыми; собственные подписи сохраняются. Значения по умолчанию: 4 раунда, 45 секунд голосования, обновление сообщения раз в 1 секунду, длительность сообщения 1,5 секунды. Игровой API принимает только целые секунды длительности броадкаста, поэтому плагин передаёт 2 секунды и завершает своё сообщение через 1,5 секунды, если его не заменило следующее.

## Build from source

For a Git checkout, use .NET SDK and an SCP:SL dedicated server with LabAPI 1.1.7. Download the pinned ProjectMER and AudioPlayerApi build references, then build:

```powershell
.\build-exiled.ps1 -ManagedPath 'C:\path\to\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed' -DownloadDependencies
```

The script checks LabAPI 1.1.7, restores NuGet packages and builds `AutoEvent/bin/Release/net48/AutoEvent.dll`. The same scripts run in GitHub Actions after SteamCMD installs the server. If the public server build has an incompatible LabAPI version, the build stops with an explicit error. See `DEPENDENCIES.md` for exact versions and checksums. Run the voting rules checks with `dotnet run --project Tests/AutoVotingRules.Tests.csproj -c Release`.
