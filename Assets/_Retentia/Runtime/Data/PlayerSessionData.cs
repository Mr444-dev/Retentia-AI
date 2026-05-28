// -----------------------------------------------------------------------------
// PlayerSessionData.cs
// Real-time session tracking data for player behavior analysis.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Retentia.Data
{
    #region Session State Enum

    /// <summary>
    /// Current state of the player session.
    /// </summary>
    public enum SessionState
    {
        NotStarted = 0,
        Active = 1,
        Paused = 2,
        Ended = 3
    }

    #endregion

    #region Session Event

    /// <summary>
    /// Represents an event that occurred during a session.
    /// </summary>
    [Serializable]
    public struct SessionEvent
    {
        /// <summary>
        /// Event name/type.
        /// </summary>
        public string EventName;

        /// <summary>
        /// Timestamp when the event occurred.
        /// </summary>
        public float TimeInSession;

        /// <summary>
        /// Optional event data.
        /// </summary>
        public string Data;

        public SessionEvent(string eventName, float timeInSession, string data = null)
        {
            EventName = eventName;
            TimeInSession = timeInSession;
            Data = data;
        }
    }

    #endregion

    #region Player Session Data

    /// <summary>
    /// Real-time session tracking data.
    /// Tracks all events, performance metrics, and player actions during a session.
    /// </summary>
    [Serializable]
    public class PlayerSessionData
    {
        #region Session Info

        [Header("Session Info")]
        [SerializeField] private string _sessionId;
        [SerializeField] private string _userId;
        [SerializeField] private DateTime _sessionStart;
        [SerializeField] private DateTime _sessionEnd;
        [SerializeField] private SessionState _state;
        [SerializeField] private int _sessionNumber;

        #endregion

        #region Performance Metrics

        [Header("Performance Metrics")]
        [SerializeField] private float _sessionDurationSeconds;
        [SerializeField] private float _activePlayTimeSeconds;
        [SerializeField] private int _pauseCount;
        [SerializeField] private float _totalPauseDurationSeconds;
        [SerializeField] private float _avgFrameRate;
        [SerializeField] private float _minFrameRate;
        [SerializeField] private float _avgLatencyMs;
        [SerializeField] private int _disconnectCount;

        #endregion

        #region Gameplay Metrics

        [Header("Gameplay Metrics")]
        [SerializeField] private int _levelsAttempted;
        [SerializeField] private int _levelsCompleted;
        [SerializeField] private int _levelsFailed;
        [SerializeField] private int _retryCount;
        [SerializeField] private float _highestDifficultyReached;
        [SerializeField] private int _currencyEarned;
        [SerializeField] private int _currencySpent;
        [SerializeField] private int _adsWatched;
        [SerializeField] private int _purchasesMade;
        [SerializeField] private float _moneySpent;

        #endregion

        #region Engagement Metrics

        [Header("Engagement Metrics")]
        [SerializeField] private int _actionsPerMinute;
        [SerializeField] private int _menuOpenCount;
        [SerializeField] private int _tutorialStepsCompleted;
        [SerializeField] private int _socialInteractions;
        [SerializeField] private int _offersViewed;
        [SerializeField] private int _offersAccepted;
        [SerializeField] private int _offersDeclined;

        #endregion

        #region Event Log

        [Header("Events")]
        [SerializeField] private List<SessionEvent> _events = new List<SessionEvent>();

        #endregion

        #region Spatial Tracking

        [Header("Spatial Data")]
        [SerializeField] private List<Vector2> _heatmapPoints = new List<Vector2>();
        [SerializeField] private List<Vector2> _deathPoints = new List<Vector2>();

        #endregion

        #region Properties - Session Info

        /// <summary>
        /// Unique session identifier.
        /// </summary>
        public string SessionId
        {
            get => _sessionId;
            set => _sessionId = value;
        }

        /// <summary>
        /// User ID for this session.
        /// </summary>
        public string UserId
        {
            get => _userId;
            set => _userId = value;
        }

        /// <summary>
        /// Session start time.
        /// </summary>
        public DateTime SessionStart
        {
            get => _sessionStart;
            set => _sessionStart = value;
        }

        /// <summary>
        /// Session end time.
        /// </summary>
        public DateTime SessionEnd
        {
            get => _sessionEnd;
            set => _sessionEnd = value;
        }

        /// <summary>
        /// Current session state.
        /// </summary>
        public SessionState State
        {
            get => _state;
            set => _state = value;
        }

        /// <summary>
        /// Session number for this user.
        /// </summary>
        public int SessionNumber
        {
            get => _sessionNumber;
            set => _sessionNumber = value;
        }

        #endregion

        #region Properties - Performance

        public float SessionDurationSeconds { get => _sessionDurationSeconds; set => _sessionDurationSeconds = value; }
        public float ActivePlayTimeSeconds { get => _activePlayTimeSeconds; set => _activePlayTimeSeconds = value; }
        public int PauseCount { get => _pauseCount; set => _pauseCount = value; }
        public float TotalPauseDurationSeconds { get => _totalPauseDurationSeconds; set => _totalPauseDurationSeconds = value; }
        public float AvgFrameRate { get => _avgFrameRate; set => _avgFrameRate = value; }
        public float MinFrameRate { get => _minFrameRate; set => _minFrameRate = value; }
        public float AvgLatencyMs { get => _avgLatencyMs; set => _avgLatencyMs = value; }
        public int DisconnectCount { get => _disconnectCount; set => _disconnectCount = value; }

        #endregion

        #region Properties - Gameplay

        public int LevelsAttempted { get => _levelsAttempted; set => _levelsAttempted = value; }
        public int LevelsCompleted { get => _levelsCompleted; set => _levelsCompleted = value; }
        public int LevelsFailed { get => _levelsFailed; set => _levelsFailed = value; }
        public int RetryCount { get => _retryCount; set => _retryCount = value; }
        public float HighestDifficultyReached { get => _highestDifficultyReached; set => _highestDifficultyReached = value; }
        public int CurrencyEarned { get => _currencyEarned; set => _currencyEarned = value; }
        public int CurrencySpent { get => _currencySpent; set => _currencySpent = value; }
        public int AdsWatched { get => _adsWatched; set => _adsWatched = value; }
        public int PurchasesMade { get => _purchasesMade; set => _purchasesMade = value; }
        public float MoneySpent { get => _moneySpent; set => _moneySpent = value; }

        #endregion

        #region Properties - Engagement

        public int ActionsPerMinute { get => _actionsPerMinute; set => _actionsPerMinute = value; }
        public int MenuOpenCount { get => _menuOpenCount; set => _menuOpenCount = value; }
        public int TutorialStepsCompleted { get => _tutorialStepsCompleted; set => _tutorialStepsCompleted = value; }
        public int SocialInteractions { get => _socialInteractions; set => _socialInteractions = value; }
        public int OffersViewed { get => _offersViewed; set => _offersViewed = value; }
        public int OffersAccepted { get => _offersAccepted; set => _offersAccepted = value; }
        public int OffersDeclined { get => _offersDeclined; set => _offersDeclined = value; }

        #endregion

        #region Properties - Events

        /// <summary>
        /// Read-only list of session events.
        /// </summary>
        public IReadOnlyList<SessionEvent> Events => _events;

        /// <summary>
        /// Number of events in session.
        /// </summary>
        public int EventCount => _events.Count;

        #endregion

        #region Properties - Spatial

        public List<Vector2> HeatmapPoints => _heatmapPoints;
        public List<Vector2> DeathPoints => _deathPoints;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Win rate for this session.
        /// </summary>
        public float SessionWinRate => _levelsAttempted > 0 
            ? (float)_levelsCompleted / _levelsAttempted 
            : 0f;

        /// <summary>
        /// Average time per level in seconds.
        /// </summary>
        public float AvgTimePerLevel => _levelsCompleted > 0 
            ? _activePlayTimeSeconds / _levelsCompleted 
            : 0f;

        /// <summary>
        /// Engagement score (0-100).
        /// </summary>
        public float EngagementScore
        {
            get
            {
                var score = 0f;
                
                // Duration contribution (max 30 points for 30+ minutes)
                score += Mathf.Min(_sessionDurationSeconds / 60f, 30f);
                
                // Levels contribution (max 30 points)
                score += Mathf.Min(_levelsCompleted * 3f, 30f);
                
                // Actions per minute contribution (max 20 points)
                score += Mathf.Min(_actionsPerMinute * 2f, 20f);
                
                // Social & offers contribution (max 20 points)
                score += Mathf.Min((_socialInteractions + _offersAccepted) * 5f, 20f);
                
                return Mathf.Clamp(score, 0f, 100f);
            }
        }

        /// <summary>
        /// Frustration score based on failures and retries (0-100).
        /// </summary>
        public float FrustrationScore
        {
            get
            {
                if (_levelsAttempted == 0) return 0f;
                
                var failRate = (float)_levelsFailed / _levelsAttempted;
                var retryRate = (float)_retryCount / _levelsAttempted;
                
                return Mathf.Clamp((failRate * 50f) + (retryRate * 30f) + (_pauseCount * 2f), 0f, 100f);
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new session data instance.
        /// </summary>
        public PlayerSessionData(string userId)
        {
            _sessionId = Guid.NewGuid().ToString();
            _userId = userId;
            _sessionStart = DateTime.UtcNow;
            _state = SessionState.Active;
            _events = new List<SessionEvent>();
            _minFrameRate = float.MaxValue;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Records a session event.
        /// </summary>
        public void RecordEvent(string eventName, string data = null)
        {
            var elapsed = (float)(DateTime.UtcNow - _sessionStart).TotalSeconds;
            _events.Add(new SessionEvent(eventName, elapsed, data));
        }

        /// <summary>
        /// Updates the session duration.
        /// </summary>
        public void UpdateDuration()
        {
            if (_state == SessionState.Active)
            {
                _sessionDurationSeconds = (float)(DateTime.UtcNow - _sessionStart).TotalSeconds;
            }
        }

        /// <summary>
        /// Updates frame rate tracking.
        /// </summary>
        public void UpdateFrameRate(float currentFps)
        {
            if (currentFps < _minFrameRate)
            {
                _minFrameRate = currentFps;
            }
            
            // Running average
            _avgFrameRate = (_avgFrameRate + currentFps) / 2f;
        }

        /// <summary>
        /// Ends the session and calculates final metrics.
        /// </summary>
        public void EndSession()
        {
            _sessionEnd = DateTime.UtcNow;
            _state = SessionState.Ended;
            UpdateDuration();
            RecordEvent("SESSION_END");
        }

        /// <summary>
        /// Converts session data to dictionary for analytics.
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["session_id"] = _sessionId,
                ["user_id"] = _userId,
                ["duration_seconds"] = _sessionDurationSeconds,
                ["levels_completed"] = _levelsCompleted,
                ["levels_failed"] = _levelsFailed,
                ["money_spent"] = _moneySpent,
                ["ads_watched"] = _adsWatched,
                ["engagement_score"] = EngagementScore,
                ["frustration_score"] = FrustrationScore,
                ["avg_fps"] = _avgFrameRate,
                ["avg_latency"] = _avgLatencyMs
            };
        }

        #endregion
    }

    #endregion
}
