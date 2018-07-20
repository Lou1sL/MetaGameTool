using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityStandardUtils.Extension;
using UnityStandardUtils.Web;

namespace UnityStandardUtils
{


    public static class MetaGameTool
    {

        /// <summary>
        /// 路径
        /// </summary>
        public static class Path
        {
            public static readonly string SystemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            public static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            public static readonly string StreamingAssetsPath = Application.streamingAssetsPath;
            public static readonly string GameDataPath = Application.dataPath;
        }
        /// <summary>
        /// 文件操作相关
        /// </summary>
        public static class File
        {
            /// <summary>
            /// 从StreamingAssets复制
            /// </summary>
            /// <param name="oriname">源文件名</param>
            /// <param name="targetname">目标文件名（留空为不改名）</param>
            /// <param name="targetpath">目标路径（留空为桌面路径）</param>
            /// <returns>是否成功</returns>
            public static bool CopyFromSA(string oriname, string targetname = null, string targetpath = null)
            {
                string oripath = System.IO.Path.Combine(Application.streamingAssetsPath, oriname);
                string topath = System.IO.Path.Combine(targetpath ?? Path.DesktopPath, targetname ?? oriname);

                if (System.IO.File.Exists(topath) || !System.IO.File.Exists(oripath)) return false;


                System.IO.File.Copy(oripath, topath);

                return true;
            }
            /// <summary>
            /// 删除文件（小心！）
            /// </summary>
            /// <param name="name">文件名</param>
            /// <param name="path">路径(留空为StreamingAssets路径)</param>
            /// <returns></returns>
            public static bool DeleteFile(string name, string path = null)
            {

                string rmpath = System.IO.Path.Combine(path ?? Path.StreamingAssetsPath, name);

                if (!System.IO.File.Exists(rmpath)) return false;

                System.IO.File.Delete(rmpath);

                return true;
            }
            /// <summary>
            /// 读取字符文件
            /// </summary>
            /// <param name="name">文件名</param>
            /// <param name="path">路径（留空为桌面路径）</param>
            /// <returns></returns>
            public static string ReadFile(string name, string path = null)
            {
                string p = path ?? Path.DesktopPath;
                p = System.IO.Path.Combine(p, name);

                StreamReader sr = System.IO.File.OpenText(p);
                return sr.ReadToEnd();
            }
        }

        /// <summary>
        /// 进程相关
        /// </summary>
        public static class Task
        {
            /// <summary>
            /// 运行控制台程序，并返回控制台输出，会卡住游戏主进程
            /// </summary>
            /// <param name="process">程序名</param>
            /// <param name="args">进程参数</param>
            /// <param name="path">路径(留空为StreamingAssets路径)</param>
            /// <returns>控制台输出</returns>
            public static string RunProcess(string process, string args = null, string path = null)
            {
                string p = path ?? Path.StreamingAssetsPath;
                p = System.IO.Path.Combine(p, process);
                if (!System.IO.File.Exists(p)) return null;

                Process proc = new Process();

                proc.EnableRaisingEvents = false;
                proc.StartInfo.FileName = p;
                if (args != null) proc.StartInfo.Arguments = args;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.Start();
                string fingerprint = proc.StandardOutput.ReadLine();
                proc.WaitForExit();
                return fingerprint;
            }
            
            public class WindowStruct
            {
                public string Title { get; set; }
                public IntPtr MainWindowHandle { get; set; }
            }
            public class Window
            {
                private delegate bool CallBackPtr(int hwnd, int lParam);
                private static CallBackPtr callBackPtr = Callback;
                private static List<WindowStruct> _WinStructList = new List<WindowStruct>();

                [DllImport("user32.dll")]
                [return: MarshalAs(UnmanagedType.Bool)]
                private static extern bool EnumWindows(CallBackPtr lpEnumFunc, IntPtr lParam);

                [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
                private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

                [DllImport("user32.dll")]
                private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);
                
                [DllImport("user32.dll", EntryPoint = "GetWindowRect")]
                private static extern int GetWindowRect(IntPtr hwnd, out Rectangle rect);

