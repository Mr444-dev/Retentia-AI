// -----------------------------------------------------------------------------
// RetentiaServiceLocator.cs
// Service locator pattern implementation for dependency resolution.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Retentia.Core
{
    #region Service Locator

    /// <summary>
    /// Central service locator for managing Retentia service dependencies.
    /// Provides registration, resolution, and lifecycle management.
    /// </summary>
    public static class RetentiaServiceLocator
    {
        #region Fields

        /// <summary>
        /// Dictionary storing registered services by their interface type.
        /// </summary>
        private static readonly Dictionary<Type, IRetentiaService> _services = new Dictionary<Type, IRetentiaService>();

        /// <summary>
        /// List of all registered services for ordered initialization/disposal.
        /// </summary>
        private static readonly List<IRetentiaService> _orderedServices = new List<IRetentiaService>();

        /// <summary>
        /// Lock object for thread-safe operations.
        /// </summary>
        private static readonly object _lock = new object();

        /// <summary>
        /// Whether the service locator has been initialized.
        /// </summary>
        private static bool _isInitialized;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the service locator is initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the count of registered services.
        /// </summary>
        public static int ServiceCount => _services.Count;

        #endregion

        #region Registration Methods

        /// <summary>
        /// Registers a service instance with the locator.
        /// </summary>
        /// <typeparam name="TInterface">Service interface type.</typeparam>
        /// <param name="service">Service instance to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when service is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when service type is already registered.</exception>
        public static void Register<TInterface>(IRetentiaService service) where TInterface : class, IRetentiaService
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service), "[Retentia] Cannot register null service.");
            }

            var interfaceType = typeof(TInterface);

            lock (_lock)
            {
                if (_services.ContainsKey(interfaceType))
                {
                    throw new InvalidOperationException($"[Retentia] Service of type {interfaceType.Name} is already registered.");
                }

                _services[interfaceType] = service;
                _orderedServices.Add(service);

                DebugLogger.Log($"[Retentia] Registered service: {interfaceType.Name}", LogLevel.Info);
            }
        }

        /// <summary>
        /// Unregisters a service from the locator.
        /// </summary>
        /// <typeparam name="TInterface">Service interface type to unregister.</typeparam>
        /// <returns>True if service was found and removed.</returns>
        public static bool Unregister<TInterface>() where TInterface : class, IRetentiaService
        {
            var interfaceType = typeof(TInterface);

            lock (_lock)
            {
                if (_services.TryGetValue(interfaceType, out var service))
                {
                    _services.Remove(interfaceType);
                    _orderedServices.Remove(service);
                    service.Dispose();

                    DebugLogger.Log($"[Retentia] Unregistered service: {interfaceType.Name}", LogLevel.Info);
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Resolution Methods

        /// <summary>
        /// Gets a registered service by interface type.
        /// </summary>
        /// <typeparam name="TInterface">Service interface type.</typeparam>
        /// <returns>Service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when service is not registered.</exception>
        public static TInterface Get<TInterface>() where TInterface : class, IRetentiaService
        {
            var interfaceType = typeof(TInterface);

            lock (_lock)
            {
                if (_services.TryGetValue(interfaceType, out var service))
                {
                    return service as TInterface;
                }
            }

            throw new InvalidOperationException($"[Retentia] Service of type {interfaceType.Name} is not registered.");
        }

        /// <summary>
        /// Tries to get a registered service.
        /// </summary>
        /// <typeparam name="TInterface">Service interface type.</typeparam>
        /// <param name="service">Output service instance.</param>
        /// <returns>True if service was found.</returns>
        public static bool TryGet<TInterface>(out TInterface service) where TInterface : class, IRetentiaService
        {
            var interfaceType = typeof(TInterface);
            service = null;

            lock (_lock)
            {
                if (_services.TryGetValue(interfaceType, out var foundService))
                {
                    service = foundService as TInterface;
                    return service != null;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a service type is registered.
        /// </summary>
        /// <typeparam name="TInterface">Service interface type.</typeparam>
        /// <returns>True if registered.</returns>
        public static bool IsRegistered<TInterface>() where TInterface : class, IRetentiaService
        {
            lock (_lock)
            {
                return _services.ContainsKey(typeof(TInterface));
            }
        }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Initializes all registered services in priority order.
        /// </summary>
        public static async Cysharp.Threading.Tasks.UniTask InitializeAllAsync()
        {
            DebugLogger.Log("[Retentia] Initializing all services...", LogLevel.Info);

            // Sort by initialization priority
            _orderedServices.Sort((a, b) => a.InitializationPriority.CompareTo(b.InitializationPriority));

            foreach (var service in _orderedServices)
            {
                if (!service.IsInitialized)
                {
                    await service.InitializeAsync();
                    DebugLogger.Log($"[Retentia] Initialized: {service.GetType().Name}", LogLevel.Info);
                }
            }

            _isInitialized = true;
            DebugLogger.Log("[Retentia] All services initialized successfully.", LogLevel.Info);
        }

        /// <summary>
        /// Disposes all registered services and clears the locator.
        /// </summary>
        public static void DisposeAll()
        {
            DebugLogger.Log("[Retentia] Disposing all services...", LogLevel.Info);

            lock (_lock)
            {
                // Dispose in reverse order
                for (int i = _orderedServices.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        _orderedServices[i].Dispose();
                        DebugLogger.Log($"[Retentia] Disposed: {_orderedServices[i].GetType().Name}", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Log($"[Retentia] Error disposing service: {ex.Message}", LogLevel.Error);
                    }
                }

                _services.Clear();
                _orderedServices.Clear();
                _isInitialized = false;
            }

            DebugLogger.Log("[Retentia] All services disposed.", LogLevel.Info);
        }

        /// <summary>
        /// Resets the service locator (for testing purposes).
        /// </summary>
        public static void Reset()
        {
            DisposeAll();
        }

        #endregion
    }

    #endregion
}
