using Fairwood.Math;
using UnityEngine;
using System.Collections;
using Scripts.Silk;

public class SilkEnd : SilkPoint
{
    public SilkEndState State = SilkEndState.Held;


    /// <summary>
    /// Spit、Free状态下需要个临时小质量绑在前端
    /// </summary>
    public GameObject TempMass;

    public SilkEnd(Silk silk) : base(silk)
    {
        TempMass = new GameObject("End TempMass(ERROR:NO NAME)", typeof (Rigidbody2D));
        TempMass.layer = LayerManager.Layer.Silk;
        TempMass.transform.parent = silk.transform;//使用时再放出来
        var rigidbody = TempMass.GetComponent<Rigidbody2D>();
        rigidbody.mass = Silk.TempMass;
        rigidbody.drag = 0f;
        rigidbody.centerOfMass = Vector3.zero;
        rigidbody.inertia = 0.1f;
        //rigidbody.gravityScale = 1f; 不需有重力,仅当Free时才会有重力
        TempMass.SetActive(false);
    }
}