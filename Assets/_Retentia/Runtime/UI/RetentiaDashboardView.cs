// -----------------------------------------------------------------------------
// RetentiaDashboardView.cs
// MonoBehaviour dashboard for displaying retention metrics with DoTween animations.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Retentia.Data;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Retentia.Core
{
    #region Dashboard Section Base

    /// <summary>
    /// Base class for dashboard sections.
    /// </summary>
    [Serializable]
    public abstract class DashboardSection
    {
        public RectTransform Container;
        public CanvasGroup CanvasGroup;

        public virtual void Show(float duration = 0.3f)
        {
            if (CanvasGroup != null)
            {
                CanvasGroup.DOFade(1f, duration);
            }
            if (Container != null)
            {
                Container.DOScale(1f, duration).SetEase(Ease.OutBack);
            }
        }

        public virtual void Hide(float duration = 0.2f)
        {
            if (CanvasGroup != null)
            {
                CanvasGroup.DOFade(0f, duration);
            }
            if (Container != null)
            {
                Container.DOScale(0.9f, duration);
            }
        }
    }

    #endregion

    #region Retention Dashboard View

    /// <summary>
    /// Main dashboard MonoBehaviour for displaying player retention metrics.
    /// Uses DoTween for smooth animations and transitions.
    /// </summary>
    public class RetentiaDashboardView : MonoBehaviour
    {
        #region Serialized Fields

        #if ODIN_INSPECTOR
        [FoldoutGroup("References")]
        #endif
        [Header("References")]
        [SerializeField] private RectTransform _dashboardContainer;
        [SerializeField] private CanvasGroup _mainCanvasGroup;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Score Display")]
        #endif
        [Header("Score Display")]
        [SerializeField] private Text _retentionScoreText;
        [SerializeField] private Image _retentionScoreFill;
        [SerializeField] private Text _riskLevelText;
        [SerializeField] private Image _riskLevelBackground;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Metrics")]
        #endif
        [Header("Metrics")]
        [SerializeField] private Text _churnProbabilityText;
        [SerializeField] private Text _engagementScoreText;
        [SerializeField] private Text _sessionCountText;
        [SerializeField] private Text _avgSessionDurationText;
        [SerializeField] private Text _daysSinceLoginText;
        [SerializeField] private Text _totalSpentText;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Segment Display")]
        #endif
        [Header("Segment Display")]
        [SerializeField] private Text _playerSegmentText;
        [SerializeField] private Image _segmentIcon;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Flags Display")]
        #endif
        [Header("Flags Display")]
        [SerializeField] private Transform _flagsContainer;
        [SerializeField] private GameObject _flagItemPrefab;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Recommendations")]
        #endif
        [Header("Recommendations")]
        [SerializeField] private Transform _recommendationsContainer;
        [SerializeField] private GameObject _recommendationItemPrefab;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Animation Settings")]
        #endif
        [Header("Animation Settings")]
        [SerializeField] private float _showDuration = 0.5f;
        [SerializeField] private float _updateDuration = 0.3f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _updateEase = Ease.OutQuad;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Colors")]
        #endif
        [Header("Colors")]
        [SerializeField] private Color _lowRiskColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _mediumRiskColor = new Color(1f, 0.8f, 0f);
        [SerializeField] private Color _highRiskColor = new Color(1f, 0.4f, 0f);
        [SerializeField] private Color _criticalRiskColor = new Color(0.9f, 0.1f, 0.1f);

        #endregion

        #region Private Fields

        private Sequence _currentSequence;
        private bool _isVisible;
        private RetentionScoreResult _lastResult;

        #endregion

        #region Properties

        public bool IsVisible => _isVisible;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Initial state
            if (_mainCanvasGroup != null)
            {
                _mainCanvasGroup.alpha = 0f;
            }
            _isVisible = false;
        }

        private void OnDestroy()
        {
            // Kill any running tweens
            _currentSequence?.Kill();
            DOTween.Kill(transform);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the dashboard with animation.
        /// </summary>
        #if ODIN_INSPECTOR
        [Button("Show Dashboard")]
        #endif
        public void Show()
        {
            if (_isVisible) return;

            _currentSequence?.Kill();
            _currentSequence = DOTween.Sequence();

            if (_mainCanvasGroup != null)
            {
                _currentSequence.Append(_mainCanvasGroup.DOFade(1f, _showDuration));
            }

            if (_dashboardContainer != null)
            {
                _dashboardContainer.localScale = Vector3.one * 0.8f;
                _currentSequence.Join(_dashboardContainer.DOScale(1f, _showDuration).SetEase(_showEase));
            }

            _isVisible = true;
            DebugLogger.Debug("[Dashboard] Shown");
        }

        /// <summary>
        /// Hides the dashboard with animation.
        /// </summary>
        #if ODIN_INSPECTOR
        [Button("Hide Dashboard")]
        #endif
        public void Hide()
        {
            if (!_isVisible) return;

            _currentSequence?.Kill();
            _currentSequence = DOTween.Sequence();

            if (_mainCanvasGroup != null)
            {
                _currentSequence.Append(_mainCanvasGroup.DOFade(0f, _showDuration * 0.5f));
            }

            if (_dashboardContainer != null)
            {
                _currentSequence.Join(_dashboardContainer.DOScale(0.9f, _showDuration * 0.5f));
            }

            _isVisible = false;
            DebugLogger.Debug("[Dashboard] Hidden");
        }

        /// <summary>
        /// Toggles dashboard visibility.
        /// </summary>
        public void Toggle()
        {
            if (_isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// Updates the dashboard with player data.
        /// </summary>
        public void UpdateDisplay(UserProfile profile, RetentionScoreResult scoreResult, PlayerSegment segment)
        {
            _lastResult = scoreResult;

            // Animate score update
            AnimateScoreUpdate(scoreResult.OverallScore, scoreResult.RiskLevel);

            // Update metrics
            UpdateMetrics(profile);

            // Update segment
            UpdateSegment(segment);

            // Update churn probability
            if (_churnProbabilityText != null)
            {
                float targetValue = scoreResult.ChurnProbability * 100f;
                float initialChurn = 0f;
                if (_churnProbabilityText.text != null)
                {
                    float.TryParse(_churnProbabilityText.text.Replace("%", ""), out initialChurn);
                }
                DOTween.To(
                    () => initialChurn,
                    x => {
                        initialChurn = x;
                        if (_churnProbabilityText != null) _churnProbabilityText.text = $"{x:F1}%";
                    },
                    targetValue,
                    _updateDuration
                );
            }

            // Update recommendations
            UpdateRecommendations(scoreResult.Recommendations);

            DebugLogger.Debug($"[Dashboard] Updated - Score: {scoreResult.OverallScore:F1}, Risk: {scoreResult.RiskLevel}");
        }

        /// <summary>
        /// Updates with flags.
        /// </summary>
        public void UpdateFlags(List<PlayerFlagEntry> flags)
        {
            if (_flagsContainer == null || _flagItemPrefab == null) return;

            // Clear existing
            foreach (Transform child in _flagsContainer)
            {
                Destroy(child.gameObject);
            }

            // Add flags with staggered animation
            int index = 0;
            foreach (var flag in flags)
            {
                var item = Instantiate(_flagItemPrefab, _flagsContainer);
                var text = item.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.text = $"{flag.Type}: {flag.Reason}";
                }

                // Animate in
                var canvasGroup = item.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.DOFade(1f, 0.3f).SetDelay(index * 0.1f);
                }

                index++;
            }
        }

        /// <summary>
        /// Plays a pulse animation on the score.
        /// </summary>
        public void PulseScore()
        {
            if (_retentionScoreText != null)
            {
                _retentionScoreText.transform
                    .DOPunchScale(Vector3.one * 0.2f, 0.3f, 5)
                    .SetEase(Ease.OutElastic);
            }
        }

        #endregion

        #region Private Methods

        private void AnimateScoreUpdate(float score, string riskLevel)
        {
            // Animate score fill
            if (_retentionScoreFill != null)
            {
                _retentionScoreFill.DOFillAmount(score / 100f, _updateDuration).SetEase(_updateEase);
            }

            // Animate score text
            if (_retentionScoreText != null)
            {
                float initialScore = 0f;
                if (_retentionScoreText.text != null)
                {
                    float.TryParse(_retentionScoreText.text, out initialScore);
                }
                DOTween.To(
                    () => initialScore,
                    x => {
                        initialScore = x;
                        if (_retentionScoreText != null) _retentionScoreText.text = $"{x:F0}";
                    },
                    score,
                    _updateDuration
                ).SetEase(_updateEase);
            }

            // Update risk level with color
            if (_riskLevelText != null)
            {
                _riskLevelText.text = riskLevel;
            }

            if (_riskLevelBackground != null)
            {
                Color targetColor = riskLevel switch
                {
                    "Low" => _lowRiskColor,
                    "Medium" => _mediumRiskColor,
                    "High" => _highRiskColor,
                    "Critical" => _criticalRiskColor,
                    _ => _mediumRiskColor
                };

                _riskLevelBackground.DOColor(targetColor, _updateDuration);
            }
        }

        private void UpdateMetrics(UserProfile profile)
        {
            if (_sessionCountText != null)
            {
                _sessionCountText.text = profile.TotalSessions.ToString();
            }

            if (_avgSessionDurationText != null)
            {
                _avgSessionDurationText.text = $"{profile.AvgSessionDuration:F1} min";
            }

            if (_daysSinceLoginText != null)
            {
                _daysSinceLoginText.text = profile.DaysSinceLastLogin.ToString();
            }

            if (_totalSpentText != null)
            {
                _totalSpentText.text = $"${profile.TotalSpent:F2}";
            }

            if (_engagementScoreText != null)
            {
                _engagementScoreText.text = $"{_lastResult.EngagementScore:F0}";
            }
        }

        private void UpdateSegment(PlayerSegment segment)
        {
            if (_playerSegmentText != null)
            {
                _playerSegmentText.text = segment.ToString();

                // Animate color based on segment
                Color segmentColor = segment switch
                {
                    PlayerSegment.Whale => new Color(0.8f, 0.6f, 1f),
                    PlayerSegment.HighValue => new Color(0.6f, 0.8f, 1f),
                    PlayerSegment.Engaged => _lowRiskColor,
                    PlayerSegment.AtRisk => _highRiskColor,
                    PlayerSegment.Churned => _criticalRiskColor,
                    _ => Color.white
                };

                _playerSegmentText.DOColor(segmentColor, _updateDuration);
            }
        }

        private void UpdateRecommendations(string[] recommendations)
        {
            if (_recommendationsContainer == null || _recommendationItemPrefab == null) return;
            if (recommendations == null || recommendations.Length == 0) return;

            // Clear existing
            foreach (Transform child in _recommendationsContainer)
            {
                Destroy(child.gameObject);
            }

            // Add recommendations
            int index = 0;
            foreach (var rec in recommendations)
            {
                var item = Instantiate(_recommendationItemPrefab, _recommendationsContainer);
                var text = item.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.text = $"• {rec}";
                }

                // Slide in animation
                var rect = item.GetComponent<RectTransform>();
                if (rect != null)
                {
                    var startPos = rect.anchoredPosition;
                    startPos.x += 100f;
                    rect.anchoredPosition = startPos;
                    rect.DOAnchorPosX(startPos.x - 100f, 0.3f).SetDelay(index * 0.1f).SetEase(Ease.OutQuad);
                }

                index++;
            }
        }

        #endregion

        #region Editor Testing

        #if ODIN_INSPECTOR && UNITY_EDITOR
        [Button("Test Animation", ButtonSizes.Large)]
        private void TestAnimation()
        {
            var testProfile = new UserProfile
            {
                TotalSessions = 25,
                AvgSessionDuration = 12.5f,
                DaysSinceLastLogin = 2,
                TotalSpent = 49.99f,
                WinRatePercent = 65,
                LevelsCompleted = 42
            };

            var testResult = new RetentionScoreResult
            {
                OverallScore = 72.5f,
                EngagementScore = 68f,
                ChurnProbability = 0.35f,
                RiskLevel = "Medium",
                Recommendations = new[] { "Send daily rewards", "Unlock premium content" }
            };

            Show();
            UpdateDisplay(testProfile, testResult, PlayerSegment.Regular);
        }
        #endif

        #endregion
    }

    #endregion
}
