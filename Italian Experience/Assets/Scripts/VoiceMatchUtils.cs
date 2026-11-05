using System.Text;

namespace Bellavalle.Voice
{
    /// <summary>
    /// Normalizzazione e matching del testo trascritto da Whisper contro le
    /// parole chiave (PlayerOption.voiceTriggers) definite nei nodi di dialogo
    /// con useVoiceInput = true.
    ///
    /// Tenuto volutamente semplice: minuscolo, accenti tolti (così "sì"
    /// combacia con "si", "perché" con "perche"...), punteggiatura rimossa,
    /// poi un "contains" tra il testo normalizzato e ogni trigger normalizzato.
    /// </summary>
    public static class VoiceMatchUtils
    {
        public static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            string lower = s.ToLowerInvariant();
            lower = lower
                .Replace('à', 'a').Replace('á', 'a')
                .Replace('è', 'e').Replace('é', 'e')
                .Replace('ì', 'i').Replace('í', 'i')
                .Replace('ò', 'o').Replace('ó', 'o')
                .Replace('ù', 'u').Replace('ú', 'u');

            var sb = new StringBuilder(lower.Length);
            foreach (char c in lower)
            {
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                    sb.Append(c);
                else
                    sb.Append(' '); // la punteggiatura diventa spazio, non sparisce (evita di fondere due parole)
            }

            // Comprimi spazi multipli
            var collapsed = new StringBuilder(sb.Length);
            bool lastWasSpace = false;
            foreach (char c in sb.ToString().Trim())
            {
                bool isSpace = char.IsWhiteSpace(c);
                if (isSpace && lastWasSpace) continue;
                collapsed.Append(c);
                lastWasSpace = isSpace;
            }

            return collapsed.ToString();
        }

        /// <summary>
        /// True se 'trigger' compare come sotto-sequenza di parole dentro 'transcribed'
        /// (entrambi già normalizzati, o li normalizza se serve).
        /// </summary>
        public static bool Matches(string transcribedRaw, string triggerRaw)
        {
            if (string.IsNullOrWhiteSpace(transcribedRaw) || string.IsNullOrWhiteSpace(triggerRaw))
                return false;

            string transcribed = Normalize(transcribedRaw);
            string trigger = Normalize(triggerRaw);

            if (transcribed.Length == 0 || trigger.Length == 0) return false;

            // Contains su stringhe con spazi ai bordi evita match parziali dentro parole
            // diverse (es. "si" non deve accendersi dentro "cosi" per errore).
            return (" " + transcribed + " ").Contains(" " + trigger + " ")
                   || transcribed == trigger;
        }

        /// <summary>
        /// Cerca tra le opzioni del nodo quella il cui voiceTriggers combacia per prima
        /// con il testo trascritto. Ritorna -1 se nessuna combacia.
        /// </summary>
        public static int FindMatchingOption(string transcribed, Bellavalle.Data.PlayerOption[] options)
        {
            if (options == null) return -1;

            for (int i = 0; i < options.Length; i++)
            {
                var triggers = options[i].voiceTriggers;
                if (triggers == null) continue;

                foreach (var trig in triggers)
                    if (Matches(transcribed, trig))
                        return i;
            }
            return -1;
        }
    }
}
