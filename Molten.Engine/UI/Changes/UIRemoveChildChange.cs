﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Molten.UI
{
    internal struct UIRemoveChildChange : IUIChange
    {
        internal UIRenderData Parent;

        internal UIRenderData Child;

        public void Process()
        {
            Parent.Children.Remove(Child);
        }
    }
}
