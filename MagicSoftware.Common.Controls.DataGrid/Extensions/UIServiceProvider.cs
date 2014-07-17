﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using log4net;
using MagicSoftware.Common.Utils;

namespace MagicSoftware.Common.Controls.Table.Extensions
{
   /// <summary>
   ///
   /// </summary>
   /// <example>
   /// var service = UIServiceProvider.GetServiceProvider(element).GetService(serviceType);
   /// </example>
   public class UIServiceProvider : IWeakEventListener
   {
      #region Static members

      /// <summary>
      /// Defines the list of services assigned to an element.
      /// </summary>
      public static readonly DependencyProperty ServiceListProperty =
          DependencyProperty.RegisterAttached("ServiceList", typeof(UIServiceCollection), typeof(UIServiceProvider), new UIPropertyMetadata(new UIServiceCollection(), OnServiceListChanged));

      private static readonly DependencyProperty ServiceProviderProperty =
          DependencyProperty.RegisterAttached("ServiceProvider", typeof(UIServiceProvider), typeof(UIServiceProvider), new UIPropertyMetadata(null));

      public static RoutedEvent ServiceProviderFullyAttachedEvent = EventManager.RegisterRoutedEvent("ServiceProviderFullyAttachedEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIServiceProvider));

      public static void AddServiceProviderFullyAttachedHandler(DependencyObject obj, RoutedEventHandler handler)
      {
         UIElement uie = obj as UIElement;
         if (uie != null)
         {
            uie.AddHandler(UIServiceProvider.ServiceProviderFullyAttachedEvent, handler);
         }
      }

      public static void RemoveServiceProviderFullyAttachedHandler(DependencyObject obj, RoutedEventHandler handler)
      {
         UIElement uie = obj as UIElement;
         if (uie != null)
         {
            uie.RemoveHandler(UIServiceProvider.ServiceProviderFullyAttachedEvent, handler);
         }
      }

      private static ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

      public static IUIService GetService(FrameworkElement element, Type serviceType)
      {
         IUIService service = null;
         var serviceProvider = GetServiceProvider(element);
         if (serviceProvider == null)
         {
            var serviceList = GetServiceList(element);
            if (serviceList != null)
            {
               log.DebugFormat("Creating a new service provider for {0}", element);
               serviceProvider = new UIServiceProvider();
               SetServiceProvider(element, serviceProvider);
               serviceProvider.AttachToElement(element, serviceList);
            }
         }
         if (serviceProvider != null)
         {
            if (!serviceProvider.IsFullyAttached)
            {
               log.DebugFormat("Enforcing service provider load on {0}", element);
               serviceProvider.Element_Loaded(element, new RoutedEventArgs());
            }
            service = serviceProvider.GetService(serviceType);
         }
         return service;
      }

      public static T GetService<T>(FrameworkElement element)
      {
         return (T)GetService(element, typeof(T));
      }

      public static void SetServiceList(DependencyObject obj, UIServiceCollection value)
      {
         obj.SetValue(ServiceListProperty, value);
      }

      private static UIServiceCollection GetServiceList(DependencyObject obj)
      {
         return (UIServiceCollection)obj.GetValue(ServiceListProperty);
      }

      private static UIServiceProvider GetServiceProvider(DependencyObject obj)
      {
         return (UIServiceProvider)obj.GetValue(ServiceProviderProperty);
      }

      private static void OnServiceListChanged(DependencyObject sender, DependencyPropertyChangedEventArgs changeArgs)
      {
         var element = sender as FrameworkElement;
         var oldList = changeArgs.OldValue as UIServiceCollection;
         if (element != null)
         {
            if (oldList != null)
            {
               var oldProvider = GetServiceProvider(element);
               if (oldProvider != null)
                  oldProvider.Dispose();
            }

            // Create a service provider for the element.
            UIServiceProvider newProvider = null;
            var newList = changeArgs.NewValue as UIServiceCollection;
            if (newList != null)
            {
               newProvider = new UIServiceProvider();
               newProvider.AttachToElement(element, newList);
            }
            SetServiceProvider(element, newProvider);
         }
      }

      private static void SetServiceProvider(DependencyObject obj, UIServiceProvider value)
      {
         obj.SetValue(ServiceProviderProperty, value);
      }

      #endregion Static members

      //--------------------------------------------------------------------------------------------------------//
      // End of static part.
      //--------------------------------------------------------------------------------------------------------//

      private FrameworkElement element;
      private bool isFullyAttached = false;
      private Window owningWindow;
      private Dictionary<Type, IUIService> serviceImplementations = new Dictionary<Type, IUIService>();

      /// <summary>
      /// Gets a value denoting whether the service is fully attached to the element or not.
      /// 'Fully Attached' means that all services of this service provider where attached to
      /// a loaded element.
      /// </summary>
      public bool IsFullyAttached { get { return isFullyAttached; } }

      public void Dispose()
      {
         log.DebugFormat(FrameworkElementFormatter.GetInstance(), "Detaching service provider from {0}", element);
         if (owningWindow != null)
            owningWindow.Closed -= OwningWindow_Closed;
         LoadedEventManager.RemoveListener(element, this);
         UnloadedEventManager.RemoveListener(element, this);
         DetachFromElement(element, serviceImplementations.Values);
         foreach (var service in serviceImplementations.Values)
         {
            service.Dispose();
         }
      }

      void OnFullyAttached()
      {
         var args = new RoutedEventArgs(UIServiceProvider.ServiceProviderFullyAttachedEvent, this);
         this.element.RaiseEvent(args);
      }

      public IUIService GetService(Type serviceType)
      {
         if (serviceImplementations.ContainsKey(serviceType))
            return serviceImplementations[serviceType];

         return null;
      }

      public T GetService<T>()
         where T : class
      {
         return GetService(typeof(T)) as T;
      }

      private void AddServiceAs(Type serviceType, IUIService service)
      {
         log.DebugFormat("Adding service {0} => {1}", serviceType.Name, service.GetType().Name);
         serviceImplementations.Add(serviceType, service);
      }

      private void AttachToElement(FrameworkElement element, IEnumerable<IUIServiceFactory> serviceList)
      {
         if (isFullyAttached)
            return;

         log.DebugFormat(FrameworkElementFormatter.GetInstance(), "Attaching service list for {0}", element);
         this.element = element;
         foreach (var serviceFactory in serviceList)
         {
            var service = serviceFactory.CreateUIService();
            var serviceType = service.GetType();
            var customAttrs = serviceType.GetCustomAttributes(typeof(ImplementedServiceAttribute), true);
            if (customAttrs != null && customAttrs.Count() > 0)
            {
               foreach (ImplementedServiceAttribute customAttr in customAttrs)
               {
                  serviceType = customAttr.ImplementedServiceType;
                  AddServiceAs(serviceType, service);
               }
            }
            else
               AddServiceAs(serviceType, service);
         }
         LoadedEventManager.AddListener(element, this);
         UnloadedEventManager.AddListener(element, this);
         if (element.IsLoaded)
            Element_Loaded(element, new RoutedEventArgs());
      }

      private void DetachFromElement(FrameworkElement element, IEnumerable<IUIService> oldServiceList)
      {
         isFullyAttached = false;
         foreach (var service in oldServiceList)
            service.DetachFromElement(element);
      }

      private void Element_Loaded(object sender, RoutedEventArgs args)
      {
         isFullyAttached = true;

         foreach (var service in serviceImplementations.Values)
            service.AttachToElement((FrameworkElement)sender);

         owningWindow = UIUtils.GetAncestor<Window>(element);
         if (owningWindow != null)
            owningWindow.Closed += OwningWindow_Closed;

         OnFullyAttached();
      }

      private void Element_Unloaded(object sender, RoutedEventArgs args)
      {
         DetachFromElement((FrameworkElement)sender, serviceImplementations.Values);
      }

      private void OwningWindow_Closed(object sender, EventArgs e)
      {
         Dispose();
      }

      #region IWeakEventListener Members

      public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
      {
         if (managerType == typeof(LoadedEventManager))
            Element_Loaded(sender, (RoutedEventArgs)e);
         else if (managerType == typeof(UnloadedEventManager))
            Element_Unloaded(sender, (RoutedEventArgs)e);
         else
            return false;

         return true;
      }

      #endregion IWeakEventListener Members
   }

