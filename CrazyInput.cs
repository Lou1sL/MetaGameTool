using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Windows.Forms;
using UnityStandardUtils.Extension;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System;
using UnityStandardUtils;

public class CrazyInput : SingletonMonoBehaviour<CrazyInput>
{

    public enum MouseClick
    {
        Click, ClickDown, ClickUp
    }
    public class InputAction
    {
        private enum ActType
        {
            MouseMove, MouseClick, Keyboard,Action
        }

        private ActType ActT;
        private float WaitTimeCounter;
        private bool IsWaiting = false;

        private MouseClick Click;
        private Transform ToPosition;
        private string KBKey;
        private Action Act;

        public InputAction(float waitT)
        {
            IsWaiting = true;
            WaitTimeCounter = waitT;
        }
        public InputAction(string key,float waitT = 0.2f)
        {
            WaitTimeCounter = waitT;
            ActT = ActType.Keyboard;
            KBKey = key;
        }
        public InputAction(Transform p)
        {
            ActT = ActType.MouseMove;
            ToPosition = p;
        }
        public InputAction(MouseClick mc,float waitT = 0.5f)
        {
            WaitTimeCounter = waitT;
            ActT = ActType.MouseClick;
            Click = mc;
        }
        public InputAction(Action a,float waitT = 1f)
        {
            WaitTimeCounter = waitT;
            ActT = ActType.Action;
            Act = a;
        }

        public bool Action()
        {
            if (IsWaiting) return Waiting();


            switch (ActT)
            {
                case ActType.MouseMove:
                    return LerpMove(ToPosition.position);

                case ActType.MouseClick:
                    switch (Click)
                    {
                        case MouseClick.Click: LeftClick(); break;
                        case MouseClick.ClickDown: LeftClickDown(); break;
                        case MouseClick.ClickUp: LeftClickUp(); break;
                    }
                    break;

                case ActType.Keyboard:
                    SendKeys.SendWait(KBKey);
                    break;

                case ActType.Action:
                    Act.Invoke();
                    break;
            }
            IsWaiting = true;
            
            return false;
        }
        private bool Waiting()
        {
            WaitTimeCounter -= Time.deltaTime;
            return WaitTimeCounter < 0;
        }
    }
    private static List<InputAction> ProceduralAction = new List<InputAction>();
    
    private static MetaGameTool.Task.WindowStruct GameWindow;
    private void Start()
    {
        GameWindow = MetaGameTool.Task.Window.GetWindow(Process.GetCurrentProcess());
        

        //TEST
        ProceduralAction = new List<InputAction>()
        {
            new InputAction(5),
            new InputAction(()=>{ AI2DTargetManagerSingleton.Instance.Player.GetComponent<CC2DBackpackUserInput>().Opened = true; }),
            new InputAction(AI2DTargetManagerSingleton.Instance.Player.GetComponent<PlayerBackpackUI>().ObjSelector.transform),
            new InputAction(MouseClick.Click),
            new InputAction(AI2DTargetManagerSingleton.Instance.Player.GetComponent<PlayerBackpackUI>().Drop.transform),
            new InputAction(MouseClick.Click),
            new InputAction(()=>{ AI2DTargetManagerSingleton.Instance.Player.GetComponent<CC2DBackpackUserInput>().Opened = false; }),
        };


    }

    private void Update()
    {
        if (ProceduralAction.Count <= 0) return;

        InputAction act = ProceduralAction[0];
        if (act.Action()) ProceduralAction.RemoveAt(0);
    }
    


    /// <summary>
    /// World V3 To View V3
    /// </summary>
    /// <param name="w"></param>
    /// <returns></returns>
    private static Vector2 W2V(Vector3 w)
    {
        return Camera.main.WorldToScreenPoint(w);
    }
    /// <summary>
    /// View V3 To World V3
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    private static Vector2 V2W(Vector2 v)
    {
        return VectorExtension.ScreenToWorldPointPerspective(v);
    }
    /// <summary>
    /// V3 To Position
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    private static Point V2P(Vector3 v)
    {
        return new Point((int)v.x, UnityEngine.Screen.currentResolution.height - (int)v.y - 1);
    }
    /// <summary>
    /// Position To V3
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    private static Vector2 P2V(Point p)
    {
        return new Vector2(p.X, UnityEngine.Screen.currentResolution.height - p.Y - 1);
    }
    
    /// <summary>
    /// 鼠标在屏幕的坐标
    /// </summary>
    public static Vector2 MousePosition
    {
        get { return P2V(System.Windows.Forms.Cursor.Position); }
        set { System.Windows.Forms.Cursor.Position = V2P(value); }
    }
    /// <summary>
    /// 鼠标在游戏界面的坐标
    /// </summary>
    public static Vector2 MouseLocalPosition
    {
        get { return Input.mousePosition; }
        set { MousePosition = (MousePosition - (Vector2)Input.mousePosition) + value; }
    }
    /// <summary>
    /// 鼠标在游戏世界的坐标（get：Z取0时）
    /// </summary>
    public static Vector3 MouseWorldPosition
    {
        get { return VectorExtension.ScreenToWorldPointPerspective(MouseLocalPosition); }
        set { MouseLocalPosition = Camera.main.WorldToScreenPoint(value); }
    }

    /// <summary>
    /// 移动目标位置的百分比距离
    /// </summary>
    /// <param name="FinalWorldPosition">目标位置</param>
    /// <param name="p">移动百分比</param>
    /// <returns></returns>
    public static bool LerpMove(Vector3 FinalWorldPosition, float p = 0.2f)
    {
        bool xArrive = true;
        bool yArrive = true;

        Vector2 dis = (Vector2)Camera.main.WorldToScreenPoint(FinalWorldPosition) - MouseLocalPosition;

        if (Mathf.Abs(dis.x) > 5f)
        {
            dis.x *= p;
            xArrive = false;
        }
        if (Mathf.Abs(dis.y) > 5f)
        {
            dis.y *= p;
            yArrive = false;
        }

        MouseLocalPosition = dis + MouseLocalPosition;
        return (xArrive && yArrive);
    }

    /// <summary>
    /// 鼠标单击
    /// </summary>
    public static void LeftClick() { MetaGameTool.Task.Mouse.Click(); }
    /// <summary>
    /// 鼠标左键按下
    /// </summary>
    public static void LeftClickDown() { MetaGameTool.Task.Mouse.ClickDown(); }
    /// <summary>
    /// 鼠标左键抬起
    /// </summary>
    public static void LeftClickUp() { MetaGameTool.Task.Mouse.ClickUp(); }
    
    /// <summary>
    /// 游戏界面窗口信息
    /// </summary>
    public static Rect GameWindowRect
    {
        get { return MetaGameTool.Task.Window.GetWindowRect(GameWindow.MainWindowHandle); }
    }
    
    /// <summary>
    /// 清理队列事件
    /// </summary>
    public static void ClrProceduralAction()
    {
        ProceduralAction.Clear();
    }
    /// <summary>
    /// 设置队列事件
    /// </summary>
    /// <param name="list"></param>
    public static void SetProceduralAction(List<InputAction> list)
    {
        ProceduralAction.Clear();
        ProceduralAction = list;
    }
    /// <summary>
    /// 添加一个事件到队列
    /// </summary>
    /// <param name="ca"></param>
    public static void AddProceduralAction(InputAction ca)
    {
        ProceduralAction.Add(ca);
    }
}
