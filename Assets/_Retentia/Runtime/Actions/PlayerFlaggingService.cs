// -----------------------------------------------------------------------------
// PlayerFlaggingService.cs
// Service for flagging at-risk and special players.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Linq;
using Retentia.Data;
using UnityEngine;

namespace Retentia.Core
{
    #region Player Flag Entry

    /// <summary>
    /// An active flag on a player.
    /// </summary>
    [Serializable]
    public class PlayerFlagEntry
    {
        public string UserId;
        public FlagType Type;
        public string RuleId;
        public string Reason;
        public DateTime AppliedAt;
        public DateTime? ExpiresAt;
        public FlagPriority Priority;
        public bool IsActive;

        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }

    #endregion

    #region Player Flagging Service

    /// <summary>
    /// Service for applying and managing player flags based on rules.
    /// Integrates with offer engine for triggered actions.
    /// </summary>
    public class PlayerFlaggingService : IRetentiaService
    {
        #region Fields

        private readonly RetentionConfigSO _config;
        private readonly List<PlayerFlagRuleSO> _rules;
        private readonly Dictionary<string, List<PlayerFlagEntry>> _activeFlags;
        private readonly Dictionary<string, DateTime> _ruleCooldowns;
        private readonly OfferEngine _offerEngine;
        private bool _isInitialized;

        #endregion

        #region Properties

        public bool IsInitialized => _isInitialized;
        public int InitializationPriority => 45;
        public int RuleCount => _rules.Count;
        public int ActiveFlagCount => GetTotalActiveFlags();

        #endregion

        #region Constructor

        public PlayerFlaggingService(RetentionConfigSO config = null, OfferEngine offerEngine = null)
        {
            _config = config;
            _offerEngine = offerEngine;
            _rules = new List<PlayerFlagRuleSO>();
            _activeFlags = new Dictionary<string, List<PlayerFlagEntry>>();
            _ruleCooldowns = new Dictionary<string, DateTime>();
        }

        #endregion

        #region IRetentiaService Implementation

