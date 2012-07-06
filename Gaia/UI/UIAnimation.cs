using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gaia.UI
{
    public enum UIAnimationFlags
    {
        AnimatePosition = 0x01,
        AnimateRotation = 0x02,
        AnimateScale = 0x04,
        AnimateColor = 0x08,
        Count = 4,
    }
}
