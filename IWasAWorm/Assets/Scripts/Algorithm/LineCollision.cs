using System;
using UnityEngine;
using System.Collections;
using System.Linq;
using Fairwood.PhysicsLib;

public static class LineCollision
{
    /// <summary>
    /// 计算角平分线，类似Slerp
    /// </summary>
    /// <param name="vertex">顶点，不能与边点重合！</param>
    /// <param name="sidePoint1">边1上一点</param>
    /// <param name="sidePoint2">边2上一点</param>
    /// <returns>角度平分，长度平分，角平分线上的一点</returns>
    public static Vector2 CalcAngleBisector(Vector2 vertex, Vector2 sidePoint1, Vector2 sidePoint2)
    {
        if (vertex == sidePoint1 || vertex == sidePoint2)
        {
            Debug.LogError("vertex CANNOT equels to any sidePoint!");
            return vertex;
        }
        if (sidePoint1 == sidePoint2) return sidePoint1;

        var side1NormalPnt = vertex + (sidePoint1 - vertex).normalized;
        var side2NormalPnt = vertex + (sidePoint2 - vertex).normalized;
        var normalBisector = (side1NormalPnt + side2NormalPnt)*0.5f;
        var bisector = vertex + (normalBisector - vertex)*(sidePoint1.magnitude + sidePoint2.magnitude)*0.5f;
        return bisector;
    }

    ///// <summary>
    ///// 计算碰撞点
    ///// </summary>
    ///// <param name="vertex"></param>
    ///// <param name="sidePoint1"></param>
    ///// <param name="sidePoint2"></param>
    ///// <param name="collisionPoint"></param>
    ///// <returns>有没有碰撞</returns>
    //public static bool TryGetLineCollisionPoint(Vector2 vertex, Vector2 sidePoint1, Vector2 sidePoint2, out Vector2 collisionPoint)
    //{
    //    throw new Exception("TODO");
    //}

