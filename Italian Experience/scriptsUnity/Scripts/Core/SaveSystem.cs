using UnityEngine;

namespace Bellavalle.Core
{
    /// <summary>
    /// Salvataggio DISATTIVATO: nessuna persistenza su disco.
    /// Ogni avvio parte da uno stato pulito (niente scene "già completate").
    /// I metodi restano per compatibilità con GameManager.
    /// </summary>
    public static class SaveSystem
    {
        public static void Save(GameState state)
        {
            // no-op: non salviamo nulla
        }

        public static GameState Load()
        {
            // sempre stato nuovo
            return new GameState();
        }

        public static void Delete()
        {
            // no-op
        }
    }
}