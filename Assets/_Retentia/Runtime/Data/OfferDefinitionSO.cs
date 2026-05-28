// -----------------------------------------------------------------------------
// OfferDefinitionSO.cs
// ScriptableObject for defining dynamic offers and their trigger conditions.
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
    #region Offer Type Enum

    /// <summary>
    /// Types of offers that can be presented to players.
    /// </summary>
    public enum OfferType
    {
        Discount = 0,
        BonusCurrency = 1,
        FreeItem = 2,
        ExclusiveContent = 3,
        TimeExtension = 4,
        VIPAccess = 5,
        LimitedBundle = 6,
        WelcomeBack = 7,
        DifficultyAssist = 8,
        Custom = 99
    }

    #endregion

    #region Offer Trigger Condition

    /// <summary>
    /// Condition that must be met to trigger an offer.
    /// </summary>
    [Serializable]
    public class OfferTriggerCondition
    {
        #if ODIN_INSPECTOR
        [HorizontalGroup("Condition", 0.4f)]
        [LabelWidth(80)]
        #endif
        [Tooltip("Type of condition to check")]
        public TriggerType Type;

        #if ODIN_INSPECTOR
        [HorizontalGroup("Condition", 0.3f)]
        [LabelWidth(60)]
        #endif
        [Tooltip("Comparison operator")]
        public ComparisonOperator Operator;

        #if ODIN_INSPECTOR
        [HorizontalGroup("Condition", 0.3f)]
        [LabelWidth(50)]
        #endif
        [Tooltip("Value to compare against")]
        public float Value;

        public enum TriggerType
        {
            ChurnProbability,
            DaysSinceLastLogin,
            SessionCount,
            TotalSpent,
            WinRate,
            FrustrationScore,
            EngagementScore,
            LevelsCompleted,
            SessionDuration,
            AdsWatched,
            PlayerSegment
        }

        public enum ComparisonOperator
        {
            GreaterThan,
            LessThan,
            Equals,
            GreaterOrEqual,
            LessOrEqual,
            NotEquals
        }

        /// <summary>
        /// Evaluates the condition against a player profile.
        /// </summary>
        public bool Evaluate(UserProfile profile)
        {
            float actualValue = Type switch
            {
                TriggerType.ChurnProbability => profile.ChurnProbability,
                TriggerType.DaysSinceLastLogin => profile.DaysSinceLastLogin,
                TriggerType.SessionCount => profile.TotalSessions,
                TriggerType.TotalSpent => profile.TotalSpent,
                TriggerType.WinRate => profile.WinRatePercent,
                TriggerType.LevelsCompleted => profile.LevelsCompleted,
                TriggerType.AdsWatched => profile.AdsWatched,
                _ => 0f
            };

            return Operator switch
            {
                ComparisonOperator.GreaterThan => actualValue > Value,
                ComparisonOperator.LessThan => actualValue < Value,
                ComparisonOperator.Equals => Mathf.Approximately(actualValue, Value),
                ComparisonOperator.GreaterOrEqual => actualValue >= Value,
                ComparisonOperator.LessOrEqual => actualValue <= Value,
                ComparisonOperator.NotEquals => !Mathf.Approximately(actualValue, Value),
                _ => false
            };
        }
    }

    #endregion

    #region Offer Reward

    /// <summary>
    /// Reward given when an offer is accepted.
    /// </summary>
    [Serializable]
    public class OfferReward
    {
        #if ODIN_INSPECTOR
        [HorizontalGroup("Reward")]
        [LabelWidth(80)]
        #endif
        [Tooltip("Type of reward")]
        public RewardType Type;

        #if ODIN_INSPECTOR
        [HorizontalGroup("Reward")]
        [LabelWidth(50)]
        #endif
        [Tooltip("Reward amount")]
        public int Amount;

        #if ODIN_INSPECTOR
        [HorizontalGroup("Reward")]
        [LabelWidth(50)]
        #endif
        [Tooltip("Optional item ID")]
        public string ItemId;

        public enum RewardType
        {
            SoftCurrency,
            HardCurrency,
            Item,
            Experience,
            Energy,
            LivesRefill,
            Booster,
            TimeExtension,
            DiscountPercent,
            Custom
        }
    }

    #endregion

    #region Offer Definition ScriptableObject

    /// <summary>
    /// Defines a dynamic offer with trigger conditions, rewards, and display info.
    /// </summary>
    [CreateAssetMenu(fileName = "NewOffer", menuName = "Retentia AI/Offer Definition", order = 1)]
    public class OfferDefinitionSO : ScriptableObject
    {
        #region Identity

        #if ODIN_INSPECTOR
        [FoldoutGroup("Identity")]
        [Title("Offer Info")]
        #endif
        [Header("Identity")]
        [Tooltip("Unique identifier for this offer")]
        [SerializeField] private string _offerId;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Identity")]
        #endif
        [Tooltip("Display name shown to player")]
        [SerializeField] private string _displayName;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Identity")]
        [TextArea(2, 4)]
        #endif
        [Tooltip("Description shown to player")]
        [SerializeField] private string _description;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Identity")]
        #endif
        [Tooltip("Type of offer")]
        [SerializeField] private OfferType _offerType = OfferType.Discount;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Identity")]
        [PreviewField(50)]
        #endif
        [Tooltip("Icon to display")]
        [SerializeField] private Sprite _icon;

        #endregion

        #region Targeting

        #if ODIN_INSPECTOR
        [FoldoutGroup("Targeting")]
        [Title("Player Targeting")]
        #endif
        [Header("Targeting")]
        [Tooltip("Player segments this offer targets")]
        [SerializeField] private List<PlayerSegment> _targetSegments = new List<PlayerSegment>();

        #if ODIN_INSPECTOR
        [FoldoutGroup("Targeting")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [Tooltip("Conditions that must ALL be met")]
        [SerializeField] private List<OfferTriggerCondition> _conditions = new List<OfferTriggerCondition>();

        #if ODIN_INSPECTOR
        [FoldoutGroup("Targeting")]
        #endif
        [Tooltip("Priority when multiple offers are eligible (higher = more priority)")]
        [Range(0, 100)]
        [SerializeField] private int _priority = 50;

        #endregion

        #region Rewards

        #if ODIN_INSPECTOR
        [FoldoutGroup("Rewards")]
        [Title("Offer Rewards")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [Header("Rewards")]
        [Tooltip("Rewards given when offer is accepted")]
        [SerializeField] private List<OfferReward> _rewards = new List<OfferReward>();

        #if ODIN_INSPECTOR
        [FoldoutGroup("Rewards")]
        #endif
        [Tooltip("Price in soft currency (0 for free)")]
        [SerializeField] private int _priceSoftCurrency;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Rewards")]
        #endif
        [Tooltip("Price in hard currency (0 for free)")]
        [SerializeField] private int _priceHardCurrency;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Rewards")]
        #endif
        [Tooltip("Real money price (0 for in-game only)")]
        [SerializeField] private float _priceRealMoney;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Rewards")]
        #endif
        [Tooltip("Original price to show as discounted from")]
        [SerializeField] private float _originalPrice;

        #endregion

        #region Validity

        #if ODIN_INSPECTOR
        [FoldoutGroup("Validity")]
        [Title("Time Constraints")]
        #endif
        [Header("Validity")]
        [Tooltip("How long the offer is valid once shown (seconds, 0 = unlimited)")]
        [SerializeField] private float _validityDuration = 3600f;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Validity")]
        #endif
        [Tooltip("Maximum times this offer can be shown to a player")]
        [SerializeField] private int _maxShowCount = 3;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Validity")]
        #endif
        [Tooltip("Cooldown after being shown (seconds)")]
        [SerializeField] private float _showCooldown = 86400f;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Validity")]
        #endif
        [Tooltip("Start date for this offer (empty = always valid)")]
        [SerializeField] private string _validFromDate;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Validity")]
        #endif
        [Tooltip("End date for this offer (empty = never expires)")]
        [SerializeField] private string _validUntilDate;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Validity")]
        #endif
        [Tooltip("Is this offer currently active?")]
        [SerializeField] private bool _isActive = true;

        #endregion

        #region Display

        #if ODIN_INSPECTOR
        [FoldoutGroup("Display")]
        [Title("UI Settings")]
        #endif
        [Header("Display")]
        [Tooltip("Background color for the offer")]
        [SerializeField] private Color _backgroundColor = Color.white;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Display")]
        #endif
        [Tooltip("Accent color for highlights")]
        [SerializeField] private Color _accentColor = new Color(1f, 0.8f, 0f);

        #if ODIN_INSPECTOR
        [FoldoutGroup("Display")]
        #endif
        [Tooltip("Show urgency timer")]
        [SerializeField] private bool _showTimer = true;

        #if ODIN_INSPECTOR
        [FoldoutGroup("Display")]
        #endif
        [Tooltip("Play special animation on appear")]
        [SerializeField] private bool _playSpecialAnimation = false;

        #endregion

        #region Properties

        public string OfferId => string.IsNullOrEmpty(_offerId) ? name : _offerId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public OfferType OfferType => _offerType;
        public Sprite Icon => _icon;
        public IReadOnlyList<PlayerSegment> TargetSegments => _targetSegments;
        public IReadOnlyList<OfferTriggerCondition> Conditions => _conditions;
        public int Priority => _priority;
        public IReadOnlyList<OfferReward> Rewards => _rewards;
        public int PriceSoftCurrency => _priceSoftCurrency;
        public int PriceHardCurrency => _priceHardCurrency;
        public float PriceRealMoney => _priceRealMoney;
        public float OriginalPrice => _originalPrice;
        public float ValidityDuration => _validityDuration;
        public int MaxShowCount => _maxShowCount;
        public float ShowCooldown => _showCooldown;
        public bool IsActive => _isActive;
        public Color BackgroundColor => _backgroundColor;
        public Color AccentColor => _accentColor;
        public bool ShowTimer => _showTimer;
        public bool PlaySpecialAnimation => _playSpecialAnimation;

        /// <summary>
        /// Gets the discount percentage if applicable.
        /// </summary>
        public int DiscountPercent
        {
            get
            {
                if (_originalPrice <= 0 || _priceRealMoney <= 0) return 0;
                return Mathf.RoundToInt((1f - (_priceRealMoney / _originalPrice)) * 100f);
            }
        }

        /// <summary>
        /// Gets whether this offer is free.
        /// </summary>
        public bool IsFree => _priceSoftCurrency == 0 && _priceHardCurrency == 0 && _priceRealMoney <= 0;

        #endregion

        #region Methods

        /// <summary>
        /// Checks if this offer is valid for the given date.
        /// </summary>
        public bool IsValidForDate(DateTime date)
        {
            if (!_isActive) return false;

            if (!string.IsNullOrEmpty(_validFromDate))
            {
                if (DateTime.TryParse(_validFromDate, out var fromDate) && date < fromDate)
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(_validUntilDate))
            {
                if (DateTime.TryParse(_validUntilDate, out var untilDate) && date > untilDate)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if this offer is eligible for the given player profile.
        /// </summary>
        public bool IsEligible(UserProfile profile, PlayerSegment segment)
        {
            if (!_isActive) return false;

            // Check segment targeting
            if (_targetSegments.Count > 0 && !_targetSegments.Contains(segment))
            {
                return false;
            }

            // Check all conditions
            foreach (var condition in _conditions)
            {
                if (!condition.Evaluate(profile))
                {
                    return false;
                }
            }

            return true;
        }

        public void SetupMock(string offerId, string displayName, string description, float price)
        {
            _offerId = offerId;
            _displayName = displayName;
            _description = description;
            _priceRealMoney = price;
            _priceSoftCurrency = 0;
            _priceHardCurrency = 0;
            _backgroundColor = new Color(0.2f, 0.15f, 0.3f);
            _showTimer = true;
            _validityDuration = 300f; // 5 min
            _isActive = true;
        }

        #endregion

        #region Validation

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_offerId))
            {
                _offerId = name;
            }
        }
        #endif

        #endregion
    }

    #endregion
}
