using UnityEngine;
using UnityEngine.InputSystem;

namespace Bellavalle.Voice
{
    /// <summary>
    /// Trigger da TASTIERA per il push-to-talk, da usare SOLO per testare in
    /// Editor quando non hai i controller collegati (o quando il gesto "che
    /// vuoi" non è ancora pronto). Fa esattamente quello che fa VRPushToTalkInput
    /// col grip sinistro, ma con un tasto — stesso identico
    /// PushToTalkRecorder.StartRecording()/StopRecording() di sempre, quindi il
    /// microfono fisico del PC funziona già (PushToTalkRecorder lo seleziona da
    /// solo quando XRSettings.isDeviceActive è false, vedi
    /// SelectPhysicalMicrophone()) e tutto quello a valle (DialogueManager,
    /// NameInputHandler, VoiceMatchUtils) non sa e non gli importa come è
    /// partita la registrazione.
    ///
    /// Setup:
    ///  1. Metti questo script sullo stesso GameObject di PushToTalkRecorder
    ///     (es. WhisperManager), IN AGGIUNTA a VRPushToTalkInput — possono
    ///     convivere tranquillamente, userai solo quello che ti serve.
    ///  2. Assegna pushToTalk nell'Inspector.
    ///  3. Premi e tieni premuto talkKey (default: Spazio) per registrare,
    ///     rilascia per trascrivere — in Play Mode, senza cuffie/controller.
    ///  4. Quando avrai il gesto "che vuoi" pronto, puoi lasciare questo
    ///     script attivo com'è (utile per testare rapidamente da desktop
    ///     senza indossare il visore) oppure disattivarlo.
    /// </summary>
    public class KeyboardPushToTalkInput : MonoBehaviour
    {
        [SerializeField] PushToTalkRecorder pushToTalk;
        [SerializeField] Key talkKey = Key.Space;

        [Header("Debug")]
        [Tooltip("Logga in Console ogni volta che il tasto viene premuto/rilasciato, " +
                 "PRIMA ancora di chiamare StartRecording()/StopRecording(). Serve a capire " +
                 "se il problema è che il tasto non viene letto affatto (es. Game view senza " +
                 "focus, o pushToTalk non assegnato) oppure se il tasto funziona ma è la " +
                 "registrazione/trascrizione a non partire.")]
        [SerializeField] bool logToConsole = true;

        bool _wasPressed;

        void Update()
        {
            if (pushToTalk == null)
            {
                // Logga una volta sola per frame sarebbe troppo rumoroso: qui va bene
                // solo al primo Update in cui ce ne accorgiamo, quindi lo lasciamo
                // come LogWarning "silenzioso" — controlla comunque questo campo per primo
                // se non vedi mai nessun log da questo script.
                return;
            }
            if (Keyboard.current == null) return;

            bool isPressed = Keyboard.current[talkKey].isPressed;

            if (isPressed && !_wasPressed)
            {
                if (logToConsole) Debug.Log($"[KeyboardPushToTalkInput] Tasto '{talkKey}' premuto → StartRecording()");
                pushToTalk.StartRecording();
            }

            if (!isPressed && _wasPressed)
            {
                if (logToConsole) Debug.Log($"[KeyboardPushToTalkInput] Tasto '{talkKey}' rilasciato → StopRecording()");
                pushToTalk.StopRecording();
            }

            _wasPressed = isPressed;
        }

        void Awake()
        {
            if (pushToTalk == null)
                Debug.LogWarning("[KeyboardPushToTalkInput] Campo 'Push To Talk' non assegnato nell'Inspector — " +
                                  "il tasto non farà assolutamente nulla finché non lo colleghi.");
        }
    }
}