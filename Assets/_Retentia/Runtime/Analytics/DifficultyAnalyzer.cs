// -----------------------------------------------------------------------------
// DifficultyAnalyzer.cs
// Analyzes player performance to calculate difficulty scores.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Retentia.Data;
using Retentia.Experiments;
using Retentia.Core;
using UnityEngine;

namespace Retentia.Analytics
{
    #region Level Attempt Data

    /// <summary>
    /// Data about a single level attempt.
    /// </summary>
    [Serializable]
    public struct LevelAttemptData
    {
        public int LevelId;
        public float Difficulty;
        public float Duration;
        public bool Completed;
        public int RetryCount;
        public DateTime Timestamp;
    }

    #endregion

    #region Difficulty Analysis Result

    /// <summary>
    /// Result of difficulty analysis.
    /// </summary>
    [Serializable]
    public struct DifficultyAnalysisResult
    {
        /// <summary>
        /// Overall difficulty score (0-100).
        /// Higher = player is struggling more.
        /// </summary>
        public float DifficultyScore;

        /// <summary>
        /// Frustration score (0-100).
        /// </summary>
        public float FrustrationScore;

        /// <summary>
        /// Win rate for recent levels.
        /// </summary>
        public float RecentWinRate;

        /// <summary>
        /// Average retries per level.
        /// </summary>
        public float AvgRetriesPerLevel;

        /// <summary>
        /// Whether player is struggling.
        /// </summary>
        public bool IsStruggling;

        /// <summary>
        /// Recommended difficulty adjustment.
        /// Negative = easier, Positive = harder.
        /// </summary>
        public float RecommendedDifficultyAdjustment;
    }

    #endregion

    #region Difficulty Analyzer Service

    /// <summary>
    /// Service for analyzing player difficulty and frustration levels.
    /// Provides metrics for dynamic difficulty adjustment.
    /// </summary>
    public class DifficultyAnalyzer : IRetentiaService
    {
        #region Fields

        /// <summary>
        /// Configuration reference.
        /// </summary>
        private readonly RetentionConfigSO _config;

        /// <summary>
        /// Recent level attempts for analysis.
        /// </summary>
        private readonly Queue<LevelAttemptData> _recentAttempts;

        /// <summary>
        /// Maximum attempts to keep in history.
        /// </summary>
        private const int MAX_HISTORY = 20;

        /// <summary>
        /// Whether service is initialized.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Last analysis result.
        /// </summary>
        private DifficultyAnalysisResult _lastResult;

        private readonly PlayerAnalyticsService _analyticsService;
        private readonly ABTestingService _abTesting;
 
        #endregion

        #region Properties

        public bool IsInitialized => _isInitialized;
        public int InitializationPriority => 30;
        public DifficultyAnalysisResult LastResult => _lastResult;
        public int AttemptCount => _recentAttempts.Count;

        #endregion

        #region Constructor

        public DifficultyAnalyzer(RetentionConfigSO config, PlayerAnalyticsService analyticsService)
        {
            _config = config;
            _analyticsService = analyticsService;
            _recentAttempts = new Queue<LevelAttemptData>(MAX_HISTORY);
            
            // Try resolve optional AB testing service
            RetentiaServiceLocator.TryGet(out _abTesting);
        }

        #endregion

        #region IRetentiaService Implementation

