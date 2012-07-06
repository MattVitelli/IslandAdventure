using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gaia.UI
{
    public abstract class UIScreen
    {
        protected List<UIControl> controls = new List<UIControl>();

        protected int drawOrder = 0;

        public int GetDrawOrder() { return drawOrder; }

        public virtual void OnUpdate(float timeDT)
        {
            for (int i = 0; i < controls.Count; i++)
            {
                controls[i].OnUpdate(timeDT);
            }
        }

        public virtual void OnRender()
        {
            for (int i = 0; i < controls.Count; i++)
            {
                controls[i].OnDraw();
            }
        }
    }
}
