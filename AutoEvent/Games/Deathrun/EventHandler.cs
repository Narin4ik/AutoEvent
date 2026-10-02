using Exiled.Events.EventArgs.Player;
using GameCore;
using UnityEngine;

namespace AutoEvent.Games.Deathrun;
public class EventHandler
{
    public void OnSearchingPickup(SearchingPickupEventArgs ev)
    {
        ev.IsAllowed = false;

        DebugLogger.LogDebug("[Deathrun] Нажата кнопка.");
        
        // Start the animation when click on the button
        Animator animator = ev.Pickup.GameObject.GetComponentInParent<Animator>();
        if (animator != null)
        {
            DebugLogger.LogDebug($"[Deathrun] Запущена анимация {animator.name}.");
            animator.Play(animator.name + "action");
        }
    }
}
