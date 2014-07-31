﻿using System;

namespace MagicSoftware.Common.Controls.Table.Extensions
{
   public interface IFocusManagementService
   {
      IDisposable DeferFocusChanges();

      void UpdateFocus();
   }
}