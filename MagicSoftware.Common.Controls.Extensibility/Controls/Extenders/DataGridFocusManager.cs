﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using log4net;

namespace MagicSoftware.Common.Controls.ExtendersX
{
   public class DataGridFocusManager : FocusManagerBase
   {
      ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

      Selector monitoredElement;

      ItemCollection currentItemCollection;

      #region IFocusManager Members

      protected override void Initialize()
      {
         Debug.Assert(AttachedElement is Selector);
         monitoredElement = AttachedElement as Selector;
         currentItemCollection = null;
         Items = monitoredElement.Items;
         monitoredElement.DataContextChanged += new DependencyPropertyChangedEventHandler(monitoredElement_DataContextChanged);
         monitoredElement.SelectionChanged += new SelectionChangedEventHandler(monitoredElement_SelectionChanged);
      }

      void monitoredElement_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         log.DebugFormat("Selection changed on " + sender);
         using (InhibitFocusUpdates())
         {
            Items.MoveCurrentTo(monitoredElement.SelectedItem);
         }
      }

      public override void DetachFromElement()
      {
         currentItemCollection = null;
      }

      protected override void PerformFocusUpdate()
      {
         int itemIndex = monitoredElement.Items.CurrentPosition;
         //monitoredElement.SelectedIndex = itemIndex;
         if (itemIndex < 0)
            return;

         FrameworkElement itemContainer = monitoredElement.ItemContainerGenerator.ContainerFromIndex(itemIndex) as FrameworkElement;
         if (itemContainer != null )
         {
            itemContainer.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
         }
      }

      #endregion

      void Items_CurrentChanging(object sender, CurrentChangingEventArgs e)
      {
         log.DebugFormat("CurrentItem is changing");
      }

      void Items_CurrentChanged(object sender, EventArgs e)
      {
         log.DebugFormat("CurrentItem was changed.");
         UpdateFocus();
      }

      void monitoredElement_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
      {
      }

      ItemCollection Items
      {
         get { return currentItemCollection; }
         set
         {
            if (currentItemCollection != null)
            {
               currentItemCollection.CurrentChanging -= new CurrentChangingEventHandler(Items_CurrentChanging);
               currentItemCollection.CurrentChanged -= new EventHandler(Items_CurrentChanged);
            }

            currentItemCollection = value;

            if (currentItemCollection != null)
            {
               currentItemCollection.CurrentChanging += new CurrentChangingEventHandler(Items_CurrentChanging);
               currentItemCollection.CurrentChanged += new EventHandler(Items_CurrentChanged);
            }
         }
      }

   }
}
