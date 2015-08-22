using Fairwood.Math;
using UnityEngine;
using System.Collections;
using Scripts.Silk;

public class SilkMidPoint : SilkPoint
{
    private int _indexOnSilk;
    private HandType _hand;

    public SilkMidPoint(Silk silk, int indexOnSilk, Vector2 nearerPoint, Vector2 myPos, Vector2 fartherPoint) : base(silk)
    {
        _indexOnSilk = indexOnSilk;
        var f = fartherPoint - myPos;
        var n = nearerPoint - myPos;
        _hand = (f.x*n.y - f.y*n.x) > 0 ? HandType.Right : HandType.Left;
    }

    public void SetIndexOnSilk(int indexOnSilk)
    {
        _indexOnSilk = indexOnSilk;
    }

    /// <summary>
    /// 由Silk在Update里每帧调用，检查手性是否改变，变了则要把前后两截合并成一节直线
    /// </summary>
    public bool CheckHandChange()
    {
        //检测是否改变了手性

        var f = FartherPoint - Position;
        if (_indexOnSilk + 1 >= MySilk.Points.Count)
        {
            Debug.LogFormat("GetNearerPoint[{0}] Count={1}", _indexOnSilk, MySilk.Points.Count);
            Debug.LogFormat("GetNearerPoint({0})", MySilk.Points[_indexOnSilk + 1]);
        }
        var n = NearerPoint - Position;
        var curHand = (f.x * n.y - f.y * n.x) > 0 ? HandType.Right : HandType.Left;
        if (curHand != _hand)
        {
            Debug.LogFormat("HandChanged[{7}] {0}→{1} Far={2} Near={3} Pos={4} f={5} n={6}", _hand, curHand, FartherPoint.ToAccurateString(), NearerPoint.ToAccurateString(), Position.ToAccurateString(), f.ToAccurateString(), n.ToAccurateString(), _indexOnSilk);
            //手性改变，双截合并
            MySilk.CombineSegments(_indexOnSilk - 1);
            return true;
        }
        return false;
    }

    #region Utilities

    private Vector2 NearerPoint
    {
        get
        {
            return MySilk.Points[_indexOnSilk + 1].Position;
        }
    }

    Vector2 FartherPoint { get { return MySilk.Points[_indexOnSilk - 1].Position; } }
    #endregion

    /// <summary>
    /// 手性，从近端向远端看去，符合哪只手的螺旋定则
    /// </summary>
    public enum HandType
    {
        Left,
        Right
    }
}