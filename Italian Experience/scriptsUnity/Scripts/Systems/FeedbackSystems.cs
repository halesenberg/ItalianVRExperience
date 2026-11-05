using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

namespace Bellavalle.Systems
{
    // ══════════════════════════════════════════════════════════════════
    // WorldFeedback
    // Mostra la frase chiave su un oggetto fisico del mondo (insegna del
    // bar, muro, tazza) dopo ogni scambio. Verde = corretto, arancione = sbagliato.
    // ══════════════════════════════════════════════════════════════════
    public class WorldFeedback : MonoBehaviour
    {
        [SerializeField] GameObject  feedbackPrefab;    // prefab: TMP_Text su Canvas WorldSpace
        [SerializeField] Color       correctColor   = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] Color       incorrectColor = new Color(0.9f, 0.5f, 0.1f);

        public void Show(string phrase, Transform anchor, bool correct, float duration)
        {
            if (feedbackPrefab == null || anchor == null) return;
            StartCoroutine(ShowRoutine(phrase, anchor, correct, duration));
        }

        IEnumerator ShowRoutine(string phrase, Transform anchor, bool correct, float duration)
        {
            var go  = Instantiate(feedbackPrefab, anchor.position + Vector3.up * 0.3f,
                                  Quaternion.identity);
            var tmp = go.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                tmp.text  = phrase;
                tmp.color = correct ? correctColor : incorrectColor;
            }

            // Guarda verso la camera
            var cam = Camera.main;
            if (cam != null) go.transform.LookAt(cam.transform);

            // Fade out
            var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
            yield return new WaitForSeconds(duration * 0.7f);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / (duration * 0.3f);
                cg.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
            Destroy(go);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // HapticFeedback
    // Feedback aptico differenziato: corretto = impulso breve e morbido,
    // sbagliato = impulso lungo e marcato.
    // Attacca questo component al rig VR o al GameManager.
    // ══════════════════════════════════════════════════════════════════
    public class HapticFeedback : MonoBehaviour
    {
        // Per OpenXR/SteamVR usa XRBaseController
        [SerializeField] XRBaseController leftController;
        [SerializeField] XRBaseController rightController;

        public void Give(bool correct)
        {
            float amplitude = correct ? 0.25f : 0.55f;
            float duration  = correct ? 0.08f : 0.25f;

            leftController? .SendHapticImpulse(amplitude, duration);
            rightController?.SendHapticImpulse(amplitude, duration);
        }

        /// Haptic per quando si raccoglie un item corretto nella missione spesa
        public void GivePickup()
        {
            leftController? .SendHapticImpulse(0.3f, 0.05f);
            rightController?.SendHapticImpulse(0.3f, 0.05f);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // WordFlashUI
    // Flash breve che mostra "✓ caffè" quando si impara una parola.
    // Il canvas è fisso nel campo visivo del player (screen-space overlay
    // oppure World Space agganciato alla camera).
    // ══════════════════════════════════════════════════════════════════
    public class WordFlashUI : MonoBehaviour
    {
        [SerializeField] TMP_Text  flashText;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] float     displayDuration = 1.8f;

        Coroutine _current;

        public void Show(string word)
        {
            if (_current != null) StopCoroutine(_current);
            _current = StartCoroutine(FlashRoutine(word));
        }

        IEnumerator FlashRoutine(string word)
        {
            flashText.text   = $"✓  {word}";
            canvasGroup.alpha = 1f;

            yield return new WaitForSeconds(displayDuration * 0.75f);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / (displayDuration * 0.25f);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
    }
}