    /// <summary>
    /// 应该只在start时无碰撞end时有碰撞的情况返回true，给出有效hit。对运动物体会出错
    /// </summary>
    /// <param name="startLine"></param>
    /// <param name="endLine"></param>
    /// <param name="hitInfo"></param>
    /// <param name="layerMask"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    public static bool TryGetLineCollisionPoint(Vector4 startLine, Vector4 endLine, out LineCollisionHit hitInfo, LayerMask layerMask, float epsilon)
    {
        hitInfo = new LineCollisionHit();
        RaycastHit2D curHit;
        if (!CheckLineCollision(endLine, out curHit, layerMask))
        {
            Debug.LogWarning("Penetration may happen!");
            return false; //endLine没撞到就别调用这个方法
        }
        var startLineRay = new Ray2D(startLine.P1(), startLine.Direction());

        RaycastHit2D startLineHit;//活动物体就不考虑碰撞了，现在只考虑地形
        if (curHit.collider.Raycast(startLineRay, out startLineHit, startLine.Length()))
        {
            SilkDebug.DrawCross(startLineHit.point, 0.1f, new Color(0.6f, 0.6f, 0));
            Debug.LogWarningFormat("Penetration may happen at {0},{1}! startLine={2} endLine={3}, distanceToCenter={4}", startLineHit.point.x, startLineHit.point.y, startLine, endLine, CalcPointToLineVertical(startLine, curHit.collider.transform.position).magnitude);
            return false; //startLine都碰撞到这个东西了上一帧就该处理
        }

        var latestHit = curHit;
        var vertical = CalcPointToLineVertical(startLine, latestHit.point);
        var iterationTimes = 0;
        while (vertical.magnitude >= epsilon)
        {
            iterationTimes ++;
            if (iterationTimes > 50)
            {
                Debug.LogFormat("iteration too many times > 50!");
                break;
            }
            var midLine = Vector4.Lerp(startLine, endLine, 0.5f);
            if (CheckLineCollision(midLine, out curHit, layerMask))
            {
                latestHit = curHit;
                endLine = midLine;
            }
            else
            {
                startLine = midLine;
            }
            vertical = CalcPointToLineVertical(startLine, latestHit.point);
        }
        var nearHitInfo = latestHit;

        #region 计算FarHit

        var vectorF2N = new Vector2(endLine.x, endLine.y) - new Vector2(endLine.z, endLine.w);
        var fnRay = new Ray2D(new Vector2(endLine.z, endLine.w), vectorF2N);//Far->Near的射线
        var hits = Physics2D.RaycastAll(fnRay.origin, fnRay.direction, vectorF2N.magnitude, layerMask);
        if (hits.Length == 0)//不可能没有的
        {
            SilkDebug.DrawCross(new Vector2(endLine.x, endLine.y), 0.1f, new Color(0.5f, 0.5f, 0.1f));
            SilkDebug.DrawCross(new Vector2(endLine.z, endLine.w), 0.1f, new Color(0.4f, 0.4f, 0.3f));
            Debug.LogError("Error Cannot find farHit @" + Time.frameCount);
            Debug.LogWarningFormat("hits.Length == 0; startLine={0} endLine={1}, distanceToCenter={2}", startLine, endLine, CalcPointToLineVertical(startLine, curHit.collider.transform.position).magnitude);
            return false;
        }
        if (hits.Length > 1)//很少见，穿透了多次地形
        {
            Debug.LogWarning("hits.Length > 1 ! Very rare!");
            var rightHits = hits.Where(x => x.collider == nearHitInfo.collider).ToList();//取出碰撞到指定碰撞器的
            rightHits.Sort((x, y) => (int)Mathf.Sign(x.distance - y.distance));//按到FarPoint的远近排序
        }
        var farHitInfo = hits[hits.Length - 1];//取出最远的

        #endregion

        SilkDebug.DrawCross(nearHitInfo.point, 0.2f, Color.yellow);
        SilkDebug.DrawCross(farHitInfo.point, 0.2f, Color.yellow);

        #region 计算尖点和角平分线

        Vector2 c1 = nearHitInfo.point;
        Vector2 c2 = farHitInfo.point;
        var n1 = nearHitInfo.normal;
        var n2 = farHitInfo.normal;
        var n1v = new Vector2(-n1.y, n1.x);//n1的垂线，右手螺旋，即c1所在边
        var n2v = new Vector2(-n2.y, n2.x);//n2的垂线，右手螺旋，即c2所在边
        Vector2 vertex;//顶点，尖点
        if (Vector2.Dot(n2v, n1) == 0)
        {
            vertex = (c1 + c2) * 0.5f;//如果n1v,n2v平行，则无交点，取c1,c2中点。不太可能发生
        }
        else
        {
            vertex = c2 + Vector2.Dot(c1 - c2, n1) / Vector2.Dot(n2v, n1) * n2v; //找到尖点
        }
        var bisector = (n2v - n1v).normalized;//角平分线，不保证方向

        var bv = new Vector2(-bisector.y, bisector.x);//角平分线的垂线
        //找startLine与角平分线的交点
        var p1 = new Vector2(startLine.x, startLine.y);//p1,p2是startLine线段的两个端点
        var p2 = new Vector2(startLine.z, startLine.w);//p1,p2是startLine线段的两个端点
        var p1p2 = p2 - p1;
        var v = p1 + Vector2.Dot(vertex - p1, bv) / Vector2.Dot(p1p2, bv) * p1p2;//那个我们需要的碰撞点，浮于碰撞器表面一个缓冲值以内

        //如果碰撞点离碰撞器过近（小于SurfaceLayerMinThickness），则强行拉回此值之外
        var vertex2v = v - vertex;
        if (vertex2v.magnitude < PhysicsConfig.SurfaceLayerMinThickness)
        {
            //找endLine与角平分线的交点
            var p1endLine = new Vector2(endLine.x, endLine.y);
            var p2endLine = new Vector2(endLine.z, endLine.w);
            var p1p2endLine = p2endLine - p1endLine;
            var vendLine = p1endLine + Vector2.Dot(vertex - p1endLine, bv) / Vector2.Dot(p1p2endLine, bv) * p1p2endLine;

            Debug.LogFormat("OutPush vertex={0} v={1} distance={2}", vertex, v, vertex2v.magnitude);
            v = v + (v-vendLine).normalized*PhysicsConfig.SurfaceLayerMinThickness;
        }

        SilkDebug.DrawCross(vertex, 0.3f, Color.magenta);
        Debug.DrawRay(vertex, bisector);
        SilkDebug.DrawLine(startLine, Color.blue);
        SilkDebug.DrawLine(endLine, Color.blue);
        #endregion

        hitInfo = new LineCollisionHit(nearHitInfo.collider, (v - p1).magnitude, (v - vertex).normalized, v);

        return true;
    }

