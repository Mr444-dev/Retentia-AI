// -----------------------------------------------------------------------------
// IRetentiaService.cs
// Base interface for all Retentia services with lifecycle management.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using Cysharp.Threading.Tasks;

namespace Retentia.Core
{
    #region Service Interface

    /// <summary>
    /// Base interface for all Retentia services.
    /// Provides lifecycle management and dependency resolution.
    /// </summary>
    public interface IRetentiaService
    {
        #region Properties

        /// <summary>
        /// Gets whether the service has been initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets the service priority for initialization order.
        /// Lower values are initialized first.
        /// </summary>
        int InitializationPriority { get; }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Initializes the service asynchronously.
        /// </summary>
        /// <returns>UniTask representing the async operation.</returns>
        UniTask InitializeAsync();

        /// <summary>
        /// Disposes the service and releases resources.
        /// </summary>
        void Dispose();

        #endregion
    }

    #endregion

    #region Analytics Provider Interface

    /// <summary>
    /// Interface for analytics tracking providers.
    /// </summary>
    public interface IAnalyticsProvider : IRetentiaService
    {
        /// <summary>
        /// Tracks a custom event.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="parameters">Optional event parameters.</param>
        void TrackEvent(string eventName, System.Collections.Generic.Dictionary<string, object> parameters = null);

        /// <summary>
        /// Tracks a numeric metric.
        /// </summary>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="value">Metric value.</param>
        void TrackMetric(string metricName, float value);

        /// <summary>
        /// Flushes any queued analytics data.
        /// </summary>
        UniTask FlushAsync();
    }

    #endregion

    #region Inference Engine Interface

    /// <summary>
    /// Interface for ML inference engines.
    /// </summary>
    public interface ISentisInferenceEngine : IRetentiaService
    {
        /// <summary>
        /// Predicts churn probability for a player.
        /// </summary>
        /// <param name="inputFeatures">Normalized input features array.</param>
        /// <returns>Churn probability between 0 and 1.</returns>
        UniTask<float> PredictChurnAsync(float[] inputFeatures);

        /// <summary>
        /// Gets whether the model is loaded and ready.
        /// </summary>
        bool IsModelReady { get; }
    }

    #endregion

    #region Retention Action Interface

    /// <summary>
    /// Interface for retention actions that can be executed on players.
    /// </summary>
    public interface IRetentionAction
    {
        /// <summary>
        /// Gets the unique identifier for this action.
        /// </summary>
        string ActionId { get; }

        /// <summary>
        /// Gets the display name for this action.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Checks if this action is eligible for the given player.
        /// </summary>
        /// <param name="profile">Player profile to check.</param>
        /// <returns>True if eligible, false otherwise.</returns>
        bool IsEligible(Retentia.Data.UserProfile profile);

        /// <summary>
        /// Executes the retention action.
        /// </summary>
        /// <param name="profile">Target player profile.</param>
        /// <returns>True if action was successful.</returns>
        UniTask<bool> ExecuteAsync(Retentia.Data.UserProfile profile);
    }

    #endregion
}
