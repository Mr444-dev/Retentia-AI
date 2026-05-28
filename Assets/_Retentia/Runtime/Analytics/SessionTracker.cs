// -----------------------------------------------------------------------------
// SessionTracker.cs
// Component for tracking session lifecycle with MonoBehaviour integration.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using Cysharp.Threading.Tasks;
using Retentia.Data;
using UnityEngine;

namespace Retentia.Core
{
    #region Session Tracker

    /// <summary>
    /// MonoBehaviour component for tracking player session lifecycle.
    /// Integrates with PlayerAnalyticsService for automatic session management.
    /// </summary>
    public class SessionTracker : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Configuration")]
        [SerializeField]
        [Tooltip("User ID for the session (leave empty for device ID)")]
        private string _userId;

        [SerializeField]
        [Tooltip("How often to update session metrics (seconds)")]
        private float _updateInterval = 1f;

        [SerializeField]
        [Tooltip("Auto-start session on enable")]
        private bool _autoStart = true;

        #endregion

        #region Private Fields

        private PlayerAnalyticsService _analyticsService;
        private bool _isTracking;
        private float _lastUpdateTime;
        private int _frameCount;
        private float _fpsAccumulator;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether session tracking is active.
        /// </summary>
        public bool IsTracking => _isTracking;

        /// <summary>
        /// Gets the current session data.
        /// </summary>
        public PlayerSessionData CurrentSession => _analyticsService?.CurrentSession;

        /// <summary>
        /// Gets the current FPS.
        /// </summary>
        public float CurrentFPS { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            DebugLogger.Verbose("[SessionTracker] OnEnable");

            if (_autoStart)
            {
                StartTrackingAsync().Forget();
            }
        }

        private void OnDisable()
        {
            DebugLogger.Verbose("[SessionTracker] OnDisable");
            StopTracking();
        }

        private void Update()
        {
            if (!_isTracking) return;

            // FPS calculation
            _frameCount++;
            _fpsAccumulator += Time.unscaledDeltaTime;

            // Update at interval
            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                UpdateSessionMetrics();
                _lastUpdateTime = Time.time;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!_isTracking) return;

            if (pauseStatus)
            {
                _analyticsService?.PauseSession();
            }
            else
            {
                _analyticsService?.ResumeSession();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!_isTracking) return;

            if (!hasFocus)
            {
                _analyticsService?.PauseSession();
            }
            else
            {
                _analyticsService?.ResumeSession();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts session tracking.
        /// </summary>
        public async UniTask StartTrackingAsync()
        {
            if (_isTracking)
            {
                DebugLogger.Warning("[SessionTracker] Already tracking.");
                return;
            }

            DebugLogger.Info("[SessionTracker] Starting session tracking...");

            // Get or create analytics service
            if (!RetentiaServiceLocator.TryGet<IAnalyticsProvider>(out var provider))
            {
                // Create and register a new analytics service
                _analyticsService = new PlayerAnalyticsService();
                await _analyticsService.InitializeAsync();
                RetentiaServiceLocator.Register<IAnalyticsProvider>(_analyticsService);
            }
            else
            {
                _analyticsService = provider as PlayerAnalyticsService;
            }

            // Determine user ID
            var userId = string.IsNullOrEmpty(_userId) ? SystemInfo.deviceUniqueIdentifier : _userId;

            // Start session
            _analyticsService?.StartSession(userId);

            _isTracking = true;
            _lastUpdateTime = Time.time;
            _frameCount = 0;
            _fpsAccumulator = 0f;

            DebugLogger.Info("[SessionTracker] Session tracking started.");
        }

        /// <summary>
        /// Stops session tracking.
        /// </summary>
        public void StopTracking()
        {
            if (!_isTracking) return;

            DebugLogger.Info("[SessionTracker] Stopping session tracking...");

            _analyticsService?.EndSession();
            _isTracking = false;

            DebugLogger.Info("[SessionTracker] Session tracking stopped.");
        }

        /// <summary>
        /// Manually sets the user ID.
        /// </summary>
        public void SetUserId(string userId)
        {
            _userId = userId;

            // If already tracking, restart with new user ID
            if (_isTracking)
            {
                StopTracking();
                StartTrackingAsync().Forget();
            }
        }

        #endregion

        #region Private Methods

        private void UpdateSessionMetrics()
        {
            // Calculate FPS
            if (_fpsAccumulator > 0)
            {
                CurrentFPS = _frameCount / _fpsAccumulator;
                _analyticsService?.UpdateFrameRate(CurrentFPS);
            }

            _frameCount = 0;
            _fpsAccumulator = 0f;

            // Update session duration
            _analyticsService?.UpdateSessionDuration();
        }

        #endregion
    }

    #endregion
}