    static bool CheckLineCollision(Vector4 line, LayerMask layerMask)
    {
        return Physics.Linecast(new Vector2(line.x, line.y), new Vector2(line.z, line.w), layerMask);
    }
    /// <summary>
    /// 从Near射向Far，返回第一个碰撞
    /// </summary>
    /// <param name="line"></param>
    /// <param name="hitInfo">从Near射向Far</param>
    /// <param name="layerMask"></param>
    /// <returns></returns>
    private static bool CheckLineCollision(Vector4 line, out RaycastHit2D hitInfo, LayerMask layerMask)
    {
        hitInfo = Physics2D.Linecast(new Vector2(line.x, line.y), new Vector2(line.z, line.w), layerMask);
        return hitInfo.collider;
    }
    
    /// <summary>
    /// 点到直线的垂线，向量从直线指向点
    /// </summary>
    /// <param name="line"></param>
    /// <param name="point"></param>
    /// <returns>从直线指向点的垂线向量</returns>
    public static Vector2 CalcPointToLineVertical(Vector4 line, Vector2 point)
    {
        var p1 = new Vector2(line.x, line.y);
        var p2 = new Vector2(line.z, line.w);
        var c1p1 = point - p1;
        var p2p1 = p2 - p1;
        var d = c1p1 - Vector2.Dot(c1p1, p2p1)/Vector2.SqrMagnitude(p2p1)*p2p1;
        return d;
    }


}

public struct LineCollisionHit
{
    public LineCollisionHit(Collider2D collider, float distanceFromNear, Vector2 normal, Vector2 point) : this()
    {
        Collider = collider;
        DistanceFromNear = distanceFromNear;
        Normal = normal;
        Point = point;
    }

    public Collider2D Collider { get; set; }
    public float DistanceFromNear { get; set; }
    public Vector2 Normal { get; set; }
    public Vector2 Point { get; set; }

    public Rigidbody2D Rigidbody
    {
        get { return Collider.GetComponent<Rigidbody2D>(); }
    }

    public Transform Transform
    {
        get { return Collider.transform; }
    }
}

public static class LineExtensions
{
    public static Vector2 P1(this Vector4 line)
    {
        return new Vector2(line.x, line.y);
    }
    public static Vector2 P2(this Vector4 line)
    {
        return new Vector2(line.z, line.w);
    }
    /// <summary>
    /// P1��P2����
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public static Vector2 P1P2(this Vector4 line)
    {
        return line.P2() - line.P1();
    }
    /// <summary>
    /// P1��P2���򣬹�һ����
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public static Vector2 Direction(this Vector4 line)
    {
        return line.P1P2().normalized;
    }

    public static float Length(this Vector4 line)
    {
        return line.P1P2().magnitude;
    }

    public static Vector4 GetNearPart(this Vector4 line, float portion)
    {
        Vector4 nearPart;
        nearPart.x = line.x;
        nearPart.y = line.y;
        nearPart.z = nearPart.x + (line.z - line.x)*portion;
        nearPart.w = nearPart.y + (line.w - line.y)*portion;
        return nearPart;
    }
    public static Vector4 GetFarPart(this Vector4 line, float portion)
    {
        Vector4 farPart;
        farPart.z = line.z;
        farPart.w = line.w;
        farPart.x = farPart.z + (line.x - line.z)*portion;
        farPart.y = farPart.w + (line.y - line.w)*portion;
        return farPart;
    }
}

/*
Vector4 line:x,y NearPoint; z,w FarPoint


*/