public static class LayerManager
{
    public static class Layer
    {
        public const int Ground = 8;
        public const int Silk = 9;
        public const int ColliderRaycast = 12;
    }
    public static class LayerMask
    {
        public const int Ground = 1 << Layer.Ground;
        public const int ColliderRaycast = 1 << Layer.ColliderRaycast;
        public const int SilkJoint = Ground;
    }
}