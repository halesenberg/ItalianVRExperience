using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Bellavalle.Data;
using Bellavalle.Voice;
using Bellavalle.Core;
using Whisper;

namespace Bellavalle.AI
{
    public class AIGrammarHelperService : MonoBehaviour
    {
        [Header("Ollama")]
        [SerializeField] string ollamaUrl = "http://localhost:11434/api/generate";
        [SerializeField] string model = "mistral";
        [Tooltip("Secondi di attesa massimi per una risposta di Ollama prima di considerarla fallita. " +
                 "Senza questo, un server bloccato/lento lascerebbe IsWaitingResponse bloccato per sempre.")]
        [SerializeField] int requestTimeoutSeconds = 15;

        [Header("Dati")]
        [SerializeField] VerbConjugationDatabase verbDatabase;
        [SerializeField] VocabularyDatabase vocabularyDatabase;

        [Header("Whisper (per il toggle inglese/italiano)")]
        [SerializeField] WhisperManager whisperManager;

        [Header("Input")]
        [Tooltip("Assegnalo nell'Inspector — l'abbonamento all'evento avviene solo " +
                 "quando il pannello AI è aperto (EnterEnglishMode), non sempre.")]
        [SerializeField] PushToTalkRecorder pushToTalkRecorder;

        [Header("Debug")]
        [SerializeField] bool logToConsole = true;

        /// <summary>Risposta valida pronta da mostrare (coniugazione vera, da database).</summary>
        public event Action<string> OnAnswerReady;

        /// <summary>Domanda capita ma fuori dallo scope attuale.</summary>
        public event Action<string> OnHelpNeeded;

        /// <summary>Problema tecnico (Ollama non raggiungibile, risposta vuota).</summary>
        public event Action<string> OnError;

        bool _isWaitingResponse;
        bool _isActive; // true solo quando il pannello AI è aperto
        string _languageBeforeEnglishMode;

        public bool IsWaitingResponse => _isWaitingResponse;

        void Awake()
        {
            string configPath = System.IO.Path.Combine(Application.persistentDataPath, "ollama_config.txt");
            if (System.IO.File.Exists(configPath))
            {
                string overrideUrl = System.IO.File.ReadAllText(configPath).Trim();
                if (!string.IsNullOrEmpty(overrideUrl))
                {
                    ollamaUrl = overrideUrl;
                    if (logToConsole) Debug.Log($"[AIGrammarHelperService] ollamaUrl da file di config: {ollamaUrl}");
                }
            }
            else if (logToConsole)
            {
                Debug.Log($"[AIGrammarHelperService] Nessun ollama_config.txt trovato, uso il default: {ollamaUrl}");
            }
        }

        // ── OnEnable/OnDisable NON si abbonano più all'evento ───────────
        // L'abbonamento avviene solo dentro EnterEnglishMode/ExitEnglishMode,
        // così AIGrammarHelperService riceve trascrizioni SOLO quando il pannello AI è aperto.

        // ── Toggle lingua Whisper + abbonamento evento ───────────────────
        public void EnterEnglishMode()
        {
            // Idempotente: se il pannello viene riaperto mentre è già attivo (es. il
            // player ripreme il bottone tab "AI" già selezionato), non rifare il
            // salvataggio/toggle — altrimenti _languageBeforeEnglishMode verrebbe
            // sovrascritto con "en" e ExitEnglishMode non tornerebbe più all'italiano.
            if (_isActive) return;
            if (whisperManager == null) return;

            _languageBeforeEnglishMode = whisperManager.language;
            whisperManager.language = "en";
            _isActive = true;

            if (pushToTalkRecorder != null)
                pushToTalkRecorder.OnTranscriptionReady += AskAboutVerb;

            if (logToConsole) Debug.Log("[AIGrammarHelperService] Pannello AI aperto — Whisper in inglese, in ascolto.");
        }

        public void ExitEnglishMode()
        {
            // Idempotente, simmetrico a EnterEnglishMode: evita un secondo -= senza
            // il corrispondente += e non tocca whisperManager.language se non eravamo
            // in modalità inglese.
            if (!_isActive) return;
            if (whisperManager == null) return;

            whisperManager.language = string.IsNullOrEmpty(_languageBeforeEnglishMode)
                ? "it"
                : _languageBeforeEnglishMode;
            _isActive = false;

            if (pushToTalkRecorder != null)
                pushToTalkRecorder.OnTranscriptionReady -= AskAboutVerb;

            if (logToConsole) Debug.Log("[AIGrammarHelperService] Pannello AI chiuso — Whisper tornato a: " + whisperManager.language);
        }

        void OnDisable()
        {
            // Sicurezza: se il GameObject viene disattivato mentre il pannello è aperto
            // (es. player chiude il pannello AI, o cambio scena) mentre una richiesta a
            // Ollama è in volo, la coroutine SendExtractionRequest viene interrotta a metà
            // da Unity senza mai raggiungere la riga che resetta _isWaitingResponse — lo
            // resettiamo esplicitamente qui per non restare bloccati "in attesa" per sempre.
            _isWaitingResponse = false;

            if (pushToTalkRecorder != null)
                pushToTalkRecorder.OnTranscriptionReady -= AskAboutVerb;
        }

