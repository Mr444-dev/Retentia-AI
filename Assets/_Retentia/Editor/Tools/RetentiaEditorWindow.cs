// -----------------------------------------------------------------------------
// RetentiaEditorWindow.cs
// Custom editor window for Retentia AI configuration and testing.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Retentia.Data;
using UnityEditor;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
#endif

namespace Retentia.Core.Editor
{
    #region Retentia Editor Window

    /// <summary>
    /// Custom editor window for configuring and testing Retentia AI.
    /// </summary>
    #if ODIN_INSPECTOR
    public class RetentiaEditorWindow : OdinEditorWindow
    #else
    public class RetentiaEditorWindow : EditorWindow
    #endif
    {
        #region Menu Item

        [MenuItem("Window/Retentia AI/Dashboard", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<RetentiaEditorWindow>();
            window.titleContent = new GUIContent("Retentia AI", EditorGUIUtility.IconContent("d_UnityEditor.ProfilerWindow").image);
            window.minSize = new Vector2(450, 600);
            window.Show();
        }

        [MenuItem("Window/Retentia AI/Create Config", false, 100)]
        public static void CreateConfig()
        {
            var config = ScriptableObject.CreateInstance<RetentionConfigSO>();
            string path = EditorUtility.SaveFilePanelInProject("Create Retention Config", "RetentionConfig", "asset", "Save config");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                Selection.activeObject = config;
            }
        }

        [MenuItem("Window/Retentia AI/Create Offer", false, 101)]
        public static void CreateOffer()
        {
            var offer = ScriptableObject.CreateInstance<OfferDefinitionSO>();
            string path = EditorUtility.SaveFilePanelInProject("Create Offer Definition", "NewOffer", "asset", "Save offer");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(offer, path);
                AssetDatabase.SaveAssets();
                Selection.activeObject = offer;
            }
        }

