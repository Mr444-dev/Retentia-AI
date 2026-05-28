// -----------------------------------------------------------------------------
// PlayerFlagRuleSO.cs
// ScriptableObject for defining player flagging rules.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using Retentia.Core;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Retentia.Data
{
    #region Flag Type Enum

    /// <summary>
    /// Types of flags that can be applied to players.
    /// </summary>
    public enum FlagType
    {
        None = 0,
        AtRisk = 1,
        HighValue = 2,
        Frustrated = 3,
        NetworkIssues = 4,
        DifficultyStrugging = 5,
        Engaged = 6,
        Whale = 7,
        NewPlayer = 8,
        Returning = 9,
        Inactive = 10,
        VIP = 11,
        Cheater = 12,
        Support = 13,
        Custom = 99
    }

    #endregion

    #region Flag Action Enum

    /// <summary>
    /// Actions that can be triggered when a flag is applied.
    /// </summary>
    public enum FlagAction
    {
        None = 0,
        LogOnly = 1,
        SendNotification = 2,
        TriggerOffer = 3,
        AdjustDifficulty = 4,
        SendToSupport = 5,
        GrantReward = 6,
        TrackAnalytics = 7,
        WebhookNotify = 8,
        Custom = 99
    }

    #endregion

    #region Flag Priority Enum

    /// <summary>
    /// Priority level for flags.
    /// </summary>
    public enum FlagPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    #endregion

    #region Flag Rule Condition

    /// <summary>
    /// Condition that triggers a flag.
    /// </summary>
    [Serializable]
    public class FlagRuleCondition
    {
        #if ODIN_INSPECTOR
        [HorizontalGroup("Condition", 0.35f)]
        [LabelWidth(60)]
        #endif
        [Tooltip("Metric to evaluate")]
        public MetricType Metric;

        #if ODIN_INSPECTOR
        [HorizontalGroup("Condition", 0.25f)]
        [LabelWidth(55)]
        #endif
        [Tooltip("Comparison operator")]
        public ComparisonOp Operator;

        #if ODIN_INSPECTOR
        [HorizontalGroup("Condition", 0.2f)]
        [LabelWidth(45)]
        #endif
        [Tooltip("Threshold value")]
        public float Value;

        #if ODIN_INSPECTOR
        [HorizontalGroup("Condition", 0.2f)]
        [LabelWidth(55)]
        #endif
        [Tooltip("Weight for scoring (0-1)")]
        [Range(0f, 1f)]
        public float Weight = 1f;

        public enum MetricType
        {
            ChurnProbability,
            DaysSinceLastLogin,
            TotalSessions,
            AvgSessionDuration,
            TotalSpent,
            WinRate,
            FrustrationScore,
            EngagementScore,
            LevelsCompleted,
            LevelsFailed,
            RetryCount,
            AvgLatencyMs,
            DisconnectCount,
            SupportTickets,
            AdsWatched,
            SessionDuration
        }

        public enum ComparisonOp
        {
            GreaterThan,
            LessThan,
            Equals,
            GreaterOrEqual,
            LessOrEqual,
            Between,
            NotBetween
        }

        /// <summary>
        /// Upper bound for Between/NotBetween operators.
        /// </summary>
        #if ODIN_INSPECTOR
        [ShowIf("@Operator == ComparisonOp.Between || Operator == ComparisonOp.NotBetween")]
        #endif
        public float ValueMax;

        /// <summary>
        /// Evaluates this condition against a player profile.
        /// </summary>
        public bool Evaluate(UserProfile profile, PlayerSessionData session = null)
        {
            float actualValue = GetMetricValue(profile, session);

            return Operator switch
            {
                ComparisonOp.GreaterThan => actualValue > Value,
                ComparisonOp.LessThan => actualValue < Value,
                ComparisonOp.Equals => Mathf.Approximately(actualValue, Value),
                ComparisonOp.GreaterOrEqual => actualValue >= Value,
                ComparisonOp.LessOrEqual => actualValue <= Value,
                ComparisonOp.Between => actualValue >= Value && actualValue <= ValueMax,
                ComparisonOp.NotBetween => actualValue < Value || actualValue > ValueMax,
                _ => false
            };
        }

        /// <summary>
        /// Gets the metric value from profile/session.
        /// </summary>
        private float GetMetricValue(UserProfile profile, PlayerSessionData session)
        {
            return Metric switch
            {
                MetricType.ChurnProbability => profile.ChurnProbability,
                MetricType.DaysSinceLastLogin => profile.DaysSinceLastLogin,
                MetricType.TotalSessions => profile.TotalSessions,
                MetricType.AvgSessionDuration => profile.AvgSessionDuration,
                MetricType.TotalSpent => profile.TotalSpent,
                MetricType.WinRate => profile.WinRatePercent,
                MetricType.LevelsCompleted => profile.LevelsCompleted,
                MetricType.SupportTickets => profile.SupportTicketsOpen,
                MetricType.AdsWatched => profile.AdsWatched,
                MetricType.FrustrationScore => session?.FrustrationScore ?? 0f,
                MetricType.EngagementScore => session?.EngagementScore ?? 0f,
                MetricType.LevelsFailed => session?.LevelsFailed ?? 0,
                MetricType.RetryCount => session?.RetryCount ?? 0,
                MetricType.AvgLatencyMs => session?.AvgLatencyMs ?? 0f,
                MetricType.DisconnectCount => session?.DisconnectCount ?? 0,
                MetricType.SessionDuration => session?.SessionDurationSeconds ?? 0f,
                _ => 0f
            };
        }
    }

    #endregion

    #region Flag Triggered Action

    /// <summary>
    /// Action to execute when a flag is applied.
    /// </summary>
    [Serializable]
    public class FlagTriggeredAction
    {
        #if ODIN_INSPECTOR
        [HorizontalGroup("Action", 0.4f)]
        [LabelWidth(60)]
        #endif
        [Tooltip("Action type to execute")]
        public FlagAction Action;

        #if ODIN_INSPECTOR
        [HorizontalGroup("Action", 0.6f)]
        [LabelWidth(80)]
        #endif
        [Tooltip("Action parameter (offer ID, reward ID, etc.)")]
        public string Parameter;

        #if ODIN_INSPECTOR
        [ShowIf("Action", FlagAction.TriggerOffer)]
        #endif
        [Tooltip("Reference to offer to trigger")]
        public OfferDefinitionSO OfferToTrigger;
    }

    #endregion

    #region Player Flag Rule ScriptableObject

    /// <summary>
    /// Defines a player flagging rule with conditions and triggered actions.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFlagRule", menuName = "Retentia AI/Flag Rule", order = 2)]
    public class PlayerFlagRuleSO : ScriptableObject
    {
        #region Identity

        #if ODIN_INSPECTOR
        [FoldoutGroup("Identity")]
        [Title("Rule Info")]
        #endif
        [Header("Identity")]
        [Tooltip("Unique identifier for this rule")]
        [SerializeField] private string _ruleId;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Identity")]
        #endif
        [Tooltip("Display name for this rule")]
        [SerializeField] private string _ruleName;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Identity")]
        [TextArea(2, 3)]
        #endif
        [Tooltip("Description of what this rule detects")]
        [SerializeField] private string _description;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Identity")]
        #endif
        [Tooltip("Flag type to apply when conditions are met")]
        [SerializeField] private FlagType _flagType = FlagType.AtRisk;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Identity")]
        #endif
        [Tooltip("Priority of this flag")]
        [SerializeField] private FlagPriority _priority = FlagPriority.Medium;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Identity")]
        #endif
        [Tooltip("Is this rule currently active?")]
        [SerializeField] private bool _isActive = true;

        #endregion

        #region Conditions

        #if ODIN_INSPECTOR
        [FoldoutGroup("Conditions")]
        [Title("Trigger Conditions")]
        #endif
        [Header("Conditions")]
        [Tooltip("How conditions are combined")]
        [SerializeField] private ConditionMode _conditionMode = ConditionMode.All;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Conditions")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [Tooltip("Conditions to evaluate")]
        [SerializeField] private List<FlagRuleCondition> _conditions = new List<FlagRuleCondition>();

        #if ODIN_INSPECTOR
        [FoldoutGroup("Conditions")]
        #endif
        [Tooltip("Minimum weighted score for scoring mode (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _minScoreThreshold = 0.7f;

        public enum ConditionMode
        {
            /// <summary>All conditions must pass.</summary>
            All,
            /// <summary>Any condition must pass.</summary>
            Any,
            /// <summary>Use weighted scoring.</summary>
            Weighted
        }

        #endregion

        #region Actions

        #if ODIN_INSPECTOR
        [FoldoutGroup("Actions")]
        [Title("Triggered Actions")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [Header("Actions")]
        [Tooltip("Actions to execute when flag is applied")]
        [SerializeField] private List<FlagTriggeredAction> _actions = new List<FlagTriggeredAction>();

        #endregion

        #region Timing

        #if ODIN_INSPECTOR
        [FoldoutGroup("Timing")]
        [Title("Evaluation Timing")]
        #endif
        [Header("Timing")]
        [Tooltip("How often to evaluate this rule (seconds)")]
        [SerializeField] private float _evaluationInterval = 60f;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Timing")]
        #endif
        [Tooltip("Cooldown after flag is applied (seconds)")]
        [SerializeField] private float _applyCooldown = 3600f;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Timing")]
        #endif
        [Tooltip("How long the flag remains active (seconds, 0 = until manually removed)")]
        [SerializeField] private float _flagDuration = 0f;

        #endregion

        #region Properties

        public string RuleId => string.IsNullOrEmpty(_ruleId) ? name : _ruleId;
        public string RuleName => _ruleName;
        public string Description => _description;
        public FlagType FlagType => _flagType;
        public FlagPriority Priority => _priority;
        public bool IsActive => _isActive;
        public ConditionMode Mode => _conditionMode;
        public IReadOnlyList<FlagRuleCondition> Conditions => _conditions;
        public float MinScoreThreshold => _minScoreThreshold;
        public IReadOnlyList<FlagTriggeredAction> Actions => _actions;
        public float EvaluationInterval => _evaluationInterval;
        public float ApplyCooldown => _applyCooldown;
        public float FlagDuration => _flagDuration;

        #endregion

        #region Methods

        /// <summary>
        /// Evaluates if this rule should apply to the given player.
        /// </summary>
        /// <param name="profile">Player profile to evaluate.</param>
        /// <param name="session">Optional current session data.</param>
        /// <returns>True if the flag should be applied.</returns>
        public bool Evaluate(UserProfile profile, PlayerSessionData session = null)
        {
            if (!_isActive || _conditions.Count == 0)
            {
                return false;
            }

            switch (_conditionMode)
            {
                case ConditionMode.All:
                    foreach (var condition in _conditions)
                    {
                        if (!condition.Evaluate(profile, session))
                        {
                            return false;
                        }
                    }
                    return true;

                case ConditionMode.Any:
                    foreach (var condition in _conditions)
                    {
                        if (condition.Evaluate(profile, session))
                        {
                            return true;
                        }
                    }
                    return false;

                case ConditionMode.Weighted:
                    return CalculateScore(profile, session) >= _minScoreThreshold;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Calculates the weighted score for this rule.
        /// </summary>
        public float CalculateScore(UserProfile profile, PlayerSessionData session = null)
        {
            if (_conditions.Count == 0) return 0f;

            float totalWeight = 0f;
            float weightedSum = 0f;

            foreach (var condition in _conditions)
            {
                totalWeight += condition.Weight;
                if (condition.Evaluate(profile, session))
                {
                    weightedSum += condition.Weight;
                }
            }

            return totalWeight > 0 ? weightedSum / totalWeight : 0f;
        }

        #endregion

        #region Validation

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_ruleId))
            {
                _ruleId = name;
            }
            if (string.IsNullOrEmpty(_ruleName))
            {
                _ruleName = name;
            }
        }
        #endif

        #endregion
    }

    #endregion
}
