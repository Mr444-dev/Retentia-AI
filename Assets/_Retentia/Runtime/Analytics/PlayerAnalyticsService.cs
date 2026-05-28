// -----------------------------------------------------------------------------
// PlayerAnalyticsService.cs
// Main analytics service for tracking player behavior and events.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Retentia.Data;
using UnityEngine;

namespace Retentia.Core
{
    #region Analytics Event

    /// <summary>
    /// Represents a queued analytics event.
    /// </summary>
    [Serializable]
    public struct AnalyticsEvent
    {
        public string EventName;
        public DateTime Timestamp;
        public Dictionary<string, object> Parameters;

        public AnalyticsEvent(string name, Dictionary<string, object> parameters = null)
        {
            EventName = name;
            Timestamp = DateTime.UtcNow;
            Parameters = parameters ?? new Dictionary<string, object>();
        }
    }

    #endregion

    #region Player Analytics Service

    /// <summary>
    /// Service for tracking player analytics events and metrics.
    /// Implements IAnalyticsProvider for modular analytics tracking.
    /// </summary>
    public class PlayerAnalyticsService : IAnalyticsProvider
    {
        #region Fields

        /// <summary>
        /// Queue of pending analytics events.
        /// </summary>
        private readonly Queue<AnalyticsEvent> _eventQueue;

        /// <summary>
        /// Dictionary of tracked metrics.
        /// </summary>
        private readonly Dictionary<string, float> _metrics;

        /// <summary>
        /// Configuration reference.
        /// </summary>
        private readonly RetentionConfigSO _config;

        /// <summary>
        /// Current session data.
        /// </summary>
        private PlayerSessionData _currentSession;

        /// <summary>
        /// Whether the service is initialized.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Last flush timestamp.
        /// </summary>
        private DateTime _lastFlushTime;

        #endregion

        #region Properties

        public bool IsInitialized => _isInitialized;
        public int InitializationPriority => 10;
        public PlayerSessionData CurrentSession => _currentSession;
        public int QueuedEventCount => _eventQueue.Count;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new analytics service instance.
        /// </summary>
        /// <param name="config">Retention configuration.</param>
        public PlayerAnalyticsService(RetentionConfigSO config = null)
        {
            _config = config;
            _eventQueue = new Queue<AnalyticsEvent>();
            _metrics = new Dictionary<string, float>();
        }

        #endregion

        #region IRetentiaService Implementation

        public async UniTask InitializeAsync()
        {
            DebugLogger.LogServiceInit("PlayerAnalyticsService");

            _eventQueue.Clear();
            _metrics.Clear();
            _lastFlushTime = DateTime.UtcNow;
            _isInitialized = true;

            // Subscribe to events
            RetentiaEventBus.Subscribe<SessionStartedEvent>(OnSessionStarted);
            RetentiaEventBus.Subscribe<SessionEndedEvent>(OnSessionEnded);

            DebugLogger.LogServiceReady("PlayerAnalyticsService");
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            DebugLogger.LogServiceDispose("PlayerAnalyticsService");

            // Unsubscribe from events
            RetentiaEventBus.Unsubscribe<SessionStartedEvent>(OnSessionStarted);
            RetentiaEventBus.Unsubscribe<SessionEndedEvent>(OnSessionEnded);

            // Final flush
            FlushAsync().Forget();

            _eventQueue.Clear();
            _metrics.Clear();
            _isInitialized = false;
        }

        #endregion

        #region IAnalyticsProvider Implementation

        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!_isInitialized)
            {
                DebugLogger.Warning("[Analytics] Cannot track event - service not initialized.");
                return;
            }

            var evt = new AnalyticsEvent(eventName, parameters);
            _eventQueue.Enqueue(evt);

            // Record in session
            _currentSession?.RecordEvent(eventName, parameters != null ? UnityEngine.JsonUtility.ToJson(parameters) : null);

            if (_config != null && _config.VerboseAnalytics)
            {
                DebugLogger.Debug($"[Analytics] Tracked: {eventName}");
            }

