using Fairwood.Math;
using UnityEngine;

/// <summary>
/// 辅助的用于画原型碰撞器的
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(CircleCollider2D))]
public class TmpDrawCircleCollider2D : MonoBehaviour
{
    //void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.green;
    //    Gizmos.DrawWireSphere(transform.position, GetComponent<CircleCollider2D>().radius);
        
    //}

    private const int RangeCount = 200;
    private const float DeltaAngle = 0.002f;
    public void DrawArc(Vector2 position)
    {
        var center = transform.position.ToVector2();
        var radius = GetComponent<CircleCollider2D>().radius;
        var dir = position - center;
        var ang = Mathf.Atan2(dir.y, dir.x);
        for (int i = -RangeCount; i < RangeCount; i++)
        {
            var a0 = ang + i * DeltaAngle;
            var a1 = ang + (i + 1) * DeltaAngle;
            Debug.DrawLine(center + new Vector2(radius * Mathf.Cos(a0), radius * Mathf.Sin(a0)), center + new Vector2(radius * Mathf.Cos(a1), radius * Mathf.Sin(a1)), new Color(0.1f, 0.9f, 0.1f), 0.1f);
        }
    }
}