        // ── API pubblica ─────────────────────────────────────────────────
        public void AskAboutVerb(string playerQuestionEN)
        {
            if (!_isActive)
            {
                if (logToConsole) Debug.Log("[AIGrammarHelperService] Pannello chiuso — trascrizione ignorata.");
                return;
            }

            if (string.IsNullOrWhiteSpace(playerQuestionEN))
            {
                if (logToConsole) Debug.Log("[AIGrammarHelperService] Domanda vuota, ignorata.");
                return;
            }

            if (verbDatabase == null)
            {
                Debug.LogError("[AIGrammarHelperService] VerbConjugationDatabase non assegnato!");
                OnError?.Invoke("Database verbi non configurato.");
                return;
            }

            if (_isWaitingResponse)
            {
                if (logToConsole) Debug.Log("[AIGrammarHelperService] Richiesta già in corso, ignorata.");
                return;
            }

            StartCoroutine(SendExtractionRequest(playerQuestionEN));
        }

        string BuildExtractionPrompt(string playerQuestionEN)
        {
            string mappingBlock = verbDatabase.BuildEnglishToItalianMappingBlock();

            return
                "You are a strict information-extraction tool inside an Italian-learning VR game.\n" +
                "The player is an English speaker with NO knowledge of Italian yet. They will ask,\n" +
                "entirely in English, about verbs or words they encountered in the game.\n\n" +
                "If asking about a verb:\n" +
                "  - They name it in ENGLISH (e.g. \"have\", \"to have\", \"how do I conjugate 'to go'?\")\n" +
                "  - Extract the Italian infinitive from the list below and the tense if specified\n\n" +
                "If asking about a word:\n" +
                "  - They ask \"What does X mean?\" or \"What is X in English?\"\n" +
                "  - Extract the Italian word and mark it as a generic word (not a verb)\n\n" +
                "Here are the verbs you recognize:\n" +
                $"{mappingBlock}\n" +
                "Your ONLY job is to extract the word/verb and tense (if any), and answer with\n" +
                "EXACTLY one line of JSON, nothing else — no explanation, no markdown:\n" +
                "{\"verbo\": \"<Italian verb infinitive or null>\", \"parola\": \"<Italian word or null>\", \"tempo\": \"<tense or null>\"}\n\n" +
                "Rules:\n" +
                "1. If it's a verb question, fill \"verbo\" with the ITALIAN infinitive from the list.\n" +
                "   Match by meaning even if the player says \"have\", \"to have\", \"how to conjugate to go\", etc.\n" +
                "2. If it's NOT a verb question (e.g. \"What does 'caffè' mean?\"), fill \"parola\" with\n" +
                "   the Italian word the player asked about, in lowercase.\n" +
                "3. \"tempo\" is ONLY for verbs: \"presente\" by default, or other tense if clearly specified.\n" +
                "4. NEVER output conjugated forms or translations yourself. NEVER add commentary.\n" +
                "5. Output ONLY the JSON object, on a single line. Set unused fields to null.\n\n" +
                $"Question: \"{playerQuestionEN}\"";
        }

        IEnumerator SendExtractionRequest(string playerQuestionEN)
        {
            _isWaitingResponse = true;

            string prompt = BuildExtractionPrompt(playerQuestionEN);
            string jsonBody = BuildJsonRequest(model, prompt);

            if (logToConsole) Debug.Log($"[AIGrammarHelperService] Invio a Ollama: \"{playerQuestionEN}\"");

            using (var request = new UnityWebRequest(ollamaUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = requestTimeoutSeconds;

                yield return request.SendWebRequest();

                _isWaitingResponse = false;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string err = $"[AIGrammarHelperService] Errore Ollama: {request.error}. " +
                                 "Verifica che Ollama sia in esecuzione (ollama run mistral).";
                    Debug.LogError(err);
                    OnError?.Invoke(err);
                    yield break;
                }

                string rawGenerated = ExtractOllamaResponseField(request.downloadHandler.text);
                if (string.IsNullOrWhiteSpace(rawGenerated))
                {
                    Debug.LogWarning("[AIGrammarHelperService] Risposta vuota da Ollama.");
                    OnError?.Invoke("Risposta vuota da Ollama.");
                    yield break;
                }

                if (logToConsole) Debug.Log($"[AIGrammarHelperService] Estrazione grezza: \"{rawGenerated}\"");

                ResolveAnswer(rawGenerated);
            }
        }

