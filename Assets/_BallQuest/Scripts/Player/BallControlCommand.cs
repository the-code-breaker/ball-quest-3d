using UnityEngine;

namespace BallQuest
{
    /// <summary>One physics command path for touch, keyboard and AI. Values are normalized.</summary>
    public struct BallControlCommand
    {
        public float throttle;
        public float steering;
        public float brake;
        public bool jump;
        public BallControlCommand(float throttle, float steering, float brake = 0, bool jump = false)
        {
            this.throttle = Safe(throttle, -1, 1);
            this.steering = Safe(steering, -1, 1);
            this.brake = Safe(brake, 0, 1);
            this.jump = jump;
        }
        static float Safe(float value,float min,float max)=>float.IsNaN(value)||float.IsInfinity(value)?0:Mathf.Clamp(value,min,max);
    }
}
