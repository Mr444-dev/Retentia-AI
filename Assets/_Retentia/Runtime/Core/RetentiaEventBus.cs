// -----------------------------------------------------------------------------
// RetentiaEventBus.cs
// Event-driven messaging system for decoupled communication.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Retentia.Core
{
    #region Event Base Classes

    /// <summary>
    /// Base class for all Retentia events.
    /// </summary>
    /// <summary>
    /// Known simple event types for quick categorization.
    /// </summary>
    public enum RetentiaEventType
    {
        Unknown = 0,
        SessionStart,
        SessionEnd,
        LevelFail,
        Custom
    }

    /// <summary>
    /// Base class for all Retentia events. Made concrete to support lightweight
    /// ad-hoc events (previous codebase expected to instantiate RetentiaEvent).
    /// </summary>
    public class RetentiaEvent
    {
        /// <summary>
        /// Timestamp when the event was created.
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        /// <summary>
        /// Optional typed categorization for the event.
        /// </summary>
        public RetentiaEventType Type { get; }

        /// <summary>
        /// Friendly event name (for custom/simple events).
        /// </summary>
        public string EventName { get; }

        /// <summary>
        /// Optional payload dictionary for custom events.
        /// </summary>
        public Dictionary<string, object> Payload { get; }

        /// <summary>
        /// Returns the concrete type name when available.
        /// </summary>
        public string EventTypeName => GetType().Name;

        public RetentiaEvent()
        {
            Type = RetentiaEventType.Unknown;
            EventName = EventTypeName;
            Payload = null;
        }

        public RetentiaEvent(RetentiaEventType type, string eventName = null, Dictionary<string, object> payload = null)
        {
            Type = type;
            EventName = string.IsNullOrEmpty(eventName) ? type.ToString() : eventName;
            Payload = payload;
        }
    }

    #endregion

    #region Common Events

    /// <summary>
    /// Event fired when a player session starts.
    /// </summary>
    public class SessionStartedEvent : RetentiaEvent
    {
        public string UserId { get; }
        public DateTime SessionStart { get; }

        public SessionStartedEvent(string userId)
        {
            UserId = userId;
            SessionStart = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event fired when a player session ends.
    /// </summary>
    public class SessionEndedEvent : RetentiaEvent
    {
        public string UserId { get; }
        public float SessionDurationMinutes { get; }

        public SessionEndedEvent(string userId, float durationMinutes)
        {
            UserId = userId;
            SessionDurationMinutes = durationMinutes;
        }
    }

    /// <summary>
    /// Event fired when churn prediction is calculated.
    /// </summary>
    public class ChurnPredictionEvent : RetentiaEvent
    {
        public string UserId { get; }
        public float ChurnProbability { get; }
        public bool IsAtRisk { get; }

        public ChurnPredictionEvent(string userId, float probability, float threshold = 0.7f)
        {
            UserId = userId;
            ChurnProbability = probability;
            IsAtRisk = probability >= threshold;
        }
    }

    /// <summary>
    /// Event fired when a player is flagged.
    /// </summary>
    public class PlayerFlaggedEvent : RetentiaEvent
    {
        public string UserId { get; }
        public string FlagType { get; }
        public string Reason { get; }

        public PlayerFlaggedEvent(string userId, string flagType, string reason)
        {
            UserId = userId;
            FlagType = flagType;
            Reason = reason;
        }
    }

    /// <summary>
    /// Event fired when an offer is presented.
    /// </summary>
    public class OfferPresentedEvent : RetentiaEvent
    {
        public string UserId { get; }
        public string OfferId { get; }
        public string OfferName { get; }

        public OfferPresentedEvent(string userId, string offerId, string offerName)
        {
            UserId = userId;
            OfferId = offerId;
            OfferName = offerName;
        }
    }

    /// <summary>
    /// Event fired when an offer is accepted or rejected.
    /// </summary>
    public class OfferResponseEvent : RetentiaEvent
    {
        public string UserId { get; }
        public string OfferId { get; }
        public bool Accepted { get; }

        public OfferResponseEvent(string userId, string offerId, bool accepted)
        {
            UserId = userId;
            OfferId = offerId;
            Accepted = accepted;
        }
    }

    /// <summary>
    /// Event fired when player segment changes.
    /// </summary>
    public class SegmentChangedEvent : RetentiaEvent
    {
        public string UserId { get; }
        public PlayerSegment PreviousSegment { get; }
        public PlayerSegment NewSegment { get; }

        public SegmentChangedEvent(string userId, PlayerSegment previous, PlayerSegment current)
        {
            UserId = userId;
            PreviousSegment = previous;
            NewSegment = current;
        }
    }

    #endregion

    #region Player Segment Enum

    /// <summary>
    /// Player segments for targeted actions.
    /// </summary>
    public enum PlayerSegment
    {
        Unknown = 0,
        NewPlayer = 1,
        Casual = 2,
        Regular = 3,
        Engaged = 4,
        HighValue = 5,
        Whale = 6,
        AtRisk = 7,
        Churned = 8,
        Returning = 9
    }

    #endregion

    #region Event Bus

    /// <summary>
    /// Thread-safe event bus for publishing and subscribing to Retentia events.
    /// Supports both synchronous and asynchronous event handling.
    /// </summary>
    public static class RetentiaEventBus
    {
        #region Fields

        /// <summary>
        /// Dictionary of event handlers by event type.
        /// </summary>
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        /// <summary>
        /// Lock object for thread-safe operations.
        /// </summary>
        private static readonly object _lock = new object();

        /// <summary>
        /// Event history for debugging.
        /// </summary>
        private static readonly Queue<RetentiaEvent> _eventHistory = new Queue<RetentiaEvent>();

        /// <summary>
        /// Maximum number of events to keep in history.
        /// </summary>
        private const int MAX_HISTORY_SIZE = 50;

        #endregion

        #region Subscription Methods

        /// <summary>
        /// Subscribes to an event type with a synchronous handler.
        /// </summary>
        /// <typeparam name="TEvent">Event type to subscribe to.</typeparam>
        /// <param name="handler">Event handler delegate.</param>
        public static void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : RetentiaEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var eventType = typeof(TEvent);

            lock (_lock)
            {
                if (!_handlers.ContainsKey(eventType))
                {
                    _handlers[eventType] = new List<Delegate>();
                }

                _handlers[eventType].Add(handler);
                DebugLogger.Verbose($"[EventBus] Subscribed to {eventType.Name}");
            }
        }

        /// <summary>
        /// Subscribes to an event type with an async handler.
        /// </summary>
        /// <typeparam name="TEvent">Event type to subscribe to.</typeparam>
        /// <param name="handler">Async event handler delegate.</param>
        public static void SubscribeAsync<TEvent>(Func<TEvent, UniTask> handler) where TEvent : RetentiaEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var eventType = typeof(TEvent);

            lock (_lock)
            {
                if (!_handlers.ContainsKey(eventType))
                {
                    _handlers[eventType] = new List<Delegate>();
                }

                _handlers[eventType].Add(handler);
                DebugLogger.Verbose($"[EventBus] Subscribed async to {eventType.Name}");
            }
        }

        /// <summary>
        /// Unsubscribes a handler from an event type.
        /// </summary>
        /// <typeparam name="TEvent">Event type.</typeparam>
        /// <param name="handler">Handler to remove.</param>
        public static void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : RetentiaEvent
        {
            if (handler == null) return;

            var eventType = typeof(TEvent);

            lock (_lock)
            {
                if (_handlers.TryGetValue(eventType, out var handlers))
                {
                    handlers.Remove(handler);
                    DebugLogger.Verbose($"[EventBus] Unsubscribed from {eventType.Name}");
                }
            }
        }

        /// <summary>
        /// Unsubscribes an async handler from an event type.
        /// </summary>
        /// <typeparam name="TEvent">Event type.</typeparam>
        /// <param name="handler">Async handler to remove.</param>
        public static void UnsubscribeAsync<TEvent>(Func<TEvent, UniTask> handler) where TEvent : RetentiaEvent
        {
            if (handler == null) return;

            var eventType = typeof(TEvent);

            lock (_lock)
            {
                if (_handlers.TryGetValue(eventType, out var handlers))
                {
                    handlers.Remove(handler);
                    DebugLogger.Verbose($"[EventBus] Unsubscribed async from {eventType.Name}");
                }
            }
        }

        #endregion

        #region Publishing Methods

        /// <summary>
        /// Publishes an event to all subscribers synchronously.
        /// </summary>
        /// <typeparam name="TEvent">Event type.</typeparam>
        /// <param name="eventData">Event instance to publish.</param>
        public static void Publish<TEvent>(TEvent eventData) where TEvent : RetentiaEvent
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            var eventType = typeof(TEvent);
            AddToHistory(eventData);
            DebugLogger.Debug($"[EventBus] Publishing {eventType.Name}");

            List<Delegate> handlersCopy;

            lock (_lock)
            {
                if (!_handlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
                {
                    return;
                }

                handlersCopy = new List<Delegate>(handlers);
            }

            foreach (var handler in handlersCopy)
            {
                try
                {
                    if (handler is Action<TEvent> syncHandler)
                    {
                        syncHandler.Invoke(eventData);
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.Exception(ex, $"[EventBus] Error in handler for {eventType.Name}");
                }
            }
        }

        /// <summary>
        /// Publishes an event and awaits all async handlers.
        /// </summary>
        /// <typeparam name="TEvent">Event type.</typeparam>
        /// <param name="eventData">Event instance to publish.</param>
        public static async UniTask PublishAsync<TEvent>(TEvent eventData) where TEvent : RetentiaEvent
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            var eventType = typeof(TEvent);
            AddToHistory(eventData);
            DebugLogger.Debug($"[EventBus] Publishing async {eventType.Name}");

            List<Delegate> handlersCopy;

            lock (_lock)
            {
                if (!_handlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
                {
                    return;
                }

                handlersCopy = new List<Delegate>(handlers);
            }

            var tasks = new List<UniTask>();

            foreach (var handler in handlersCopy)
            {
                try
                {
                    if (handler is Action<TEvent> syncHandler)
                    {
                        syncHandler.Invoke(eventData);
                    }
                    else if (handler is Func<TEvent, UniTask> asyncHandler)
                    {
                        tasks.Add(asyncHandler.Invoke(eventData));
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.Exception(ex, $"[EventBus] Error in handler for {eventType.Name}");
                }
            }

            if (tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }
        }

        #endregion

        #region History Methods

        /// <summary>
        /// Adds an event to the history buffer.
        /// </summary>
        private static void AddToHistory(RetentiaEvent eventData)
        {
            lock (_lock)
            {
                if (_eventHistory.Count >= MAX_HISTORY_SIZE)
                {
                    _eventHistory.Dequeue();
                }
                _eventHistory.Enqueue(eventData);
            }
        }

        /// <summary>
        /// Gets recent event history.
        /// </summary>
        public static RetentiaEvent[] GetEventHistory()
        {
            lock (_lock)
            {
                return _eventHistory.ToArray();
            }
        }

        /// <summary>
        /// Clears event history.
        /// </summary>
        public static void ClearHistory()
        {
            lock (_lock)
            {
                _eventHistory.Clear();
            }
        }

        #endregion

        #region Cleanup Methods

        /// <summary>
        /// Clears all subscriptions and history.
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _handlers.Clear();
                _eventHistory.Clear();
            }

            DebugLogger.Info("[EventBus] Reset complete.");
        }

        /// <summary>
        /// Gets the count of subscribers for an event type.
        /// </summary>
        public static int GetSubscriberCount<TEvent>() where TEvent : RetentiaEvent
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
                {
                    return handlers.Count;
                }
                return 0;
            }
        }

        #endregion
    }

    #endregion
}
