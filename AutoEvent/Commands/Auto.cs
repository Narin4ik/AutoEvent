using System;
using CommandSystem;
using Exiled.Permissions.Extensions;

namespace AutoEvent.Commands;

internal sealed class Auto : ICommand, IUsageProvider
{
    public string Command => "auto";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Управление автоматическими ивентами";
    public string[] Usage => new[] { "status|reset|on|off" };

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission("ev.auto"))
        {
            response = "Недостаточно прав: требуется ev.auto.";
            return false;
        }

        var system = AutoEvent.Singleton?.VotingSystem;
        if (system == null)
        {
            response = "Система авто-ивентов не загружена.";
            return false;
        }

        string action = arguments.Count > 0 ? arguments.At(0).ToLowerInvariant() : "status";
        switch (action)
        {
            case "status": response = system.Status; return true;
            case "reset": system.Reset(); response = "Счётчик авто-ивентов сброшен. " + system.Status; return true;
            case "on": system.SetEnabled(true); response = system.Status; return true;
            case "off": system.SetEnabled(false); response = system.Status; return true;
            default: response = "Использование: ev auto status|reset|on|off"; return false;
        }
    }
}
