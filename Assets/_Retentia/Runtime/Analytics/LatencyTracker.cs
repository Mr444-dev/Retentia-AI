// -----------------------------------------------------------------------------
// LatencyTracker.cs
// Tracks network latency and connection quality for mobile optimization.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Retentia.Data;
using UnityEngine;

namespace Retentia.Core
{
    #region Connection Quality Enum

    /// <summary>
    /// Connection quality levels.
    /// </summary>
    public enum ConnectionQuality
    {
        Excellent = 0,  // < 50ms
        Good = 1,       // 50-100ms
        Fair = 2,       // 100-200ms
        Poor = 3,       // 200-500ms
        Critical = 4    // > 500ms
    }

    #endregion

    #region Latency Sample

    /// <summary>
    /// A single latency measurement.
    /// </summary>
    [Serializable]
    public struct LatencySample
    {
        public float LatencyMs;
        public DateTime Timestamp;
        public bool IsTimeout;
    }

    #endregion

    #region Latency Stats

    /// <summary>
    /// Aggregated latency statistics.
    /// </summary>
    [Serializable]
    public struct LatencyStats
    {
        public float CurrentLatencyMs;
        public float AverageLatencyMs;
        public float MinLatencyMs;
        public float MaxLatencyMs;
        public float JitterMs;
        public int TimeoutCount;
        public int DisconnectCount;
        public float PacketLossPercent;
        public ConnectionQuality Quality;
    }

    #endregion

    #region Latency Tracker Service

    /// <summary>
    /// Service for tracking network latency and connection quality.
    /// Helps identify players with network issues for retention purposes.
    /// </summary>
    public class LatencyTracker : IRetentiaService
    {
        #region Fields

        /// <summary>
        /// Configuration reference.
        /// </summary>
        private readonly RetentionConfigSO _config;

        /// <summary>
        /// Recent latency samples.
        /// </summary>
        private readonly Queue<LatencySample> _samples;

        /// <summary>
        /// Maximum samples to keep.
        /// </summary>
        private const int MAX_SAMPLES = 60;

        /// <summary>
        /// Whether service is initialized.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Current latency statistics.
        /// </summary>
        private LatencyStats _currentStats;

        /// <summary>
        /// Timeout threshold in ms.
        /// </summary>
        private const float TIMEOUT_THRESHOLD_MS = 5000f;

        /// <summary>
        /// Total sent packets for loss calculation.
        /// </summary>
        private int _totalPacketsSent;

        /// <summary>
        /// Disconnect count for session.
        /// </summary>
        private int _disconnectCount;

        #endregion

        #region Properties

        public bool IsInitialized => _isInitialized;
        public int InitializationPriority => 35;
        public LatencyStats CurrentStats => _currentStats;
        public ConnectionQuality CurrentQuality => _currentStats.Quality;
        public int SampleCount => _samples.Count;

        #endregion

        #region Constructor

        public LatencyTracker(RetentionConfigSO config = null)
        {
            _config = config;
            _samples = new Queue<LatencySample>(MAX_SAMPLES);
        }

        #endregion

        #region IRetentiaService Implementation

