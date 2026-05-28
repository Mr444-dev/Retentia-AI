using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Retentia.Core;
using Retentia.Data;

namespace Retentia.Web
{
    /// <summary>
    /// Polls a local JSON file for commands from the Web Dashboard and executes them.
    /// </summary>
    public class WebCommandReader : IRetentiaService
    {
        private readonly RetentionConfigSO _config;
        private string _commandPath;
        private bool _isInitialized;
        private bool _isRunning;

        public bool IsInitialized => _isInitialized;
        public int InitializationPriority => 95;

        // Command definitions
        [Serializable]
        public class WebCommand
        {
            public string id;
            public string type; // e.g., "TRIGGER_OFFER", "ADD_CURRENCY"
            public string payload; // JSON string payload
            public long timestamp;
        }

        [Serializable]
        public class CommandBatch
        {
            public List<WebCommand> commands = new List<WebCommand>();
        }

        private HashSet<string> _processedCommandIds = new HashSet<string>();

        public WebCommandReader(RetentionConfigSO config)
        {
            _config = config;
        }

        public async UniTask InitializeAsync()
        {
            // Path: project_root/_WebDashboard/public/commands.json
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var webPublicDir = Path.Combine(projectRoot, "_WebDashboard", "public");
            
            if (!Directory.Exists(webPublicDir))
            {
                Directory.CreateDirectory(webPublicDir);
            }

            _commandPath = Path.Combine(webPublicDir, "commands.json");
            
            // Ensure file exists
            if (!File.Exists(_commandPath))
            {
                await WriteEmptyCommandFile();
            }

            _isInitialized = true;
            _isRunning = true;
            
            DebugLogger.Info($"[WebCommandReader] Polling commands from: {_commandPath}");
            
            PollLoop().Forget();
        }

        private async UniTask PollLoop()
        {
            while (_isRunning)
            {
                await UniTask.Delay(1000); // Poll every 1 second

                try
                {
                    if (File.Exists(_commandPath))
                    {
                        string json = await File.ReadAllTextAsync(_commandPath);
                        if (!string.IsNullOrEmpty(json))
                        {
                            var batch = JsonConvert.DeserializeObject<CommandBatch>(json);
                            if (batch != null && batch.commands != null && batch.commands.Count > 0)
                            {
                                await ProcessCommands(batch.commands);
                                // Clear file after processing to avoid re-execution
                                // In a real app, we might perform a more atomic operation or use a database
                                await WriteEmptyCommandFile(); 
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.Error($"[WebCommandReader] Error polling commands: {ex.Message}");
                }
            }
        }

        private async UniTask ProcessCommands(List<WebCommand> commands)
        {
            foreach (var cmd in commands)
            {
                if (_processedCommandIds.Contains(cmd.id)) continue;
                
                DebugLogger.Info($"[WebCommandReader] Executing command: {cmd.type}");

                switch (cmd.type)
                {
                    case "TRIGGER_OFFER":
                        // Payload might contain Offer ID or Segment
                        // For now, let's trigger a generic evaluation
                        RetentiaEventBus.Publish(new RetentiaEvent(RetentiaEventType.SessionStart, "WebCommand")); // Force re-eval
                        DebugLogger.Info("Triggered Offer Evaluation via Web Command");
                        break;
                        
                    case "ADD_COINS":
                        // Mocking adding currency
                        DebugLogger.Info($"Added 100 Coins (Simulated) - Payload: {cmd.payload}");
                        break;
                    
                    case "SET_EXPERIMENT":
                         if (RetentiaServiceLocator.TryGet(out Experiments.ABTestingService abService))
                        {
                            // Payload: { "group": "VariantA" }
                             try {
                                var pl = JsonConvert.DeserializeObject<Dictionary<string, string>>(cmd.payload);
                                if (pl != null && pl.ContainsKey("group") && Enum.TryParse(pl["group"], out Experiments.ExperimentGroup group))
                                {
                                    abService.SetExperimentGroup(group);
                                }
                            } catch (Exception ex) {
                                DebugLogger.Error($"[Command] Failed to set experiment: {ex.Message}");
                            }
                        }
                        break;

                    default:
                        DebugLogger.Warning($"Unknown command type: {cmd.type}");
                        break;
                }

                _processedCommandIds.Add(cmd.id);
            }
            
            // Cleanup old IDs periodically? For now, unlimited set is fine for dev.
        }

        private async UniTask WriteEmptyCommandFile()
        {
            var empty = new CommandBatch();
            string json = JsonConvert.SerializeObject(empty, Formatting.Indented);
            await File.WriteAllTextAsync(_commandPath, json);
        }

        public void Dispose()
        {
            _isRunning = false;
            _isInitialized = false;
        }
    }
}
