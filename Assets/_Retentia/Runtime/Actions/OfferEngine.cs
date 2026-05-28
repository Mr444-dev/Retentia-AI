// -----------------------------------------------------------------------------
// OfferEngine.cs
// Dynamic offer selection and management engine.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Retentia.Data;
using UnityEngine;

namespace Retentia.Core
{
    #region Offer Presentation Result

    /// <summary>
    /// Result of offer selection.
    /// </summary>
    [Serializable]
    public struct OfferPresentationResult
    {
        public bool HasOffer;
        public OfferDefinitionSO Offer;
        public string Reason;
        public float Score;
        public DateTime ExpiresAt;
    }

    #endregion

    #region Offer History Entry

    /// <summary>
    /// Tracks offer presentation history.
    /// </summary>
    [Serializable]
    public class OfferHistoryEntry
    {
        public string OfferId;
        public DateTime PresentedAt;
        public bool Accepted;
        public bool Declined;
        public int ShowCount;
    }

    #endregion

    #region Offer Engine

    /// <summary>
    /// Engine for selecting and presenting dynamic offers to players.
    /// Evaluates player state and selects most appropriate offers.
    /// </summary>
    public class OfferEngine : IRetentiaService
    {
        #region Fields

        private readonly RetentionConfigSO _config;
        private readonly List<OfferDefinitionSO> _availableOffers;
        private readonly Dictionary<string, OfferHistoryEntry> _offerHistory;
        private readonly PlayerSegmentationService _segmentationService;
        private bool _isInitialized;
        private int _offersShownThisSession;
        private DateTime _lastOfferTime;

        #endregion

        #region Properties

        public bool IsInitialized => _isInitialized;
        public int InitializationPriority => 40;
        public int OffersShownThisSession => _offersShownThisSession;
        public int AvailableOfferCount => _availableOffers.Count;

        #endregion

        #region Constructor

        public OfferEngine(RetentionConfigSO config = null, PlayerSegmentationService segmentationService = null)
        {
            _config = config;
            _segmentationService = segmentationService;
            _availableOffers = new List<OfferDefinitionSO>();
            _offerHistory = new Dictionary<string, OfferHistoryEntry>();
        }

        #endregion

        #region IRetentiaService Implementation