            // Auto-flush if queue is full
            var maxEvents = _config?.MaxQueuedEvents ?? 100;
            if (_eventQueue.Count >= maxEvents)
            {
                FlushAsync().Forget();
            }
        }

        public void TrackMetric(string metricName, float value)
        {
            if (!_isInitialized) return;

            _metrics[metricName] = value;

            if (_config != null && _config.VerboseAnalytics)
            {
                DebugLogger.Debug($"[Analytics] Metric: {metricName} = {value}");
            }
        }

        public async UniTask FlushAsync()
        {
            if (_eventQueue.Count == 0)
            {
                return;
            }

            DebugLogger.Debug($"[Analytics] Flushing {_eventQueue.Count} events...");

            // In a real implementation, this would send to a backend
            // For now, we just clear the queue
            var events = new List<AnalyticsEvent>();
            while (_eventQueue.Count > 0)
            {
                events.Add(_eventQueue.Dequeue());
            }

            _lastFlushTime = DateTime.UtcNow;

            // Simulate network delay
            await UniTask.Delay(10);

            DebugLogger.Debug($"[Analytics] Flushed {events.Count} events.");
        }

        #endregion

        #region Session Tracking

        /// <summary>
        /// Starts a new session for the given user.
        /// </summary>
        public PlayerSessionData StartSession(string userId)
        {
            if (_currentSession != null && _currentSession.State == SessionState.Active)
            {
                EndSession();
            }

            _currentSession = new PlayerSessionData(userId);
            _currentSession.RecordEvent("SESSION_START");

            TrackEvent("session_start", new Dictionary<string, object>
            {
                ["user_id"] = userId,
                ["session_id"] = _currentSession.SessionId
            });

            DebugLogger.Info($"[Analytics] Session started: {_currentSession.SessionId}");

            return _currentSession;
        }

        /// <summary>
        /// Ends the current session.
        /// </summary>
        public void EndSession()
        {
            if (_currentSession == null || _currentSession.State == SessionState.Ended)
            {
                return;
            }

            _currentSession.EndSession();

            TrackEvent("session_end", _currentSession.ToDictionary());

            DebugLogger.Info($"[Analytics] Session ended: {_currentSession.SessionId}, Duration: {_currentSession.SessionDurationSeconds:F1}s");
        }

        /// <summary>
        /// Pauses the current session.
        /// </summary>
        public void PauseSession()
        {
            if (_currentSession != null && _currentSession.State == SessionState.Active)
            {
                _currentSession.State = SessionState.Paused;
                _currentSession.PauseCount++;
                _currentSession.RecordEvent("SESSION_PAUSE");

                TrackEvent("session_pause");
            }
        }

        /// <summary>
        /// Resumes the current session.
        /// </summary>
        public void ResumeSession()
        {
            if (_currentSession != null && _currentSession.State == SessionState.Paused)
            {
                _currentSession.State = SessionState.Active;
                _currentSession.RecordEvent("SESSION_RESUME");

                TrackEvent("session_resume");
            }
        }

        #endregion

        #region Gameplay Tracking

        /// <summary>
        /// Tracks level start.
        /// </summary>
        public void TrackLevelStart(int levelId, float difficulty = 0f)
        {
            if (_currentSession != null)
            {
                _currentSession.LevelsAttempted++;
            }

            TrackEvent("level_start", new Dictionary<string, object>
            {
                ["level_id"] = levelId,
                ["difficulty"] = difficulty
            });
        }

        /// <summary>
        /// Tracks level completion.
        /// </summary>
        public void TrackLevelComplete(int levelId, float duration, int score = 0)
        {
            if (_currentSession != null)
            {
                _currentSession.LevelsCompleted++;
            }

            TrackEvent("level_complete", new Dictionary<string, object>
            {
                ["level_id"] = levelId,
                ["duration"] = duration,
                ["score"] = score
            });
        }

        /// <summary>
        /// Tracks level failure.
        /// </summary>
        public void TrackLevelFailed(int levelId, string reason = null)
        {
            if (_currentSession != null)
            {
                _currentSession.LevelsFailed++;
            }

            TrackEvent("level_failed", new Dictionary<string, object>
            {
                ["level_id"] = levelId,
                ["reason"] = reason ?? "unknown"
            });
        }

        /// <summary>
        /// Tracks level retry.
        /// </summary>
        public void TrackLevelRetry(int levelId)
        {
            if (_currentSession != null)
            {
                _currentSession.RetryCount++;
            }

            TrackEvent("level_retry", new Dictionary<string, object>
            {
                ["level_id"] = levelId
            });
        }

        /// <summary>
        /// Tracks ad watched.
        /// </summary>
        public void TrackAdWatched(string adType, bool completed)
        {
            if (_currentSession != null && completed)
            {
                _currentSession.AdsWatched++;
            }

            TrackEvent("ad_watched", new Dictionary<string, object>
            {
                ["ad_type"] = adType,
                ["completed"] = completed
            });
        }

        /// <summary>
        /// Tracks purchase.
        /// </summary>
        public void TrackPurchase(string productId, float amount, string currency)
        {
            if (_currentSession != null)
            {
                _currentSession.PurchasesMade++;
                _currentSession.MoneySpent += amount;
            }

            TrackEvent("purchase", new Dictionary<string, object>
            {
                ["product_id"] = productId,
                ["amount"] = amount,
                ["currency"] = currency
            });
        }

        #endregion

        #region Event Handlers

        private void OnSessionStarted(SessionStartedEvent evt)
        {
            StartSession(evt.UserId);
        }

        private void OnSessionEnded(SessionEndedEvent evt)
        {
            EndSession();
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets all current metrics.
        /// </summary>
        public IReadOnlyDictionary<string, float> GetMetrics()
        {
            return _metrics;
        }

        /// <summary>
        /// Updates the session with frame rate data.
        /// </summary>
        public void UpdateFrameRate(float fps)
        {
            _currentSession?.UpdateFrameRate(fps);
        }

        /// <summary>
        /// Updates the session duration.
        /// </summary>
        public void UpdateSessionDuration()
        {
            _currentSession?.UpdateDuration();
        }

        /// <summary>
        /// Records a player's spatial heatmap point.
        /// </summary>
        public void RecordHeatmapPoint(Vector2 point)
        {
            _currentSession?.HeatmapPoints.Add(point);
        }

        /// <summary>
        /// Records a player's death position.
        /// </summary>
        public void RecordDeathPoint(Vector2 point)
        {
            _currentSession?.DeathPoints.Add(point);
        }

        #endregion
    }

    #endregion
}
