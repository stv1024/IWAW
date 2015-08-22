
public static class PhysicsConfig
{
    public const float SurfaceLayerThickness = 1f;
    /// <summary>
    /// 如果碰撞点离真实表面的距离小于此值，则强行拉回成此值。这是为了防止离碰撞器过近，尤其是距离小于实数的精度时，碰撞检测出错
    /// </summary>
    public const float SurfaceLayerMinThickness = 0.01f;
}