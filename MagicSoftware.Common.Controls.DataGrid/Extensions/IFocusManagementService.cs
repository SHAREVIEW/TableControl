﻿using System;

namespace MagicSoftware.Common.Controls.Table.Extensions
{
   public interface IFocusManagementService
   {
      IDisposable DeferFocusUpdate();

      void UpdateFocus();
      void SetFocusOnTargetElement();
      bool IsRestoringFocusOnElement { get; }
   }
}