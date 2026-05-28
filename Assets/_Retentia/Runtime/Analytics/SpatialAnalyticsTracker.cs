using System;
using System.Collections.Generic;
using UnityEngine;
using Retentia.Core;
using Retentia.Data;
using Cysharp.Threading.Tasks;

namespace Retentia.Analytics
{
    /// <summary>
    /// Tracks player spatial data (position) over time and on specific events (e.g., death).
    /// Used for generating heatmaps.
    /// </summary>
    public class SpatialAnalyticsTracker : IRetentiaService
    {
        private readonly RetentionConfigSO _config;
        private readonly PlayerAnalyticsService _analyticsService;
        
        private bool _isTracking;
        private float _trackingInterval = 2.0f; // Track position every 2 seconds
        private Transform _playerTransform;
        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;
        public int InitializationPriority => 80;

        public SpatialAnalyticsTracker(RetentionConfigSO config, PlayerAnalyticsService analyticsService)
        {
            _config = config;
            _analyticsService = analyticsService;
        }

        public async UniTask InitializeAsync()
        {
            // In a real game, we'd hook into a PlayerSpawned event or find the player tag.
            // For this simulation, we'll try to find an object tagged "Player" or just simulate it.
            _isTracking = true;
            _isInitialized = true;
            DebugLogger.Info("[SpatialAnalyticsTracker] Initialized.");
            
            RetentiaEventBus.Subscribe<RetentiaEvent>(OnEventFired);
            
            TrackingLoop().Forget();
            
            await UniTask.CompletedTask;
        }

        public void SetPlayerTransform(Transform player)
        {
            _playerTransform = player;
        }

        private void OnEventFired(RetentiaEvent evt)
        {
            if (evt.Type == RetentiaEventType.LevelFail || evt.EventName == "PlayerDeath")
            {
                RecordDeathPosition();
            }
        }

        private async UniTask TrackingLoop()
        {
            while (_isTracking)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_trackingInterval));

                if (_playerTransform != null)
                {
                    Vector3 pos = _playerTransform.position;
                    // We project to 2D for the heatmap (x, z) usually for top-down 3D games
                    _analyticsService.RecordHeatmapPoint(new Vector2(pos.x, pos.z));
                }
                else
                {
                    // SIMULATION MODE: Generate random walk data if no player exists
                    SimulateMovement();
                }
            }
        }

        private void SimulateMovement()
        {
            // Simulate a player wandering around map center (0,0) with radius 50
            // This is just to demonstrate data flow to the dashboard
            float time = Time.time;
            float x = Mathf.Sin(time * 0.5f) * 20f + UnityEngine.Random.Range(-5f, 5f);
            float y = Mathf.Cos(time * 0.3f) * 20f + UnityEngine.Random.Range(-5f, 5f);
            
            _analyticsService.RecordHeatmapPoint(new Vector2(x, y));
        }

        private void RecordDeathPosition()
        {
             if (_playerTransform != null)
            {
                Vector3 pos = _playerTransform.position;
                _analyticsService.RecordDeathPoint(new Vector2(pos.x, pos.z));
                DebugLogger.Info($"[Spatial] Death recorded at {pos}");
            }
            else
            {
                // Simulate death around the "dangerous" areas
                float x = UnityEngine.Random.Range(10f, 15f); // "Danger zone"
                float y = UnityEngine.Random.Range(10f, 15f);
                _analyticsService.RecordDeathPoint(new Vector2(x, y));
                DebugLogger.Info($"[Spatial] Simulated Death recorded at ({x:F1}, {y:F1})");
            }
        }

        public void Dispose()
        {
            _isTracking = false;
            _isInitialized = false;
            RetentiaEventBus.Unsubscribe<RetentiaEvent>(OnEventFired);
        }
    }
}