   internal class LoadedEventManager : RoutedEventManagerBase
   {
      public override RoutedEvent Event
      {
         get { return FrameworkElement.LoadedEvent; }
      }

      public static void AddListener(UIElement source, IWeakEventListener listener)
      {
         GetManager<LoadedEventManager>().ProtectedAddListener(source, listener);
      }

      public static void RemoveListener(UIElement source, IWeakEventListener listener)
      {
         GetManager<LoadedEventManager>().ProtectedRemoveListener(source, listener);
      }
   }

   internal abstract class RoutedEventManagerBase : WeakEventManager
   {
      public abstract RoutedEvent Event { get; }

      protected static T GetManager<T>()
         where T : RoutedEventManagerBase, new()
      {
         var manager_type = typeof(T);
         var manager = WeakEventManager.GetCurrentManager(manager_type) as T;

         if (manager == null)
         {
            manager = new T();
            WeakEventManager.SetCurrentManager(manager_type, manager);
         }

         return manager;
      }

      protected override void StartListening(object source)
      {
         var sourceElement = (UIElement)source;
         sourceElement.AddHandler(Event, new RoutedEventHandler(OnRoutedEvent), true);
      }

      protected override void StopListening(object source)
      {
         var sourceElement = (UIElement)source;
         sourceElement.RemoveHandler(Event, new RoutedEventHandler(OnRoutedEvent));
      }

      private void OnRoutedEvent(object sender, RoutedEventArgs args)
      {
         DeliverEvent(sender, args);
      }
   }

   internal class UnloadedEventManager : RoutedEventManagerBase
   {
      public override RoutedEvent Event
      {
         get { return FrameworkElement.UnloadedEvent; }
      }

      public static void AddListener(UIElement source, IWeakEventListener listener)
      {
         GetManager<UnloadedEventManager>().ProtectedAddListener(source, listener);
      }

      public static void RemoveListener(UIElement source, IWeakEventListener listener)
      {
         GetManager<UnloadedEventManager>().ProtectedRemoveListener(source, listener);
      }
   }
}