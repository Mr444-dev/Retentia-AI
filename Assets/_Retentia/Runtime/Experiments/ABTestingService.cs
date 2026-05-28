using System;
using System.Collections.Generic;
using UnityEngine;
using Retentia.Core;
using Retentia.Data;
using Cysharp.Threading.Tasks;

namespace Retentia.Experiments
{
    public enum ExperimentGroup
    {
        Control = 0,
        VariantA = 1,
        VariantB = 2
    }

    /// <summary>
    /// Manages active A/B testing experiments and provides configuration overrides.
    /// </summary>
    public class ABTestingService : IRetentiaService
    {
        private readonly RetentionConfigSO _config;
        private ExperimentGroup _currentGroup = ExperimentGroup.Control;
        private bool _isInitialized;
        
        // Overrides dictionary: ConfigKey -> Value
        private Dictionary<string, object> _overrides = new Dictionary<string, object>();

        public bool IsInitialized => _isInitialized;
        public int InitializationPriority => 30;
        public ExperimentGroup CurrentGroup => _currentGroup;

        public ABTestingService(RetentionConfigSO config)
        {
            _config = config;
        }

        public async UniTask InitializeAsync()
        {
            // By default assign random group or load from save
            // For now, default to Control until commanded
            DebugLogger.Info($"[ABTesting] Service Initialized. Group: {_currentGroup}");
            _isInitialized = true;
            await UniTask.CompletedTask;
        }

        public void SetExperimentGroup(ExperimentGroup group)
        {
            if (_currentGroup != group)
            {
                _currentGroup = group;
                ApplyOverridesForGroup(group);
                DebugLogger.Info($"[ABTesting] Switched to {group}");
                RetentiaEventBus.Publish(new RetentiaEvent(RetentiaEventType.Custom, "ExperimentChanged", new Dictionary<string, object> { { "group", group.ToString() } }));
            }
        }

        private void ApplyOverridesForGroup(ExperimentGroup group)
        {
            _overrides.Clear();
            
            switch (group)
            {
                case ExperimentGroup.VariantA: 
                    // Example: Harder difficulty, higher rewards
                    _overrides["DifficultyMultiplier"] = 1.5f;
                    _overrides["RewardMultiplier"] = 2.0f;
                    break;
                    
                case ExperimentGroup.VariantB:
                    // Example: Easier difficulty, lower rewards
                    _overrides["DifficultyMultiplier"] = 0.8f;
                    _overrides["RewardMultiplier"] = 0.5f;
                    break;
                    
                case ExperimentGroup.Control:
                default:
                    // No overrides
                    break;
            }
        }

        /// <summary>
        /// Gets a value, potentially overridden by the active experiment.
        /// </summary>
        public T GetValue<T>(string key, T defaultValue)
        {
            if (_overrides.ContainsKey(key))
            {
                try
                {
                    return (T)Convert.ChangeType(_overrides[key], typeof(T));
                }
                catch
                {
                    DebugLogger.Warning($"[ABTesting] Cast failed for key {key}");
                }
            }
            return defaultValue;
        }

        public void Dispose()
        {
            _overrides.Clear();
            _isInitialized = false;
        }
    }
}