        [MenuItem("Window/Retentia AI/Create Flag Rule", false, 102)]
        public static void CreateFlagRule()
        {
            var rule = ScriptableObject.CreateInstance<PlayerFlagRuleSO>();
            string path = EditorUtility.SaveFilePanelInProject("Create Flag Rule", "NewFlagRule", "asset", "Save rule");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(rule, path);
                AssetDatabase.SaveAssets();
                Selection.activeObject = rule;
            }
        }

        #endregion

        #region Fields

        #if ODIN_INSPECTOR
        [BoxGroup("Configuration")]
        [InlineEditor(InlineEditorModes.GUIOnly)]
        #endif
        [SerializeField]
        private RetentionConfigSO _currentConfig;

        #if ODIN_INSPECTOR
        [BoxGroup("Test Player Profile")]
        [Title("Player Data")]
        #endif
        [SerializeField]
        private UserProfile _testProfile = new UserProfile
        {
            UserId = "test_user_001",
            TotalSessions = 15,
            AvgSessionDuration = 8.5f,
            DaysSinceLastLogin = 2,
            TotalSpent = 24.99f,
            SupportTicketsOpen = 0,
            AdsWatched = 20,
            LevelsCompleted = 35,
            SocialShares = 3,
            WinRatePercent = 62,
            PlatformId = 0
        };

        #if ODIN_INSPECTOR
        [BoxGroup("Test Results")]
        [Title("Prediction Results")]
        [ReadOnly]
        #endif
        [SerializeField]
        private float _lastChurnProbability;

        #if ODIN_INSPECTOR
        [BoxGroup("Test Results")]
        [ReadOnly]
        #endif
        [SerializeField]
        private string _playerSegment;

        #if ODIN_INSPECTOR
        [BoxGroup("Test Results")]
        [ReadOnly]
        #endif
        [SerializeField]
        private float _retentionScore;

        #if ODIN_INSPECTOR
        [BoxGroup("Test Results")]
        [ReadOnly]
        #endif
        [SerializeField]
        private string _riskLevel;

        #if ODIN_INSPECTOR
        [BoxGroup("Test Results")]
        [ReadOnly]
        [TextArea(3, 5)]
        #endif
        [SerializeField]
        private string _recommendations;

        private PlayerSegmentationService _segmentationService;
        private RetentionScoreCalculator _scoreCalculator;
        private Vector2 _scrollPosition;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            // Find config
            if (_currentConfig == null)
            {
                var configs = AssetDatabase.FindAssets("t:RetentionConfigSO");
                if (configs.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(configs[0]);
                    _currentConfig = AssetDatabase.LoadAssetAtPath<RetentionConfigSO>(path);
                }
            }

            // Initialize services
            _segmentationService = new PlayerSegmentationService(_currentConfig);
            _scoreCalculator = new RetentionScoreCalculator(_currentConfig, null);
        }

        #endregion

        #region GUI

        #if !ODIN_INSPECTOR
        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Retentia AI Dashboard", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Config section
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            _currentConfig = (RetentionConfigSO)EditorGUILayout.ObjectField("Current Config", _currentConfig, typeof(RetentionConfigSO), false);
            EditorGUILayout.Space(10);

            // Test profile section
            EditorGUILayout.LabelField("Test Player Profile", EditorStyles.boldLabel);
            _testProfile.UserId = EditorGUILayout.TextField("User ID", _testProfile.UserId);
            _testProfile.TotalSessions = EditorGUILayout.IntField("Total Sessions", _testProfile.TotalSessions);
            _testProfile.AvgSessionDuration = EditorGUILayout.FloatField("Avg Session Duration", _testProfile.AvgSessionDuration);
            _testProfile.DaysSinceLastLogin = EditorGUILayout.IntField("Days Since Login", _testProfile.DaysSinceLastLogin);
            _testProfile.TotalSpent = EditorGUILayout.FloatField("Total Spent ($)", _testProfile.TotalSpent);
            _testProfile.SupportTicketsOpen = EditorGUILayout.IntField("Support Tickets", _testProfile.SupportTicketsOpen);
            _testProfile.AdsWatched = EditorGUILayout.IntField("Ads Watched", _testProfile.AdsWatched);
            _testProfile.LevelsCompleted = EditorGUILayout.IntField("Levels Completed", _testProfile.LevelsCompleted);
            _testProfile.SocialShares = EditorGUILayout.IntField("Social Shares", _testProfile.SocialShares);
            _testProfile.WinRatePercent = EditorGUILayout.IntSlider("Win Rate %", _testProfile.WinRatePercent, 0, 100);
            _testProfile.PlatformId = EditorGUILayout.Popup("Platform", _testProfile.PlatformId, new[] { "Android", "iOS" });
            EditorGUILayout.Space(10);

            // Run test button
            if (GUILayout.Button("Run Analysis", GUILayout.Height(40)))
            {
                RunAnalysis();
            }

            EditorGUILayout.Space(10);

            // Results section
            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Churn Probability", _lastChurnProbability);
            EditorGUILayout.TextField("Player Segment", _playerSegment);
            EditorGUILayout.FloatField("Retention Score", _retentionScore);
            EditorGUILayout.TextField("Risk Level", _riskLevel);
            EditorGUILayout.LabelField("Recommendations:");
            EditorGUILayout.TextArea(_recommendations);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);

            // Quick actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            if (GUILayout.Button("Randomize Profile"))
            {
                RandomizeProfile();
            }
            if (GUILayout.Button("Reset to Default"))
            {
                ResetProfile();
            }

            EditorGUILayout.EndScrollView();
        }
        #endif

        #endregion

        #region Actions

        #if ODIN_INSPECTOR
        [BoxGroup("Actions")]
        [Button("Run Analysis", ButtonSizes.Large), GUIColor(0.2f, 0.8f, 0.2f)]
        #endif
        private void RunAnalysis()
        {
            // Segment player
            if (_segmentationService != null)
            {
                var segResult = _segmentationService.SegmentPlayer(_testProfile);
                _playerSegment = $"{segResult.PrimarySegment} (Confidence: {segResult.Confidence:P0})";
            }

            // Calculate retention score
            if (_scoreCalculator != null)
            {
                var scoreResult = _scoreCalculator.CalculateScoreSync(_testProfile);
                _retentionScore = scoreResult.OverallScore;
                _riskLevel = scoreResult.RiskLevel;
                _lastChurnProbability = scoreResult.ChurnProbability;

                if (scoreResult.Recommendations != null && scoreResult.Recommendations.Length > 0)
                {
                    _recommendations = string.Join("\n• ", scoreResult.Recommendations);
                }
                else
                {
                    _recommendations = "No recommendations";
                }
            }

            Debug.Log($"[Retentia] Analysis complete - Score: {_retentionScore:F1}, Segment: {_playerSegment}");
        }

        #if ODIN_INSPECTOR
        [BoxGroup("Actions")]
        [Button("Randomize Profile"), GUIColor(0.8f, 0.8f, 0.2f)]
        #endif
        private void RandomizeProfile()
        {
            _testProfile = new UserProfile
            {
                UserId = $"random_user_{UnityEngine.Random.Range(1000, 9999)}",
                TotalSessions = UnityEngine.Random.Range(1, 100),
                AvgSessionDuration = UnityEngine.Random.Range(1f, 45f),
                DaysSinceLastLogin = UnityEngine.Random.Range(0, 30),
                TotalSpent = UnityEngine.Random.Range(0f, 200f),
                SupportTicketsOpen = UnityEngine.Random.Range(0, 5),
                AdsWatched = UnityEngine.Random.Range(0, 50),
                LevelsCompleted = UnityEngine.Random.Range(0, 100),
                SocialShares = UnityEngine.Random.Range(0, 20),
                WinRatePercent = UnityEngine.Random.Range(10, 95),
                PlatformId = UnityEngine.Random.Range(0, 2)
            };

            RunAnalysis();
        }

        #if ODIN_INSPECTOR
        [BoxGroup("Actions")]
        [Button("Reset to Default")]
        #endif
        private void ResetProfile()
        {
            _testProfile = new UserProfile
            {
                UserId = "test_user_001",
                TotalSessions = 15,
                AvgSessionDuration = 8.5f,
                DaysSinceLastLogin = 2,
                TotalSpent = 24.99f,
                SupportTicketsOpen = 0,
                AdsWatched = 20,
                LevelsCompleted = 35,
                SocialShares = 3,
                WinRatePercent = 62,
                PlatformId = 0
            };

            _lastChurnProbability = 0f;
            _playerSegment = "";
            _retentionScore = 0f;
            _riskLevel = "";
            _recommendations = "";
        }

        #if ODIN_INSPECTOR
        [BoxGroup("Actions")]
        [Button("Create At-Risk Test Profile"), GUIColor(0.9f, 0.3f, 0.3f)]
        #endif
        private void CreateAtRiskProfile()
        {
            _testProfile = new UserProfile
            {
                UserId = "atrisk_user_001",
                TotalSessions = 45,
                AvgSessionDuration = 3.2f,
                DaysSinceLastLogin = 8,
                TotalSpent = 5.99f,
                SupportTicketsOpen = 2,
                AdsWatched = 5,
                LevelsCompleted = 12,
                SocialShares = 0,
                WinRatePercent = 28,
                PlatformId = 0
            };

            RunAnalysis();
        }

        #if ODIN_INSPECTOR
        [BoxGroup("Actions")]
        [Button("Create Whale Test Profile"), GUIColor(0.3f, 0.3f, 0.9f)]
        #endif
        private void CreateWhaleProfile()
        {
            _testProfile = new UserProfile
            {
                UserId = "whale_user_001",
                TotalSessions = 150,
                AvgSessionDuration = 25.5f,
                DaysSinceLastLogin = 0,
                TotalSpent = 499.99f,
                SupportTicketsOpen = 0,
                AdsWatched = 0,
                LevelsCompleted = 200,
                SocialShares = 15,
                WinRatePercent = 85,
                PlatformId = 1
            };

            RunAnalysis();
        }

        #endregion
    }

    #endregion
}
#endif
