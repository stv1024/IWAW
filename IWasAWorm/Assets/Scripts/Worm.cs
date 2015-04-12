using Fairwood.Math;
using UnityEngine;
using Fairwood;
using Fairwood.PhysicsLib;

public class Worm : MonoBehaviour
{
    public GameObject SilkPrefab;

    public float ShootSpeed = 100;
    public float ShootRange = 200;

    public Silk SilkInMouth;

    public Vector2 MouthPosLocal;
    public Vector2 MouthPosWorld
    {
        get { return transform.TransformPoint(MouthPosLocal); }
    }

    void Awake()
    {
        GetComponent<Rigidbody2D>().centerOfMass = Vector2.zero;//将质心固定为原点
        GetComponent<Rigidbody2D>().inertia = 1;//将惯性张量固定为有限值

        //TODO TEST
        //var cldr = GameObject.Find("Rect 4").GetComponent<Collider2D>();
        //RaycastHit2D hit;
        //var bl = cldr.Raycast(new Ray2D(Vector2.zero, new Vector2(1, 1)), out hit, 100);

        //Debug.Log("bl=" + bl);
        //Debug.Log("collider=" + hit.collider.name);
        //Debug.DrawRay(Vector3.zero, new Vector3(1, 1, 0), Color.cyan, 5);
        //if (bl) SilkDebug.DrawCross(hit.point, 0.2f, Color.blue, 5);
    }

    public void Spit(Vector2 dir)
    {
        if (dir.x == 0 && dir.y == 0)
        {
            Debug.LogError("dir == 0");
            return;
        }
        if (SilkInMouth)//嘴里有丝
        {
            
        }
        else//嘴里无丝
        {
            var go = Instantiate(SilkPrefab);
            go.transform.ResetAs(SilkPrefab.transform);
            //var go = new GameObject("Silk " + Time.frameCount);
            SilkInMouth = go.GetComponent<Silk>();
            SilkInMouth.NewSpit(
                transform.TransformPoint(MouthPosLocal).ToVector2(), dir.normalized*ShootSpeed, this, ShootRange);
        }
    }

    public void BreakUpHeldSilk()
    {
        Debug.Log("BreakUpHeldSilk");
        if (!SilkInMouth) return;
        SilkInMouth.BreakUp();
        SilkInMouth = null;
    }

    public void LengthenSilk(float dl)
    {
        if (dl == 0) return;
        if (!SilkInMouth) return;
        SilkInMouth.Lengthen(dl);
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.T))
        {
            var str = "{COM:" + GetComponent<Rigidbody>().centerOfMass + ", Tensor:" + GetComponent<Rigidbody>().inertiaTensor + "}";
            Debug.LogWarning(str);
        }
    }
}