using UnityEngine;
using UnityEngine.InputSystem;
using Bellavalle.Voice;

namespace Bellavalle.Voice
{
    /// <summary>
    /// Collega il grip sinistro del controller VR al push-to-talk.
    /// Tieni premuto il grip = registra, rilascia = trascrive.
    ///
    /// Setup:
    ///  1. Metti questo script sullo stesso GameObject di PushToTalkRecorder
    ///     (es. WhisperManager)
    ///  2. Assegna pushToTalk nell'Inspector
    ///  3. Assegna gripAction: trascina l'InputActionReference del grip sinistro
    ///     (lo trovi dentro XR Interaction Toolkit > Starter Assets > 
    ///      XRI Default Input Actions > XRI Left > Grip)
    /// </summary>
    public class VRPushToTalkInput : MonoBehaviour
    {
        [SerializeField] PushToTalkRecorder pushToTalk;
        [SerializeField] InputActionReference gripAction;

        [Header("Soglia di attivazione")]
        [SerializeField] float pressThreshold = 0.5f;

        bool _wasPressed;

        void OnEnable()
        {
            if (gripAction != null && gripAction.action != null)
                gripAction.action.Enable();
        }

        void OnDisable()
        {
            if (gripAction != null && gripAction.action != null)
                gripAction.action.Disable();
        }

        void Update()
        {
            if (pushToTalk == null) return;
            if (gripAction == null || gripAction.action == null) return;

            float gripValue = gripAction.action.ReadValue<float>();
            bool isPressed = gripValue >= pressThreshold;

            if (isPressed && !_wasPressed)
                pushToTalk.StartRecording();

            if (!isPressed && _wasPressed)
                pushToTalk.StopRecording();

            _wasPressed = isPressed;
        }
    }
}