// -----------------------------------------------------------------------------
// RetentiaDemoSimulator.cs
// Interactive simulation manager for showing off Retentia AI features.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Retentia.Data;
using Retentia.Experiments;

namespace Retentia.Core
{
    public enum PlayerPersona
    {
        CasualWatcher,
        WhaleSpender,
        FrustratedRookie,
        BoredVeteran
    }

    /// <summary>
    /// Simulates player telemetry in Unity to demonstrate the end-to-end AI workflow.
    /// Exposes a GUI overlay to control the active persona and trigger telemetry events.
    /// </summary>
    public class RetentiaDemoSimulator : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Simulation Configuration")]
        [SerializeField] private PlayerPersona _selectedPersona = PlayerPersona.CasualWatcher;
        [SerializeField] private float _actionIntervalSeconds = 3.0f;
        [SerializeField] private bool _runAutomatically = true;

        [Header("UI Reference")]
        [SerializeField] private OfferPopupController _offerPopup;

        #endregion

        #region Private Fields

        private UserProfile _activeProfile;
        private bool _isInitialized;
        private PlayerPersona _lastPersona;
        private float _timer;
        private string _lastStatus = "Starting...";
        private readonly List<string> _commandLog = new List<string>();

        #endregion

        #region Properties

        public UserProfile ActiveProfile => _activeProfile;
        public PlayerPersona SelectedPersona => _selectedPersona;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Wait for bootstrapper to be ready before initializing profile
            InitializeSimulator();
        }

        private void Update()
        {
            if (!_isInitialized) return;

            // Handle persona change in inspector
            if (_selectedPersona != _lastPersona)
            {
                ApplyPersonaTemplate(_selectedPersona);
            }

            // Automatic simulation loop
            if (_runAutomatically)
            {
                _timer += Time.deltaTime;
                if (_timer >= _actionIntervalSeconds)
                {
                    _timer = 0f;
                    SimulatePlayerAction().Forget();
                }
            }
        }

        #endregion

        #region Initialization

        private void InitializeSimulator()
        {
            // Subscribe to event bus for remote commands
            RetentiaEventBus.Subscribe<OfferPresentedEvent>(OnOfferPresented);

            // Generate initial profile
            ApplyPersonaTemplate(_selectedPersona);
            _lastPersona = _selectedPersona;
            _isInitialized = true;
            _lastStatus = "Simulator Ready.";

            DebugLogger.Info($"[Simulator] Started simulation with persona: {_selectedPersona}");
        }

        private void OnDestroy()
        {
            RetentiaEventBus.Unsubscribe<OfferPresentedEvent>(OnOfferPresented);
        }

        #endregion

        #region Persona Management

        private void ApplyPersonaTemplate(PlayerPersona persona)
        {
            _selectedPersona = persona;
            _lastPersona = persona;

            _activeProfile = new UserProfile
            {
                UserId = $"Sim_{persona}_{UnityEngine.Random.Range(1000, 9999)}",
                DaysSinceLastLogin = 0,
                PlatformId = UnityEngine.Random.Range(0, 2), // Android or iOS
                DifficultyScore = 1.0f,
                LatencyAvgMs = UnityEngine.Random.Range(30f, 90f)
            };

            switch (persona)
            {
                case PlayerPersona.CasualWatcher:
                    _activeProfile.TotalSessions = 8;
                    _activeProfile.AvgSessionDuration = 5.2f;
                    _activeProfile.TotalSpent = 0f;
                    _activeProfile.SupportTicketsOpen = 0;
                    _activeProfile.AdsWatched = 15;
                    _activeProfile.LevelsCompleted = 6;
                    _activeProfile.SocialShares = 2;
                    _activeProfile.WinRatePercent = 75;
                    _activeProfile.ChurnProbability = 0.25f;
                    break;

                case PlayerPersona.WhaleSpender:
                    _activeProfile.TotalSessions = 65;
                    _activeProfile.AvgSessionDuration = 22.5f;
                    _activeProfile.TotalSpent = 389.50f;
                    _activeProfile.SupportTicketsOpen = 1;
                    _activeProfile.AdsWatched = 2;
                    _activeProfile.LevelsCompleted = 84;
                    _activeProfile.SocialShares = 12;
                    _activeProfile.WinRatePercent = 82;
                    _activeProfile.ChurnProbability = 0.05f;
                    break;

                case PlayerPersona.FrustratedRookie:
                    _activeProfile.TotalSessions = 2;
                    _activeProfile.AvgSessionDuration = 3.1f;
                    _activeProfile.TotalSpent = 0f;
                    _activeProfile.SupportTicketsOpen = 0;
                    _activeProfile.AdsWatched = 0;
                    _activeProfile.LevelsCompleted = 0;
                    _activeProfile.SocialShares = 0;
                    _activeProfile.WinRatePercent = 12;
                    _activeProfile.ChurnProbability = 0.85f;
                    break;

                case PlayerPersona.BoredVeteran:
                    _activeProfile.TotalSessions = 112;
                    _activeProfile.AvgSessionDuration = 2.4f;
                    _activeProfile.TotalSpent = 4.99f;
                    _activeProfile.SupportTicketsOpen = 0;
                    _activeProfile.AdsWatched = 1;
                    _activeProfile.LevelsCompleted = 145;
                    _activeProfile.SocialShares = 5;
                    _activeProfile.WinRatePercent = 96;
                    _activeProfile.ChurnProbability = 0.65f;
                    break;
            }

            // Sync with current session if analytics is active
            if (RetentiaServiceLocator.TryGet(out PlayerAnalyticsService analytics))
            {
                analytics.StartSession(_activeProfile.UserId);
            }

            _timer = 0f;
            _commandLog.Clear();
            _lastStatus = $"Persona changed to {persona}";
            DebugLogger.Info($"[Simulator] Loaded profile for {persona}: User={_activeProfile.UserId}");

            // Immediately run a prediction / score calculation to update display
            UpdateCalculators().Forget();
        }

        #endregion

        #region Simulation Logic

        private async UniTask SimulatePlayerAction()
        {
            if (!RetentiaServiceLocator.TryGet(out PlayerAnalyticsService analytics)) return;

            var currentSession = analytics.CurrentSession;
            if (currentSession == null) return;

            // Update session active play time
            currentSession.UpdateDuration();
            _activeProfile.AvgSessionDuration = Mathf.Max(2f, (currentSession.SessionDurationSeconds / 60f + _activeProfile.AvgSessionDuration) / 2f);

            // Trigger action based on persona
            float rand = UnityEngine.Random.value;

            switch (_selectedPersona)
            {
                case PlayerPersona.CasualWatcher:
                    if (rand < 0.2f)
                    {
                        // Play and complete level
                        _activeProfile.LevelsCompleted++;
                        analytics.TrackLevelStart(_activeProfile.LevelsCompleted, _activeProfile.DifficultyScore);
                        await UniTask.Delay(500);
                        analytics.TrackLevelComplete(_activeProfile.LevelsCompleted, UnityEngine.Random.Range(45f, 90f), 1000);
                        _lastStatus = $"Completed Level {_activeProfile.LevelsCompleted}";
                    }
                    else if (rand < 0.5f)
                    {
                        // Watch an ad
                        _activeProfile.AdsWatched++;
                        analytics.TrackAdWatched("RewardedGold", true);
                        _lastStatus = "Watched Rewarded Video Ad";
                    }
                    else if (rand < 0.6f)
                    {
                        // Share on social media
                        _activeProfile.SocialShares++;
                        analytics.TrackEvent("social_share", new Dictionary<string, object> { { "network", "Twitter" } });
                        _lastStatus = "Shared score on Twitter";
                    }
                    break;

                case PlayerPersona.WhaleSpender:
                    if (rand < 0.3f)
                    {
                        // Complete level
                        _activeProfile.LevelsCompleted++;
                        analytics.TrackLevelStart(_activeProfile.LevelsCompleted, _activeProfile.DifficultyScore);
                        await UniTask.Delay(300);
                        analytics.TrackLevelComplete(_activeProfile.LevelsCompleted, UnityEngine.Random.Range(30f, 60f), 2500);
                        _lastStatus = $"Completed Level {_activeProfile.LevelsCompleted}";
                    }
                    else if (rand < 0.5f)
                    {
                        // Make a purchase
                        float price = UnityEngine.Random.Range(0.0f, 1.0f) < 0.3f ? 99.99f : 9.99f;
                        _activeProfile.TotalSpent += price;
                        analytics.TrackPurchase("GemPack_" + price, price, "USD");
                        _lastStatus = $"Bought Gem Pack for ${price}";
                    }
                    break;

                case PlayerPersona.FrustratedRookie:
                    // High failure rate, no levels completed, records death points
                    if (rand < 0.6f)
                    {
                        // Fail level
                        analytics.TrackLevelStart(1, _activeProfile.DifficultyScore);
                        await UniTask.Delay(400);
                        analytics.TrackLevelFailed(1, "PlayerDied");
                        _lastStatus = "Failed Level 1: Player Died";
                    }
                    else
                    {
                        // Retry level
                        analytics.TrackLevelRetry(1);
                        _lastStatus = "Retried Level 1";
                    }
                    break;

                case PlayerPersona.BoredVeteran:
                    // High win rate but fast session abandonment
                    if (rand < 0.4f)
                    {
                        _activeProfile.LevelsCompleted++;
                        analytics.TrackLevelStart(_activeProfile.LevelsCompleted, _activeProfile.DifficultyScore);
                        await UniTask.Delay(200);
                        analytics.TrackLevelComplete(_activeProfile.LevelsCompleted, UnityEngine.Random.Range(20f, 40f), 5000);
                        _lastStatus = $"Completed Level {_activeProfile.LevelsCompleted}";
                    }
                    else if (rand < 0.5f)
                    {
                        // Opens menu out of boredom
                        analytics.TrackEvent("menu_open", new Dictionary<string, object> { { "menu_id", "Settings" } });
                        _lastStatus = "Opened settings menu";
                    }
                    break;
            }

            // Evaluate flags and recalculate score
            await UpdateCalculators();
        }

        private async UniTask UpdateCalculators()
        {
            RetentiaServiceLocator.TryGet(out ChurnPredictionEngine churnEngine);
            RetentiaServiceLocator.TryGet(out RetentionScoreCalculator scoreCalculator);
            RetentiaServiceLocator.TryGet(out PlayerFlaggingService flaggingService);
            RetentiaServiceLocator.TryGet(out PlayerAnalyticsService analytics);

            if (flaggingService != null)
            {
                await flaggingService.EvaluatePlayerAsync(_activeProfile, analytics?.CurrentSession);
            }

            if (churnEngine != null && churnEngine.IsModelReady)
            {
                _activeProfile.ChurnProbability = await churnEngine.PredictChurnForProfileAsync(_activeProfile);
            }
            else
            {
                // Fallback churn simulation rule-based calculations
                if (_selectedPersona == PlayerPersona.FrustratedRookie)
                {
                    _activeProfile.ChurnProbability = Mathf.Clamp(_activeProfile.ChurnProbability + 0.02f, 0.5f, 0.99f);
                }
                else if (_selectedPersona == PlayerPersona.WhaleSpender)
                {
                    _activeProfile.ChurnProbability = Mathf.Clamp(_activeProfile.ChurnProbability - 0.01f, 0.01f, 0.1f);
                }
            }

            // Sync segment id
            if (RetentiaServiceLocator.TryGet(out PlayerSegmentationService segmentation))
            {
                segmentation.UpdateProfileSegment(ref _activeProfile);
            }
        }

        #endregion

        #region Event Handlers & Web Commands

        private void OnOfferPresented(OfferPresentedEvent evt)
        {
            _commandLog.Add($"[{DateTime.Now:HH:mm:ss}] Remote Command: Present Offer '{evt.OfferName}'");
            DebugLogger.Info($"[Simulator] Handling remote offer presentation: {evt.OfferName}");

            if (_offerPopup == null)
            {
                // Try to find the component in the scene if reference is missing
                _offerPopup = FindObjectOfType<OfferPopupController>();
            }

            if (_offerPopup != null)
            {
                // Load a mock OfferDefinitionSO resource or find one
                var offers = Resources.LoadAll<OfferDefinitionSO>("Offers");
                var offer = Array.Find(offers, o => o.OfferId == evt.OfferId);

                if (offer == null)
                {
                    // Create dummy one if none exists in resources
                    offer = ScriptableObject.CreateInstance<OfferDefinitionSO>();
                    offer.SetupMock(evt.OfferId, evt.OfferName, "Custom discount bundle.", 4.99f);
                }

                _offerPopup.ShowOffer(
                    offer,
                    onAccept: () =>
                    {
                        _commandLog.Add($"[{DateTime.Now:HH:mm:ss}] Player Accepted Offer '{evt.OfferName}'");
                        _activeProfile.TotalSpent += offer.PriceRealMoney > 0 ? offer.PriceRealMoney : 1.99f;
                        if (RetentiaServiceLocator.TryGet(out OfferEngine engine))
                        {
                            engine.AcceptOffer(offer.OfferId, _activeProfile.UserId);
                        }
                    },
                    onDecline: () =>
                    {
                        _commandLog.Add($"[{DateTime.Now:HH:mm:ss}] Player Declined Offer '{evt.OfferName}'");
                        if (RetentiaServiceLocator.TryGet(out OfferEngine engine))
                        {
                            engine.DeclineOffer(offer.OfferId, _activeProfile.UserId);
                        }
                    }
                );
            }
            else
            {
                // No popup UI found, auto-simulate accept/decline based on persona
                bool accept = _selectedPersona == PlayerPersona.WhaleSpender || UnityEngine.Random.value < 0.3f;
                if (accept)
                {
                    _commandLog.Add($"[{DateTime.Now:HH:mm:ss}] Simulated Accept of '{evt.OfferName}'");
                    _activeProfile.TotalSpent += 4.99f;
                }
                else
                {
                    _commandLog.Add($"[{DateTime.Now:HH:mm:ss}] Simulated Decline of '{evt.OfferName}'");
                }
            }
        }

        #endregion

        #region Debug OnGUI Overlay

        private void OnGUI()
        {
            if (!_isInitialized) return;

            // Set GUI layout styles
            GUI.Box(new Rect(10, 10, 320, 520), "<b>RETENTIA AI - DEMO SIMULATOR</b>");

            int yOffset = 40;
            int rowHeight = 25;

            // 1. Persona Selector
            GUI.Label(new Rect(20, yOffset, 120, rowHeight), "<b>Player Persona:</b>");
            if (GUI.Button(new Rect(140, yOffset, 170, rowHeight), _selectedPersona.ToString()))
            {
                // Rotate personas on click
                var nextPersona = (PlayerPersona)(((int)_selectedPersona + 1) % Enum.GetNames(typeof(PlayerPersona)).Length);
                ApplyPersonaTemplate(nextPersona);
            }

            yOffset += rowHeight + 10;

            // 2. Metrics Block
            GUI.Label(new Rect(20, yOffset, 280, rowHeight), "<b>Simulated Profile Telemetry:</b>");
            yOffset += rowHeight;

            GUI.Label(new Rect(30, yOffset, 150, rowHeight), $"User ID: ");
            GUI.Label(new Rect(180, yOffset, 140, rowHeight), _activeProfile.UserId);
            yOffset += rowHeight;

            GUI.Label(new Rect(30, yOffset, 150, rowHeight), $"Sessions Played: ");
            GUI.Label(new Rect(180, yOffset, 140, rowHeight), _activeProfile.TotalSessions.ToString());
            yOffset += rowHeight;

            GUI.Label(new Rect(30, yOffset, 150, rowHeight), $"Avg Session Time: ");
            GUI.Label(new Rect(180, yOffset, 140, rowHeight), $"{_activeProfile.AvgSessionDuration:F1} min");
            yOffset += rowHeight;

            GUI.Label(new Rect(30, yOffset, 150, rowHeight), $"Win Rate: ");
            GUI.Label(new Rect(180, yOffset, 140, rowHeight), $"{_activeProfile.WinRatePercent}%");
            yOffset += rowHeight;

            GUI.Label(new Rect(30, yOffset, 150, rowHeight), $"Total Spent: ");
            GUI.Label(new Rect(180, yOffset, 140, rowHeight), $"${_activeProfile.TotalSpent:F2}");
            yOffset += rowHeight;

            GUI.Label(new Rect(30, yOffset, 150, rowHeight), $"Ads Watched: ");
            GUI.Label(new Rect(180, yOffset, 140, rowHeight), _activeProfile.AdsWatched.ToString());
            yOffset += rowHeight;

            GUI.Label(new Rect(30, yOffset, 150, rowHeight), $"Win Rate: ");
            GUI.Label(new Rect(180, yOffset, 140, rowHeight), $"{_activeProfile.WinRatePercent}%");
            yOffset += rowHeight;

            // AI results
            GUI.Label(new Rect(30, yOffset, 150, rowHeight), "<b>Churn Prob (AI):</b>");
            GUI.Label(new Rect(180, yOffset, 140, rowHeight), $"<color=red><b>{_activeProfile.ChurnProbability:P1}</b></color>");
            yOffset += rowHeight + 10;

            // 3. Actions / Controller Block
            GUI.Label(new Rect(20, yOffset, 280, rowHeight), "<b>Trigger Manual Telemetry:</b>");
            yOffset += rowHeight;

            if (GUI.Button(new Rect(30, yOffset, 130, rowHeight), "Level Win (+1)"))
            {
                _activeProfile.LevelsCompleted++;
                _activeProfile.TotalSessions++;
                if (RetentiaServiceLocator.TryGet(out PlayerAnalyticsService a))
                {
                    a.TrackLevelStart(_activeProfile.LevelsCompleted, 1.0f);
                    a.TrackLevelComplete(_activeProfile.LevelsCompleted, 60f, 1000);
                }
                UpdateCalculators().Forget();
                _lastStatus = "Manual Event: Level Win (+1)";
            }

            if (GUI.Button(new Rect(170, yOffset, 130, rowHeight), "Level Fail (+1)"))
            {
                _activeProfile.TotalSessions++;
                if (RetentiaServiceLocator.TryGet(out PlayerAnalyticsService a))
                {
                    a.TrackLevelStart(1, 1.0f);
                    a.TrackLevelFailed(1, "PlayerDied");
                }
                UpdateCalculators().Forget();
                _lastStatus = "Manual Event: Level Fail (+1)";
            }

            yOffset += rowHeight + 5;

            if (GUI.Button(new Rect(30, yOffset, 130, rowHeight), "Watch Ad (+1)"))
            {
                _activeProfile.AdsWatched++;
                if (RetentiaServiceLocator.TryGet(out PlayerAnalyticsService a))
                {
                    a.TrackAdWatched("RewardedLife", true);
                }
                UpdateCalculators().Forget();
                _lastStatus = "Manual Event: Watched Ad";
            }

            if (GUI.Button(new Rect(170, yOffset, 130, rowHeight), "Buy $9.99 Bundle"))
            {
                _activeProfile.TotalSpent += 9.99f;
                if (RetentiaServiceLocator.TryGet(out PlayerAnalyticsService a))
                {
                    a.TrackPurchase("Pack_Gold", 9.99f, "USD");
                }
                UpdateCalculators().Forget();
                _lastStatus = "Manual Event: Spent $9.99";
            }

            yOffset += rowHeight + 15;

            // 4. Status Bar
            GUI.Label(new Rect(20, yOffset, 280, rowHeight), $"<i>Status: {_lastStatus}</i>");
            yOffset += rowHeight + 5;

            // 5. Remote commands log
            GUI.Label(new Rect(20, yOffset, 280, rowHeight), "<b>Dashboard Commands Log:</b>");
            yOffset += rowHeight;

            int logStart = Mathf.Max(0, _commandLog.Count - 3);
            for (int i = logStart; i < _commandLog.Count; i++)
            {
                GUI.Label(new Rect(25, yOffset, 290, rowHeight), $"<size=9>{_commandLog[i]}</size>");
                yOffset += 18;
            }
        }

        #endregion
    }
}
