using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Bellavalle.Core;

namespace Bellavalle.AI
{
    /// <summary>
    /// Collega il testo trascritto da Whisper a Ollama (Mistral) per generare
    /// risposte libere di un NPC, mantenendo un italiano semplice A1/A2.
    ///
    /// Setup:
    ///  1. Assicurati che Ollama sia in esecuzione sul PC (ollama run mistral
    ///     gia' scaricato in precedenza)
    ///  2. Metti questo script su un GameObject in scena (es. un GameObject
    ///     "AIDialogueService" vicino al WhisperManager)
    ///  3. Collega PushToTalkRecorder.OnTranscriptionReady -> AskNpc(string)
    ///  4. Ascolta OnNpcResponseReady per mostrare la risposta nel DialogueCanvas
    ///
    /// Uso tipico:
    ///   aiDialogueService.SetCharacter("Carla", "una vegetaia simpatica del mercato");
    ///   aiDialogueService.AskNpc("Cosa fai oggi?");
    /// </summary>
    public class AIDialogueService : MonoBehaviour
    {
        [Header("Ollama")]
        [SerializeField] string ollamaUrl = "http://localhost:11434/api/generate";
        [SerializeField] string model = "mistral";

        [Header("Personaggio corrente")]
        [SerializeField] string characterName = "Carla";
        [SerializeField] string characterDescription = "una vegetaia gentile del mercato di Bellavalle";

        [Header("Debug")]
        [SerializeField] bool logToConsole = true;

        /// <summary>
        /// Invocato quando la risposta dell'NPC e' pronta.
        /// DialogueManager/DialogueUI si aggancia qui per mostrarla nel canvas.
        /// </summary>
        public event System.Action<string> OnNpcResponseReady;

        /// <summary>
        /// Invocato se la richiesta a Ollama fallisce (es. Ollama non in esecuzione).
        /// </summary>
        public event System.Action<string> OnError;

        bool _isWaitingResponse;

        public bool IsWaitingResponse => _isWaitingResponse;

        // ── Configurazione personaggio ──────────────────────────────────
        public void SetCharacter(string name, string description)
        {
            characterName = name;
            characterDescription = description;
        }

        // ── API pubblica — collega qui PushToTalkRecorder.OnTranscriptionReady ──
        public void AskNpc(string playerText)
        {
            if (string.IsNullOrWhiteSpace(playerText))
            {
                if (logToConsole) Debug.Log("[AIDialogueService] Testo vuoto, ignorato.");
                return;
            }

            if (_isWaitingResponse)
            {
                if (logToConsole) Debug.Log("[AIDialogueService] Richiesta gia' in corso, ignorata.");
                return;
            }

            StartCoroutine(SendToOllama(playerText));
        }

        // ── Costruzione del prompt rigido A1/A2 ─────────────────────────
        string BuildSystemPrompt()
        {
            return
                $"Sei {characterName}, {characterDescription}, in un gioco VR per imparare l'italiano. L'utente medio è anglofono di livello principiante assoluto. \n" +
                "Regole STRETTE da seguire sempre:\n" +
                "1. Usa SOLO frasi semplici, massimo 8-10 parole per frase.\n" +
                "2. Usa SOLO vocabolario di livello A1-A2 (principianti assoluti).\n" +
                "3. Usa SOLO il presente indicativo, mai tempi verbali complessi.\n" +
                "4. Non usare MAI espressioni idiomatiche, slang o modi di dire.\n" +
                "5. Controlla la grammatica con attenzione: zero errori.\n" +
                "6. Rispondi SEMPRE in massimo 1-2 frasi, non di piu'.\n" +
                "7. Resta nel personaggio, sii gentile e naturale.\n" +
                "8. Rispondi SOLO in italiano, niente traduzioni o parentesi in inglese." +
                "9. Basati su \n";
        }

        // ── Chiamata HTTP a Ollama ───────────────────────────────────────
        IEnumerator SendToOllama(string playerText)
        {
            _isWaitingResponse = true;

            string fullPrompt = BuildSystemPrompt() +
                                 $"\nIl player ha detto: \"{playerText}\"\n" +
                                 $"Rispondi come {characterName}, seguendo tutte le regole sopra.";

            string jsonBody = BuildJsonRequest(model, fullPrompt);

            if (logToConsole) Debug.Log($"[AIDialogueService] Invio a Ollama: \"{playerText}\"");

            using (var request = new UnityWebRequest(ollamaUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                _isWaitingResponse = false;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string err = $"[AIDialogueService] Errore Ollama: {request.error}. " +
                                 "Verifica che Ollama sia in esecuzione (ollama run mistral).";
                    Debug.LogError(err);
                    OnError?.Invoke(err);
                    yield break;
                }

                string responseText = ExtractResponseText(request.downloadHandler.text);
                if (string.IsNullOrWhiteSpace(responseText))
                {
                    Debug.LogWarning("[AIDialogueService] Risposta vuota da Ollama.");
                    OnError?.Invoke("Risposta vuota da Ollama.");
                    yield break;
                }

                if (logToConsole) Debug.Log($"[AIDialogueService] Risposta {characterName}: \"{responseText}\"");
                OnNpcResponseReady?.Invoke(responseText);
            }
        }

        // ── Costruzione JSON richiesta (manuale, niente dipendenze extra) ──
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

        // ── Estrazione del campo "response" dalla risposta JSON di Ollama ──
        string ExtractResponseText(string json)
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