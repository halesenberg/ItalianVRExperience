using UnityEngine;
using UnityEngine.InputSystem;
using Bellavalle.Voice;

namespace Bellavalle.Voice
{
    /// <summary>
    /// SOLO PER TEST — simula il grip XR con la barra spaziatrice.
    /// Usa il nuovo Input System (richiesto per XR).
    /// Da rimuovere (o disattivare) una volta collegato il vero input XR.
    ///
    /// Setup:
    ///  1. Metti questo script sullo stesso GameObject di PushToTalkRecorder
    ///     (es. WhisperManager)
    ///  2. Assegna pushToTalk nell'Inspector
    ///  3. Premi Play, tieni premuto SPAZIO per parlare, rilascia per trascrivere
    ///  4. Guarda la Console per il risultato
    /// </summary>
    public class KeyboardPushToTalkTest : MonoBehaviour
    {
        [SerializeField] PushToTalkRecorder pushToTalk;
        [SerializeField] Whisper.Utils.MicrophoneRecord microphoneRecord;

        bool _wasPressed;

        void Awake()
        {
            
            // Forza il microfono fisico, ignora i device virtuali Oculus/Meta
            if (microphoneRecord != null)
            {
                foreach (var device in Microphone.devices)
                {
                    if (device.Contains("Oculus") || device.Contains("Virtual"))
                        continue;

                    microphoneRecord.SelectedMicDevice = device;
                    Debug.Log($"[TEST] Microfono forzato a: \"{device}\"");
                    break;
                }
            }
        }

        void OnDestroy()
        {
            if (pushToTalk != null)
                pushToTalk.OnTranscriptionReady -= OnTranscription;
        }

        void Update()
        {
            if (pushToTalk == null) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            bool isPressed = keyboard.spaceKey.isPressed;

            if (isPressed && !_wasPressed)
                pushToTalk.StartRecording();

            if (!isPressed && _wasPressed)
                pushToTalk.StopRecording();

            _wasPressed = isPressed;
        }

        void OnTranscription(string text)
        {
            Debug.Log($"[TEST] Il player ha detto: \"{text}\"");
        }
    }
}