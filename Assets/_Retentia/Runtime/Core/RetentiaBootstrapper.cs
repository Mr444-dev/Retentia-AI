// -----------------------------------------------------------------------------
// RetentiaBootstrapper.cs
// Main bootstrapper MonoBehaviour to initialize all Retentia services.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Retentia.Analytics;
using Retentia.Experiments;
using Retentia.Web;

namespace Retentia.Core
{
    #region Bootstrapper Configuration

    /// <summary>
    /// Configuration for the Retentia bootstrapper.
    /// </summary>
    [Serializable]
    public class BootstrapperConfig
    {
        [Header("Logging")]
        [Tooltip("Enable debug logging")]
        public bool EnableLogging = true;

        [Tooltip("Minimum log level to display")]
        public LogLevel MinimumLogLevel = LogLevel.Debug;

        [Header("Services")]
        [Tooltip("Auto-initialize services on Awake")]
        public bool AutoInitialize = true;

        [Tooltip("Keep bootstrapper alive across scenes")]
        public bool DontDestroyOnLoad = true;

        [Header("Analytics")]
        [Tooltip("Enable analytics tracking")]
        public bool EnableAnalytics = true;

        [Tooltip("Analytics flush interval in seconds")]
        public float AnalyticsFlushInterval = 30f;

        [Header("ML Inference")]
        [Tooltip("Enable churn prediction")]
        public bool EnableChurnPrediction = true;

        [Tooltip("Path to ONNX model in Resources")]
        public string ModelResourcePath = "Models/churn_model";

        [Header("Offers")]
        [Tooltip("Enable dynamic offers system")]
        public bool EnableOffers = true;

        [Tooltip("Maximum offers to present per session")]
        public int MaxOffersPerSession = 3;
    }

    #endregion

    #region Bootstrapper MonoBehaviour

