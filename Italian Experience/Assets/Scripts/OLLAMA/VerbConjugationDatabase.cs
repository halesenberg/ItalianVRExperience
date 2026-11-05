using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Bellavalle.Data
{
    /// <summary>
    /// ScriptableObject con le coniugazioni verificate a mano dei verbi
    /// coperti dalla "guida grammaticale" (AIGrammarHelperService).
    ///
    /// IMPORTANTE — perché esiste questo file:
    /// Ollama NON genera mai le forme verbali (rischio di allucinazione
    /// anche su verbi semplici). Ollama serve solo a capire QUALE verbo
    /// e QUALE tempo il player sta chiedendo — E a fare il mapping
    /// inglese→italiano, perché il player è un principiante assoluto e
    /// non conosce la parola italiana: chiederà di "to have", non di
    /// "avere". La risposta vera arriva sempre da qui, scritta e
    /// controllata da te, sempre in coppia inglese/italiano per restare
    /// leggibile a chi l'italiano ancora non lo sa.
    ///
    /// v1: solo presente indicativo, solo essere/avere.
    /// Per aggiungere un verbo: crea una nuova voce in "entries" con
    /// infinito italiano, infinito inglese, e le 6 forme del presente.
    /// Per aggiungere un tempo in futuro (es. passato prossimo): duplica
    /// il blocco di 6 campi con un prefisso diverso e aggiungi la logica
    /// in BuildPresenteText/ResolveAnswer — non serve toccare il resto.
    ///
    /// Setup:
    ///  1. Project → Create → Bellavalle → Verb Conjugation Database
    ///  2. Compila "entries" nell'Inspector con i verbi verificati,
    ///     RICORDATI di compilare anche infinitiveEN (es. "to have")
    ///     — è obbligatorio, non solo un'etichetta decorativa: serve a
    ///     Ollama per capire la domanda e al player per orientarsi
    ///     (per ampliare il set in futuro, usa le liste lessicali per
    ///     livello del Profilo della lingua italiana come riferimento:
    ///     https://www.unistrapg.it/profilo_lingua_italiana/site/liste_lessicali_a1.html
    ///     e liste_lessicali_a2.html)
    ///  3. Assegna questo asset ad AIGrammarHelperService nell'Inspector
    /// </summary>
    [CreateAssetMenu(menuName = "Bellavalle/Verb Conjugation Database")]
    public class VerbConjugationDatabase : ScriptableObject
    {
        [Tooltip("Un elemento per verbo. Infinito italiano sempre minuscolo. infinitiveEN è obbligatorio.")]
        public VerbEntry[] entries;

        Dictionary<string, VerbEntry> _byInfinitive;
        Dictionary<string, VerbEntry> _byAnyForm;

        /// <summary>Cerca un verbo per infinito ITALIANO (case-insensitive).
        /// Usalo con il valore "verbo" che torna da Ollama.</summary>
        public VerbEntry Get(string infinitiveIT)
        {
            if (string.IsNullOrWhiteSpace(infinitiveIT)) return null;
            _byInfinitive ??= BuildIndex();
            return _byInfinitive.TryGetValue(Normalize(infinitiveIT), out var e) ? e : null;
        }

        public bool Contains(string infinitiveIT) => Get(infinitiveIT) != null;

        /// <summary>
        /// Cerca un verbo per QUALSIASI forma coniugata al presente (es. "spendi",
        /// "vado", "capisce"), non solo l'infinito. Serve quando il player chiede
        /// "what does X mean?" nominando la forma che ha effettivamente visto nei
        /// dialoghi invece dell'infinito inglese (che Ollama userebbe per "verbo").
        /// </summary>
        public VerbEntry GetByAnyForm(string form)
        {
            if (string.IsNullOrWhiteSpace(form)) return null;
            _byAnyForm ??= BuildAnyFormIndex();
            return _byAnyForm.TryGetValue(Normalize(form), out var e) ? e : null;
        }

        /// <summary>
        /// Blocco di testo "english" -> "italiano" da incollare nel prompt di
        /// estrazione: dice a Ollama quali verbi inglesi sa riconoscere e a
        /// quale infinito italiano corrispondono. Questo È il mapping che
        /// permette a un player che non sa l'italiano di farsi capire.
        /// </summary>
        public string BuildEnglishToItalianMappingBlock()
        {
            var sb = new StringBuilder();
            foreach (var e in entries)
            {
                if (string.IsNullOrWhiteSpace(e.infinitive) || string.IsNullOrWhiteSpace(e.infinitiveEN))
                    continue;
                sb.AppendLine($"\"{e.infinitiveEN.Trim().ToLowerInvariant()}\" -> \"{e.infinitive.Trim().ToLowerInvariant()}\"");
            }
            return sb.ToString();
        }

        /// <summary>Infiniti ITALIANI supportati — usali per validazione interna,
        /// MAI in un messaggio mostrato al player (non conosce l'italiano ancora).</summary>
        public string[] SupportedInfinitives()
        {
            var list = new List<string>();
            foreach (var e in entries)
                if (!string.IsNullOrWhiteSpace(e.infinitive))
                    list.Add(e.infinitive.Trim().ToLowerInvariant());
            return list.ToArray();
        }

        /// <summary>Glosse INGLESI supportate — questo è ciò che mostri al player
        /// in un messaggio tipo "per ora posso aiutarti solo con questi verbi".</summary>
        public string[] SupportedEnglishGlosses()
        {
            var list = new List<string>();
            foreach (var e in entries)
                if (!string.IsNullOrWhiteSpace(e.infinitiveEN))
                    list.Add(e.infinitiveEN.Trim());
            return list.ToArray();
        }

        static string Normalize(string s) => s.Trim().ToLowerInvariant();

        Dictionary<string, VerbEntry> BuildIndex()
        {
            var d = new Dictionary<string, VerbEntry>();
            foreach (var e in entries)
            {
                if (string.IsNullOrWhiteSpace(e.infinitive)) continue;
                d[Normalize(e.infinitive)] = e;
            }
            return d;
        }

        Dictionary<string, VerbEntry> BuildAnyFormIndex()
        {
            var d = new Dictionary<string, VerbEntry>();
            foreach (var e in entries)
            {
                void Add(string form)
                {
                    if (!string.IsNullOrWhiteSpace(form))
                        d[Normalize(form)] = e;
                }
                Add(e.infinitive);
                Add(e.io);
                Add(e.tu);
                Add(e.luiLei);
                Add(e.noi);
                Add(e.voi);
                Add(e.loro);
            }
            return d;
        }
    }

    [Serializable]
    public class VerbEntry
    {
        [Header("Identità")]
        [Tooltip("Es. \"essere\" — sempre minuscolo, univoco nel database")]
        public string infinitive;

        [Tooltip("OBBLIGATORIO. Es. \"to be\". Usato sia per far capire la domanda a Ollama, " +
                 "sia per mostrare la risposta al player, che non conosce ancora l'italiano.")]
        public string infinitiveEN;

        [Header("Presente indicativo (verificato a mano)")]
        public string io;
        public string tu;
        public string luiLei;
        public string noi;
        public string voi;
        public string loro;

        /// <summary>
        /// Costruisce la frase finale mostrata al player: sempre bilingue,
        /// perché è un principiante assoluto e da solo l'infinito italiano
        /// non gli dice ancora nulla. Interamente deterministico: nessuna
        /// parte di questo testo arriva da Ollama.
        /// </summary>
        public string BuildPresenteText()
        {
            return $"\"{infinitiveEN}\" = \"{infinitive}\" (presente):\n" +
                   $"io {io}\n" +
                   $"tu {tu}\n" +
                   $"lui/lei/Lei {luiLei}\n" +
                   $"noi {noi}\n" +
                   $"voi {voi}\n" +
                   $"loro {loro}\n\n" +
                   $"Note: \"{luiLei}\" is also the polite/formal \"you\" (Lei) — " +
                   $"NPCs like the barista use this form to address you politely.";
        }
    }
}
