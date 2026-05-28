// -----------------------------------------------------------------------------
// ChurnPredictionEngine.cs
// ML inference engine using Unity Sentis for churn prediction.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using Cysharp.Threading.Tasks;
using Retentia.Data;
using UnityEngine;
using Unity.InferenceEngine;

namespace Retentia.Core
{
    #region Churn Prediction Engine

    /// <summary>
    /// Unity Sentis-based ML inference engine for predicting player churn probability.
    /// Uses ONNX model for real-time prediction.
    /// </summary>
    public class ChurnPredictionEngine : ISentisInferenceEngine
    {
        #region Fields

        /// <summary>
        /// Configuration reference.
        /// </summary>
        private readonly RetentionConfigSO _config;

        /// <summary>
        /// Loaded Sentis model.
        /// </summary>
        private Model _model;

        /// <summary>
        /// Sentis worker for inference.
        /// </summary>
        private Worker _worker;

        /// <summary>
        /// Whether service is initialized.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Whether model is loaded.
        /// </summary>
        private bool _isModelReady;

        /// <summary>
        /// Backend type to use.
        /// </summary>
        private readonly BackendType _backendType;

        /// <summary>
        /// Last prediction result.
        /// </summary>
        private float _lastPrediction;

        #endregion

        #region Properties

        public bool IsInitialized => _isInitialized;
        public int InitializationPriority => 20;
        public bool IsModelReady => _isModelReady;
        public float LastPrediction => _lastPrediction;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new churn prediction engine.
        /// </summary>
        /// <param name="config">Retention configuration.</param>
        /// <param name="backendType">Sentis backend type.</param>
        public ChurnPredictionEngine(RetentionConfigSO config = null, BackendType backendType = BackendType.GPUCompute)
        {
            _config = config;
            _backendType = backendType;
        }

        #endregion

        #region IRetentiaService Implementation

        public async UniTask InitializeAsync()
        {
            DebugLogger.LogServiceInit("ChurnPredictionEngine");

            try
            {
                await LoadModelAsync();
                _isInitialized = true;
                DebugLogger.LogServiceReady("ChurnPredictionEngine");
            }
            catch (Exception ex)
            {
                DebugLogger.Exception(ex, "[ChurnPredictionEngine] Failed to initialize");
                _isModelReady = false;
            }
        }

        public void Dispose()
        {
            DebugLogger.LogServiceDispose("ChurnPredictionEngine");

            _worker?.Dispose();
            _worker = null;
            _model = null;
            _isModelReady = false;
            _isInitialized = false;
        }

        #endregion

        #region ISentisInferenceEngine Implementation

        public async UniTask<float> PredictChurnAsync(float[] inputFeatures)
        {
            if (!_isModelReady)
            {
                DebugLogger.Warning("[ChurnPredictionEngine] Model not ready, returning default.");
                return 0.5f;
            }

            if (inputFeatures == null || inputFeatures.Length == 0)
            {
                DebugLogger.Error("[ChurnPredictionEngine] Invalid input features.");
                return 0.5f;
            }

            try
            {
                // Create input tensor
                using var inputTensor = new Tensor<float>(new TensorShape(1, inputFeatures.Length), inputFeatures);

                // Schedule inference
                _worker.Schedule(inputTensor);

                // Get output tensor (assuming single output named "churn_probability")
                var outputTensor = _worker.PeekOutput() as Tensor<float>;

                if (outputTensor == null)
                {
                    DebugLogger.Error("[ChurnPredictionEngine] Failed to get output tensor.");
                    return 0.5f;
                }

                // Read result via Sentis readback API
                outputTensor.ReadbackRequest();
                // wait until readback is completed
                while (!outputTensor.IsReadbackRequestDone())
                {
                    await UniTask.Yield();
                }

                using var cpuCopy = outputTensor.ReadbackAndClone();
                var result = cpuCopy.DownloadToArray();
                _lastPrediction = result.Length > 0 ? Mathf.Clamp01(result[0]) : 0.5f;

                DebugLogger.Debug($"[ChurnPredictionEngine] Prediction: {_lastPrediction:P1}");

                await UniTask.CompletedTask;
                return _lastPrediction;
            }
            catch (Exception ex)
            {
                DebugLogger.Exception(ex, "[ChurnPredictionEngine] Prediction failed");
                return 0.5f;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Predicts churn probability for a user profile.
        /// </summary>
        public async UniTask<float> PredictChurnForProfileAsync(UserProfile profile)
        {
            var features = profile.ToTensorArray();
            var normalized = NormalizeFeatures(features);
            return await PredictChurnAsync(normalized);
        }

        /// <summary>
        /// Runs batch prediction for multiple profiles.
        /// </summary>
        public async UniTask<float[]> PredictChurnBatchAsync(UserProfile[] profiles)
        {
            var results = new float[profiles.Length];

            for (int i = 0; i < profiles.Length; i++)
            {
                results[i] = await PredictChurnForProfileAsync(profiles[i]);
            }

            return results;
        }

        /// <summary>
        /// Checks if a player is at risk based on churn prediction.
        /// </summary>
        public async UniTask<bool> IsPlayerAtRiskAsync(UserProfile profile)
        {
            var threshold = _config?.ChurnRiskThreshold ?? 0.7f;
            var probability = await PredictChurnForProfileAsync(profile);
            return probability >= threshold;
        }

        #endregion

        #region Private Methods

        private async UniTask LoadModelAsync()
        {
            var modelPath = _config?.ModelResourcePath ?? "Models/churn_model";

            DebugLogger.Debug($"[ChurnPredictionEngine] Loading model from: {modelPath}");

            // Load model from Resources
            var modelAsset = Resources.Load<ModelAsset>(modelPath);

            if (modelAsset == null)
            {
                DebugLogger.Warning($"[ChurnPredictionEngine] Model not found at {modelPath}. Using fallback.");
                _isModelReady = false;
                await UniTask.CompletedTask;
                return;
            }

            _model = ModelLoader.Load(modelAsset);

            // Create worker
            _worker = new Worker(_model, _backendType);

            _isModelReady = true;

            DebugLogger.Info($"[ChurnPredictionEngine] Model loaded successfully. Backend: {_backendType}");
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// Normalizes input features for the model.
        /// </summary>
        private float[] NormalizeFeatures(float[] features)
        {
            // Expected feature order and normalization ranges:
            // [0] TotalSessions: 0-100 -> 0-1
            // [1] AvgSessionDuration: 0-60 -> 0-1
            // [2] DaysSinceLastLogin: 0-30 -> 0-1
            // [3] TotalSpent: 0-1000 -> 0-1
            // [4] SupportTicketsOpen: 0-10 -> 0-1
            // [5] AdsWatched: 0-100 -> 0-1
            // [6] LevelsCompleted: 0-100 -> 0-1
            // [7] SocialShares: 0-50 -> 0-1
            // [8] WinRatePercent: 0-100 -> 0-1
            // [9] PlatformId: 0-1 -> 0-1

            float[] normalized = new float[features.Length];
            float[] maxValues = { 100f, 60f, 30f, 1000f, 10f, 100f, 100f, 50f, 100f, 1f };

            for (int i = 0; i < features.Length && i < maxValues.Length; i++)
            {
                normalized[i] = Mathf.Clamp01(features[i] / maxValues[i]);
            }

            return normalized;
        }

        #endregion
    }

    #endregion
}
