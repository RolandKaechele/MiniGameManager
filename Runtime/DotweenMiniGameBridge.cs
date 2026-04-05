#if MINIGAMEMANAGER_DOTWEEN
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace MiniGameManager.Runtime
{
    /// <summary>
    /// Optional bridge that adds DOTween-driven UI animations around mini-game lifecycle events:
    /// a launch overlay fades in when a mini-game starts, a completion panel slides in with score
    /// roll, and an abort flash plays on abort.
    /// Enable define <c>MINIGAMEMANAGER_DOTWEEN</c> in Player Settings › Scripting Define Symbols.
    /// Requires <b>DOTween Pro</b>.
    /// <para>
    /// Assign <see cref="overlayGroup"/> to the CanvasGroup of your mini-game launch overlay,
    /// <see cref="resultPanel"/> to the completion result panel root, and optionally
    /// <see cref="scoreLabel"/> to a TMP/Text component for the animated score counter.
    /// </para>
    /// </summary>
    [AddComponentMenu("MiniGameManager/DOTween Bridge")]
    [DisallowMultipleComponent]
    public class DotweenMiniGameBridge : MonoBehaviour
    {
        [Header("Launch Overlay")]
        [Tooltip("CanvasGroup of the full-screen overlay shown when a mini-game launches.")]
        [SerializeField] private CanvasGroup overlayGroup;

        [Tooltip("Duration for the launch overlay to fade in at mini-game start.")]
        [SerializeField] private float launchFadeInDuration = 0.3f;

        [Tooltip("Duration for the launch overlay to fade out after the mini-game is ready.")]
        [SerializeField] private float launchFadeOutDuration = 0.4f;

        [Tooltip("Hold time before fading the launch overlay out.")]
        [SerializeField] private float launchHoldDuration = 0.5f;

        [Tooltip("DOTween ease for launch overlay transitions.")]
        [SerializeField] private Ease launchEase = Ease.InOutSine;

        [Header("Result Panel")]
        [Tooltip("RectTransform of the result / completion panel that slides in.")]
        [SerializeField] private RectTransform resultPanel;

        [Tooltip("CanvasGroup on the result panel for fade.")]
        [SerializeField] private CanvasGroup resultGroup;

        [Tooltip("Pixel offset from which the result panel slides in from below.")]
        [SerializeField] private float resultSlideOffset = 80f;

        [Tooltip("Duration for the result panel to slide and fade in.")]
        [SerializeField] private float resultInDuration = 0.4f;

        [Tooltip("DOTween ease for result panel slide-in.")]
        [SerializeField] private Ease resultEase = Ease.OutBack;

        [Header("Score Counter")]
        [Tooltip("Optional TMPro or UI Text component used to display theAnimatableScore.")]
        [SerializeField] private Component scoreLabel;

        [Tooltip("Duration for the score number rolling animation.")]
        [SerializeField] private float scoreRollDuration = 1.0f;

        // -------------------------------------------------------------------------

        private MiniGameManager _mgm;
        private Sequence         _launchSequence;

        private void Awake()
        {
            _mgm = GetComponent<MiniGameManager>() ?? FindFirstObjectByType<MiniGameManager>();
            if (_mgm == null) Debug.LogWarning("[MiniGameManager/DotweenMiniGameBridge] MiniGameManager not found.");

            if (overlayGroup  != null) overlayGroup.alpha  = 0f;
            if (resultGroup   != null) resultGroup.alpha   = 0f;
        }

        private void OnEnable()
        {
            if (_mgm == null) return;
            _mgm.OnMiniGameStarted   += OnMiniGameStarted;
            _mgm.OnMiniGameCompleted += OnMiniGameCompleted;
            _mgm.OnMiniGameAborted   += OnMiniGameAborted;
        }

        private void OnDisable()
        {
            if (_mgm == null) return;
            _mgm.OnMiniGameStarted   -= OnMiniGameStarted;
            _mgm.OnMiniGameCompleted -= OnMiniGameCompleted;
            _mgm.OnMiniGameAborted   -= OnMiniGameAborted;
            _launchSequence?.Kill();
        }

        // -------------------------------------------------------------------------

        private void OnMiniGameStarted(string id)
        {
            if (overlayGroup == null) return;

            _launchSequence?.Kill();
            _launchSequence = DOTween.Sequence();

            overlayGroup.alpha = 0f;
            _launchSequence
                .Append(overlayGroup.DOFade(1f, launchFadeInDuration).SetEase(launchEase))
                .AppendInterval(launchHoldDuration)
                .Append(overlayGroup.DOFade(0f, launchFadeOutDuration).SetEase(launchEase));
        }

        private void OnMiniGameCompleted(MiniGameResult result)
        {
            if (resultPanel == null) return;

            Vector2 finalPos = resultPanel.anchoredPosition;
            resultPanel.anchoredPosition = finalPos + Vector2.down * resultSlideOffset;
            if (resultGroup != null) resultGroup.alpha = 0f;

            var seq = DOTween.Sequence();
            seq.Join(resultPanel.DOAnchorPos(finalPos, resultInDuration).SetEase(resultEase));
            if (resultGroup != null)
                seq.Join(resultGroup.DOFade(1f, resultInDuration));

            // Animate score counter
            if (scoreLabel != null && result.score > 0)
            {
                seq.Join(DOVirtual.Int(0, result.score, scoreRollDuration, v =>
                {
                    SetLabelText(scoreLabel, v.ToString());
                }).SetEase(Ease.OutCubic));
            }
        }

        private void OnMiniGameAborted(string id)
        {
            if (resultPanel == null) return;
            DOTween.Kill(resultPanel);
            resultPanel.DOShakePosition(0.3f, 8f, 20, 90f);
        }

        // -------------------------------------------------------------------------

        private static void SetLabelText(Component label, string text)
        {
            // Support both TMPro and legacy UI.Text without a hard reference.
            var tmpText = label as TMPro.TMP_Text;
            if (tmpText != null) { tmpText.text = text; return; }
            var uiText = label as UnityEngine.UI.Text;
            if (uiText != null) { uiText.text = text; }
        }
    }
}
#else
namespace MiniGameManager.Runtime
{
    /// <summary>No-op stub — enable define <c>MINIGAMEMANAGER_DOTWEEN</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("MiniGameManager/DOTween Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class DotweenMiniGameBridge : UnityEngine.MonoBehaviour { }
}
#endif