        public async UniTask InitializeAsync()
        {
            DebugLogger.LogServiceInit("LatencyTracker");

            _samples.Clear();
            _currentStats = new LatencyStats
            {
                MinLatencyMs = float.MaxValue,
                Quality = ConnectionQuality.Excellent
            };
            _totalPacketsSent = 0;
            _disconnectCount = 0;
            _isInitialized = true;

            DebugLogger.LogServiceReady("LatencyTracker");
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            DebugLogger.LogServiceDispose("LatencyTracker");
            _samples.Clear();
            _isInitialized = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Records a latency sample.
        /// </summary>
        /// <param name="latencyMs">Measured latency in milliseconds.</param>
        public void RecordSample(float latencyMs)
        {
            var sample = new LatencySample
            {
                LatencyMs = latencyMs,
                Timestamp = DateTime.UtcNow,
                IsTimeout = latencyMs >= TIMEOUT_THRESHOLD_MS
            };

            _totalPacketsSent++;

            // Maintain max size
            if (_samples.Count >= MAX_SAMPLES)
            {
                _samples.Dequeue();
            }

            _samples.Enqueue(sample);

            // Update stats
            UpdateStats();

            // Check for critical latency
            CheckLatencyThresholds(latencyMs);
        }

        /// <summary>
        /// Records a timeout (no response).
        /// </summary>
        public void RecordTimeout()
        {
            RecordSample(TIMEOUT_THRESHOLD_MS);
        }

        /// <summary>
        /// Records a disconnect event.
        /// </summary>
        public void RecordDisconnect()
        {
            _disconnectCount++;
            UpdateStats();

            DebugLogger.Warning($"[LatencyTracker] Disconnect #{_disconnectCount} recorded.");

            // Fire event for high disconnect count
            if (_disconnectCount >= 3)
            {
                RetentiaEventBus.Publish(new PlayerFlaggedEvent(
                    "current_user",
                    "NetworkIssues",
                    $"Disconnect count: {_disconnectCount}"
                ));
            }
        }

        /// <summary>
        /// Gets the current connection quality.
        /// </summary>
        public ConnectionQuality GetConnectionQuality()
        {
            return _currentStats.Quality;
        }

        /// <summary>
        /// Checks if the player has acceptable connection.
        /// </summary>
        public bool HasAcceptableConnection()
        {
            var maxLatency = _config?.MaxAcceptableLatencyMs ?? 200f;
            return _currentStats.AverageLatencyMs <= maxLatency && _currentStats.Quality <= ConnectionQuality.Fair;
        }

        /// <summary>
        /// Resets statistics for a new session.
        /// </summary>
        public void Reset()
        {
            _samples.Clear();
            _currentStats = new LatencyStats
            {
                MinLatencyMs = float.MaxValue,
                Quality = ConnectionQuality.Excellent
            };
            _totalPacketsSent = 0;
            _disconnectCount = 0;
        }

        /// <summary>
        /// Performs a simulated ping (for testing).
        /// </summary>
        public async UniTask<float> SimulatePingAsync()
        {
            var startTime = Time.realtimeSinceStartup;

            // Simulate network delay
            await UniTask.Delay(UnityEngine.Random.Range(20, 150));

            var latency = (Time.realtimeSinceStartup - startTime) * 1000f;
            RecordSample(latency);

            return latency;
        }

        #endregion

        #region Private Methods

        private void UpdateStats()
        {
            if (_samples.Count == 0) return;

            var samplesArray = _samples.ToArray();
            float sum = 0f;
            float min = float.MaxValue;
            float max = float.MinValue;
            int timeouts = 0;

            foreach (var sample in samplesArray)
            {
                sum += sample.LatencyMs;
                if (sample.LatencyMs < min) min = sample.LatencyMs;
                if (sample.LatencyMs > max) max = sample.LatencyMs;
                if (sample.IsTimeout) timeouts++;
            }

            var avg = sum / samplesArray.Length;
            var current = samplesArray[samplesArray.Length - 1].LatencyMs;

            // Calculate jitter (standard deviation)
            float variance = 0f;
            foreach (var sample in samplesArray)
            {
                variance += (sample.LatencyMs - avg) * (sample.LatencyMs - avg);
            }
            float jitter = Mathf.Sqrt(variance / samplesArray.Length);

            // Calculate packet loss
            float packetLoss = _totalPacketsSent > 0 ? (float)timeouts / _totalPacketsSent * 100f : 0f;

            // Determine quality
            ConnectionQuality quality;
            if (avg < 50f && jitter < 20f)
            {
                quality = ConnectionQuality.Excellent;
            }
            else if (avg < 100f && jitter < 40f)
            {
                quality = ConnectionQuality.Good;
            }
            else if (avg < 200f)
            {
                quality = ConnectionQuality.Fair;
            }
            else if (avg < 500f)
            {
                quality = ConnectionQuality.Poor;
            }
            else
            {
                quality = ConnectionQuality.Critical;
            }

            _currentStats = new LatencyStats
            {
                CurrentLatencyMs = current,
                AverageLatencyMs = avg,
                MinLatencyMs = min,
                MaxLatencyMs = max,
                JitterMs = jitter,
                TimeoutCount = timeouts,
                DisconnectCount = _disconnectCount,
                PacketLossPercent = packetLoss,
                Quality = quality
            };
        }

        private void CheckLatencyThresholds(float latencyMs)
        {
            var maxLatency = _config?.MaxAcceptableLatencyMs ?? 200f;

            if (latencyMs > maxLatency)
            {
                DebugLogger.Debug($"[LatencyTracker] High latency detected: {latencyMs:F0}ms");

                // Fire event for consistently poor connection
                if (_currentStats.Quality >= ConnectionQuality.Poor)
                {
                    RetentiaEventBus.Publish(new PlayerFlaggedEvent(
                        "current_user",
                        "NetworkIssues",
                        $"Average latency: {_currentStats.AverageLatencyMs:F0}ms, Quality: {_currentStats.Quality}"
                    ));
                }
            }
        }

        #endregion
    }

    #endregion
}