        public async UniTask InitializeAsync()
        {
            DebugLogger.LogServiceInit("OfferEngine");

            // Load all offer definitions from Resources
            var offers = Resources.LoadAll<OfferDefinitionSO>("Offers");
            _availableOffers.AddRange(offers);

            DebugLogger.Info($"[OfferEngine] Loaded {_availableOffers.Count} offer definitions.");

            _offersShownThisSession = 0;
            _lastOfferTime = DateTime.MinValue;
            _isInitialized = true;

            DebugLogger.LogServiceReady("OfferEngine");
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            DebugLogger.LogServiceDispose("OfferEngine");
            _availableOffers.Clear();
            _offerHistory.Clear();
            _isInitialized = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers an offer definition.
        /// </summary>
        public void RegisterOffer(OfferDefinitionSO offer)
        {
            if (offer != null && !_availableOffers.Contains(offer))
            {
                _availableOffers.Add(offer);
                DebugLogger.Debug($"[OfferEngine] Registered offer: {offer.DisplayName}");
            }
        }

        /// <summary>
        /// Selects the best offer for the given player.
        /// </summary>
        public OfferPresentationResult SelectBestOffer(UserProfile profile, PlayerSessionData session = null)
        {
            var result = new OfferPresentationResult { HasOffer = false };

            // Check session limits
            var maxOffers = _config?.MaxOffersPerSession ?? 3;
            if (_offersShownThisSession >= maxOffers)
            {
                result.Reason = "Maximum offers per session reached.";
                return result;
            }

            // Check cooldown
            var minInterval = _config?.MinOfferInterval ?? 300f;
            if ((DateTime.UtcNow - _lastOfferTime).TotalSeconds < minInterval)
            {
                result.Reason = "Offer cooldown active.";
                return result;
            }

            // Get player segment
            var segment = PlayerSegment.Unknown;
            if (_segmentationService != null)
            {
                var segResult = _segmentationService.SegmentPlayer(profile);
                segment = segResult.PrimarySegment;
            }
            else
            {
                segment = (PlayerSegment)profile.PlayerSegmentId;
            }

            // Score and filter offers
            var scoredOffers = new List<(OfferDefinitionSO offer, float score)>();

            foreach (var offer in _availableOffers)
            {
                if (!IsOfferEligible(offer, profile, segment))
                {
                    continue;
                }

                var score = ScoreOffer(offer, profile, segment, session);
                if (score > 0)
                {
                    scoredOffers.Add((offer, score));
                }
            }

            if (scoredOffers.Count == 0)
            {
                result.Reason = "No eligible offers available.";
                return result;
            }

            // Select highest scoring offer
            scoredOffers.Sort((a, b) => b.score.CompareTo(a.score));
            var selectedOffer = scoredOffers[0];

            result.HasOffer = true;
            result.Offer = selectedOffer.offer;
            result.Score = selectedOffer.score;
            result.Reason = "Offer selected based on player profile.";
            result.ExpiresAt = DateTime.UtcNow.AddSeconds(selectedOffer.offer.ValidityDuration);

            return result;
        }

        /// <summary>
        /// Presents the selected offer to the player.
        /// </summary>
        public void PresentOffer(OfferDefinitionSO offer, string userId)
        {
            _offersShownThisSession++;
            _lastOfferTime = DateTime.UtcNow;

            // Update history
            if (!_offerHistory.TryGetValue(offer.OfferId, out var history))
            {
                history = new OfferHistoryEntry { OfferId = offer.OfferId };
                _offerHistory[offer.OfferId] = history;
            }
            history.PresentedAt = DateTime.UtcNow;
            history.ShowCount++;

            // Fire event
            RetentiaEventBus.Publish(new OfferPresentedEvent(userId, offer.OfferId, offer.DisplayName));

            DebugLogger.Info($"[OfferEngine] Presented offer: {offer.DisplayName} to {userId}");
        }

        /// <summary>
        /// Records offer acceptance.
        /// </summary>
        public void AcceptOffer(string offerId, string userId)
        {
            if (_offerHistory.TryGetValue(offerId, out var history))
            {
                history.Accepted = true;
            }

            RetentiaEventBus.Publish(new OfferResponseEvent(userId, offerId, true));

            DebugLogger.Info($"[OfferEngine] Offer accepted: {offerId}");
        }

        /// <summary>
        /// Records offer decline.
        /// </summary>
        public void DeclineOffer(string offerId, string userId)
        {
            if (_offerHistory.TryGetValue(offerId, out var history))
            {
                history.Declined = true;
            }

            RetentiaEventBus.Publish(new OfferResponseEvent(userId, offerId, false));

            DebugLogger.Info($"[OfferEngine] Offer declined: {offerId}");
        }

        /// <summary>
        /// Resets session state.
        /// </summary>
        public void ResetSession()
        {
            _offersShownThisSession = 0;
            _lastOfferTime = DateTime.MinValue;
        }

        #endregion

        #region Private Methods

        private bool IsOfferEligible(OfferDefinitionSO offer, UserProfile profile, PlayerSegment segment)
        {
            // Check if offer is active
            if (!offer.IsActive)
            {
                return false;
            }

            // Check date validity
            if (!offer.IsValidForDate(DateTime.UtcNow))
            {
                return false;
            }

            // Check eligibility conditions
            if (!offer.IsEligible(profile, segment))
            {
                return false;
            }

            // Check show count limit
            if (_offerHistory.TryGetValue(offer.OfferId, out var history))
            {
                if (history.ShowCount >= offer.MaxShowCount)
                {
                    return false;
                }

                // Check cooldown after decline
                if (history.Declined)
                {
                    var cooldown = _config?.DeclinedOfferCooldown ?? 600f;
                    if ((DateTime.UtcNow - history.PresentedAt).TotalSeconds < cooldown)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private float ScoreOffer(OfferDefinitionSO offer, UserProfile profile, PlayerSegment segment, PlayerSessionData session)
        {
            float score = offer.Priority;

            // Bonus for segment targeting
            if (offer.TargetSegments.Contains(segment))
            {
                score += 20f;
            }

            // Bonus for at-risk players
            if (segment == PlayerSegment.AtRisk && profile.ChurnProbability > 0.5f)
            {
                score += 30f;
            }

            // Bonus for high-value players
            if (segment == PlayerSegment.Whale || segment == PlayerSegment.HighValue)
            {
                score += 15f;
            }

            // Bonus for frustrated players (difficulty assist offers)
            if (session != null && session.FrustrationScore > 50f && offer.OfferType == OfferType.DifficultyAssist)
            {
                score += 25f;
            }

            // Bonus for returning players
            if (segment == PlayerSegment.Returning && offer.OfferType == OfferType.WelcomeBack)
            {
                score += 40f;
            }

            // Penalty for recently shown
            if (_offerHistory.TryGetValue(offer.OfferId, out var history))
            {
                score -= history.ShowCount * 10f;
            }

            return Mathf.Max(score, 0f);
        }

        #endregion
    }

    #endregion
}
