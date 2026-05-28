// -----------------------------------------------------------------------------
// PlayerSegmentationService.cs
// Service for segmenting players based on behavior and value.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using Cysharp.Threading.Tasks;
using Retentia.Data;
using UnityEngine;

namespace Retentia.Core
{
    #region Segmentation Result

    /// <summary>
    /// Result of player segmentation.
    /// </summary>
    [Serializable]
    public struct SegmentationResult
    {
        /// <summary>
        /// Primary player segment.
        /// </summary>
        public PlayerSegment PrimarySegment;

        /// <summary>
        /// Secondary segment (if applicable).
        /// </summary>
        public PlayerSegment SecondarySegment;

        /// <summary>
        /// Confidence score (0-1).
        /// </summary>
        public float Confidence;

        /// <summary>
        /// Segment description.
        /// </summary>
        public string Description;

        /// <summary>
        /// Recommended actions for this segment.
        /// </summary>
        public string[] RecommendedActions;
    }

    #endregion

    #region Player Segmentation Service

    /// <summary>
    /// Service for segmenting players into behavioral and value-based categories.
    /// Used for targeting offers and personalization.
    /// </summary>
    public class PlayerSegmentationService : IRetentiaService
    {
        #region Fields

        private readonly RetentionConfigSO _config;
        private bool _isInitialized;

        #endregion

        #region Properties

        public bool IsInitialized => _isInitialized;
        public int InitializationPriority => 26;

        #endregion

        #region Constructor

        public PlayerSegmentationService(RetentionConfigSO config = null)
        {
            _config = config;
        }

        #endregion

        #region IRetentiaService Implementation