        public async UniTask InitializeAsync()
        {
            DebugLogger.LogServiceInit("DifficultyAnalyzer");

            _recentAttempts.Clear();
            _lastResult = default;
            _isInitialized = true;

            DebugLogger.LogServiceReady("DifficultyAnalyzer");
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            DebugLogger.LogServiceDispose("DifficultyAnalyzer");
            _recentAttempts.Clear();
            _isInitialized = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Records a level attempt for analysis.
        /// </summary>
        public void RecordAttempt(int levelId, float difficulty, float duration, bool completed, int retryCount = 0)
        {
            var attempt = new LevelAttemptData
            {
                LevelId = levelId,
                Difficulty = difficulty,
                Duration = duration,
                Completed = completed,
                RetryCount = retryCount,
                Timestamp = DateTime.UtcNow
            };

            // Maintain max history size
            if (_recentAttempts.Count >= MAX_HISTORY)
            {
                _recentAttempts.Dequeue();
            }

            _recentAttempts.Enqueue(attempt);

            DebugLogger.Debug($"[DifficultyAnalyzer] Recorded attempt: Level={levelId}, Completed={completed}, Retries={retryCount}");

            // Trigger analysis and potential flags
            Analyze();
        }

        /// <summary>
        /// Analyzes player difficulty and returns results.
        /// </summary>
        public DifficultyAnalysisResult Analyze()
        {
            if (_recentAttempts.Count == 0)
            {
                _lastResult = default;
                return _lastResult;
            }

            var attempts = _recentAttempts.ToArray();
            int completed = 0;
            int failed = 0;
            int totalRetries = 0;
            float totalDifficulty = 0f;

            foreach (var attempt in attempts)
            {
                if (attempt.Completed)
                {
                    completed++;
                }
                else
                {
                    failed++;
                }

                totalRetries += attempt.RetryCount;
                totalDifficulty += attempt.Difficulty;
            }

            var total = completed + failed;
            var winRate = total > 0 ? (float)completed / total : 0f;
            var avgRetries = total > 0 ? (float)totalRetries / total : 0f;
            var avgDifficulty = total > 0 ? totalDifficulty / total : 0f;

            // Calculate difficulty score (0-100)
            // Based on: low win rate, high retries, high level difficulty
            float difficultyScore = 0f;
            difficultyScore += (1f - winRate) * 40f;  // Win rate contribution
            difficultyScore += Mathf.Min(avgRetries * 10f, 30f);  // Retry contribution
            difficultyScore += Mathf.Min(avgDifficulty * 10f, 30f);  // Level difficulty contribution

            // Calculate frustration score
            // Based on: consecutive failures, high retries, time on failed levels
            float frustrationScore = CalculateFrustrationScore(attempts);

            // Determine if player is struggling
            var lowWinThreshold = _config?.LowWinRateThreshold ?? 0.3f;
            var highRetryThreshold = _config?.HighRetryThreshold ?? 5;
            bool isStruggling = winRate < lowWinThreshold || avgRetries > highRetryThreshold;

            // Calculate recommended adjustment
            float recommendedAdjustment = 0f;
            if (winRate < 0.3f)
            {
                recommendedAdjustment = -0.3f;  // Make easier
            }
            else if (winRate < 0.5f)
            {
                recommendedAdjustment = -0.15f;
            }
            else if (winRate > 0.9f)
            {
                recommendedAdjustment = 0.2f;  // Make harder
            }
            else if (winRate > 0.75f)
            {
                recommendedAdjustment = 0.1f;
            }

            _lastResult = new DifficultyAnalysisResult
            {
                DifficultyScore = Mathf.Clamp(difficultyScore, 0f, 100f),
                FrustrationScore = frustrationScore,
                RecentWinRate = winRate,
                AvgRetriesPerLevel = avgRetries,
                IsStruggling = isStruggling,
                RecommendedDifficultyAdjustment = recommendedAdjustment
            };

            // Fire event if player is struggling
            if (isStruggling)
            {
                var frustrationThreshold = _config?.FrustrationThreshold ?? 60f;
                if (frustrationScore >= frustrationThreshold)
                {
                    RetentiaEventBus.Publish(new PlayerFlaggedEvent(
                        "current_user",
                        "Frustrated",
                        $"Frustration score: {frustrationScore:F1}, Win rate: {winRate:P0}"
                    ));
                }
            }

            return _lastResult;
        }

        /// <summary>
        /// Gets difficulty recommendation for a new level.
        /// </summary>
        public float GetRecommendedDifficulty(float baseDifficulty)
        {
            return Mathf.Clamp01(baseDifficulty + _lastResult.RecommendedDifficultyAdjustment);
        }

        /// <summary>
        /// Clears all recorded attempts.
        /// </summary>
        public void ClearHistory()
        {
            _recentAttempts.Clear();
            _lastResult = default;
        }

        #endregion

        #region Private Methods

        private float CalculateFrustrationScore(LevelAttemptData[] attempts)
        {
            if (attempts.Length == 0) return 0f;

            float score = 0f;
            int consecutiveFailures = 0;

            for (int i = attempts.Length - 1; i >= 0; i--)
            {
                if (!attempts[i].Completed)
                {
                    consecutiveFailures++;
                    score += consecutiveFailures * 5f;  // Exponential frustration from consecutive failures
                    score += attempts[i].RetryCount * 3f;
                }
                else
                {
                    consecutiveFailures = 0;
                }
            }

            return Mathf.Clamp(score, 0f, 100f);
        }

        #endregion
    }

    #endregion
}
