using System;
using System.Collections.Generic;

namespace Gaia.Core
{
    public struct TimeScale
    {
        public float DT;
        public float ElapsedTime;
        public float TotalTime;
        public TimeScale(int arg)
        {
            ElapsedTime = 0;
            TotalTime = 0;
            DT = 0;
        }

        public void Elapse(int msDelta) //Advances the timestep. msDelta is in milliseconds
        {

            ElapsedTime = (float)msDelta / 1000.0f;
            TotalTime += ElapsedTime;
        }
    }

    public static class Time
    {
        public static TimeScale GameTime = new TimeScale(0);
        public static TimeScale RenderTime = new TimeScale(0);
    }
}
