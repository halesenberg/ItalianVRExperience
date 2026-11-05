using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Bellavalle.Core;
using Bellavalle.Data;
using Whisper;

namespace Bellavalle.Voice
{
    public class VoiceRecognitionLogger : MonoBehaviour
    {
        [Header("Riferimenti")]
        [SerializeField] PushToTalkRecorder pushToTalkRecorder;

        [Header("Sessione di test")]
        [Tooltip("Da incrociare col questionario demografico compilato durante il test. Impostalo prima di ogni sessione.")]
        [SerializeField] string participantId = "";

        [Tooltip("Nome del file audio della sessione (es. P01_recording.m4a). Usato per sincronizzare la valutazione umana con il CSV.")]
        [SerializeField] string audioFileName = "";

        [Header("Debug")]
        [SerializeField] bool logToConsole = true;

        string _filePath;
        bool _fileReady = false;

        DialogueNode _currentNode;
        string _currentTreeId = "";

        // ── Contatore tentativi per nodo ───────────────────────────────
        // Chiave: nodeId. Azzerato a ogni nuovo dialogo.
        // Conta solo i tentativi su nodi con opzioni vocali (isMatch 1/0).
        readonly Dictionary<string, int> _nodeAttempts = new Dictionary<string, int>();

        // ── Marker audio vuoto prodotti da Whisper ─────────────────────
        // Usati per assegnare n/d_audio invece di valutare il match.
        static readonly HashSet<string> _emptyAudioMarkers = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "[blank_audio]", "[ silence ]", "[silence]",
            "[musica]", "[music]", "[rumore]", "[noise]", ""
        };

        // ── Singleton persistente ───────────────────────────────────────
        public static VoiceRecognitionLogger Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Unity lifecycle ────────────────────────────────────────────
        void OnEnable()
        {
            if (pushToTalkRecorder != null)
                pushToTalkRecorder.OnTranscriptionResultReady += OnTranscriptionResultReady;

            EventBus.On(GameEvent.NodeEntered, OnNodeEntered);
            EventBus.On(GameEvent.DialogueStarted, OnDialogueStarted);
            EventBus.On(GameEvent.DialogueEnded, OnDialogueEnded);
        }

        void OnDisable()
        {
            if (pushToTalkRecorder != null)
                pushToTalkRecorder.OnTranscriptionResultReady -= OnTranscriptionResultReady;

            EventBus.Off(GameEvent.NodeEntered, OnNodeEntered);
            EventBus.Off(GameEvent.DialogueStarted, OnDialogueStarted);
            EventBus.Off(GameEvent.DialogueEnded, OnDialogueEnded);
        }

        void Start()
        {
            if (!string.IsNullOrWhiteSpace(participantId))
            {
                InitialiseFile();
            }
            else
            {
                Debug.LogWarning("[VoiceRecognitionLogger] ParticipantId vuoto — " +
                                 "chiama SetParticipantId() prima di iniziare la sessione. " +
                                 "Nessun file verrà creato fino ad allora.");
            }

            if (string.IsNullOrWhiteSpace(audioFileName))
                Debug.LogWarning("[VoiceRecognitionLogger] AudioFileName vuoto — impostalo " +
                                 "con il nome del file di registrazione (es. P01_recording.m4a).");
        }

