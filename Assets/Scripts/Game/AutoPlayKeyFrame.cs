namespace Game
{
    public class AutoPlayKeyFrame
    {
        public enum Type
        {
            PressDown,
            Hold,
            PressUp
        }

        public bool HoldNote;

        public Type ManipulateType;

        public string SensorId;
        public int Time;

        public AutoPlayKeyFrame(Type manipulateType, int time)
        {
            ManipulateType = manipulateType;
            Time = time;
        }
    }
}