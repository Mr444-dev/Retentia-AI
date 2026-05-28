// -----------------------------------------------------------------------------
// WebDataWriter.cs
// Service to export real-time data to a JSON file for the Web Dashboard.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Retentia.Data;
using UnityEngine;

namespace Retentia.Core
{
    #region Export Data Structure

    [Serializable]
    public class DashboardExportData
    {
        public string timestamp;
        public string user_id;
        public string segment;
        public float churn_prob;
        public float retention_score;
        public SessionState session_state;
        public float session_duration;
        public List<string> recent_logs = new List<string>();
        public List<string> active_flags = new List<string>();
        public List<string> active_offers = new List<string>();
        public List<Vector2> heatmap_points = new List<Vector2>();
        public List<Vector2> death_points = new List<Vector2>();
    }

    #endregion

    /// <summary>
    /// Writes real-time data to a JSON file that the Web Dashboard polls.
    /// This acts as a simple local "backend" for the dashboard.
    /// </summary>
    public class WebDataWriter : IRetentiaService
    {
        private readonly RetentionConfigSO _config;
        private string _exportPath;
        private bool _isInitialized;
        private bool _isRunning;

        public bool IsInitialized => _isInitialized;
        public int InitializationPriority => 90;

        public WebDataWriter(RetentionConfigSO config)
        {
            _config = config;
        }

        public async UniTask InitializeAsync()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var webPublicDir = Path.Combine(projectRoot, "_WebDashboard", "public");

            if (!Directory.Exists(webPublicDir))
            {
                Directory.CreateDirectory(webPublicDir);
            }

            _exportPath = Path.Combine(webPublicDir, "data.json");
            _isInitialized = true;
            _isRunning = true;

            DebugLogger.Info($"[WebDataWriter] Exporting telemetry to: {_exportPath}");
            WriteLoop().Forget();
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _isRunning = false;
            _isInitialized = false;
        }

        private async UniTask WriteLoop()
        {
            while (_isRunning)
            {
                WriteData();
                await UniTask.Delay(1000); // Export every 1 second
            }
        }

        private void WriteData()
        {
            try
            {
                // Resolve dependencies dynamically to handle late-bound initialization
                RetentiaServiceLocator.TryGet(out PlayerAnalyticsService analytics);
                RetentiaServiceLocator.TryGet(out PlayerFlaggingService flags);
                RetentiaServiceLocator.TryGet(out ChurnPredictionEngine churnEngine);
                RetentiaServiceLocator.TryGet(out RetentionScoreCalculator scoreCalculator);
                RetentiaServiceLocator.TryGet(out PlayerSegmentationService segmentation);

                var currentSession = analytics?.CurrentSession;
                var userId = currentSession?.UserId ?? "Waiting...";

                var data = new DashboardExportData
                {
                    timestamp = DateTime.Now.ToString("O"),
                    user_id = userId,
                    session_state = currentSession?.State ?? SessionState.NotStarted,
                    session_duration = currentSession?.SessionDurationSeconds ?? 0f,
                    recent_logs = new List<string>(DebugLogger.RecentLogs)
                };

                if (currentSession != null)
                {
                    data.heatmap_points = new List<Vector2>(currentSession.HeatmapPoints);
                    data.death_points = new List<Vector2>(currentSession.DeathPoints);
                }

                // Add active flags
                if (flags != null && currentSession != null)
                {
                    var activeFlags = flags.GetActiveFlags(userId);
                    foreach (var f in activeFlags)
                    {
                        data.active_flags.Add($"{f.Type}: {f.Reason}");
                    }
                }

                // Add active segment description
                // Find local mock player segment or calculate it
                UserProfile activeProfile = default;
                bool hasProfile = false;

                // Check if the simulator class is present and retrieve its active profile
                var simulator = GameObject.FindObjectOfType<RetentiaDemoSimulator>();
                if (simulator != null)
                {
                    activeProfile = simulator.ActiveProfile;
                    hasProfile = true;
                }

                if (hasProfile)
                {
                    if (segmentation != null)
                    {
                        var segResult = segmentation.SegmentPlayer(activeProfile);
                        data.segment = segResult.PrimarySegment.ToString();
                    }
                    else
                    {
                        data.segment = ((PlayerSegment)activeProfile.PlayerSegmentId).ToString();
                    }

                    if (churnEngine != null && churnEngine.IsModelReady)
                    {
                        data.churn_prob = churnEngine.LastPrediction;
                    }
                    else
                    {
                        data.churn_prob = activeProfile.ChurnProbability;
                    }

                    if (scoreCalculator != null)
                    {
                        var scoreResult = scoreCalculator.CalculateScoreSync(activeProfile, currentSession);
                        data.retention_score = scoreResult.OverallScore;
                    }
                    else
                    {
                        data.retention_score = 50f; // Fallback
                    }
                }
                else
                {
                    data.segment = "Unknown";
                    data.churn_prob = 0.5f;
                    data.retention_score = 50f;
                }

                // Add active offers
                var offerPopup = GameObject.FindObjectOfType<OfferPopupController>();
                if (offerPopup != null && offerPopup.IsShowing && offerPopup.CurrentOffer != null)
                {
                    data.active_offers.Add(offerPopup.CurrentOffer.DisplayName);
                }

                // Serialize
                var json = JsonUtility.ToJson(data, true);
                File.WriteAllText(_exportPath, json);
            }
            catch (Exception ex)
            {
                // Don't spam errors every frame
                if (Time.frameCount % 300 == 0)
                    Debug.LogWarning($"[WebDataWriter] Failed to write: {ex.Message}");
            }
        }
    }
}
