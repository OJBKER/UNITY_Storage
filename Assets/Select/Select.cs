using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
public class Select : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,IPointerClickHandler
{
    public struct CameraData//Ӱ���������������Ĳ���
    {
        Vector3 pos;
        Quaternion ro;
        float fov;
        public void ExchangeData(Camera B)
        {
            pos = B.transform.position;
            ro = B.transform.rotation;
            fov = B.fieldOfView;
        }
        public bool Compared(Camera B)
        {
            if(pos == B.transform.position&&ro== B.transform.rotation&& fov == B.fieldOfView)
            {
                return true;
            }
            return false;
        }
    }
    public GameObject SelectBox;//image��ѡ�п�
    public Vector2 BegainPoint = Vector2.zero;//��ʼ��ק��
    public Vector2 EndPoint = Vector2.zero;//������ק��
    public Transform[] OBJs;
    private List<Transform> VisibleObjs;
    private CameraData cameradata;
    private List<Transform> Temp_selected;
    private Dictionary<int,Dictionary<string,Transform>> Temp_unselected;
    private bool IsDrag = false;

    private void Start()
    {       
        OBJs = gameObject.GetComponentsInChildren<Transform>().Where(t => t != transform).ToArray();//Linq�﷨��ȥ������
        Temp_unselected = new Dictionary<int, Dictionary<string, Transform>>() {//0Ϊδѡ�ж��У�1Ϊδѡ�����еĻ��壨�Զ������޷�ʹ��ֱ�ӿ����������Ҫ��������Ԫ�أ�
            {0,new Dictionary<string, Transform>() },
            {1,new Dictionary<string, Transform>() }
        };
        Temp_selected = new List<Transform>();
        cameradata = new CameraData();
        VisibleObjs = new List<Transform>();
        
        DisableSelectBox();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        //��¼��ʼλ�ã��Ƿ��ÿɼ���Ϣ
        IsDrag = true;
        BegainPoint = eventData.position;
        EnableSelectBox();
        if(cameradata.Compared(Camera.main))
        {
            cameradata.ExchangeData(Camera.main);
            Debug.Log("��ͬ�ľ�ͷ������visible��Ϣ");
            return;
        }
        else//�����ͷ���ݸı������¼���ɼ�����
        {
            VisibleObjs.Clear();
            cameradata.ExchangeData(Camera.main);
            foreach (Transform i in OBJs)
            {
                if (i.GetComponent<Renderer>() == null) { continue; }
                if (IsVisibleFrom(i.GetComponent<Renderer>(), Camera.main))
                {
                    VisibleObjs.Add(i);
                }
            }            
        }        
    }
    public void OnDrag(PointerEventData eventData)
    {
        //��¼ʵʱλ�ã�����ѡ�п�
        Vector2 ResentPoint = eventData.position;
        FlashBox(BegainPoint, ResentPoint);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndPoint = eventData.position;
        DisableSelectBox();
        //Dir�ж���������ϵ������
        Vector2 Dir = BegainPoint - EndPoint;
        //ȡ�þ��ε��ĸ�����        
        Vector3 A = BegainPoint;
        Vector3 B = new Vector2(BegainPoint.x, EndPoint.y);
        Vector3 C = EndPoint;
        Vector3 D = new Vector2(EndPoint.x, BegainPoint.y);
        if (Input.GetKey(KeyCode.LeftShift)&&Temp_unselected[0].Count!=0)
        {
            //List<Transform> Not_Select = VisibleObjs.Except(Temp_selected).ToList();            
            for (int i = 0;i< Temp_unselected[0].Count; i++)
            {                
                IfInRect(Temp_unselected[0].ElementAt(i).Value, Dir, A, B, C, D, Temp_unselected[1]);
                Debug.Log("Shiftƴ��ѡ����" + Temp_unselected[0].ElementAt(i).Value);
            }
            for(int i = 0;i<Temp_unselected[0].Count;i++)
            {
                Debug.Log("Shift�ֵ����" + i);
                string X = Temp_unselected[0].ElementAt(i).Key;
                if (!Temp_unselected[1].ContainsKey(X))
                {
                    Temp_unselected[0].Remove(X);
                }
            }
            Temp_unselected[1].Clear();         
        }
        else
        {
            Temp_unselected[0].Clear();
            foreach (Transform i in VisibleObjs)
            {
                IfInRect(i, Dir, A, B, C, D,Temp_unselected[0]);
            }
            Debug.Log("����VisibleObjs");
        }
        
    }
    public void OnPointerClick(PointerEventData eventData)//���δѡ�����У�ȡ��ѡ������
    {
        if (IsDrag)
        {
            IsDrag = false;
            return;
        }
        if (!Input.GetKey(KeyCode.LeftShift))
        {
            //����ѡ��,����ֵ�
            foreach (Transform i in Temp_selected)
            {
                i.gameObject.tag = "Finish";
            }
            Debug.Log("ȡ����"+Temp_selected.Count);
            Temp_selected.Clear();
            Temp_unselected[0].Clear();
        }            
       
    }
    //�ж��Ƿ��ھ�����
    public void IfInRect(Transform i,Vector2 Dir, Vector3 A, Vector3 B, Vector3 C, Vector3 D,Dictionary<string ,Transform> Temp )
    {
        //�ò���ж��Ƿ����ڲ�
        Vector3 P = Camera.main.WorldToScreenPoint(i.position);
        Vector3 c1 = Vector3.Cross((A - B), (A - P));
        Vector3 c2 = Vector3.Cross((B - C), (B - P));
        Vector3 c3 = Vector3.Cross((C - D), (C - P));
        Vector3 c4 = Vector3.Cross((D - A), (D - P));
        //���ε���ѡ�ͷ�ѡ�������
        if (Dir.x < 0 && Dir.y > 0 || Dir.y < 0 && Dir.x > 0)
        {

            if (c1.z > 0 && c2.z > 0 && c3.z > 0 && c4.z > 0)     
            {
                //�ھ�����δ��ѡ�У������ѡ�д���
                    if(i.gameObject.tag !="Select")
                    {
                    i.gameObject.tag = "Select";
                     Temp_selected.Add(i);   //����ѡ�����У���ȡ��ѡ��ʱʹ��                     
                    }                     
            }
            else
            {                
                Temp.Add(i.name, i);               
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    //���ھ����ڣ���û��סshift��Ŀ��ȡ��ѡ��
                    i.gameObject.tag = "Finish";
                }

            }
        }
        else
        {
            if (c1.z < 0 && c2.z < 0 && c3.z < 0 && c4.z < 0)
            {
                //�ھ�����δ��ѡ�У������ѡ�д���
                if (i.gameObject.tag != "Select")
                {
                    Temp_selected.Add(i);
                    i.gameObject.tag = "Select";
                }
            }
            else
            {
                Temp.Add(i.name,i); 
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    //���ھ����ڣ���û��סshift��Ŀ��ȡ��ѡ��
                    i.gameObject.tag = "Finish";
                }               
            }
        }
    }
    public void FlashBox(Vector2 A , Vector2 B)//���ƾ��ο�
    {
        Vector2 CenterPos = (A + B) / 2;
        float Width = Mathf.Abs(A.x - B.x);
        float Height = Mathf.Abs(A.y - B.y);
        SelectBox.GetComponent<RectTransform>().position = CenterPos;
        SelectBox.GetComponent<RectTransform>().sizeDelta = new Vector2(Width, Height);
    }
    public void DisableSelectBox()//�ص�ѡ�п�
    {
        SelectBox.SetActive(false); 
    }
    public void EnableSelectBox()//����ѡ�п�
    {
        SelectBox.SetActive(true);
    }
    public bool IsVisibleFrom(Renderer renderer, Camera camera)//�ж��Ƿ��������Ұ��
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
    }
}