        public void SetParticipantId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("[VoiceRecognitionLogger] SetParticipantId chiamato con id vuoto — ignorato.");
                return;
            }

            participantId = id;
            InitialiseFile();
        }

        public void SetAudioFileName(string name) => audioFileName = name;

        // ── Inizializzazione file ──────────────────────────────────────
        void InitialiseFile()
        {
            string safeId = string.Concat(participantId.Split(Path.GetInvalidFileNameChars()));
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"VoiceLog_{safeId}_{timestamp}.csv";
            _filePath = Path.Combine(Application.persistentDataPath, fileName);

            string header = string.Join(",",
                "InteractionType", "AttemptNumber",
                "Timestamp", "ParticipantId", "AudioFile", "Scene", "TreeId", "NodeId",
                "NpcLineIT", "ExpectedPhrases", "TranscribedText",
                "MatchedOption", "IsMatch", "AvgConfidence", "TokenCount");

            File.WriteAllText(_filePath, header + Environment.NewLine, Encoding.UTF8);
            _fileReady = true;

            if (logToConsole)
                Debug.Log($"[VoiceRecognitionLogger] File sessione creato: {_filePath}");
        }

        // ── Tracciamento contesto dialogo ──────────────────────────────
        void OnDialogueStarted(object data)
        {
            _currentTreeId = data as string ?? "";
            _nodeAttempts.Clear();
        }

        void OnDialogueEnded(object data)
        {
            _currentTreeId = "";
            _currentNode = null;
        }

        void OnNodeEntered(object data) => _currentNode = data as DialogueNode;

        // ── Dialogo: riceve testo + confidenza da Whisper ───────────────
        void OnTranscriptionResultReady(WhisperResult result)
        {
            if (result == null) return;

            string text = (result.Result ?? "").Trim();
            float avgConfidence = ComputeAverageConfidence(result);
            int tokenCount = CountRealTokens(result);

            string expectedPhrases = "";
            string matchedOption = "";
            string isMatch;
            string attemptNumber = "";

            var node = _currentNode;

            // ── IsMatch granulare ──────────────────────────────────────
            // n/d_audio → Whisper non ha ricevuto segnale utile
            // n/d_nome  → nodo cattura nome: nessun match calcolabile
            // n/d_vuoto → nodo senza opzioni vocali (pannello puro)
            // 1 / 0     → match riuscito / fallito su opzioni vocali
            if (_emptyAudioMarkers.Contains(text))
            {
                isMatch = "n/d_audio";
            }
            else if (node == null)
            {
                isMatch = "n/d_vuoto";
            }
            else if (node.isNameCapture)
            {
                expectedPhrases = "[nome libero]";
                isMatch = "n/d_nome";
            }
            else if (node.options != null && node.options.Length > 0)
            {
                expectedPhrases = string.Join(" | ", node.options.Select(o => o.text_IT));
                int matchIndex = VoiceMatchUtils.FindMatchingOption(text, node.options);
                isMatch = matchIndex >= 0 ? "1" : "0";
                matchedOption = matchIndex >= 0 ? node.options[matchIndex].text_IT : "";

                // AttemptNumber: incrementa solo per nodi vocali valutabili
                string nodeKey = node.nodeId ?? "";
                if (!_nodeAttempts.ContainsKey(nodeKey))
                    _nodeAttempts[nodeKey] = 0;
                _nodeAttempts[nodeKey]++;
                attemptNumber = _nodeAttempts[nodeKey].ToString();
            }
            else
            {
                isMatch = "n/d_vuoto";
            }

            WriteRow(
                "dialogo",
                attemptNumber,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                participantId,
                audioFileName,
                SceneManager.GetActiveScene().name,
                _currentTreeId,
                node?.nodeId ?? "",
                node?.npcLine_IT ?? "",
                expectedPhrases,
                text,
                matchedOption,
                isMatch,
                avgConfidence >= 0f ? avgConfidence.ToString("F3") : "",
                tokenCount.ToString()
            );

            if (logToConsole)
            {
                string confStr = avgConfidence >= 0f ? avgConfidence.ToString("F2") : "n/d";
                Debug.Log($"[VoiceRecognitionLogger] \"{text}\" — match={isMatch} " +
                          $"tentativo={attemptNumber} confidenza media={confStr}");
            }
        }

        // ── Listen and repeat: chiamato da ListenAndRepeatActivator ─────
        public void LogListenAndRepeat(
            string targetWord,
            int attemptNumber,
            bool isMatch,
            WhisperResult result)
        {
            if (result == null) return;

            string text = (result.Result ?? "").Trim();
            float avgConfidence = ComputeAverageConfidence(result);
            int tokenCount = CountRealTokens(result);

            WriteRow(
                "listen_repeat",
                attemptNumber.ToString(),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                participantId,
                audioFileName,
                SceneManager.GetActiveScene().name,
                "",
                "",
                "",
                targetWord,
                text,
                isMatch ? targetWord : "",
                isMatch ? "1" : "0",
                avgConfidence >= 0f ? avgConfidence.ToString("F3") : "",
                tokenCount.ToString()
            );

            if (logToConsole)
            {
                string confStr = avgConfidence >= 0f ? avgConfidence.ToString("F2") : "n/d";
                Debug.Log($"[VoiceRecognitionLogger] L&R tentativo {attemptNumber}: \"{text}\" — " +
                          $"target=\"{targetWord}\" match={isMatch} confidenza={confStr}");
            }
        }

        // ── Export griglia valutazione percettiva ──────────────────────
        // Tasto destro sul componente nell'Inspector → "Esporta griglia valutazione".
        // Legge il VoiceLog_*.csv della sessione corrente e produce
        // GrigliaValutazione_*.csv nella stessa cartella, con le colonne
        // pre-compilate dai dati del logger e le colonne vuote (T, P, C,
        // Fenomeno, Note) pronte per la valutazione manuale.
        // Salta le righe n/d_nome (cattura nome) e n/d_vuoto (non valutabili).
        [ContextMenu("Esporta griglia valutazione")]
        public void ExportGriglia()
        {
            if (!_fileReady)
            {
                Debug.LogWarning("[VoiceRecognitionLogger] ExportGriglia: nessun file di sessione " +
                                 "inizializzato. Imposta ParticipantId prima.");
                return;
            }

            if (!File.Exists(_filePath))
            {
                Debug.LogWarning($"[VoiceRecognitionLogger] ExportGriglia: file non trovato: {_filePath}");
                return;
            }

            try
            {
                string dir = Path.GetDirectoryName(_filePath);
                string baseName = Path.GetFileNameWithoutExtension(_filePath);
                string grigliePath = Path.Combine(dir,
                    baseName.Replace("VoiceLog_", "GrigliaValutazione_") + ".csv");

                var sb = new StringBuilder();
                sb.AppendLine(string.Join(",",
                    "PID", "Timestamp", "AudioFile", "Tipo",
                    "FraseAttesa", "Trascritto", "IM",
                    "T", "P", "C", "Fenomeno", "Note"));

                string[] logLines = File.ReadAllLines(_filePath, Encoding.UTF8);

                for (int i = 1; i < logLines.Length; i++)
                {
                    string line = logLines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    string[] fields = ParseCsvLine(line);
                    if (fields.Length < 15) continue;

                    // Indici colonne VoiceLog:
                    // 0=InteractionType  1=AttemptNumber  2=Timestamp
                    // 3=ParticipantId    4=AudioFile      5=Scene
                    // 6=TreeId           7=NodeId         8=NpcLineIT
                    // 9=ExpectedPhrases  10=TranscribedText
                    // 11=MatchedOption   12=IsMatch
                    // 13=AvgConfidence   14=TokenCount

                    string im = fields[12];

                    // Salta righe non valutabili con la griglia percettiva
                    if (im == "n/d_nome") continue;
                    if (im == "n/d_vuoto") continue;

                    sb.AppendLine(string.Join(",",
                        CsvEscape(fields[3]),   // PID
                        CsvEscape(fields[2]),   // Timestamp
                        CsvEscape(fields[4]),   // AudioFile
                        CsvEscape(fields[0]),   // Tipo
                        CsvEscape(fields[9]),   // FraseAttesa
                        CsvEscape(fields[10]),  // Trascritto
                        CsvEscape(im),          // IM
                        "", "", "", "", ""       // T P C Fenomeno Note
                    ));
                }

                File.WriteAllText(grigliePath, sb.ToString(), Encoding.UTF8);

                if (logToConsole)
                    Debug.Log($"[VoiceRecognitionLogger] Griglia esportata: {grigliePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceRecognitionLogger] ExportGriglia fallito: {e}");
            }
        }

        // ── Parser CSV minimale ────────────────────────────────────────
        // Gestisce campi quoted con virgole interne (es. "Ops, scusa!").
        static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        { current.Append('"'); i++; }
                        else
                        { inQuotes = false; }
                    }
                    else { current.Append(c); }
                }
                else
                {
                    if (c == '"') { inQuotes = true; }
                    else if (c == ',') { fields.Add(current.ToString()); current.Clear(); }
                    else { current.Append(c); }
                }
            }
            fields.Add(current.ToString());
            return fields.ToArray();
        }

        // ── Confidenza media ───────────────────────────────────────────
        float ComputeAverageConfidence(WhisperResult result)
        {
            if (result.Segments == null) return -1f;

            float sum = 0f;
            int count = 0;

            foreach (var seg in result.Segments)
            {
                if (seg.Tokens == null) continue;
                foreach (var tok in seg.Tokens)
                {
                    if (tok.IsSpecial) continue;
                    sum += tok.Prob;
                    count++;
                }
            }

            return count > 0 ? sum / count : -1f;
        }

        int CountRealTokens(WhisperResult result)
        {
            if (result.Segments == null) return 0;

            int count = 0;
            foreach (var seg in result.Segments)
            {
                if (seg.Tokens == null) continue;
                foreach (var tok in seg.Tokens)
                    if (!tok.IsSpecial) count++;
            }
            return count;
        }

        // ── Scrittura CSV ──────────────────────────────────────────────
        void WriteRow(params string[] fields)
        {
            if (!_fileReady)
            {
                Debug.LogWarning("[VoiceRecognitionLogger] Tentativo di scrittura prima che il file " +
                                 "fosse inizializzato — riga scartata. Chiama SetParticipantId() prima.");
                return;
            }

            try
            {
                string line = string.Join(",", fields.Select(CsvEscape));
                File.AppendAllText(_filePath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceRecognitionLogger] Scrittura fallita: {e}");
            }
        }

        string CsvEscape(string field)
        {
            field ??= "";
            bool needsQuotes = field.Contains(',') || field.Contains('"') ||
                               field.Contains('\n') || field.Contains('\r');
            if (needsQuotes)
                field = "\"" + field.Replace("\"", "\"\"") + "\"";
            return field;
        }
    }
}