using System;
using UnityEngine;
using System.Collections;
using System.Linq;

public static class LineCollision
{
    /// <summary>
    /// ������ƽ���ߣ�����Slerp
    /// </summary>
    /// <param name="vertex">���㣬�������ߵ��غϣ�</param>
    /// <param name="sidePoint1">��1��һ��</param>
    /// <param name="sidePoint2">��2��һ��</param>
    /// <returns>�Ƕ�ƽ�֣�����ƽ�֣���ƽ�����ϵ�һ��</returns>
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
    ///// ������ײ��
    ///// </summary>
    ///// <param name="vertex"></param>
    ///// <param name="sidePoint1"></param>
    ///// <param name="sidePoint2"></param>
    ///// <param name="collisionPoint"></param>
    ///// <returns>��û����ײ</returns>
    //public static bool TryGetLineCollisionPoint(Vector2 vertex, Vector2 sidePoint1, Vector2 sidePoint2, out Vector2 collisionPoint)
    //{
    //    throw new Exception("TODO");
    //}

    /// <summary>
    /// Ӧ��ֻ��startʱ����ײendʱ����ײ����������true��������Чhit�����˶�����������
    /// </summary>
    /// <param name="startLine"></param>
    /// <param name="endLine"></param>
    /// <param name="hitInfo"></param>
    /// <param name="layerMask"></param>
    /// <returns></returns>
    public static bool TryGetLineCollisionPoint(Vector4 startLine, Vector4 endLine, out LineCollisionHit hitInfo, LayerMask layerMask, float epsilon)
    {
        hitInfo = new LineCollisionHit();
        RaycastHit curHit;
        if (!CheckLineCollision(endLine, out curHit, layerMask)) return false;//endLineûײ���ͱ�������������
        var startLineRay = new Ray(startLine.P1(), startLine.Direction());

        RaycastHit startLineHit;//TODO:�˶�������ô��
        if (curHit.collider.Raycast(startLineRay, out startLineHit, startLine.Length())) return false;//startLine����ײ��������������һ֡�͸ô���
        
        var latestHit = curHit;
        var vertical = CalcPointToLineVertical(startLine, latestHit.point);
        while (vertical.magnitude >= epsilon)
        {
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

        #region ����FarHit

        var vectorF2N = new Vector2(endLine.x, endLine.y) - new Vector2(endLine.z, endLine.w);
        var fnRay = new Ray(new Vector2(endLine.z, endLine.w),vectorF2N);//Far->Near������
        var hits = Physics.RaycastAll(fnRay, vectorF2N.magnitude, layerMask);
        if (hits.Length == 0)//������û�е�
        {
            Debug.LogError("Error");
            return false;
        }
        if (hits.Length > 1)//���ټ�
        {
            var rightHits = hits.Where(x => x.collider == nearHitInfo.collider).ToList();//ȡ����ײ��ָ����ײ����
            rightHits.Sort((x, y) => (int)Mathf.Sign(x.distance - y.distance));//����FarPoint��Զ������
        }
        var farHitInfo = hits[hits.Length - 1];//ȡ����Զ��

        #endregion

        SilkDebug.DrawCross(nearHitInfo.point, 0.2f, Color.yellow);
        SilkDebug.DrawCross(farHitInfo.point, 0.2f, Color.yellow);

        #region ���������ͽ�ƽ����

        Vector2 c1 = nearHitInfo.point;
        Vector2 c2 = farHitInfo.point;
        var n1 = nearHitInfo.normal;
        var n2 = farHitInfo.normal;
        var n1v = new Vector2(-n1.y, n1.x);//n1�Ĵ��ߣ�������������c1���ڱ�
        var n2v = new Vector2(-n2.y, n2.x);//n2�Ĵ��ߣ�������������c2���ڱ�
        Vector2 vertex;//���㣬����
        if (Vector2.Dot(n2v, n1) == 0)
        {
            vertex = (c1 + c2)*0.5f;//����n1v,n2vƽ�У����޽��㣬ȡc1,c2�е㡣��̫���ܷ���
        }
        else
        {
            vertex = c2 + Vector2.Dot(c1 - c2, n1)/Vector2.Dot(n2v, n1)*n2v; //�ҵ�����
        }
        var bisector = (n2v - n1v).normalized;//��ƽ���ߣ�����֤����

        var bv = new Vector2(-bisector.y, bisector.x);//��ƽ���ߵĴ���
        var p1 = new Vector2(startLine.x, startLine.y);//��startLine����ƽ���ߵĽ���
        var p2 = new Vector2(startLine.z, startLine.w);//p1,p2��startLine�߶ε������˵�
        var p1p2 = p2 - p1;
        var v = p1 + Vector2.Dot(vertex - p1, bv)/Vector2.Dot(p1p2, bv)*p1p2;//�Ǹ�������Ҫ����ײ�㣬������ײ������һ������ֵ����

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
    /// //��Near����Far
    /// </summary>
    /// <param name="line"></param>
    /// <param name="hitInfo">//��Near����Far</param>
    /// <param name="layerMask"></param>
    /// <returns></returns>
    private static bool CheckLineCollision(Vector4 line, out RaycastHit hitInfo, LayerMask layerMask)
    {
        return Physics.Linecast(new Vector2(line.x, line.y), new Vector2(line.z, line.w), out hitInfo, layerMask);
    }

    /// <summary>
    /// �㵽ֱ�ߵĴ��ߣ�������ֱ��ָ����
    /// </summary>
    /// <param name="line"></param>
    /// <param name="point"></param>
    /// <returns>��ֱ��ָ�����Ĵ�������</returns>
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
    public LineCollisionHit(Collider collider, float distanceFromNear, Vector2 normal, Vector2 point) : this()
    {
        Collider = collider;
        DistanceFromNear = distanceFromNear;
        Normal = normal;
        Point = point;
    }

    public Collider Collider { get; set; }
    public float DistanceFromNear { get; set; }
    public Vector2 Normal { get; set; }
    public Vector2 Point { get; set; }

    public Rigidbody Rigidbody
    {
        get { return Collider.GetComponent<Rigidbody>(); }
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
}

/*
Vector4 line:x,y NearPoint; z,w FarPoint


*/