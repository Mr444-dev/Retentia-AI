// -----------------------------------------------------------------------------
// PlayerSimulator.cs
// Editor tool for simulating player profiles and testing the retention system.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Retentia.Data;
using UnityEditor;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
#endif

namespace Retentia.Core.Editor
{
    #region Player Preset

    /// <summary>
    /// Predefined player profile presets for testing.
    /// </summary>
    [Serializable]
    public class PlayerPreset
    {
        public string Name;
        public UserProfile Profile;
        public string Description;
    }

    #endregion

    #region Player Simulator Window

    /// <summary>
    /// Editor window for simulating player profiles.
    /// </summary>
    #if ODIN_INSPECTOR
    public class PlayerSimulator : OdinEditorWindow
    #else
    public class PlayerSimulator : EditorWindow
    #endif
    {
        #region Menu Item

        [MenuItem("Window/Retentia AI/Player Simulator", false, 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<PlayerSimulator>();
            window.titleContent = new GUIContent("Player Simulator");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        #endregion

        #region Fields

        #if ODIN_INSPECTOR
        [BoxGroup("Presets")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = false)]
        #endif
        [SerializeField]
        private List<PlayerPreset> _presets = new List<PlayerPreset>();

        #if ODIN_INSPECTOR
        [BoxGroup("Simulation")]
        #endif
        [SerializeField]
        private int _numberOfProfiles = 100;

        #if ODIN_INSPECTOR
        [BoxGroup("Simulation")]
        [ProgressBar(0, 100)]
        [ReadOnly]
        #endif
        [SerializeField]
        private float _simulationProgress;

        #if ODIN_INSPECTOR
        [BoxGroup("Results")]
        [ReadOnly]
        #endif
        [SerializeField]
        private int _atRiskCount;

        #if ODIN_INSPECTOR
        [BoxGroup("Results")]
        [ReadOnly]
        #endif
        [SerializeField]
        private int _whaleCount;

        #if ODIN_INSPECTOR
        [BoxGroup("Results")]
        [ReadOnly]
        #endif
        [SerializeField]
        private float _avgRetentionScore;

        #if ODIN_INSPECTOR
        [BoxGroup("Results")]
        [ReadOnly]
        #endif
        [SerializeField]
        private float _avgChurnProbability;

        private List<UserProfile> _generatedProfiles;
        private RetentionConfigSO _config;
        private PlayerSegmentationService _segmentationService;
        private RetentionScoreCalculator _scoreCalculator;
        private Vector2 _scrollPosition;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            InitializePresets();

            // Find config
            var configs = AssetDatabase.FindAssets("t:RetentionConfigSO");
            if (configs.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(configs[0]);
                _config = AssetDatabase.LoadAssetAtPath<RetentionConfigSO>(path);
            }

            _segmentationService = new PlayerSegmentationService(_config);
            _scoreCalculator = new RetentionScoreCalculator(_config, null);
            _generatedProfiles = new List<UserProfile>();
        }

        #endregion

        #region Presets

        private void InitializePresets()
        {
            if (_presets.Count > 0) return;

            _presets = new List<PlayerPreset>
            {
                new PlayerPreset
                {
                    Name = "New Player",
                    Description = "Just installed the game",
                    Profile = new UserProfile
                    {
                        TotalSessions = 2,
                        AvgSessionDuration = 5f,
                        DaysSinceLastLogin = 0,
                        TotalSpent = 0f,
                        AdsWatched = 1,
                        LevelsCompleted = 3,
                        WinRatePercent = 50,
                        PlatformId = 0
                    }
                },
                new PlayerPreset
                {
                    Name = "Engaged Player",
                    Description = "Active daily player",
                    Profile = new UserProfile
                    {
                        TotalSessions = 50,
                        AvgSessionDuration = 20f,
                        DaysSinceLastLogin = 0,
                        TotalSpent = 9.99f,
                        AdsWatched = 30,
                        LevelsCompleted = 80,
                        SocialShares = 5,
                        WinRatePercent = 70,
                        PlatformId = 0
                    }
                },
                new PlayerPreset
                {
                    Name = "At-Risk Player",
                    Description = "Showing churn signals",
                    Profile = new UserProfile
                    {
                        TotalSessions = 40,
                        AvgSessionDuration = 5f,
                        DaysSinceLastLogin = 7,
                        TotalSpent = 0f,
                        SupportTicketsOpen = 1,
                        AdsWatched = 3,
                        LevelsCompleted = 15,
                        WinRatePercent = 25,
                        PlatformId = 0
                    }
                },
                new PlayerPreset
                {
                    Name = "Whale",
                    Description = "High-value spender",
                    Profile = new UserProfile
                    {
                        TotalSessions = 200,
                        AvgSessionDuration = 30f,
                        DaysSinceLastLogin = 0,
                        TotalSpent = 350f,
                        AdsWatched = 0,
                        LevelsCompleted = 250,
                        SocialShares = 20,
                        WinRatePercent = 88,
                        PlatformId = 1
                    }
                },
                new PlayerPreset
                {
                    Name = "Churned Player",
                    Description = "Hasn't played in 2 weeks",
                    Profile = new UserProfile
                    {
                        TotalSessions = 25,
                        AvgSessionDuration = 8f,
                        DaysSinceLastLogin = 18,
                        TotalSpent = 4.99f,
                        AdsWatched = 10,
                        LevelsCompleted = 20,
                        WinRatePercent = 45,
                        PlatformId = 0
                    }
                }
            };
        }

        #endregion

        #region Actions

        #if ODIN_INSPECTOR
        [BoxGroup("Simulation")]
        [Button("Generate Random Profiles", ButtonSizes.Large), GUIColor(0.2f, 0.8f, 0.2f)]
        #endif
        private void GenerateRandomProfiles()
        {
            _generatedProfiles.Clear();
            _atRiskCount = 0;
            _whaleCount = 0;
            float totalScore = 0f;
            float totalChurn = 0f;

            for (int i = 0; i < _numberOfProfiles; i++)
            {
                _simulationProgress = (float)i / _numberOfProfiles * 100f;

                var profile = GenerateRandomProfile();
                _generatedProfiles.Add(profile);

                // Analyze profile
                var segment = _segmentationService.SegmentPlayer(profile);
                var score = _scoreCalculator.CalculateScoreSync(profile);

                totalScore += score.OverallScore;
                totalChurn += score.ChurnProbability;

                if (segment.PrimarySegment == PlayerSegment.AtRisk || segment.PrimarySegment == PlayerSegment.Churned)
                {
                    _atRiskCount++;
                }
                if (segment.PrimarySegment == PlayerSegment.Whale)
                {
                    _whaleCount++;
                }
            }

            _simulationProgress = 100f;
            _avgRetentionScore = totalScore / _numberOfProfiles;
            _avgChurnProbability = totalChurn / _numberOfProfiles;

            Debug.Log($"[PlayerSimulator] Generated {_numberOfProfiles} profiles. " +
                      $"At-Risk: {_atRiskCount}, Whales: {_whaleCount}, " +
                      $"Avg Score: {_avgRetentionScore:F1}, Avg Churn: {_avgChurnProbability:P1}");
        }

        #if ODIN_INSPECTOR
        [BoxGroup("Simulation")]
        [Button("Export to CSV")]
        #endif
        private void ExportToCsv()
        {
            if (_generatedProfiles.Count == 0)
            {
                EditorUtility.DisplayDialog("Export", "No profiles to export. Generate profiles first.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Export Profiles", "", "player_profiles.csv", "csv");
            if (string.IsNullOrEmpty(path)) return;

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("UserId,TotalSessions,AvgSessionDuration,DaysSinceLastLogin,TotalSpent,AdsWatched,LevelsCompleted,WinRate,Platform");

            foreach (var profile in _generatedProfiles)
            {
                csv.AppendLine($"{profile.UserId},{profile.TotalSessions},{profile.AvgSessionDuration:F2},{profile.DaysSinceLastLogin},{profile.TotalSpent:F2},{profile.AdsWatched},{profile.LevelsCompleted},{profile.WinRatePercent},{profile.PlatformId}");
            }

            System.IO.File.WriteAllText(path, csv.ToString());
            EditorUtility.DisplayDialog("Export", $"Exported {_generatedProfiles.Count} profiles to CSV.", "OK");
        }

        private UserProfile GenerateRandomProfile()
        {
            // Weighted random to create realistic distribution
            var rand = UnityEngine.Random.value;

            if (rand < 0.05f)
            {
                // 5% Whales
                return GenerateWhaleProfile();
            }
            else if (rand < 0.15f)
            {
                // 10% At-Risk
                return GenerateAtRiskProfile();
            }
            else if (rand < 0.25f)
            {
                // 10% New players
                return GenerateNewPlayerProfile();
            }
            else
            {
                // 75% Regular distribution
                return GenerateRegularProfile();
            }
        }

        private UserProfile GenerateWhaleProfile()
        {
            return new UserProfile
            {
                UserId = $"whale_{Guid.NewGuid().ToString().Substring(0, 8)}",
                TotalSessions = UnityEngine.Random.Range(100, 300),
                AvgSessionDuration = UnityEngine.Random.Range(20f, 45f),
                DaysSinceLastLogin = UnityEngine.Random.Range(0, 2),
                TotalSpent = UnityEngine.Random.Range(100f, 500f),
                SupportTicketsOpen = 0,
                AdsWatched = 0,
                LevelsCompleted = UnityEngine.Random.Range(150, 300),
                SocialShares = UnityEngine.Random.Range(10, 30),
                WinRatePercent = UnityEngine.Random.Range(75, 95),
                PlatformId = UnityEngine.Random.value > 0.5f ? 1 : 0
            };
        }

        private UserProfile GenerateAtRiskProfile()
        {
            return new UserProfile
            {
                UserId = $"atrisk_{Guid.NewGuid().ToString().Substring(0, 8)}",
                TotalSessions = UnityEngine.Random.Range(10, 50),
                AvgSessionDuration = UnityEngine.Random.Range(2f, 8f),
                DaysSinceLastLogin = UnityEngine.Random.Range(7, 20),
                TotalSpent = UnityEngine.Random.Range(0f, 10f),
                SupportTicketsOpen = UnityEngine.Random.Range(0, 3),
                AdsWatched = UnityEngine.Random.Range(0, 10),
                LevelsCompleted = UnityEngine.Random.Range(5, 25),
                SocialShares = 0,
                WinRatePercent = UnityEngine.Random.Range(15, 40),
                PlatformId = 0
            };
        }

        private UserProfile GenerateNewPlayerProfile()
        {
            return new UserProfile
            {
                UserId = $"new_{Guid.NewGuid().ToString().Substring(0, 8)}",
                TotalSessions = UnityEngine.Random.Range(1, 3),
                AvgSessionDuration = UnityEngine.Random.Range(3f, 10f),
                DaysSinceLastLogin = 0,
                TotalSpent = 0f,
                SupportTicketsOpen = 0,
                AdsWatched = UnityEngine.Random.Range(0, 2),
                LevelsCompleted = UnityEngine.Random.Range(1, 5),
                SocialShares = 0,
                WinRatePercent = UnityEngine.Random.Range(30, 60),
                PlatformId = UnityEngine.Random.value > 0.6f ? 1 : 0
            };
        }

        private UserProfile GenerateRegularProfile()
        {
            return new UserProfile
            {
                UserId = $"player_{Guid.NewGuid().ToString().Substring(0, 8)}",
                TotalSessions = UnityEngine.Random.Range(5, 80),
                AvgSessionDuration = UnityEngine.Random.Range(5f, 25f),
                DaysSinceLastLogin = UnityEngine.Random.Range(0, 7),
                TotalSpent = UnityEngine.Random.Range(0f, 50f),
                SupportTicketsOpen = UnityEngine.Random.Range(0, 1),
                AdsWatched = UnityEngine.Random.Range(5, 40),
                LevelsCompleted = UnityEngine.Random.Range(10, 100),
                SocialShares = UnityEngine.Random.Range(0, 10),
                WinRatePercent = UnityEngine.Random.Range(40, 80),
                PlatformId = UnityEngine.Random.value > 0.5f ? 1 : 0
            };
        }

        #endregion

        #region GUI

        #if !ODIN_INSPECTOR
        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Player Simulator", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Simulation Settings", EditorStyles.boldLabel);
            _numberOfProfiles = EditorGUILayout.IntSlider("Number of Profiles", _numberOfProfiles, 10, 1000);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Generate Random Profiles", GUILayout.Height(40)))
            {
                GenerateRandomProfiles();
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);
            EditorGUILayout.FloatField("Progress %", _simulationProgress);
            EditorGUILayout.IntField("At-Risk Count", _atRiskCount);
            EditorGUILayout.IntField("Whale Count", _whaleCount);
            EditorGUILayout.FloatField("Avg Retention Score", _avgRetentionScore);
            EditorGUILayout.FloatField("Avg Churn Probability", _avgChurnProbability);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Export to CSV"))
            {
                ExportToCsv();
            }

            EditorGUILayout.EndScrollView();
        }
        #endif

        #endregion
    }

    #endregion
}
#endif