        public async UniTask InitializeAsync()
        {
            DebugLogger.LogServiceInit("PlayerSegmentationService");
            _isInitialized = true;
            DebugLogger.LogServiceReady("PlayerSegmentationService");
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            DebugLogger.LogServiceDispose("PlayerSegmentationService");
            _isInitialized = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Segments a player based on their profile.
        /// </summary>
        public SegmentationResult SegmentPlayer(UserProfile profile)
        {
            var result = new SegmentationResult
            {
                Confidence = 0f,
                SecondarySegment = PlayerSegment.Unknown
            };

            // Check for churned first
            var churnedDays = _config?.ChurnedDaysThreshold ?? 14;
            if (profile.DaysSinceLastLogin >= churnedDays)
            {
                result.PrimarySegment = PlayerSegment.Churned;
                result.Confidence = 0.9f;
                result.Description = "Player has not logged in for an extended period.";
                result.RecommendedActions = new[] { "Send win-back campaign", "Offer returning player bonus" };
                return result;
            }

            // Check for returning player
            var atRiskDays = _config?.AtRiskDaysThreshold ?? 7;
            if (profile.DaysSinceLastLogin >= atRiskDays && profile.DaysSinceLastLogin < churnedDays)
            {
                result.PrimarySegment = PlayerSegment.AtRisk;
                result.Confidence = 0.8f;
                result.Description = "Player showing signs of disengagement.";
                result.RecommendedActions = new[] { "Send push notification", "Present limited-time offer" };
                return result;
            }

            // Check spending-based segments
            var whaleThreshold = _config?.WhaleSpendThreshold ?? 100f;
            var dolphinThreshold = _config?.DolphinSpendThreshold ?? 25f;

            if (profile.TotalSpent >= whaleThreshold)
            {
                result.PrimarySegment = PlayerSegment.Whale;
                result.Confidence = 0.95f;
                result.Description = "High-value player with significant spending.";
                result.RecommendedActions = new[] { "VIP treatment", "Exclusive content access", "Priority support" };
                return result;
            }

            if (profile.TotalSpent >= dolphinThreshold)
            {
                result.PrimarySegment = PlayerSegment.HighValue;
                result.SecondarySegment = profile.TotalSessions > 20 ? PlayerSegment.Engaged : PlayerSegment.Regular;
                result.Confidence = 0.85f;
                result.Description = "Moderate spender with growth potential.";
                result.RecommendedActions = new[] { "Upsell premium content", "Bundle offers" };
                return result;
            }

            // Check engagement-based segments
            var regularSessions = _config?.RegularPlayerSessions ?? 10;

            if (profile.TotalSessions < 3)
            {
                result.PrimarySegment = PlayerSegment.NewPlayer;
                result.Confidence = 0.9f;
                result.Description = "New player still in onboarding phase.";
                result.RecommendedActions = new[] { "Tutorial optimization", "First-purchase incentive", "Engagement hooks" };
                return result;
            }

            if (profile.TotalSessions >= regularSessions && profile.AvgSessionDuration >= 10f)
            {
                result.PrimarySegment = PlayerSegment.Engaged;
                result.Confidence = 0.8f;
                result.Description = "Highly engaged player with strong session metrics.";
                result.RecommendedActions = new[] { "Social features", "Competitive content", "Conversion offers" };
                return result;
            }

            if (profile.TotalSessions >= regularSessions)
            {
                result.PrimarySegment = PlayerSegment.Regular;
                result.Confidence = 0.75f;
                result.Description = "Regular player with consistent engagement.";
                result.RecommendedActions = new[] { "Retention rewards", "Daily login incentives" };
                return result;
            }

            // Default to casual
            result.PrimarySegment = PlayerSegment.Casual;
            result.Confidence = 0.7f;
            result.Description = "Casual player with moderate engagement.";
            result.RecommendedActions = new[] { "Engagement prompts", "Easy achievements", "Value demonstration" };

            return result;
        }

        /// <summary>
        /// Updates a profile's segment ID based on current data.
        /// </summary>
        public void UpdateProfileSegment(ref UserProfile profile)
        {
            var result = SegmentPlayer(profile);
            profile.PlayerSegmentId = (int)result.PrimarySegment;

            // Fire event if segment changed
            var previousSegment = (PlayerSegment)profile.PlayerSegmentId;
            if (previousSegment != result.PrimarySegment)
            {
                RetentiaEventBus.Publish(new SegmentChangedEvent(
                    profile.UserId,
                    previousSegment,
                    result.PrimarySegment
                ));
            }
        }

        /// <summary>
        /// Gets a description for a segment.
        /// </summary>
        public string GetSegmentDescription(PlayerSegment segment)
        {
            return segment switch
            {
                PlayerSegment.Unknown => "Segment not yet determined",
                PlayerSegment.NewPlayer => "New player (< 3 sessions)",
                PlayerSegment.Casual => "Casual player with light engagement",
                PlayerSegment.Regular => "Regular player with consistent sessions",
                PlayerSegment.Engaged => "Highly engaged player",
                PlayerSegment.HighValue => "Moderate spender ($25-$100)",
                PlayerSegment.Whale => "High-value spender ($100+)",
                PlayerSegment.AtRisk => "Player showing disengagement signs",
                PlayerSegment.Churned => "Inactive player (14+ days)",
                PlayerSegment.Returning => "Previously churned, now returning",
                _ => "Unknown segment"
            };
        }

        /// <summary>
        /// Gets the monetization potential for a segment.
        /// </summary>
        public float GetSegmentMonetizationPotential(PlayerSegment segment)
        {
            return segment switch
            {
                PlayerSegment.Whale => 1.0f,
                PlayerSegment.HighValue => 0.8f,
                PlayerSegment.Engaged => 0.6f,
                PlayerSegment.Regular => 0.4f,
                PlayerSegment.Returning => 0.5f,
                PlayerSegment.NewPlayer => 0.3f,
                PlayerSegment.Casual => 0.2f,
                PlayerSegment.AtRisk => 0.1f,
                PlayerSegment.Churned => 0.05f,
                _ => 0f
            };
        }

        #endregion
    }

    #endregion
}
