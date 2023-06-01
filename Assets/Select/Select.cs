using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
public class Select : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,IPointerClickHandler
{
    public struct CameraData//影响摄像机看见物体的参数
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
    public GameObject SelectBox;//image，选中框
    public Vector2 BegainPoint = Vector2.zero;//开始拖拽点
    public Vector2 EndPoint = Vector2.zero;//结束拖拽点
    public Transform[] OBJs;
    private List<Transform> VisibleObjs;
    private CameraData cameradata;
    private List<Transform> Temp_selected;
    private Dictionary<int,Dictionary<string,Transform>> Temp_unselected;
    private bool IsDrag = false;

    private void Start()
    {       
        OBJs = gameObject.GetComponentsInChildren<Transform>().Where(t => t != transform).ToArray();//Linq语法，去除自身
        Temp_unselected = new Dictionary<int, Dictionary<string, Transform>>() {//0为未选中队列，1为未选择序列的缓冲（自定义类无法使用直接拷贝，深拷贝需要遍历所有元素）
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
        //记录初始位置，是否复用可见信息
        IsDrag = true;
        BegainPoint = eventData.position;
        EnableSelectBox();
        if(cameradata.Compared(Camera.main))
        {
            cameradata.ExchangeData(Camera.main);
            Debug.Log("相同的镜头，复用visible信息");
            return;
        }
        else//如果镜头数据改变则重新计算可见物体
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
        //记录实时位置，绘制选中框
        Vector2 ResentPoint = eventData.position;
        FlashBox(BegainPoint, ResentPoint);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndPoint = eventData.position;
        DisableSelectBox();
        //Dir判断左手坐标系的问题
        Vector2 Dir = BegainPoint - EndPoint;
        //取得矩形的四个顶点        
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
                Debug.Log("Shift拼接选择了" + Temp_unselected[0].ElementAt(i).Value);
            }
            for(int i = 0;i<Temp_unselected[0].Count;i++)
            {
                Debug.Log("Shift字典过滤" + i);
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
            Debug.Log("遍历VisibleObjs");
        }
        
    }
    public void OnPointerClick(PointerEventData eventData)//清空未选择序列，取消选中序列
    {
        if (IsDrag)
        {
            IsDrag = false;
            return;
        }
        if (!Input.GetKey(KeyCode.LeftShift))
        {
            //重置选中,清空字典
            foreach (Transform i in Temp_selected)
            {
                i.gameObject.tag = "Finish";
            }
            Debug.Log("取消了"+Temp_selected.Count);
            Temp_selected.Clear();
            Temp_unselected[0].Clear();
        }            
       
    }
    //判断是否在矩形内
    public void IfInRect(Transform i,Vector2 Dir, Vector3 A, Vector3 B, Vector3 C, Vector3 D,Dictionary<string ,Transform> Temp )
    {
        //用叉乘判断是否在内部
        Vector3 P = Camera.main.WorldToScreenPoint(i.position);
        Vector3 c1 = Vector3.Cross((A - B), (A - P));
        Vector3 c2 = Vector3.Cross((B - C), (B - P));
        Vector3 c3 = Vector3.Cross((C - D), (C - P));
        Vector3 c4 = Vector3.Cross((D - A), (D - P));
        //矩形的正选和反选两种情况
        if (Dir.x < 0 && Dir.y > 0 || Dir.y < 0 && Dir.x > 0)
        {

            if (c1.z > 0 && c2.z > 0 && c3.z > 0 && c4.z > 0)     
            {
                //在矩形内未被选中，则进行选中处理
                    if(i.gameObject.tag !="Select")
                    {
                    i.gameObject.tag = "Select";
                     Temp_selected.Add(i);   //加入选中序列，在取消选中时使用                     
                    }                     
            }
            else
            {                
                Temp.Add(i.name, i);               
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    //不在矩形内，且没按住shift则将目标取消选中
                    i.gameObject.tag = "Finish";
                }

            }
        }
        else
        {
            if (c1.z < 0 && c2.z < 0 && c3.z < 0 && c4.z < 0)
            {
                //在矩形内未被选中，则进行选中处理
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
                    //不在矩形内，且没按住shift则将目标取消选中
                    i.gameObject.tag = "Finish";
                }               
            }
        }
    }
    public void FlashBox(Vector2 A , Vector2 B)//绘制矩形框
    {
        Vector2 CenterPos = (A + B) / 2;
        float Width = Mathf.Abs(A.x - B.x);
        float Height = Mathf.Abs(A.y - B.y);
        SelectBox.GetComponent<RectTransform>().position = CenterPos;
        SelectBox.GetComponent<RectTransform>().sizeDelta = new Vector2(Width, Height);
    }
    public void DisableSelectBox()//关掉选中框
    {
        SelectBox.SetActive(false); 
    }
    public void EnableSelectBox()//开启选中框
    {
        SelectBox.SetActive(true);
    }
    public bool IsVisibleFrom(Renderer renderer, Camera camera)//判断是否在相机视野内
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
    }
}