                [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
                private static extern bool SetWindowPos(IntPtr hwnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

                private static bool Callback(int hWnd, int lparam)
                {
                    StringBuilder sb = new StringBuilder(256);
                    int res = GetWindowText((IntPtr)hWnd, sb, 256);
                    _WinStructList.Add(new WindowStruct { MainWindowHandle = (IntPtr)hWnd, Title = sb.ToString() });
                    return true;
                }
                /// <summary>
                /// 获取全部窗口
                /// </summary>
                /// <returns></returns>
                public static List<WindowStruct> GetWindows()
                {
                    _WinStructList = new List<WindowStruct>();
                    EnumWindows(callBackPtr, IntPtr.Zero);
                    return _WinStructList;
                }
                /// <summary>
                /// 获取进程窗口
                /// </summary>
                /// <param name="proc"></param>
                /// <returns></returns>
                public static WindowStruct GetWindow(Process proc)
                {
                    foreach (WindowStruct ws in GetWindows())
                    {
                        uint pid = 0;
                        IntPtr p = GetWindowThreadProcessId(ws.MainWindowHandle, out pid);
                        if (proc.Id == pid) return ws;
                    }
                    return null;
                }

                /// <summary>
                /// 设置窗口的Rect
                /// </summary>
                /// <param name="window"></param>
                /// <param name="rect"></param>
                public static void SetWindowRect(IntPtr window, Rect rect)
                {
                    if (rect.size == Vector2.zero)
                        SetWindowPos(window, 0, (int)rect.position.x, (int)rect.position.y + 1, 0, 0, 1);
                    else
                        SetWindowPos(window, 0, (int)rect.position.x, (int)rect.position.y + 1, (int)rect.size.x, (int)rect.size.y, 0);
                }
                /// <summary>
                /// 获取窗口的Rect
                /// </summary>
                /// <param name="window"></param>
                /// <returns></returns>
                public static  Rect GetWindowRect(IntPtr window)
                {
                    Rectangle rect = new Rectangle();
                    GetWindowRect(window, out rect);

                    Rect r = new Rect();
                    r.position = new Vector2(rect.X, UnityEngine.Screen.height - rect.Y - 1);
                    r.size = new Vector2(rect.Width - rect.X, rect.Height - rect.Y);
                    return r;
                }
            }
            public class Mouse
            {
                [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
                private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
                //Mouse actions
                private const int MOUSEEVENTF_LEFTDOWN = 0x02;
                private const int MOUSEEVENTF_LEFTUP = 0x04;
                private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
                private const int MOUSEEVENTF_RIGHTUP = 0x10;

                private static void _click(uint action)
                {
                    uint X = (uint)System.Windows.Forms.Cursor.Position.X;
                    uint Y = (uint)System.Windows.Forms.Cursor.Position.Y;
                    mouse_event(action, X, Y, 0, 0);
                }

                public static void Click()
                {
                    _click(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP);
                }

                public static void ClickDown()
                {
                    _click(MOUSEEVENTF_LEFTDOWN);
                }

                public static void ClickUp()
                {
                    _click(MOUSEEVENTF_LEFTUP);
                }
            }

        }

        /// <summary>
        /// 吓人的玩意
        /// </summary>
        public static class Mad
        {
            /// <summary>
            /// CRASH!
            /// </summary>
            public static void GameCrash()
            {
                for (; ; );
            }
            /// <summary>
            /// NOOOOOOOOO!
            /// Be careful
            /// </summary>
            public static void BSOD()
            {
                Task.RunProcess("BSOD.exe");
            }
        }


        /// <summary>
        /// 地理位置
        /// </summary>
        public class Location
        {
            public string Country { get; private set; }
            public string Region { get; private set; }
            public string City { get; private set; }
            public string Isp { get; private set; }

            public Location() { }

            public Location(string c, string r, string ci, string i)
            {
                Country = c; Region = r; City = ci; Isp = i;
            }

            public override string ToString()
            {
                return Country + Region + ((Region == City) ? "" : City);
            }

            private string FixStr(string str)
            {
                if (str == "XX") return "";
                else return str;
            }
        }
        /// <summary>
        /// 系统信息
        /// </summary>
        public class SystemInfo
        {
            public string UserName;
            public string MachineName;
            public string OS;
            public int ProcessorCount;
            public bool Is64;
            public int MemorySizeInGB;

            public override string ToString()
            {
                return
                    "UserName            " + UserName + "\n" +
                    "MachineName       " + MachineName + "\n" +
                    "OS                      " + OS + "\n" +
                    "ProcessorCount    " + ProcessorCount + "\n" +
                    "Is64                    " + Is64 + "\n" +
                    "MemorySize(GB)  " + MemorySizeInGB + "\n";

            }
        }
        /// <summary>
        /// 进程信息
        /// </summary>
        public class ProcessInfo
        {
            private List<string> process = new List<string>();

            public void Add(string name)
            {
                if (!process.Contains(name)) process.Add(name);
            }
            public bool IsContain(string name)
            {
                return process.Contains(name);
            }
        }
        

        private static string ipcache = null;
        private static Location locationcache = null;
        private static SystemInfo sysinfocache = null;
        


        /// <summary>
        /// Get IP
        /// </summary>
        /// <returns></returns>
        public static string GetIP()
        {
            if (ipcache != null) return ipcache;
            try
            {
                WebRequest wr = WebRequest.Create("http://ip.chinaz.com/getip.aspx");
                Stream s = wr.GetResponse().GetResponseStream();
                StreamReader sr = new StreamReader(s, Encoding.Default);
                ipcache = SubStringFromTo(sr.ReadToEnd(), "ip:\'", "\'");
                sr.Close();
                s.Close();
            }
            catch { ipcache = null; }
            return ipcache;
        }
        /// <summary>
        /// 获取地理位置
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static Location GetLocation()
        {
            if (locationcache != null) return locationcache;
            try
            {
                WebRequest wr = WebRequest.Create("http://ip.taobao.com/service/getIpInfo.php?ip=" + GetIP());
                Stream s = wr.GetResponse().GetResponseStream();
                StreamReader sr = new StreamReader(s, Encoding.Default);
                string str = sr.ReadToEnd();


                locationcache = new Location(SubStringFromTo(str, "country\":\"", "\""),
                    SubStringFromTo(str, "region\":\"", "\""),
                    SubStringFromTo(str, "city\":\"", "\""),
                    SubStringFromTo(str, "isp\":\"", "\"")
                    );

                sr.Close();
                s.Close();
            }
            catch { locationcache = null; }

            return locationcache;
        }
        /// <summary>
        /// 获取系统信息
        /// </summary>
        /// <returns></returns>
        public static SystemInfo GetSystemInfo()
        {
            if (sysinfocache != null) return sysinfocache;
            try
            {
                sysinfocache = new SystemInfo();
                sysinfocache.UserName = Environment.UserName;
                sysinfocache.MachineName = Environment.MachineName;
                sysinfocache.OS = GetWindowsNameFromVersion(Environment.OSVersion.VersionString);
                sysinfocache.ProcessorCount = Environment.ProcessorCount;
                sysinfocache.Is64 = Environment.Is64BitOperatingSystem;
                sysinfocache.MemorySizeInGB = (int)(new PerformanceCounter("Mono Memory", "Total Physical Memory").RawValue / 1000 / 1000 / 1000);

                if (sysinfocache.OS.Contains("10"))
                {
                    string realname = GetWin10UserName();
                    if (realname != null) sysinfocache.UserName = realname;
                }

            }
            catch { sysinfocache = null; }

            return sysinfocache;
        }
        /// <summary>
        /// 获取进程信息
        /// </summary>
        /// <returns></returns>
        public static ProcessInfo GetProcessInfo()
        {
            ProcessInfo pi = new ProcessInfo();

            foreach (Process ps in Process.GetProcesses())
            {
                pi.Add(ps.ProcessName);
            }
            return pi;
        }
        /// <summary>
        /// win10的用户名获取
        /// </summary>
        /// <returns></returns>
        public static string GetWin10UserName()
        {
            return Task.RunProcess("WinAccountName.exe");
        }
        /// <summary>
        /// 将版本号转换为系统名
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private static string GetWindowsNameFromVersion(string v)
        {
            const string NamePrefix = "Microsoft Windows NT ";

            string name = "windows ";

            if (v.Contains(NamePrefix + "10")) return name + "10";
            if (v.Contains(NamePrefix + "6.3")) return name + "8.1";
            if (v.Contains(NamePrefix + "6.2")) return name + "8";
            if (v.Contains(NamePrefix + "6.1")) return name + "7";
            if (v.Contains(NamePrefix + "6.0")) return name + "vista";
            if (v.Contains(NamePrefix + "5")) return name + "xp";
            return name;
        }
        private static string SubStringFromTo(string str, string frm, string to)
        {
            int sp = str.IndexOf(frm) + frm.Length;
            int ep = str.Substring(sp).IndexOf(to);
            return str.Substring(sp, ep);
        }
    }
}