        public async UniTask InitializeAsync()
        {
            DebugLogger.LogServiceInit("PlayerFlaggingService");

            // Load flag rules from Resources
            var rules = Resources.LoadAll<PlayerFlagRuleSO>("FlagRules");
            _rules.AddRange(rules);

            DebugLogger.Info($"[PlayerFlaggingService] Loaded {_rules.Count} flag rules.");

            _isInitialized = true;
            DebugLogger.LogServiceReady("PlayerFlaggingService");
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            DebugLogger.LogServiceDispose("PlayerFlaggingService");
            _rules.Clear();
            _activeFlags.Clear();
            _ruleCooldowns.Clear();
            _isInitialized = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a flag rule.
        /// </summary>
        public void RegisterRule(PlayerFlagRuleSO rule)
        {
            if (rule != null && !_rules.Contains(rule))
            {
                _rules.Add(rule);
                DebugLogger.Debug($"[PlayerFlaggingService] Registered rule: {rule.RuleName}");
            }
        }

        /// <summary>
        /// Evaluates all rules for a player and applies flags.
        /// </summary>
        public async UniTask<List<PlayerFlagEntry>> EvaluatePlayerAsync(UserProfile profile, PlayerSessionData session = null)
        {
            var appliedFlags = new List<PlayerFlagEntry>();

            foreach (var rule in _rules)
            {
                if (!rule.IsActive)
                {
                    continue;
                }

                // Check rule cooldown
                if (IsRuleOnCooldown(rule.RuleId, profile.UserId))
                {
                    continue;
                }

                // Evaluate rule
                if (rule.Evaluate(profile, session))
                {
                    var flag = ApplyFlag(profile.UserId, rule);
                    if (flag != null)
                    {
                        appliedFlags.Add(flag);
                        await ExecuteActionsAsync(rule, profile);
                    }
                }
            }

            // Clean up expired flags
            CleanupExpiredFlags(profile.UserId);

            return appliedFlags;
        }

        /// <summary>
        /// Manually applies a flag to a player.
        /// </summary>
        public PlayerFlagEntry ApplyManualFlag(string userId, FlagType type, string reason, float? durationSeconds = null)
        {
            var flag = new PlayerFlagEntry
            {
                UserId = userId,
                Type = type,
                RuleId = "MANUAL",
                Reason = reason,
                AppliedAt = DateTime.UtcNow,
                ExpiresAt = durationSeconds.HasValue ? DateTime.UtcNow.AddSeconds(durationSeconds.Value) : null,
                Priority = FlagPriority.Medium,
                IsActive = true
            };

            AddFlag(userId, flag);

            RetentiaEventBus.Publish(new PlayerFlaggedEvent(userId, type.ToString(), reason));

            return flag;
        }

        /// <summary>
        /// Removes a specific flag from a player.
        /// </summary>
        public bool RemoveFlag(string userId, FlagType type)
        {
            if (!_activeFlags.TryGetValue(userId, out var flags))
            {
                return false;
            }

            var removed = flags.RemoveAll(f => f.Type == type);
            return removed > 0;
        }

        /// <summary>
        /// Removes all flags from a player.
        /// </summary>
        public void ClearFlags(string userId)
        {
            if (_activeFlags.ContainsKey(userId))
            {
                _activeFlags[userId].Clear();
            }
        }

        /// <summary>
        /// Gets all active flags for a player.
        /// </summary>
        public List<PlayerFlagEntry> GetActiveFlags(string userId)
        {
            if (!_activeFlags.TryGetValue(userId, out var flags))
            {
                return new List<PlayerFlagEntry>();
            }

            return flags.Where(f => f.IsActive && !f.IsExpired).ToList();
        }

        /// <summary>
        /// Checks if a player has a specific flag type.
        /// </summary>
        public bool HasFlag(string userId, FlagType type)
        {
            if (!_activeFlags.TryGetValue(userId, out var flags))
            {
                return false;
            }

            return flags.Any(f => f.Type == type && f.IsActive && !f.IsExpired);
        }

        /// <summary>
        /// Gets the highest priority flag for a player.
        /// </summary>
        public PlayerFlagEntry GetHighestPriorityFlag(string userId)
        {
            var flags = GetActiveFlags(userId);
            return flags.OrderByDescending(f => f.Priority).FirstOrDefault();
        }

        #endregion

        #region Private Methods

        private PlayerFlagEntry ApplyFlag(string userId, PlayerFlagRuleSO rule)
        {
            // Check if flag already exists
            if (HasFlag(userId, rule.FlagType))
            {
                return null;
            }

            var flag = new PlayerFlagEntry
            {
                UserId = userId,
                Type = rule.FlagType,
                RuleId = rule.RuleId,
                Reason = rule.Description,
                AppliedAt = DateTime.UtcNow,
                ExpiresAt = rule.FlagDuration > 0 ? DateTime.UtcNow.AddSeconds(rule.FlagDuration) : null,
                Priority = rule.Priority,
                IsActive = true
            };

            AddFlag(userId, flag);

            // Set rule cooldown
            var cooldownKey = $"{rule.RuleId}_{userId}";
            _ruleCooldowns[cooldownKey] = DateTime.UtcNow.AddSeconds(rule.ApplyCooldown);

            // Publish event
            RetentiaEventBus.Publish(new PlayerFlaggedEvent(userId, rule.FlagType.ToString(), rule.Description));

            DebugLogger.Info($"[PlayerFlaggingService] Applied flag {rule.FlagType} to {userId}");

            return flag;
        }

        private void AddFlag(string userId, PlayerFlagEntry flag)
        {
            if (!_activeFlags.TryGetValue(userId, out var flags))
            {
                flags = new List<PlayerFlagEntry>();
                _activeFlags[userId] = flags;
            }

            flags.Add(flag);
        }

        private bool IsRuleOnCooldown(string ruleId, string userId)
        {
            var cooldownKey = $"{ruleId}_{userId}";
            if (_ruleCooldowns.TryGetValue(cooldownKey, out var cooldownEnd))
            {
                return DateTime.UtcNow < cooldownEnd;
            }
            return false;
        }

        private async UniTask ExecuteActionsAsync(PlayerFlagRuleSO rule, UserProfile profile)
        {
            foreach (var action in rule.Actions)
            {
                try
                {
                    switch (action.Action)
                    {
                        case FlagAction.LogOnly:
                            DebugLogger.Info($"[Flag Action] {rule.FlagType}: {action.Parameter}");
                            break;

                        case FlagAction.SendNotification:
                            DebugLogger.Info($"[Flag Action] Send notification: {action.Parameter}");
                            // Would integrate with notification service
                            break;

                        case FlagAction.TriggerOffer:
                            if (action.OfferToTrigger != null && _offerEngine != null)
                            {
                                _offerEngine.PresentOffer(action.OfferToTrigger, profile.UserId);
                            }
                            break;

                        case FlagAction.AdjustDifficulty:
                            DebugLogger.Info($"[Flag Action] Adjust difficulty by: {action.Parameter}");
                            // Would integrate with game difficulty system
                            break;

                        case FlagAction.TrackAnalytics:
                            if (RetentiaServiceLocator.TryGet<IAnalyticsProvider>(out var analytics))
                            {
                                analytics.TrackEvent(
                                    "player_flagged",
                                    new Dictionary<string, object>
                                    {
                                        ["flag_type"] = rule.FlagType.ToString(),
                                        ["rule_id"] = rule.RuleId,
                                        ["user_id"] = profile.UserId
                                    }
                                );
                            }
                            break;

                        case FlagAction.GrantReward:
                            DebugLogger.Info($"[Flag Action] Grant reward: {action.Parameter}");
                            // Would integrate with reward system
                            break;
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.Exception(ex, $"[PlayerFlaggingService] Action execution failed: {action.Action}");
                }

                await UniTask.Yield();
            }
        }

        private void CleanupExpiredFlags(string userId)
        {
            if (_activeFlags.TryGetValue(userId, out var flags))
            {
                flags.RemoveAll(f => f.IsExpired);
            }
        }

        private int GetTotalActiveFlags()
        {
            int count = 0;
            foreach (var flags in _activeFlags.Values)
            {
                count += flags.Count(f => f.IsActive && !f.IsExpired);
            }
            return count;
        }

        #endregion
    }

    #endregion
}
