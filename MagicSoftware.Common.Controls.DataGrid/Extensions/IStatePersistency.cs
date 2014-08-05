﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MagicSoftware.Common.Controls.Table.Extensions
{
   interface IStatePersistency
   {
      void SaveCurrentState();
      void RestoreState();
   }
}
