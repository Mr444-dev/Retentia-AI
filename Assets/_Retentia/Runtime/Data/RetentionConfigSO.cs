// -----------------------------------------------------------------------------
// RetentionConfigSO.cs
// ScriptableObject for global Retentia configuration settings.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Retentia.Data
{
    #region Retention Config ScriptableObject

    /// <summary>
    /// Global configuration for the Retentia AI system.
    /// Centralizes all thresholds, intervals, and feature flags.
    /// </summary>
    [CreateAssetMenu(fileName = "RetentionConfig", menuName = "Retentia AI/Configuration", order = 0)]
    public class RetentionConfigSO : ScriptableObject
    {
        #region Churn Prediction Settings

        #if ODIN_INSPECTOR
        [FoldoutGroup("Churn Prediction")]
        [Title("Model Settings")]
        #endif
        [Header("Churn Prediction")]
        [Tooltip("Resource path to the ONNX churn prediction model")]
        [SerializeField] private string _modelResourcePath = "Models/churn_model";

        #if ODIN_INSPECTOR
        [FoldoutGroup("Churn Prediction")]
        #endif
        [Tooltip("Threshold for considering a player at-risk of churning (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _churnRiskThreshold = 0.7f;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Churn Prediction")]
        #endif
        [Tooltip("How often to recalculate churn probability (seconds)")]
        [SerializeField] private float _predictionInterval = 300f;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Churn Prediction")]
        #endif
        [Tooltip("Minimum sessions before running prediction")]
        [SerializeField] private int _minSessionsForPrediction = 3;

        #endregion

        #region Player Segmentation Settings

        #if ODIN_INSPECTOR
        [FoldoutGroup("Player Segmentation")]
        [Title("Spending Thresholds")]
        #endif
        [Header("Player Segmentation")]
        [Tooltip("Minimum total spend to be considered a Whale")]
        [SerializeField] private float _whaleSpendThreshold = 100f;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Player Segmentation")]
        #endif
        [Tooltip("Minimum total spend to be considered a Dolphin")]
        [SerializeField] private float _dolphinSpendThreshold = 25f;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Player Segmentation")]
        #endif
        [Tooltip("Days since last login to be considered churned")]
        [SerializeField] private int _churnedDaysThreshold = 14;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Player Segmentation")]
        #endif
        [Tooltip("Days since last login to be considered at-risk")]
        [SerializeField] private int _atRiskDaysThreshold = 7;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Player Segmentation")]
        #endif
        [Tooltip("Minimum sessions to be considered a regular player")]
        [SerializeField] private int _regularPlayerSessions = 10;

        #endregion

        #region Difficulty Analysis Settings

        #if ODIN_INSPECTOR
        [FoldoutGroup("Difficulty Analysis")]
        [Title("Performance Thresholds")]
        #endif
        [Header("Difficulty Analysis")]
        [Tooltip("Win rate below this triggers difficulty warning")]
        [Range(0f, 1f)]
        [SerializeField] private float _lowWinRateThreshold = 0.3f;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Difficulty Analysis")]
        #endif
        [Tooltip("Retry count above this indicates high frustration")]
        [SerializeField] private int _highRetryThreshold = 5;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Difficulty Analysis")]
        #endif
        [Tooltip("Maximum acceptable latency (ms)")]
        [SerializeField] private float _maxAcceptableLatencyMs = 200f;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Difficulty Analysis")]
        #endif
        [Tooltip("Frustration score threshold for intervention")]
        [Range(0f, 100f)]
        [SerializeField] private float _frustrationThreshold = 60f;

        #endregion

        #region Offer Settings

        #if ODIN_INSPECTOR
        [FoldoutGroup("Offers")]
        [Title("Offer Limits")]
        #endif
        [Header("Offers")]
        [Tooltip("Maximum offers to show per session")]
        [SerializeField] private int _maxOffersPerSession = 3;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Offers")]
        #endif
        [Tooltip("Minimum time between offers (seconds)")]
        [SerializeField] private float _minOfferInterval = 300f;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Offers")]
        #endif
        [Tooltip("Cooldown after declined offer (seconds)")]
        [SerializeField] private float _declinedOfferCooldown = 600f;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Offers")]
        #endif
        [Tooltip("How long an offer popup stays visible (seconds)")]
        [SerializeField] private float _offerDisplayDuration = 10f;

        #endregion

        #region Analytics Settings

        #if ODIN_INSPECTOR
        [FoldoutGroup("Analytics")]
        [Title("Data Collection")]
        #endif
        [Header("Analytics")]
        [Tooltip("How often to flush analytics data (seconds)")]
        [SerializeField] private float _analyticsFlushInterval = 30f;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Analytics")]
        #endif
        [Tooltip("Maximum events to queue before auto-flush")]
        [SerializeField] private int _maxQueuedEvents = 100;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Analytics")]
        #endif
        [Tooltip("Enable verbose analytics logging")]
        [SerializeField] private bool _verboseAnalytics = false;

        #endregion

        #region Feature Flags

        #if ODIN_INSPECTOR
        [FoldoutGroup("Feature Flags")]
        [Title("System Features")]
        #endif
        [Header("Feature Flags")]
        [Tooltip("Enable ML-based churn prediction")]
        [SerializeField] private bool _enableChurnPrediction = true;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Feature Flags")]
        #endif
        [Tooltip("Enable dynamic offer system")]
        [SerializeField] private bool _enableOffers = true;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Feature Flags")]
        #endif
        [Tooltip("Enable player flagging")]
        [SerializeField] private bool _enableFlagging = true;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Feature Flags")]
        #endif
        [Tooltip("Enable difficulty analysis")]
        [SerializeField] private bool _enableDifficultyAnalysis = true;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Feature Flags")]
        #endif
        [Tooltip("Enable latency tracking")]
        [SerializeField] private bool _enableLatencyTracking = true;

        #endregion

        #region Properties - Churn Prediction

        public string ModelResourcePath => _modelResourcePath;
        public float ChurnRiskThreshold => _churnRiskThreshold;
        public float PredictionInterval => _predictionInterval;
        public int MinSessionsForPrediction => _minSessionsForPrediction;

        #endregion

        #region Properties - Segmentation

        public float WhaleSpendThreshold => _whaleSpendThreshold;
        public float DolphinSpendThreshold => _dolphinSpendThreshold;
        public int ChurnedDaysThreshold => _churnedDaysThreshold;
        public int AtRiskDaysThreshold => _atRiskDaysThreshold;
        public int RegularPlayerSessions => _regularPlayerSessions;

        #endregion

        #region Properties - Difficulty

        public float LowWinRateThreshold => _lowWinRateThreshold;
        public int HighRetryThreshold => _highRetryThreshold;
        public float MaxAcceptableLatencyMs => _maxAcceptableLatencyMs;
        public float FrustrationThreshold => _frustrationThreshold;

        #endregion

        #region Properties - Offers

        public int MaxOffersPerSession => _maxOffersPerSession;
        public float MinOfferInterval => _minOfferInterval;
        public float DeclinedOfferCooldown => _declinedOfferCooldown;
        public float OfferDisplayDuration => _offerDisplayDuration;

        #endregion

        #region Properties - Analytics

        public float AnalyticsFlushInterval => _analyticsFlushInterval;
        public int MaxQueuedEvents => _maxQueuedEvents;
        public bool VerboseAnalytics => _verboseAnalytics;

        #endregion

        #region Properties - Feature Flags

        public bool EnableChurnPrediction => _enableChurnPrediction;
        public bool EnableOffers => _enableOffers;
        public bool EnableFlagging => _enableFlagging;
        public bool EnableDifficultyAnalysis => _enableDifficultyAnalysis;
        public bool EnableLatencyTracking => _enableLatencyTracking;

        #endregion

        #region Validation

        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure thresholds are logically consistent
            if (_dolphinSpendThreshold >= _whaleSpendThreshold)
            {
                Debug.LogWarning("[RetentionConfig] Dolphin threshold should be less than Whale threshold.");
            }

            if (_atRiskDaysThreshold >= _churnedDaysThreshold)
            {
                Debug.LogWarning("[RetentionConfig] At-risk days should be less than churned days.");
            }
        }
        #endif

        #endregion
    }

    #endregion
}
