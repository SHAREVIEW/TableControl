using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MagicSoftware.Common.Controls.Proxies;
using MagicSoftware.Common.Controls.Table.CellTypes;

namespace MagicSoftware.Common.Controls.Table.Extensions
{
   abstract class CurrentItemServiceBase : ICurrentItemProvider
   {
      public event CancelableRoutedEventHandler PreviewCurrentChanging
      {
         add { previewCurrentChangingEventService.PreviewCurrentChanging += value; }
         remove { previewCurrentChangingEventService.PreviewCurrentChanging -= value; }
      }

      public event RoutedEventHandler CurrentChanged
      {
         add { currentChangedEventService.CurrentChanged += value; }
         remove { currentChangedEventService.CurrentChanged -= value; }
      }

      PreviewCurrentChangingEventService previewCurrentChangingEventService;
      CurrentChangedEventService currentChangedEventService;
      protected SharedObjectsService sharedObjectsService { get; private set; }

      FrameworkElementProxy proxy;

      public CurrentItemServiceBase(UIElement servedElement)
      {
         proxy = FrameworkElementProxy.GetProxy(servedElement);
         previewCurrentChangingEventService = proxy.GetAdapter<PreviewCurrentChangingEventService>();
         currentChangedEventService = proxy.GetAdapter<CurrentChangedEventService>();
         sharedObjectsService = proxy.GetAdapter<SharedObjectsService>();
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

      protected virtual void RaisePreviewCurrentChangingEvent(object newValue, out bool canceled)
      {
         previewCurrentChangingEventService.RaisePreviewCurrentChangingEvent(CurrentItem, newValue, out canceled);
      }

      protected virtual void RaiseNonCancelablePreviewCurrentChangingEvent(object newValue)
      {
         previewCurrentChangingEventService.RaiseNonCancelablePreviewCurrentChangingEvent(CurrentItem, newValue);
      }

      protected virtual void RaiseCurrentChangedEvent()
      {
         currentChangedEventService.RaiseCurrentChangedEvent();
      }
   }
}