    /// <summary>
    /// Main entry point for the Retentia AI system.
    /// Initializes all services in the correct order and manages lifecycle.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class RetentiaBootstrapper : MonoBehaviour
    {
        #region Singleton

        /// <summary>
        /// Singleton instance.
        /// </summary>
        private static RetentiaBootstrapper _instance;

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static RetentiaBootstrapper Instance => _instance;

        /// <summary>
        /// Gets whether the system is initialized.
        /// </summary>
        public static bool IsSystemReady => _instance != null && _instance._isInitialized;

        #endregion

        #region Serialized Fields

        [SerializeField]
        [Tooltip("Bootstrapper configuration")]
        private BootstrapperConfig _config = new BootstrapperConfig();

        #endregion

        #region Private Fields

        /// <summary>
        /// Whether initialization has completed.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Cancellation token source for async operations.
        /// </summary>
        private System.Threading.CancellationTokenSource _cts;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current configuration.
        /// </summary>
        public BootstrapperConfig Config => _config;

        /// <summary>
        /// Gets whether the system is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when initialization completes.
        /// </summary>
        public static event Action OnSystemInitialized;

        /// <summary>
        /// Event fired when system is shutting down.
        /// </summary>
        public static event Action OnSystemShutdown;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            DebugLogger.Log("[RetentiaBootstrapper] Awake", LogLevel.Debug);

            // Singleton setup
            if (_instance != null && _instance != this)
            {
                DebugLogger.Log("[RetentiaBootstrapper] Duplicate instance destroyed.", LogLevel.Warning);
                Destroy(gameObject);
                return;
            }

            _instance = this;

            // Configure logging
            DebugLogger.IsEnabled = _config.EnableLogging;
            DebugLogger.MinimumLevel = _config.MinimumLogLevel;

            // DontDestroyOnLoad
            if (_config.DontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            // Auto-initialize if configured
            if (_config.AutoInitialize)
            {
                InitializeAsync().Forget();
            }
        }

        private void OnDestroy()
        {
            DebugLogger.Log("[RetentiaBootstrapper] OnDestroy", LogLevel.Debug);

            if (_instance == this)
            {
                Shutdown();
                _instance = null;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            DebugLogger.Log($"[RetentiaBootstrapper] Application pause: {pauseStatus}", LogLevel.Debug);

            if (pauseStatus)
            {
                // App is going to background - save state, flush analytics
                OnApplicationPauseAsync().Forget();
            }
        }

        private void OnApplicationQuit()
        {
            DebugLogger.Log("[RetentiaBootstrapper] Application quit", LogLevel.Info);
            Shutdown();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the Retentia system asynchronously.
        /// </summary>
        public async UniTask InitializeAsync()
        {
            if (_isInitialized)
            {
                DebugLogger.Log("[RetentiaBootstrapper] Already initialized.", LogLevel.Warning);
                return;
            }

            DebugLogger.Log("[RetentiaBootstrapper] Starting initialization...", LogLevel.Info);

            _cts = new System.Threading.CancellationTokenSource();

            try
            {
                // Register core services
                await RegisterServicesAsync();

                // Initialize all services
                await RetentiaServiceLocator.InitializeAllAsync();

                _isInitialized = true;

                DebugLogger.Log("[RetentiaBootstrapper] Initialization complete.", LogLevel.Info);

                // Fire initialization event
                OnSystemInitialized?.Invoke();

                // Publish session started event
                RetentiaEventBus.Publish(new SessionStartedEvent(GetCurrentUserId()));
            }
            catch (Exception ex)
            {
                DebugLogger.Exception(ex, "[RetentiaBootstrapper] Initialization failed");
                throw;
            }
        }

        /// <summary>
        /// Registers all Retentia services with the service locator.
        /// Override in subclass to register custom services.
        /// </summary>
        protected virtual async UniTask RegisterServicesAsync()
        {
            DebugLogger.Log("[RetentiaBootstrapper] Registering services...", LogLevel.Debug);

            // 1. Analytics Service (services accept a RetentionConfigSO; pass null here
            // because bootstrapper uses its own BootstrapperConfig for runtime flags.)
            var analytics = new PlayerAnalyticsService();
            RetentiaServiceLocator.Register<PlayerAnalyticsService>(analytics);
            RetentiaServiceLocator.Register<IAnalyticsProvider>(analytics);

            // 2. Churn Prediction Engine
            var churn = new ChurnPredictionEngine();
            RetentiaServiceLocator.Register<ChurnPredictionEngine>(churn);
            RetentiaServiceLocator.Register<ISentisInferenceEngine>(churn);

            // 3. Player Segmentation Service
            var segmentation = new PlayerSegmentationService();
            RetentiaServiceLocator.Register<PlayerSegmentationService>(segmentation);

            // 4. Retention Score Calculator
            var scoreCalculator = new RetentionScoreCalculator(null, churn);
            RetentiaServiceLocator.Register<RetentionScoreCalculator>(scoreCalculator);

            // 5. Offer Engine
            var offers = new OfferEngine(null, segmentation);
            RetentiaServiceLocator.Register<OfferEngine>(offers);

            // 6. Player Flagging Service
            var flagging = new PlayerFlaggingService(null, offers);
            RetentiaServiceLocator.Register<PlayerFlaggingService>(flagging);

            // 7. Spatial Analytics
            var spatialTracker = new SpatialAnalyticsTracker(null, analytics);
            RetentiaServiceLocator.Register<SpatialAnalyticsTracker>(spatialTracker);

            // 8. A/B Testing Service
            var abTesting = new ABTestingService(null);
            RetentiaServiceLocator.Register<ABTestingService>(abTesting);

            // 9. Web Dashboard Data Writer
            var webWriter = new WebDataWriter(null);
            RetentiaServiceLocator.Register<WebDataWriter>(webWriter);

            // 10. Web Command Reader
            var webReader = new WebCommandReader(null);
            RetentiaServiceLocator.Register<WebCommandReader>(webReader);

            await UniTask.CompletedTask;
        }

        #endregion

        #region Shutdown

        /// <summary>
        /// Shuts down the Retentia system.
        /// </summary>
        public void Shutdown()
        {
            if (!_isInitialized)
            {
                return;
            }

            DebugLogger.Log("[RetentiaBootstrapper] Shutting down...", LogLevel.Info);

            // Fire shutdown event
            OnSystemShutdown?.Invoke();

            // Cancel async operations
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            // Dispose all services
            RetentiaServiceLocator.DisposeAll();

            // Clear event bus
            RetentiaEventBus.Reset();

            _isInitialized = false;

            DebugLogger.Log("[RetentiaBootstrapper] Shutdown complete.", LogLevel.Info);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the current user ID (override for real implementation).
        /// </summary>
        protected virtual string GetCurrentUserId()
        {
            // Use device unique identifier as fallback
            return SystemInfo.deviceUniqueIdentifier;
        }

        /// <summary>
        /// Handles application pause (background).
        /// </summary>
        private async UniTask OnApplicationPauseAsync()
        {
            if (!_isInitialized) return;

            DebugLogger.Log("[RetentiaBootstrapper] Flushing analytics on pause...", LogLevel.Debug);

            // Flush analytics if available
            if (RetentiaServiceLocator.TryGet<IAnalyticsProvider>(out var analytics))
            {
                await analytics.FlushAsync();
            }
        }

        #endregion

        #region Editor Menu Items

#if UNITY_EDITOR
        [MenuItem("GameObject/Retentia AI/Add Bootstrapper", false, 10)]
        private static void AddBootstrapper()
        {
            if (FindObjectOfType<RetentiaBootstrapper>() != null)
            {
                EditorUtility.DisplayDialog("Retentia AI", "A RetentiaBootstrapper already exists in the scene.", "OK");
                return;
            }

            var go = new GameObject("RetentiaBootstrapper");
            go.AddComponent<RetentiaBootstrapper>();
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create RetentiaBootstrapper");
        }

        [MenuItem("GameObject/Retentia AI/Add Bootstrapper", true)]
        private static bool ValidateAddBootstrapper()
        {
            return FindObjectOfType<RetentiaBootstrapper>() == null;
        }
#endif

        #endregion
    }

    #endregion
}
