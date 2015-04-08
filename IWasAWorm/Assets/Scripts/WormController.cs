using Fairwood.Math;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
public class WormController : MonoBehaviour
{
    /// <summary>
    /// �����ٽ�����,��λ����
    /// </summary>
    public float CriticalClickDistanceInMillimeter = 3;
    /// <summary>
    /// �����ٽ�ʱ�䣬��λ��
    /// </summary>
    public float CriticalClickTimeInterval = 0.4f;
    /// <summary>
    /// ���Ķ�˿��ť��Χ�İ뾶
    /// </summary>
    public float BtnBreakRadiusViewport = 0.2f;

    private const float Inch2Mm = 25.4f;
    private float _criticalClickDistanceInPixel;

    private void Awake()
    {
        _criticalClickDistanceInPixel = CriticalClickDistanceInMillimeter*(Screen.dpi*Inch2Mm);
    }

    public Worm WormToControl;
    public Camera ControlCamera;
    /// <summary>
    /// ��һ֡�жϲ�������ͣ��Ϸʱ�õ�����ֹ����
    /// </summary>
    public bool CancelControlNextFrame = false;

    /// <summary>
    /// ��һ֡����λ��(px)
    /// </summary>
    private Vector2 _lastTouchPos;

    private TouchState _lastTouchState;

