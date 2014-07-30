﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using log4net;
using MagicSoftware.Common.Controls.Extensibility;

namespace MagicSoftware.Common.Controls.Table.Extensions.Editing
{
   internal class SingleLineEditStateMachine : DataGridEditStateMachine
   {
      public SingleLineEditStateMachine()
      {
      }

      public ICurrentItemService CurrentItemTracker { get; set; }

      internal override void ProcessKey(KeyEventArgs e)
      {
         log.DebugFormat("Processing key {0} on {1}", e.Key, this);
         var editProxy = UIServiceProvider.GetService<IElementEditStateService>(TargetElement);
         switch (e.Key)
         {
            case Key.Enter:
               if (editProxy.IsEditingField)
                  editProxy.CommitItemEdit();
               else
                  editProxy.BeginItemEdit();
               e.Handled = true;
               break;

            case Key.F2:
               if (!editProxy.IsEditingField)
                  editProxy.BeginItemEdit();
               e.Handled = true;
               break;

            case Key.Escape:
               if (editProxy.IsEditingField)
                  editProxy.CancelItemEdit();
               e.Handled = true;
               break;

            default:
               break;
         }
      }

      protected override bool CanLeaveCurrentLine()
      {
         var editProxy = UIServiceProvider.GetService<IElementEditStateService>(TargetElement);
         if (editProxy.IsEditingField)
         {
            return editProxy.CommitItemEdit();
         }
         return true;
      }

      protected override void PreviewCanExecuteCommand(object commandTarget, CanExecuteRoutedEventArgs args)
      {
         args.CanExecute = true;
         args.Handled = true;
      }
   }
}