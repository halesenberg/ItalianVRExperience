using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bellavalle.Core
{
    /// <summary>
    /// Bus eventi statico e tipizzato. I Manager si parlano tramite eventi,
    /// non con riferimenti diretti — facile da estendere senza accoppiamento.
    ///
    /// Uso:
    ///   EventBus.On(GameEvent.WordLearned, OnWordLearned);
    ///   EventBus.Emit(GameEvent.WordLearned, "caffè");
    ///   EventBus.Off(GameEvent.WordLearned, OnWordLearned);
    /// </summary>
    public enum GameEvent
    {
        WordLearned,
        LanguageProgressChanged,
        NpcMoodChanged,
        DialogueStarted,
        DialogueEnded,
        OptionSelected,
        MissionCompleted,
        ChapterChanged,
        PlayerAnswered,        // (bool correct)
        FreezeDetected,        // player non ha risposto in tempo
    }

    public static class EventBus
    {
        static readonly Dictionary<GameEvent, List<Action<object>>> _listeners = new();

        public static void On(GameEvent evt, Action<object> callback)
        {
            if (!_listeners.ContainsKey(evt)) _listeners[evt] = new();
            _listeners[evt].Add(callback);
        }

        public static void Off(GameEvent evt, Action<object> callback)
        {
            if (_listeners.TryGetValue(evt, out var list)) list.Remove(callback);
        }

        public static void Emit(GameEvent evt, object data = null)
        {
            if (!_listeners.TryGetValue(evt, out var list)) return;
            // Copia la lista: i listener potrebbero deregistrarsi durante l'iterazione
            foreach (var cb in list.ToArray())
            {
                try { cb?.Invoke(data); }
                catch (Exception e) { Debug.LogError($"[EventBus] {evt}: {e}"); }
            }
        }
    }
}