    private ControlState _controlState;
    private Vector2 _startControlPos;//����ʱ��λ�ã������ж��Ƿ����볬���ٽ�ֵ
    private float _startControlRealTime;//����ʱ��ʱ�䣬�����ж��Ƿ�ʱ�䳬���ٽ�ֵ
    private void Update()
    {
        if (CancelControlNextFrame)//������ֹ���ݣ���������һ֡���ڲ���״̬
        {
            _lastTouchState = TouchState.Off;
            _controlState = ControlState.NotControl;
            CancelControlNextFrame = false;
        }

        Vector2 curTouchPos;
        TouchState curTouchState;

        #region ��ȡ���ݲ���

        if (Input.GetKey(KeyCode.Mouse0)) //�����갴��
        {
            curTouchPos = Input.mousePosition; //����Ϊ�ٿ���
            curTouchState = Input.touchCount <= 0 ? TouchState.Touched : TouchState.MoreThanOne; //�Ƿ����б��Ĵ���
        }
        else //û������
        {
            if (Input.touchCount == 0) //Ҳû�д���
            {
                curTouchPos = Vector2.zero; //����Ĭ��ֵ�������õ���
                curTouchState = TouchState.Off;
            }
            else
            {
                curTouchPos = Input.touches[0].position;
                curTouchState = Input.touchCount == 1 ? TouchState.Touched : TouchState.MoreThanOne;
            }
        }

        #endregion

        #region ����������Ч��

        if (curTouchState == TouchState.Touched)
        {
            //todo:������ȷ�Ļ��������¼�����������������ǰ���Ľ������ڵ�
            var ray = ControlCamera.ScreenPointToRay(curTouchPos);
            Debug.DrawRay(ray.origin, ray.direction * ControlCamera.farClipPlane, Color.red);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, ControlCamera.farClipPlane) || hit.collider != GetComponent<Collider>())
                //��������û�����������߰����Ĳ����ң�����Ϊδ���ţ����Ż�
            {
                curTouchState = TouchState.Off;
            }
        }

        #endregion

        #region ���²���״̬

        if (_controlState == ControlState.NotControl) //��һ֡��ʼ��������һ֡���ܿ���Ч��
        {
            if (curTouchState == TouchState.Touched)
            {
                _controlState = ControlState.JustStartControl;
                _startControlPos = curTouchPos;
                _startControlRealTime = Time.realtimeSinceStartup;
            }
        }
        else if (_controlState == ControlState.JustStartControl) //��һ֡���ڸտ�ʼ����״̬
        {
            if (curTouchState == TouchState.Touched)
            {
                ProcessDrag(_lastTouchPos, curTouchPos); //������������

                if (Vector2.Distance(curTouchPos, _startControlPos) > CriticalClickDistanceInMillimeter
                    || Time.realtimeSinceStartup - _startControlRealTime > CriticalClickTimeInterval)
                    //�Ƿ񳬹��ٽ磬����Drag״̬�����ٿ����ǵ�����
                {
                    _controlState = ControlState.Drag;
                }
            }
            else if (curTouchState == TouchState.Off) //�Ǿ��ǵ�����
            {
                ProcessClick(_startControlPos); //�����¼�����˿/��˿

                _controlState = ControlState.NotControl;
            }
            else // curTouchState == TouchState.MoreThanOne//˫ָģʽ
            {
                _controlState = ControlState.NotControl;
            }
        }
        else if (_controlState == ControlState.Drag)
        {
            if (curTouchState == TouchState.Touched)
            {
                ProcessDrag(_lastTouchPos, curTouchPos); //������������
            }
            else //��ֹ����
            {
                _controlState = ControlState.NotControl;
            }
        }

        if (_controlState != ControlState.NotControl)
        {
            _lastTouchPos = curTouchPos;
        }
        _lastTouchState = curTouchState;

        #endregion

        if (_controlState == ControlState.JustStartControl || _controlState == ControlState.Drag)
        {
            var dL = (curTouchPos - _lastTouchPos).y*ControlCamera.orthographicSize*2/Screen.height;
            WormToControl.LengthenSilk(dL);
        }
    }

    void ProcessDrag(Vector2 lastPosPx, Vector2 curPosPx)
    {
        if (curPosPx.y == lastPosPx.y) return;//û�д�ֱ������������
        var direction = curPosPx.y > lastPosPx.y ? 1 : -1;//1��ʾ�쳤��-1��ʾ����
        float dl;//�쳤��
        if (ControlCamera.orthographic)
        {
            dl = (curPosPx.y - lastPosPx.y)/ControlCamera.pixelHeight*ControlCamera.orthographicSize*2;
        }
        else
        {
            //������̽�ⷨ����dl
            var ray0 = ControlCamera.ScreenPointToRay(lastPosPx);
            var ray1 = ControlCamera.ScreenPointToRay(curPosPx);
            var plane = new Plane(Vector3.back, WormToControl.transform.position);//�ؿ�һ���ǳ���back������
            float enter0, enter1;
            if (plane.Raycast(ray0, out enter0) && plane.Raycast(ray1, out enter1))
            {
                var p0 = ray0.GetPoint(enter0);
                var p1 = ray1.GetPoint(enter1);
                dl = p1.y - p0.y;
            }
            else
            {
                return;
            }
        }
        WormToControl.LengthenSilk(dl);
    }

    void ProcessClick(Vector2 clickPosPx)
    {
        Debug.Log("ProcessClick");
        //���ݷ�Χ�ж�����˿���Ƕ�˿
        var clickPosViewport = ControlCamera.ScreenToViewportPoint(clickPosPx);
        if (new Vector2(clickPosViewport.x - 0.5f, clickPosViewport.y - 0.5f).magnitude > BtnBreakRadiusViewport)//Ȧ�⣬��˿
        {
            //������̽�ⷨ���������㣬����������
            var ray = ControlCamera.ScreenPointToRay(clickPosPx);
            var plane = new Plane(Vector3.back, WormToControl.transform.position);//�ؿ�һ���ǳ���back������
            float enter0, enter1;
            if (plane.Raycast(ray, out enter0))
            {
                var p = ray.GetPoint(enter0);
                Vector2 dir = p.ToVector2() - WormToControl.MouthPosWorld;
                WormToControl.Spit(dir);
            }
        }
        else//Ȧ�ڣ���˿
        {
            WormToControl.BreakUpHeldSilk();
        }
    }

    /// <summary>
    /// ��������
    /// </summary>
    enum TouchState
    {
        Off,
        Touched,
        MoreThanOne,
    }
    enum ControlState
    {
        NotControl,
        /// <summary>
        /// �˽׶�̧�����ǵ���
        /// </summary>
        JustStartControl,
        /// <summary>
        /// �����ٽ�������ʱ��
        /// </summary>
        Drag,
    }
}