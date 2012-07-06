﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gaia.Editors
{
    public interface ICommand
    {
        void Execute();
        void Unexecute();
    }
}
