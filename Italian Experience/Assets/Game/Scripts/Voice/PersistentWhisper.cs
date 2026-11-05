using System.Linq;
using UnityEngine;
using Whisper.Utils;

namespace Bellavalle.Voice
{
    /// <summary>
    /// Rende il GameObject di WhisperManager persistente tra le scene,
    /// esattamente come GameManager. Gestisce il pattern singleton
    /// per evitare duplicati quando si carica una nuova scena.
    ///
    /// Setup:
    ///  1. Metti questo script sullo stesso GameObject che ha
    ///     WhisperManager + MicrophoneRecord + PushToTalkRecorder + VRPushToTalkInput
    ///  2. Il GameObject deve esistere nella PRIMA scena che carichi
    ///     (es. 00_Tutorial o 01_Stazione)
    ///  3. Se per sicurezza lo metti anche in altre scene, il duplicato
    ///     si autodistrugge — nessun conflitto
    /// </summary>
    public class PersistentWhisper : MonoBehaviour
    {
        public static PersistentWhisper Instance { get; private set; }

        /// <summary>
        /// Il PushToTalkRecorder sullo stesso GameObject di QUESTA istanza sopravvissuta.
        /// Le scene successive alla prima hanno la propria copia locale del prefab
        /// WhisperManager distrutta subito da Awake() qui sopra — qualsiasi script in
        /// quella scena con un campo [SerializeField] PushToTalkRecorder assegnato nel
        /// Prefab punterebbe quindi a un oggetto distrutto. Chi ha bisogno del recorder
        /// dopo la prima scena deve leggerlo da qui (PersistentWhisper.Instance.PushToTalkRecorder)
        /// invece di fidarsi del proprio campo serializzato.
        /// </summary>
        public PushToTalkRecorder PushToTalkRecorder { get; private set; }

        [Header("Riferimenti")]
        [SerializeField] MicrophoneRecord microphoneRecord;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            PushToTalkRecorder = GetComponent<PushToTalkRecorder>();
            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
            ForcePCMicrophone();
#endif
        }

#if UNITY_EDITOR
        void ForcePCMicrophone()
        {
            if (microphoneRecord == null)
            {
                Debug.LogWarning("[PersistentWhisper] MicrophoneRecord non assegnato — " +
                                 "impossibile selezionare il microfono.");
                return;
            }

            Debug.Log("[PersistentWhisper] ========== MICROPHONE DEBUG ==========");
            Debug.Log("[PersistentWhisper] Microfoni disponibili:");
            foreach (var d in Microphone.devices)
                Debug.Log($"  [{Microphone.devices.ToList().IndexOf(d)}] \"{d}\"");

            // Test in Editor con il visore collegato via Quest Link: il player parla nel
            // microfono del visore, quindi va PREFERITO (non escluso) un dispositivo
            // Oculus/Meta/Quest/Headset se presente — altrimenti si rischia di selezionare
            // per sbaglio un device di loopback (es. "Stereo Mix"), che cattura l'audio di
            // sistema invece della voce: sintomo tipico, Whisper trascrive "[Music]" perché
            // sta sentendo la musica ambientale della scena, non il player.

            string selectedDevice = null;
            // TEST: Preferisci AMD prima di cercare i device Oculus
            foreach (var d in Microphone.devices)
            {
                if (d.Contains("AMD") || d.Contains("Microphone Array"))
                {
                    selectedDevice = d;
                    break;
                }
            }

            // Se non trovi AMD, torna alla ricerca originale (Oculus/Quest/etc)
            if (selectedDevice == null)
            {
                foreach (var d in Microphone.devices)
                {
                    if (d.Contains("Oculus") || d.Contains("Meta") || d.Contains("Quest") ||
                        d.Contains("Virtual") || d.Contains("Headset") || d.Contains("XR"))
                    {
                        selectedDevice = d;
                        break;
                    }
                }
            }

            if (selectedDevice != null)
            {
                microphoneRecord.SelectedMicDevice = selectedDevice;
                Debug.Log($"[PersistentWhisper] ✓ SELEZIONATO: \"{selectedDevice}\"");
            }
            else if (Microphone.devices.Length > 0)
            {
                Debug.LogError("[PersistentWhisper] ✗ PROBLEMA: Nessun microfono del visore trovato!");
                Debug.LogError("[PersistentWhisper] I dispositivi sopra non contengono \"Oculus\", \"Meta\", \"Quest\", \"Virtual\", \"Headset\", o \"XR\"");
                Debug.LogError("[PersistentWhisper] Whisper potrebbe captare audio di sistema invece della voce del player!");
                Debug.LogError("[PersistentWhisper] SOLUZIONE: Controlla i nomi esatti sopra e modifica la lista di keywords in PersistentWhisper.cs");
            }
            else
            {
                Debug.LogError("[PersistentWhisper] ✗ PROBLEMA CRITICO: Nessun dispositivo di registrazione trovato!");
            }
            Debug.Log("[PersistentWhisper] ====================================");
        }
#endif
    }
}