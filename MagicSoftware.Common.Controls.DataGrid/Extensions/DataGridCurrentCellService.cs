﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using log4net;
using MagicSoftware.Common.Utils;

namespace MagicSoftware.Common.Controls.Table.Extensions
{
   /// <summary>
   /// Implementation of ICurrentCellService for DataGrid. The implementation operates only
   /// on accessible cells - i.e. cells that have already been loaded. It cannot move to a cell
   /// placed on an item that was not loaded (due to virtualization, for example).
   /// </summary>
   [ImplementedService(typeof(ICurrentCellService))]
   internal class DataGridCurrentCellService : ICurrentCellService, IUIService
   {
      /// <summary>
      /// Determines whether the current cell change is of an external origin, i.e. the
      /// cell position was changed by another component; or the change is caused by
      /// this class (self induced).
      /// </summary>
      protected readonly AutoResetFlag isSelfInducedCellChange = new AutoResetFlag();

      private System.Windows.Controls.DataGrid dataGrid;
      private ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

      #region CurrentCellChanging event.

      /// <summary>
      /// Event raised before changing the 'current item' indicator on the items control.
      /// </summary>
      public event EventHandler<PreviewChangeEventArgs> PreviewCurrentCellChanging;

      /// <summary>
      /// Raises the PreviewCurrentCellChangingEvent without allowing canceling of the event.
      /// </summary>
      public void RaiseNonCancelablePreviewCurrentCellChangingEvent(UniversalCellInfo newValue)
      {
         if (PreviewCurrentCellChanging != null)
         {
            var eventArgs = new PreviewChangeEventArgs(CurrentCell, newValue, false);
            PreviewCurrentCellChanging(this, eventArgs);
         }
      }

      /// <summary>
      /// Raises the PreviewCurrentCellChangingEvent, allowing the handlers to cancel it,
      /// returning the result in 'canceled'
      /// </summary>
      ///
      /// <param name="canceled">Returns whether any of the event handlers canceled the event.</param>
      public void RaisePreviewCurrentCellChangingEvent(UniversalCellInfo newValue, out bool canceled)
      {
         canceled = false;
         if (PreviewCurrentCellChanging != null)
         {
            var eventArgs = new PreviewChangeEventArgs(CurrentCell, newValue, true);
            PreviewCurrentCellChanging(this, eventArgs);
            canceled = eventArgs.Canceled;
         }
      }

      #endregion CurrentCellChanging event.

      #region CurrentCellChanged event.

      /// <summary>
      /// Event raised before changing the 'current item' indicator on the items control.
      /// </summary>
      public event EventHandler CurrentCellChanged;

      public void RaiseCurrentCellChangedEvent()
      {
         if (CurrentCellChanged != null)
            CurrentCellChanged(this, new EventArgs());
      }

      #endregion CurrentCellChanged event.

      public UniversalCellInfo CurrentCell
      {
         get;
         private set;
      }

      public FrameworkElement CurrentCellElement
      {
         get 
         {
            if (CurrentRowCellEnumerationService != null)
            {
               return CurrentRowCellEnumerationService.GetCellAt(CurrentCell.CellIndex);
            }
            return null;
         }
      }

      public virtual bool IsAttached { get { return dataGrid != null; } }

      public bool IsCellVisible
      {
         get
         {
            return false;
         }
      }

      private FrameworkElement CurrentItemContainer
      {
         get { return dataGrid.ItemContainerGenerator.ContainerFromItem(dataGrid.CurrentItem) as FrameworkElement; }
      }

      private ICellEnumerationService CurrentRowCellEnumerationService { get; set; }

      public void AttachToElement(FrameworkElement element)
      {
         log.DebugFormat("Attaching service provider {0} to {1}", this.GetType().Name, element);
         this.dataGrid = (DataGrid)element;
         //currentRowService = UIServiceProvider.GetService<ICurrentItemService>(element);
         dataGrid.CurrentCellChanged += DataGrid_CurrentCellChanged;
         dataGrid.ColumnDisplayIndexChanged += DataGrid_ColumnDisplayIndexChanged;

         UpdateCurrentCell();
      }