        void ResolveAnswer(string rawGenerated)
        {
            string verbo = ExtractJsonField(rawGenerated, "verbo");
            string tempo = ExtractJsonField(rawGenerated, "tempo");
            string parola = ExtractJsonField(rawGenerated, "parola");

            // Primo: prova a riconoscere come verbo
            if (!string.IsNullOrEmpty(verbo))
            {
                var verbEntry = verbDatabase.Get(verbo);
                if (verbEntry != null)
                {
                    if (string.IsNullOrEmpty(tempo) || !tempo.Equals("presente", StringComparison.OrdinalIgnoreCase))
                    {
                        OnHelpNeeded?.Invoke(
                            $"For now I can only explain \"{verbEntry.infinitiveEN}\" in the present tense.");
                        return;
                    }

                    // Aggiungi contesto se la parola è stata vista nei dialoghi
                    string baseResponse = verbEntry.BuildPresenteText();
                    if (GameManager.Instance != null &&
                        GameManager.Instance.State.learnedWords.Contains(verbo.ToLowerInvariant()))
                    {
                        baseResponse += "\n\n(You already saw this word in the game!)";
                    }
                    OnAnswerReady?.Invoke(baseResponse);
                    return;
                }
            }

            // Secondo: prova a riconoscere come parola/frase generica
            if (!string.IsNullOrEmpty(parola))
            {
                // Prova prima la frase intera così com'è — gestisce voci idiomatiche
                // già nel vocabolario come "va bene", "per favore", "nessun problema".
                string exactAnswer = TryResolveSingleTerm(parola);
                if (exactAnswer != null)
                {
                    OnAnswerReady?.Invoke(exactAnswer);
                    return;
                }

                // Se la frase ha più parole (es. "lei spende"), spiega ciascuna
                // singolarmente invece di arrenderti sulla frase intera.
                var words = parola.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    var explanations = new List<string>();
                    foreach (var w in words)
                    {
                        string wordAnswer = TryResolveSingleTerm(w);
                        if (wordAnswer != null)
                            explanations.Add(wordAnswer);
                    }

                    if (explanations.Count > 0)
                    {
                        OnAnswerReady?.Invoke(string.Join("\n\n---\n\n", explanations));
                        return;
                    }
                }
            }

            // Nessuno dei due: risposta di aiuto
            OnHelpNeeded?.Invoke(
                "I didn't understand your question. Try asking about a specific word or verb you saw in the game, " +
                "e.g. \"What does 'caffè' mean?\" or \"How do you conjugate 'to go'?\"");
        }

        /// <summary>
        /// Prova a spiegare UN singolo termine italiano: prima come voce del
        /// vocabolario, poi come forma verbale (infinito o coniugata). Ritorna
        /// null se non trovato da nessuna parte — nessun testo generato da
        /// Ollama entra mai in questo metodo, solo dati verificati a mano.
        /// </summary>
        string TryResolveSingleTerm(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return null;

            var vocabEntry = vocabularyDatabase?.Get(term);
            if (vocabEntry != null)
            {
                string response = vocabEntry.GetDefinition();
                if (GameManager.Instance != null &&
                    GameManager.Instance.State.learnedWords.Contains(term.ToLowerInvariant()))
                {
                    response += "\n\n(You already saw this word in the game!)";
                }
                return response;
            }

            var conjugatedEntry = verbDatabase.GetByAnyForm(term);
            if (conjugatedEntry != null)
            {
                string response = conjugatedEntry.BuildPresenteText();
                if (GameManager.Instance != null &&
                    GameManager.Instance.State.learnedWords.Contains(term.ToLowerInvariant()))
                {
                    response += "\n\n(You already saw this word in the game!)";
                }
                return response;
            }

            return null;
        }

        static string ExtractJsonField(string rawText, string fieldName)
        {
            if (string.IsNullOrEmpty(rawText)) return null;

            // Cattura l'intero valore tra virgolette (può contenere spazi, es.
            // "lei spende"), oppure il literal null non quotato.
            var pattern = $"\"{fieldName}\"\\s*:\\s*(\"([^\"]*)\"|null)";
            var match = Regex.Match(rawText, pattern, RegexOptions.IgnoreCase);
            if (!match.Success) return null;

            string value = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
            return string.IsNullOrEmpty(value) ? null : value.ToLowerInvariant();
        }

        string BuildJsonRequest(string modelName, string prompt)
        {
            string escapedPrompt = EscapeJson(prompt);
            string escapedModel = EscapeJson(modelName);
            return "{\"model\":\"" + escapedModel + "\"," +
                   "\"prompt\":\"" + escapedPrompt + "\"," +
                   "\"stream\":false}";
        }

        string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\")
                     .Replace("\"", "\\\"")
                     .Replace("\n", "\\n")
                     .Replace("\r", "");
        }

        string ExtractOllamaResponseField(string json)
        {
            const string key = "\"response\":\"";
            int start = json.IndexOf(key);
            if (start < 0) return null;
            start += key.Length;

            var sb = new StringBuilder();
            for (int i = start; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    char next = json[i + 1];
                    if (next == 'n') { sb.Append(' '); i++; continue; }
                    if (next == '"') { sb.Append('"'); i++; continue; }
                    if (next == '\\') { sb.Append('\\'); i++; continue; }
                    continue;
                }
                if (c == '"') break;
                sb.Append(c);
            }
            return sb.ToString().Trim();
        }
    }
}