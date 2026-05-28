// -----------------------------------------------------------------------------
// RetentionScoreCalculator.cs
// Calculates composite retention scores from multiple inputs.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using Cysharp.Threading.Tasks;
using Retentia.Data;
using UnityEngine;

namespace Retentia.Core
{
    #region Retention Score Result

    /// <summary>
    /// Result of retention score calculation.
    /// </summary>
    [Serializable]
    public struct RetentionScoreResult
    {
        /// <summary>
        /// Overall retention score (0-100).
        /// Higher = more likely to be retained.
        /// </summary>
        public float OverallScore;

        /// <summary>
        /// Engagement component score.
        /// </summary>
        public float EngagementScore;

        /// <summary>
        /// Monetization component score.
        /// </summary>
        public float MonetizationScore;

        /// <summary>
        /// Social component score.
        /// </summary>
        public float SocialScore;

        /// <summary>
        /// Progression component score.
        /// </summary>
        public float ProgressionScore;

        /// <summary>
        /// ML-based churn probability.
        /// </summary>
        public float ChurnProbability;

        /// <summary>
        /// Risk level string.
        /// </summary>
        public string RiskLevel;

        /// <summary>
        /// Recommendations for improving retention.
        /// </summary>
        public string[] Recommendations;
    }

    #endregion

    #region Retention Score Calculator

    /// <summary>
    /// Calculates composite retention scores combining ML predictions with rule-based analysis.
    /// </summary>
    public class RetentionScoreCalculator : IRetentiaService
    {
        #region Fields

        private readonly RetentionConfigSO _config;
        private readonly ChurnPredictionEngine _churnEngine;
        private bool _isInitialized;

        #endregion

        #region Score Weights

        private const float WEIGHT_ENGAGEMENT = 0.30f;
        private const float WEIGHT_MONETIZATION = 0.25f;
        private const float WEIGHT_SOCIAL = 0.15f;
        private const float WEIGHT_PROGRESSION = 0.20f;
        private const float WEIGHT_ML = 0.10f;

        #endregion

        #region Properties

        public bool IsInitialized => _isInitialized;
        public int InitializationPriority => 25;

        #endregion

        #region Constructor

        public RetentionScoreCalculator(RetentionConfigSO config = null, ChurnPredictionEngine churnEngine = null)
        {
            _config = config;
            _churnEngine = churnEngine;
        }

        #endregion

        #region IRetentiaService Implementation