      public void DetachFromElement(FrameworkElement element)
      {
         dataGrid.CurrentCellChanged -= DataGrid_CurrentCellChanged;
         dataGrid.ColumnDisplayIndexChanged -= DataGrid_ColumnDisplayIndexChanged;
      }

      public bool MoveDown(uint distance)
      {
         int newIndex = IndexOf(CurrentCell.Item) + (int)distance;
         int maxIndex = dataGrid.Items.Count - 1;
         if (newIndex > maxIndex)
            return false;

         return MoveTo(new UniversalCellInfo(dataGrid.Items[newIndex], CurrentCell.CellIndex));
      }

      public bool MoveLeft(uint distance)
      {
         int newCellIndex = CurrentCell.CellIndex - (int)distance;
         if (newCellIndex >= 0)
            return MoveTo(new UniversalCellInfo(CurrentCell.Item, newCellIndex));
         else
            return false;
      }

      public bool MoveRight(uint distance)
      {
         int newCellIndex = CurrentCell.CellIndex + (int)distance;
         var rowEnumSvc = GetRowEnumerationServiceForItem(CurrentCell.Item);
         if (newCellIndex < rowEnumSvc.CellCount)
            return MoveTo(new UniversalCellInfo(CurrentCell.Item, newCellIndex));
         else
            return false;
      }

      public bool MoveTo(UniversalCellInfo targetCell)
      {
         bool canceled;

         log.DebugFormat("Moving to cell {0}", targetCell);

         UpdateCurrentCell();

         if (!CanMoveTo(targetCell))
            return CannotMoveToCell(targetCell);

         RaisePreviewCurrentCellChangingEvent(targetCell, out canceled);
         if (canceled)
            return OperationCanceled();

         using (isSelfInducedCellChange.Set())
         {
            if (targetCell.CellIndex >= dataGrid.Columns.Count)
               return false;

            var rowEnumSvc = GetRowEnumerationServiceForItem(targetCell.Item);
            // If changing the row type (according to the row service), we should retake the cell index.
            // Will this be a problem with the mouse? 
            if (!rowEnumSvc.ServiceGroupIdentifier.Equals(CurrentRowCellEnumerationService.ServiceGroupIdentifier))
               rowEnumSvc.MoveToCell(rowEnumSvc.CurrentCellIndex);
            else
               rowEnumSvc.MoveToCell(targetCell.CellIndex);

            UpdateCurrentCell();
            RaiseCurrentCellChangedEvent();
         }

         return CurrentCell.Equals(targetCell);
      }

      private bool OperationCanceled()
      {
         log.DebugFormat("-- Operation was canceled.");
         return false;
      }

      public bool MoveToBottom()
      {
         var lastItem = dataGrid.Items[dataGrid.Items.Count - 1];
         if (object.ReferenceEquals(CurrentCell.Item, lastItem))
            return true;
         return MoveTo(new UniversalCellInfo(lastItem, dataGrid.CurrentColumn.DisplayIndex));
      }

      public bool MoveToLeftMost()
      {
         return MoveTo(new UniversalCellInfo(CurrentCell.Item, 0));
      }

      public bool MoveToRightMost()
      {
         return MoveTo(new UniversalCellInfo(CurrentCell.Item, CurrentRowCellEnumerationService.CellCount - 1));
      }

      public bool MoveToTop()
      {
         var firstItem = dataGrid.Items[0];
         if (object.ReferenceEquals(CurrentCell.Item, firstItem))
            return true;

         var targetColumnIndex = (dataGrid.CurrentColumn ?? dataGrid.Columns[0]).DisplayIndex;
         return MoveTo(new UniversalCellInfo(firstItem, targetColumnIndex));
      }

