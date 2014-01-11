using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MagicSoftware.Common.Controls.Proxies;
using MagicSoftware.Common.Controls.Table.CellTypes;
using System;
using MagicSoftware.Common.Utils;

namespace MagicSoftware.Common.Controls.Table.Extensions
{
   [ImplementedServiceAttribute(typeof(ICurrentItemService))]
   abstract class CurrentItemServiceBase : ICurrentItemService, IUIService
   {
      #region CurrentChanging attached event.

      public static readonly RoutedEvent PreviewCurrentChangingEvent = EventManager.RegisterRoutedEvent("PreviewCurrentChanging", RoutingStrategy.Tunnel, typeof(CancelableRoutedEventArgs), typeof(CurrentItemServiceBase));

      Dictionary<CancelableRoutedEventHandler, RoutedEventHandler> currentChangingEventHandlers = new Dictionary<CancelableRoutedEventHandler, RoutedEventHandler>();

      UIElement element;

      /// <summary>
      /// Event raised before changing the 'current item' indicator on the items control.
      /// </summary>
      public event CancelableRoutedEventHandler PreviewCurrentChanging
      {
         add
         {
            var handler = new RoutedEventHandler((obj, args) => { value(obj, (CancelableRoutedEventArgs)args); });
            element.AddHandler(PreviewCurrentChangingEvent, handler);
            currentChangingEventHandlers.Add(value, handler);
         }
         remove
         {
            RoutedEventHandler handler;
            if (currentChangingEventHandlers.TryGetValue(value, out handler))
               element.RemoveHandler(PreviewCurrentChangingEvent, handler);
         }
      }

      /// <summary>
      /// Raises the PreviewCurrentChangingEvent, allowing the handlers to cancel it,
      /// returning the result in 'canceled'
      /// </summary>
      /// 
      /// <param name="canceled">Returns whether any of the event handlers canceled the event.</param>
      public void RaisePreviewCurrentChangingEvent(object newValue, out bool canceled)
      {
         CancelableRoutedEventArgs eventArgs = new PreviewChangeEventArgs(PreviewCurrentChangingEvent, element, CurrentItem, newValue, true);
         element.RaiseEvent(eventArgs);
         canceled = eventArgs.Canceled;
      }

      /// <summary>
      /// Raises the PreviewCurrentChangingEvent without allowing canceling of the event.
      /// </summary>
      public void RaiseNonCancelablePreviewCurrentChangingEvent(object newValue)
      {
         CancelableRoutedEventArgs eventArgs = new PreviewChangeEventArgs(PreviewCurrentChangingEvent, element, CurrentItem, newValue, false);
         element.RaiseEvent(eventArgs);
      }

      #endregion

      #region CurrentChanging attached event.

      public static readonly RoutedEvent CurrentChangedEvent = EventManager.RegisterRoutedEvent("CurrentChanged", RoutingStrategy.Tunnel, typeof(RoutedEventArgs), typeof(CurrentItemServiceBase));

      /// <summary>
      /// Event raised before changing the 'current item' indicator on the items control.
      /// </summary>
      public event RoutedEventHandler CurrentChanged
      {
         add { element.AddHandler(CurrentChangedEvent, value); }
         remove { element.RemoveHandler(CurrentChangedEvent, value); }
      }

      public void RaiseCurrentChangedEvent()
      {
         RoutedEventArgs eventArgs = new RoutedEventArgs(CurrentChangedEvent, element);
         element.RaiseEvent(eventArgs);
      }

      #endregion


      protected SharedObjectsService sharedObjectsService { get; private set; }

      FrameworkElementProxy proxy;

      public CurrentItemServiceBase()
      {
         //previewCurrentChangingEvent = new SmartEvent<CancelableRoutedEventHandler>(this);
      }

      [Obsolete]
      public CurrentItemServiceBase(UIElement servedElement)
      {
         SetElement(servedElement);
      }

      public virtual void SetElement(UIElement servedElement)
      {
         proxy = FrameworkElementProxy.GetProxy(servedElement);
         sharedObjectsService = proxy.GetAdapter<SharedObjectsService>();
         element = servedElement;
      }

      public abstract object CurrentItem { get; }
      public abstract int CurrentPosition { get; }

      public abstract bool MoveCurrentTo(object item);
      public abstract bool MoveCurrentToFirst();
      public abstract bool MoveCurrentToNext();
      public abstract bool MoveCurrentToPrevious();
      public abstract bool MoveCurrentToLast();
      public abstract bool MoveCurrentToPosition(int position);
      public abstract bool MoveCurrentToRelativePosition(int offset);

      #region IUIService Members

      void IUIService.SetElement(UIElement element)
      {
         SetElement(element);
      }

      #endregion

      #region IDisposable Members

      void IDisposable.Dispose()
      {
         element = null;
         sharedObjectsService = null;
         proxy = null;
      }

      #endregion
   }
}