        public async UniTask InitializeAsync()
        {
            DebugLogger.LogServiceInit("RetentionScoreCalculator");
            _isInitialized = true;
            DebugLogger.LogServiceReady("RetentionScoreCalculator");
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            DebugLogger.LogServiceDispose("RetentionScoreCalculator");
            _isInitialized = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Calculates the full retention score for a player.
        /// </summary>
        public async UniTask<RetentionScoreResult> CalculateScoreAsync(UserProfile profile, PlayerSessionData session = null)
        {
            // Calculate component scores
            float engagement = CalculateEngagementScore(profile, session);
            float monetization = CalculateMonetizationScore(profile);
            float social = CalculateSocialScore(profile);
            float progression = CalculateProgressionScore(profile);

            // Get ML prediction if available
            float churnProb = 0.5f;
            if (_churnEngine != null && _churnEngine.IsModelReady)
            {
                churnProb = await _churnEngine.PredictChurnForProfileAsync(profile);
            }

            // Calculate weighted overall score
            // Invert churn probability (high churn = low retention score)
            float mlScore = (1f - churnProb) * 100f;

            float overallScore =
                (engagement * WEIGHT_ENGAGEMENT) +
                (monetization * WEIGHT_MONETIZATION) +
                (social * WEIGHT_SOCIAL) +
                (progression * WEIGHT_PROGRESSION) +
                (mlScore * WEIGHT_ML);

            // Determine risk level
            string riskLevel = GetRiskLevel(overallScore, churnProb);

            // Generate recommendations
            var recommendations = GenerateRecommendations(profile, session, engagement, monetization, social, progression);

            return new RetentionScoreResult
            {
                OverallScore = Mathf.Clamp(overallScore, 0f, 100f),
                EngagementScore = engagement,
                MonetizationScore = monetization,
                SocialScore = social,
                ProgressionScore = progression,
                ChurnProbability = churnProb,
                RiskLevel = riskLevel,
                Recommendations = recommendations
            };
        }

        /// <summary>
        /// Quick scoring without async ML prediction.
        /// </summary>
        public RetentionScoreResult CalculateScoreSync(UserProfile profile, PlayerSessionData session = null)
        {
            float engagement = CalculateEngagementScore(profile, session);
            float monetization = CalculateMonetizationScore(profile);
            float social = CalculateSocialScore(profile);
            float progression = CalculateProgressionScore(profile);

            // Use cached churn probability from profile
            float churnProb = profile.ChurnProbability;
            float mlScore = (1f - churnProb) * 100f;

            float overallScore =
                (engagement * WEIGHT_ENGAGEMENT) +
                (monetization * WEIGHT_MONETIZATION) +
                (social * WEIGHT_SOCIAL) +
                (progression * WEIGHT_PROGRESSION) +
                (mlScore * WEIGHT_ML);

            string riskLevel = GetRiskLevel(overallScore, churnProb);
            var recommendations = GenerateRecommendations(profile, session, engagement, monetization, social, progression);

            return new RetentionScoreResult
            {
                OverallScore = Mathf.Clamp(overallScore, 0f, 100f),
                EngagementScore = engagement,
                MonetizationScore = monetization,
                SocialScore = social,
                ProgressionScore = progression,
                ChurnProbability = churnProb,
                RiskLevel = riskLevel,
                Recommendations = recommendations
            };
        }

        #endregion

        #region Score Calculation Methods

        private float CalculateEngagementScore(UserProfile profile, PlayerSessionData session)
        {
            float score = 0f;

            // Session count contribution (max 30 points)
            score += Mathf.Min(profile.TotalSessions * 3f, 30f);

            // Session duration contribution (max 30 points)
            score += Mathf.Min(profile.AvgSessionDuration * 2f, 30f);

            // Recency contribution (max 20 points, decreases with days since login)
            score += Mathf.Max(0f, 20f - (profile.DaysSinceLastLogin * 2f));

            // Ads watched contribution (max 20 points)
            score += Mathf.Min(profile.AdsWatched * 2f, 20f);

            return Mathf.Clamp(score, 0f, 100f);
        }

        private float CalculateMonetizationScore(UserProfile profile)
        {
            float score = 0f;

            // Total spent contribution (logarithmic scale)
            if (profile.TotalSpent > 0)
            {
                score += Mathf.Min(Mathf.Log10(profile.TotalSpent + 1) * 25f, 60f);
            }

            // Support tickets contribution (negative impact)
            score -= profile.SupportTicketsOpen * 5f;

            // Ads engagement (shows willingness to engage for value)
            score += Mathf.Min(profile.AdsWatched * 0.5f, 20f);

            // Platform bonus (iOS users historically have higher LTV)
            if (profile.PlatformId == 1) // iOS
            {
                score += 10f;
            }

            return Mathf.Clamp(score, 0f, 100f);
        }

        private float CalculateSocialScore(UserProfile profile)
        {
            float score = 0f;

            // Social shares (max 60 points)
            score += Mathf.Min(profile.SocialShares * 6f, 60f);

            // Base score for any social activity
            if (profile.SocialShares > 0)
            {
                score += 20f;
            }

            // Referral bonus (if implemented in profile)
            // score += profile.ReferralCount * 10f;

            return Mathf.Clamp(score, 0f, 100f);
        }

        private float CalculateProgressionScore(UserProfile profile)
        {
            float score = 0f;

            // Levels completed (max 50 points)
            score += Mathf.Min(profile.LevelsCompleted * 0.5f, 50f);

            // Win rate contribution (max 30 points)
            score += (profile.WinRatePercent / 100f) * 30f;

            // Session-to-level ratio (engagement with content)
            if (profile.TotalSessions > 0)
            {
                float levelRatio = (float)profile.LevelsCompleted / profile.TotalSessions;
                score += Mathf.Min(levelRatio * 20f, 20f);
            }

            return Mathf.Clamp(score, 0f, 100f);
        }

        #endregion

        #region Helper Methods

        private string GetRiskLevel(float overallScore, float churnProb)
        {
            var threshold = _config?.ChurnRiskThreshold ?? 0.7f;

            if (churnProb >= threshold || overallScore < 30f)
            {
                return "Critical";
            }
            else if (churnProb >= 0.5f || overallScore < 50f)
            {
                return "High";
            }
            else if (churnProb >= 0.3f || overallScore < 70f)
            {
                return "Medium";
            }
            else
            {
                return "Low";
            }
        }

        private string[] GenerateRecommendations(
            UserProfile profile,
            PlayerSessionData session,
            float engagement,
            float monetization,
            float social,
            float progression)
        {
            var recommendations = new System.Collections.Generic.List<string>();

            if (engagement < 50f)
            {
                if (profile.DaysSinceLastLogin > 3)
                {
                    recommendations.Add("Send a 'We miss you' push notification with bonus offer.");
                }
                if (profile.AvgSessionDuration < 5f)
                {
                    recommendations.Add("Improve early-game content to increase session duration.");
                }
            }

            if (monetization < 30f && profile.TotalSpent == 0)
            {
                recommendations.Add("Present first-purchase discount offer.");
            }

            if (social < 30f)
            {
                recommendations.Add("Encourage social sharing with friend referral rewards.");
            }

            if (progression < 40f)
            {
                if (profile.WinRatePercent < 30)
                {
                    recommendations.Add("Consider reducing difficulty for this player.");
                }
                recommendations.Add("Offer gameplay tips or tutorial refresher.");
            }

            if (session != null && session.FrustrationScore > 60f)
            {
                recommendations.Add("Player showing frustration - offer help or difficulty assist.");
            }

            return recommendations.ToArray();
        }

        #endregion
    }

    #endregion
}
