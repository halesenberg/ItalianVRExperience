using System;
using UnityEngine;

namespace Bellavalle.Core
{
    public enum PhraseCategory
    {
        Domanda,
        Risposta,
        Vocabolario
    }

    [Serializable]
    public class LearnedPhrase
    {
        public string textIT;
        public string textEN;
        public PhraseCategory category;
        // Riferimento diretto alla clip (stessa clip già assegnata al nodo di
        // dialogo/vocabolario per la riproduzione in scena) — niente naming
        // manuale né cartella Resources da tenere sincronizzata. Il salvataggio
        // su disco è disattivato (vedi SaveSystem), quindi il riferimento diretto
        // non crea problemi di serializzazione.
        public AudioClip audioClip;
        public string sourceNpcId;

        public LearnedPhrase() { }

        public LearnedPhrase(string textIT, string textEN, PhraseCategory category,
                              AudioClip audioClip = null, string sourceNpcId = null)
        {
            this.textIT = textIT;
            this.textEN = textEN;
            this.category = category;
            this.audioClip = audioClip;
            this.sourceNpcId = sourceNpcId;
        }
    }
}