// -----------------------------------------------------------------------------
// NotificationService.cs
// Service for managing in-game notifications and popups.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Retentia.Data;
using UnityEngine;

namespace Retentia.Core
{
    #region Notification Type

    /// <summary>
    /// Types of in-game notifications.
    /// </summary>
    public enum NotificationType
    {
        Info = 0,
        Offer = 1,
        Reward = 2,
        Achievement = 3,
        Warning = 4,
        System = 5,
        Social = 6,
        Event = 7
    }

    #endregion

    #region Notification Data

    /// <summary>
    /// Data for a notification.
    /// </summary>
    [Serializable]
    public class NotificationData
    {
        public string Id;
        public NotificationType Type;
        public string Title;
        public string Message;
        public Sprite Icon;
        public float Duration;
        public string ActionButtonText;
        public Action OnAction;
        public Action OnDismiss;
        public DateTime CreatedAt;
        public bool IsRead;
        public int Priority;
    }

    #endregion

    #region Notification Service

    /// <summary>
    /// Service for managing in-game notifications.
    /// Provides queue management and display coordination.
    /// </summary>
    public class NotificationService : IRetentiaService
    {
        #region Fields

        private readonly RetentionConfigSO _config;
        private readonly Queue<NotificationData> _notificationQueue;
        private readonly List<NotificationData> _notificationHistory;
        private bool _isInitialized;
        private bool _isDisplaying;
        private NotificationData _currentNotification;

        private const int MAX_HISTORY = 50;
        private const float DEFAULT_DURATION = 5f;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when a notification is shown.
        /// </summary>
        public event Action<NotificationData> OnNotificationShown;

        /// <summary>
        /// Event fired when a notification is dismissed.
        /// </summary>
        public event Action<NotificationData> OnNotificationDismissed;

        /// <summary>
        /// Event fired when a notification action is triggered.
        /// </summary>
        public event Action<NotificationData> OnNotificationAction;

        #endregion

        #region Properties

        public bool IsInitialized => _isInitialized;
        public int InitializationPriority => 50;
        public int QueuedCount => _notificationQueue.Count;
        public bool IsDisplaying => _isDisplaying;
        public NotificationData CurrentNotification => _currentNotification;
        public IReadOnlyList<NotificationData> History => _notificationHistory;

        #endregion

        #region Constructor

        public NotificationService(RetentionConfigSO config = null)
        {
            _config = config;
            _notificationQueue = new Queue<NotificationData>();
            _notificationHistory = new List<NotificationData>();
        }

        #endregion

        #region IRetentiaService Implementation

        public async UniTask InitializeAsync()
        {
            DebugLogger.LogServiceInit("NotificationService");

            _notificationQueue.Clear();
            _notificationHistory.Clear();
            _isDisplaying = false;
            _currentNotification = null;
            _isInitialized = true;

            DebugLogger.LogServiceReady("NotificationService");
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            DebugLogger.LogServiceDispose("NotificationService");
            _notificationQueue.Clear();
            _notificationHistory.Clear();
            _isInitialized = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows a notification immediately or queues it.
        /// </summary>
        public NotificationData ShowNotification(
            string title,
            string message,
            NotificationType type = NotificationType.Info,
            float duration = DEFAULT_DURATION,
            Sprite icon = null,
            string actionText = null,
            Action onAction = null,
            Action onDismiss = null,
            int priority = 0)
        {
            var notification = new NotificationData
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                Title = title,
                Message = message,
                Icon = icon,
                Duration = duration,
                ActionButtonText = actionText,
                OnAction = onAction,
                OnDismiss = onDismiss,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                Priority = priority
            };

            QueueNotification(notification);
            return notification;
        }

        /// <summary>
        /// Shows an offer notification.
        /// </summary>
        public NotificationData ShowOfferNotification(OfferDefinitionSO offer, Action onAccept, Action onDecline = null)
        {
            return ShowNotification(
                title: offer.DisplayName,
                message: offer.Description,
                type: NotificationType.Offer,
                duration: _config?.OfferDisplayDuration ?? 10f,
                icon: offer.Icon,
                actionText: offer.IsFree ? "Claim" : "Get Now",
                onAction: onAccept,
                onDismiss: onDecline,
                priority: 10
            );
        }

        /// <summary>
        /// Shows a reward notification.
        /// </summary>
        public NotificationData ShowRewardNotification(string rewardName, int amount, Sprite icon = null)
        {
            return ShowNotification(
                title: "Reward!",
                message: $"You received {amount}x {rewardName}!",
                type: NotificationType.Reward,
                duration: 3f,
                icon: icon,
                priority: 5
            );
        }

        /// <summary>
        /// Shows a warning notification.
        /// </summary>
        public NotificationData ShowWarning(string message, float duration = 3f)
        {
            return ShowNotification(
                title: "Warning",
                message: message,
                type: NotificationType.Warning,
                duration: duration,
                priority: 8
            );
        }

        /// <summary>
        /// Dismisses the current notification.
        /// </summary>
        public void DismissCurrentNotification()
        {
            if (_currentNotification != null)
            {
                _currentNotification.OnDismiss?.Invoke();
                OnNotificationDismissed?.Invoke(_currentNotification);
                _currentNotification = null;
                _isDisplaying = false;

                // Process next in queue
                ProcessQueueAsync().Forget();
            }
        }

        /// <summary>
        /// Triggers the action on the current notification.
        /// </summary>
        public void TriggerCurrentAction()
        {
            if (_currentNotification != null)
            {
                _currentNotification.OnAction?.Invoke();
                OnNotificationAction?.Invoke(_currentNotification);
                DismissCurrentNotification();
            }
        }

        /// <summary>
        /// Clears all queued notifications.
        /// </summary>
        public void ClearQueue()
        {
            _notificationQueue.Clear();
        }

        /// <summary>
        /// Gets unread notification count.
        /// </summary>
        public int GetUnreadCount()
        {
            int count = 0;
            foreach (var n in _notificationHistory)
            {
                if (!n.IsRead) count++;
            }
            return count;
        }

        /// <summary>
        /// Marks all notifications as read.
        /// </summary>
        public void MarkAllAsRead()
        {
            foreach (var n in _notificationHistory)
            {
                n.IsRead = true;
            }
        }

        #endregion

        #region Private Methods

        private void QueueNotification(NotificationData notification)
        {
            // Add to history
            _notificationHistory.Add(notification);
            if (_notificationHistory.Count > MAX_HISTORY)
            {
                _notificationHistory.RemoveAt(0);
            }

            // Queue for display
            _notificationQueue.Enqueue(notification);

            DebugLogger.Debug($"[NotificationService] Queued: {notification.Title}");

            // Start processing if not already displaying
            if (!_isDisplaying)
            {
                ProcessQueueAsync().Forget();
            }
        }

        private async UniTask ProcessQueueAsync()
        {
            while (_notificationQueue.Count > 0)
            {
                _currentNotification = _notificationQueue.Dequeue();
                _isDisplaying = true;

                DebugLogger.Debug($"[NotificationService] Showing: {_currentNotification.Title}");

                OnNotificationShown?.Invoke(_currentNotification);

                // Wait for duration
                await UniTask.Delay(TimeSpan.FromSeconds(_currentNotification.Duration));

                // Auto-dismiss if still displaying the same notification
                if (_isDisplaying && _currentNotification != null)
                {
                    DismissCurrentNotification();
                }
            }

            _isDisplaying = false;
        }

        #endregion
    }

    #endregion
}