      public bool MoveUp(uint distance)
      {
         log.DebugFormat("Trying to move {0} lines up.", distance);

         int newIndex = IndexOf(CurrentCell.Item) - (int)distance;
         if (newIndex < 0)
            return InvalidLineIndex(newIndex);

         return MoveTo(new UniversalCellInfo(dataGrid.Items[newIndex], CurrentCell.CellIndex));
      }

      private bool CanMoveTo(UniversalCellInfo targetCell)
      {
         if (IndexOf(targetCell.Item) < 0)
            return false;

         var targetRowCellEnumerationService = GetRowEnumerationServiceForItem(targetCell.Item);
         if (targetRowCellEnumerationService == null)
            return false;

         if (targetCell.CellIndex < 0 || targetCell.CellIndex >= targetRowCellEnumerationService.CellCount)
            return false;

         return true;
      }

      private bool CannotMoveToCell(UniversalCellInfo targetCell)
      {
         log.DebugFormat("Cannot move to cell {0}", targetCell);
         return false;
      }

      private void DataGrid_ColumnDisplayIndexChanged(object sender, DataGridColumnEventArgs e)
      {
         if (!isSelfInducedCellChange.IsSet)
            UpdateCurrentCell();
      }

      private void DataGrid_CurrentCellChanged(object sender, EventArgs e)
      {
         if (!isSelfInducedCellChange.IsSet)
            RaiseNonCancelablePreviewCurrentCellChangingEvent(CurrentCell);
         UpdateCurrentCell();
         if (!isSelfInducedCellChange.IsSet)
            RaiseCurrentCellChangedEvent();
      }

      private UIElement ForceContainerGeneration(object item)
      {
         int itemIndex = IndexOf(item);
         UIElement container = null;
         bool isNewlyRealized = false;
         IItemContainerGenerator generator = dataGrid.ItemContainerGenerator;
         GeneratorPosition gp = generator.GeneratorPositionFromIndex(itemIndex);
         using (generator.StartAt(gp, GeneratorDirection.Forward, true))
         {
            isNewlyRealized = false;
            container = generator.GenerateNext(out isNewlyRealized) as UIElement;
         }
         return container;
      }

      private FrameworkElement GetItemContainer(object item)
      {
         return dataGrid.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
      }

      private ICellEnumerationService GetRowEnumerationServiceForItem(object item)
      {
         log.DebugFormat(FrameworkElementFormatter.GetInstance(), "Trying to get row enumeration service for {0}", item);

         var currentRow = GetItemContainer(item) as DataGridRow;
         if (currentRow == null)
         {
            log.Debug("-- Failed retrieving item container from");
            return null;
         }

         var service = UIServiceProvider.GetService<ICellEnumerationService>(currentRow);

         log.DebugFormat("-- Found service provider {0}", service);

         //((IUIService)service).AttachToElement(currentRow);

         return service;
      }

      private int IndexOf(object item)
      {
         if (item == null)
            return -1;

         return dataGrid.Items.IndexOf(item);
      }

      private bool InvalidLineIndex(int index)
      {
         log.DebugFormat("-- Computed line index {0} is invalid. Aborting action.", index);
         return false;
      }

      private void UpdateCurrentCell()
      {
         using (LoggingExtensions.Indent())
         {
            log.Debug("Updating current cell info");
            CurrentRowCellEnumerationService = GetRowEnumerationServiceForItem(dataGrid.CurrentItem);
            if (CurrentRowCellEnumerationService != null)
            {
               //if (!((IUIService)CurrentRowCellEnumerationService).IsAttached)
               //   Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Loaded, new Action(UpdateCurrentCell));
               CurrentCell = CurrentRowCellEnumerationService.GetCurrentCellInfo();
            }
         }
      }

      #region IDisposable Members

      public void Dispose()
      {
      }

      #endregion IDisposable Members
   }
}