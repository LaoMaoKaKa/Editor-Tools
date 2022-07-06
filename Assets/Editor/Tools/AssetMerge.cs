using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 资源加载模式
/// </summary>
enum LoadType
{
    WhiteModel,      //白名单
    BlackModel        //黑名单
}

public class AssetData
{
    public int AssetsPathIndex;     //路径索引
    public string AssetsPath;          //路径
    public string MD5;
    public string Name;
    public string GUID;
    public string[] Dependencies;           //依赖信息
    public string[] DependenciesUsed;   //被依赖信息
    public int UsedNum = 0;  //被依赖

    public AssetData(int assetsPathIndex, string assetsPath, string md5, string name, string guid, string[] dependencies)
    {
        AssetsPathIndex = assetsPathIndex;
        AssetsPath = assetsPath;
        MD5 = md5;
        Name = name;
        GUID = guid;
        Dependencies = dependencies;
    }

}

public class AssetToolMenu
{

    [MenuItem("MT Tools/AssetTools/CheckAsset")]
    static void AssetMergeWindow()
    {
        EditorWindow.GetWindow<AssetMerge>("资源检测工具").Show();
    }

    [MenuItem("MT Tools/AssetTools/GUID查资源")]
    static void AssetGUIDWindow()
    {
        EditorWindow.GetWindow(typeof(AssetGUIDTool), true, "GUID查资源").Show();
    }

}

public class AssetMerge : EditorWindow
{
    static string[] BlackList = new string[] { ".git", ".meta", ".md" };
    static string[] WhiteList = new string[] { ".prefab", ".png", ".jpg", ".unity", ".mat", ".ogg", ".mp3", ".shader", ".anim", ".controller", ".txt", ".json", ".asset", "fontsettings", ".tga", ".fnt", ".TTF", ".FBX", ".TGA", ".bmp" };

    static string[] texts = { "重复资源-图示化", "资源引用-图示化" };
    static int MakeUIIndex = 0;     //图示化选项

    static int MakeUiOfRepeatPase = 1;
    static int MakeUiOfUsedPase = 1;

    static string LoadPath = "Assets/";
    static bool IsHeight = false;  //高级菜单
    static bool IsOnlyMakeOf0 = true; //只看0引用的资源

    static Vector2 ConterVector = Vector2.zero;

    static int MD5DicList_Count = 0;
    static int MD5DicList_Repeat_Count = 0;

    static Dictionary<string, List<AssetData>> MD5DicList = new Dictionary<string, List<AssetData>>();
    static Dictionary<string, List<AssetData>> MD5DicList_Repeat = new Dictionary<string, List<AssetData>>();
    static Dictionary<string, string> RemoveAssetList = new Dictionary<string, string>();
    static Dictionary<string, string> AssetTypeDicList = new Dictionary<string, string>();


    void OnGUI()
    {
        GUILayout.Space(15);
        GUILayout.BeginHorizontal();
        LoadPath = EditorGUILayout.TextField("资源路径:", LoadPath);
        if (GUILayout.Button("自动填充选取文件", GUILayout.Width(100)))
        {
            SelectFile();
        }
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();
        if (GUILayout.Button("检查资源"))
        {
            Debug.Log("开始检查");
            CheckAssetInfo();
            Debug.Log("检查结束");
        }
        //  GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("查看重复资源"))
        {
            Debug.Log("开始分析");
            PrintDoubleResInfo();
            Debug.Log("分析结束");
        }
        if (GUILayout.Button("查看资源依赖"))
        {
            Debug.Log("开始分析");
            PrintDependenciesInfo();
            Debug.Log("分析结束");
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("删除已经合并依赖的资源"))
        {
            Debug.Log("开始删除");
            QuickRemoveRes();
            Debug.Log("删除结束");
        }

        if (GUILayout.Button("手动刷新资源  (手动修改过资源后执行)"))
        {
            ReLoadAsset();
        }

        if (GUILayout.Button("清除缓存数据"))
        {
            ReSetData();
        }

        IsHeight = GUILayout.Toggle(IsHeight, "高级模式");
        MakeHeight();
        MakeUIIndex = GUILayout.SelectionGrid(MakeUIIndex, texts, texts.Length);
        MakeUICont();
    }

    /// <summary>
    /// 高级模式
    /// </summary>
    void MakeHeight()
    {
        if (!IsHeight)
        {
            return;
        }
        string[] Modes = { "白名单模式", "黑名单模式(未开放)" };
        GUILayout.SelectionGrid(0, Modes, 2);
        GUILayout.BeginHorizontal();
        // GUILayout.Toggle(true, "白名单模式", GUILayout.Width(80));
        string WhiteListText = "白名单文件类型：";
        foreach (var item in WhiteList)
        {
            WhiteListText += "  " + item;
        }
        GUILayout.Label(WhiteListText);
        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();
        // GUILayout.Toggle(false, "黑名单模式", GUILayout.Width(80));
        string BlackListText = "黑名单文件类型：";
        foreach (var item in BlackList)
        {
            BlackListText += "  " + item;
        }
        GUILayout.Label(BlackListText);
        GUILayout.EndHorizontal();
        GUILayout.Label("(发现缺失处理类型需要手动添加)");


        GUILayout.BeginHorizontal();
        if (GUILayout.Button("查看文件内存在的后缀类型"))
        {
            Debug.Log("开始分析");
            PrintAllFileType();
            Debug.Log("分析结束");
        }

        GUILayout.Space(10);
        if (GUILayout.Button("查看文件依赖丢失的资源(未完成)"))
        {
            Debug.Log("开始分析");
            PrintAllFileDependencies();
            Debug.Log("分析结束");
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

    /// <summary>
    /// 选择文件/文件夹
    /// </summary>
    private void SelectFile()
    {
        if (Selection.assetGUIDs.Length != 0)
        {
            LoadPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
        }
    }

    /// <summary>
    /// 重置数据
    /// </summary>
    void ReSetData()
    {
        MD5DicList_Count = 0;
        MD5DicList_Repeat_Count = 0;
        MD5DicList = new Dictionary<string, List<AssetData>>();
        MD5DicList_Repeat = new Dictionary<string, List<AssetData>>();
        RemoveAssetList = new Dictionary<string, string>();
        AssetTypeDicList = new Dictionary<string, string>();
        MakeUiOfRepeatPase = 1;
        MakeUiOfUsedPase = 1;
    }

    /// <summary>
    /// 检查资源MD5，记录相关数据
    /// </summary>
    void CheckAssetInfo()
    {
        ReLoadAsset();

        string loadpath = LoadPath;
        Debug.Log("检查路径：" + LoadPath);
        string[] assetsPath = Directory.GetFiles(loadpath, "*.*", SearchOption.AllDirectories);

        ReSetData();

        for (int i = 0; i < assetsPath.Length; i++)
        {
            try
            {
                var cancel = EditorUtility.DisplayCancelableProgressBar(string.Format("检查资源中({0}/{1})", i, assetsPath.Length), assetsPath[i], (float)i / assetsPath.Length);
                if (cancel)
                {
                    EditorUtility.ClearProgressBar();
                    //return;
                }
                CheckFileType(assetsPath[i]);
                if (CheckWirteList(assetsPath[i]))
                {
                    //计算资源的md5
                    MD5 md5 = MD5.Create();
                    byte[] md5bytes = md5.ComputeHash(getFileByte(assetsPath[i]));
                    string filemd5 = System.BitConverter.ToString(md5bytes).Replace("-", "").ToLower();
                    string fileguid = AssetDatabase.AssetPathToGUID(assetsPath[i]);
                    string[] dependencies = AssetDatabase.GetDependencies(assetsPath[i]);
                    string name = AssetDatabase.LoadMainAssetAtPath(assetsPath[i]).name;
                    Debug.Log("检查资源路径：" + assetsPath[i] + ",MD5:" + filemd5);
                    AssetData assetData = new AssetData(i, assetsPath[i], filemd5, name, fileguid, dependencies);
                    if (!MD5DicList.ContainsKey(filemd5))
                    {
                        MD5DicList.Add(filemd5, new List<AssetData>());
                    }
                    else
                    {
                        Debug.Log("发现重复资源+1");
                    }
                    MD5DicList_Count++;
                    MD5DicList[filemd5].Add(assetData);
                }
            }
            catch
            {
                Debug.LogError("遍历资源中断，引擎缓存数据可能出现混乱，请重启再试！！！");
                Debug.LogError("一本正经地胡说八道");
            }

        }
        EditorUtility.ClearProgressBar();
        Debug.Log("MD5值不同的资源数量为：" + MD5DicList.Count);

        //构造重复的资源列表
        int repeatNum = 0;
        foreach (var item in MD5DicList)
        {
            if (item.Value.Count > 1)
            {
                MD5DicList_Repeat_Count += item.Value.Count;
                MD5DicList_Repeat.Add(item.Key, item.Value);
                repeatNum++;
                //Debug.Log("执行转移重复资源+1");
            }
        }
        Debug.LogError("MD5值相同的资源数量为：" + repeatNum);
    }

    /// <summary>
    /// 打印依赖信息
    /// </summary>
    void PrintDependenciesInfo()
    {
        if (MD5DicList.Count < 1)
        {
            Debug.LogError("没有获取到资源数据，请先执行“检查资源”");
            return;
        }
        int SliderNum = 0;
        foreach (var item in MD5DicList)
        {
            foreach (var item2 in item.Value)
            {
                SliderNum++;
                var canel = EditorUtility.DisplayCancelableProgressBar(string.Format("遍历资源依赖:{0}/{1}", SliderNum, MD5DicList_Count), item2.AssetsPath, (float)SliderNum / MD5DicList_Count);
                if (canel)
                {
                    EditorUtility.ClearProgressBar();
                }
                foreach (var item3 in item2.Dependencies)
                {
                    Debug.Log(string.Format("{0}的依赖信息:{1}", item2.AssetsPath, item3));
                }

            }
        }
        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// 分析重复的资源，并打印
    /// </summary>
    void PrintDoubleResInfo()
    {
        if (MD5DicList_Repeat.Count < 1)
        {
            Debug.LogError("没有获取到资源数据，请先执行“检查资源”");
            return;
        }

        int ResNum = 0;
        int SliderNum = 0;
        foreach (var item in MD5DicList_Repeat)
        {
            int Num = 0;
            foreach (var item2 in item.Value)
            {
                var cancel = EditorUtility.DisplayCancelableProgressBar(string.Format("遍历重复的资源中{0}/{1}", SliderNum, MD5DicList_Repeat_Count), item2.AssetsPath, (float)SliderNum / MD5DicList_Repeat_Count);
                if (cancel)
                {
                    EditorUtility.ClearProgressBar();
                    //return;
                }
                if (Num == 0)
                {
                    ResNum++;
                    Debug.Log(string.Format("第{0}组重复资源：", ResNum));
                }
                Num++;
                SliderNum++;
                Debug.Log(string.Format("({0})重复的资源：{1}", Num, item2.AssetsPath));
                //Debug.Log("Index:" + Num + ",打印重复资源的GUID：" + item2.GUID);
            }
        }
        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// 指定合并
    /// </summary>
    void AppointMerge(string AppointGUID)
    {
        if (MD5DicList_Repeat.Count < 1)
        {
            Debug.LogError("没有获取到资源数据，请先执行“检查资源”");
            return;
        }

        List<AssetData> AppointList = new List<AssetData>();
        foreach (var item in MD5DicList_Repeat)
        {
            foreach (var item2 in item.Value)
            {
                if (item2.GUID == AppointGUID)
                {
                    AppointList = item.Value;
                    goto GetAppoint;
                }
            }
        }

    GetAppoint:;
        string Dependencies_new = "";
        string Dependencies_old = "";
        Dependencies_new = AppointGUID;

        foreach (var item in AppointList)
        {
            if (item.GUID != AppointGUID)
            {
                Dependencies_old = item.GUID;
                RemoveAssetList.Add(item.AssetsPath, item.MD5);
                MoveDependencies(Dependencies_old, Dependencies_new);
            }
        }

    }

    /// <summary>
    /// 快速删除资源
    /// </summary>
    void QuickRemoveRes()
    {
        if (RemoveAssetList.Count < 1)
        {
            Debug.LogError("RemoveAssetList 为空，尚未完成前置步骤，按要求来，一个一个点");
            return;
        }
        int SliderNum = 0;
        foreach (var AssetItem in RemoveAssetList)
        {
            SliderNum++;
            var cancel = EditorUtility.DisplayCancelableProgressBar(string.Format("删除资源中{0}/{1}", SliderNum, RemoveAssetList.Count), AssetItem.Key, (float)SliderNum / RemoveAssetList.Count);
            if (cancel)
            {
                EditorUtility.ClearProgressBar();
                //return;
            }
            try
            {
                AssetDatabase.DeleteAsset(AssetItem.Key);
                AssetData RmoveAsset = null;
                foreach (var item in MD5DicList[AssetItem.Value])
                {
                    if (item.MD5 == AssetItem.Value)
                    {
                        RmoveAsset = item;
                        break;
                    }
                }
                if (RmoveAsset != null)
                {
                    MD5DicList[AssetItem.Value].Remove(RmoveAsset);
                }

                Debug.Log("删除重复资源：：" + AssetItem.Key);
            }
            catch (System.Exception)
            {
                Debug.LogError("文件状态异常：" + AssetItem.Key);
            }

        }
        RemoveAssetList.Clear();
        EditorUtility.ClearProgressBar();
        Debug.LogError("移除重复资源结束");
        //AssetDatabase.Refresh();
    }

    /// <summary>
    /// 依赖转移
    /// </summary>
    /// <param name="assetList"></param>
    /// <param name="Dependencies_old"></param>
    /// <param name="Dependencies_new"></param>
    /// <param name="path"></param>
    void MoveDependencies(string Dependencies_old, string Dependencies_new)
    {
        int SliderNum = 0;
        foreach (var item in MD5DicList)
        {
            foreach (var item2 in item.Value)
            {
                Debug.Log("遍历：" + item2.AssetsPath);
                SliderNum++;
                var cancel = EditorUtility.DisplayCancelableProgressBar("发了疯地合并中...", item2.AssetsPath, (float)SliderNum / MD5DicList_Repeat_Count);
                if (cancel)
                {
                    EditorUtility.ClearProgressBar();
                    //return;
                }
                foreach (var item3 in item2.Dependencies)
                {
                    if (item2.GUID == Dependencies_old || item2.GUID == Dependencies_new)
                    {
                        Debug.Log("重复资源本身，不执行处理，Res:" + Dependencies_old);
                        break;
                    }
                    Debug.Log(item2.Name + "de 依赖：" + item3 + "  对应的：" + Dependencies_old);
                    if (AssetDatabase.AssetPathToGUID(item3) == Dependencies_old)
                    {
                        Debug.LogError(item2.AssetsPath + ":" + item2.Name + "的依赖信息" + item3 + "改为：" + Dependencies_new);
                        FileStream fileStream = new FileStream(item2.AssetsPath, FileMode.Open);
                        byte[] bt = new byte[fileStream.Length];
                        fileStream.Read(bt, 0, bt.Length);
                        if (item2.GUID != Dependencies_old)
                        {
                            string s = new UTF8Encoding().GetString(bt);
                            s = s.Replace(Dependencies_old, Dependencies_new).Replace("\0", "");
                            byte[] bt2 = new UTF8Encoding().GetBytes(s);
                            fileStream.SetLength(0);
                            fileStream.Write(bt2, 0, bt2.Length);
                            fileStream.Close();
                            fileStream.Dispose();
                            Debug.LogError("修改" + item2.Name + "的依赖资源" + item3 + "为：" + Dependencies_new + "成功");
                            break;
                        }

                    }

                }
            }
        }
        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// 计算被引用
    /// </summary>
    void GetAssetUsedCount()
    {
        int SliderNum = 0;
        foreach (var item in MD5DicList)
        {
            foreach (var item2 in item.Value)
            {
                SliderNum++;
                var cancel = EditorUtility.DisplayCancelableProgressBar("发了疯地计算中...", item2.AssetsPath, (float)SliderNum / MD5DicList_Count);
                if (cancel)
                {
                    EditorUtility.ClearProgressBar();
                    //return;
                }
                item2.UsedNum = GetDependenciesUsedCount(item2.GUID);
            }
        }
        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// 计算被引用
    /// </summary>
    int GetDependenciesUsedCount(string GUID)
    {
        int Count = 0;
        foreach (var item in MD5DicList)
        {
            foreach (var item2 in item.Value)
            {
                foreach (var item3 in item2.Dependencies)
                {
                    if (AssetDatabase.AssetPathToGUID(item3) == GUID)
                    {
                        Count++;
                    }
                }
            }
        }
        return Count - 1;
    }

    /// <summary>
    /// 文件转字节
    /// </summary>
    /// <param name="texturePath"></param>
    /// <returns></returns>
    byte[] getFileByte(string assetsPath)
    {
        FileStream file = new FileStream(assetsPath, FileMode.Open);
        byte[] txByte = new byte[file.Length];
        file.Read(txByte, 0, txByte.Length);
        file.Close();
        file.Dispose();
        return txByte;
    }

    /// <summary>
    /// 图示化控制
    /// </summary>
    void MakeUICont()
    {
        switch (MakeUIIndex)
        {

            case 0:
                MakeUIOfRepeat();
                break;
            case 1:
                MakeUiOfUsed();
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 图示化
    /// </summary>
    void MakeUIOfRepeat()
    {
        if (MakeUIIndex != 0)
        {
            return;
        }
        ConterVector = GUILayout.BeginScrollView(ConterVector, "Box");
        int Index = 0;
        string RemoveKey = null;
        foreach (var item in MD5DicList_Repeat)
        {
            Index++;
            if ((MakeUiOfRepeatPase - 1) * 5 < Index && Index <= MakeUiOfRepeatPase * 5)
            {
                GUILayout.Label(string.Format("第{0}组：", Index));
                foreach (var item2 in item.Value)
                {
                    EditorGUILayout.BeginHorizontal("HelpBox");
                    GUILayout.Label(AssetDatabase.GetCachedIcon(item2.AssetsPath), new GUILayoutOption[] { GUILayout.Width(30), GUILayout.Height(20) });
                    GUILayout.Label(item2.AssetsPath);
                    if (GUILayout.Button("定位", GUILayout.Width(200)))
                    {
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(item2.AssetsPath);
                    }
                    if (GUILayout.Button("合并", GUILayout.Width(200)))
                    {
                        Debug.LogError(item2.AssetsPath);
                        AppointMerge(item2.GUID);
                        RemoveKey = item.Key;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        GUILayout.EndScrollView();
        if (Index > 0)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("上一页", GUILayout.Height(25)))
            {
                if (MakeUiOfRepeatPase != 1)
                    MakeUiOfRepeatPase--;
            }
            GUILayout.Label(string.Format("第{0}页", MakeUiOfRepeatPase), GUILayout.Width(45));
            if (GUILayout.Button("下一页", GUILayout.Height(25)))
            {
                int m = Index % 5;
                Index /= 5;
                if (m > 0)
                {
                    Index++;
                }
                if (MakeUiOfRepeatPase < Index)
                    MakeUiOfRepeatPase++;
            }
            GUILayout.EndHorizontal();
        }

        if (RemoveKey != null)
        {
            MD5DicList_Repeat.Remove(RemoveKey);
        }
    }

    /// <summary>
    /// 资源引用图示化
    /// </summary>
    void MakeUiOfUsed()
    {
        if (MakeUIIndex != 1)
        {
            return;
        }
        EditorGUILayout.Space();
        GUILayout.BeginHorizontal();
        IsOnlyMakeOf0 = GUILayout.Toggle(IsOnlyMakeOf0, "只查看0引用的资源");
        if (GUILayout.Button("检测资源引用信息"))
        {
            GetAssetUsedCount();
        }
        GUIStyle gUIStyle = new GUIStyle();
        gUIStyle.normal.textColor = Color.red;
        GUILayout.Label("  Tips：只检测资源依赖，无法检测代码中动态加载的资源！！！", gUIStyle);
        GUILayout.EndHorizontal();
        ConterVector = GUILayout.BeginScrollView(ConterVector, "Box");
        int index = 0;
        string RemoveKey = null;
        foreach (var item in MD5DicList)
        {
            foreach (var item2 in item.Value)
            {
                if (item2.UsedNum == 0 || IsOnlyMakeOf0 == false)
                {
                    index++;
                    if ((MakeUiOfUsedPase - 1) * 20 < index && index <= MakeUiOfUsedPase * 20)
                    {
                        EditorGUILayout.BeginHorizontal("HelpBox");
                        GUILayout.Label(AssetDatabase.GetCachedIcon(item2.AssetsPath), new GUILayoutOption[] { GUILayout.Width(30), GUILayout.Height(20) });
                        GUILayout.Label(item2.AssetsPath);
                        GUILayout.Label(string.Format("被引用：{0}", item2.UsedNum.ToString()));
                        if (GUILayout.Button("定位", GUILayout.Width(200)))
                        {
                            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(item2.AssetsPath);
                        }
                        if (GUILayout.Button("删除", GUILayout.Width(200)))
                        {
                            AssetDatabase.DeleteAsset(item2.AssetsPath);
                            RemoveKey = item.Key;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
        }
        GUILayout.EndScrollView();

        if (index > 0)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("上一页", GUILayout.Height(25)))
            {
                if (MakeUiOfUsedPase != 1)
                    MakeUiOfUsedPase--;
            }
            GUILayout.Label(string.Format("第{0}页", MakeUiOfUsedPase), GUILayout.Width(45));
            if (GUILayout.Button("下一页", GUILayout.Height(25)))
            {
                int m = index % 20;
                index /= 20;
                if (m > 0)
                {
                    index++;
                }
                if (MakeUiOfUsedPase < index)
                    MakeUiOfUsedPase++;
            }
            GUILayout.EndHorizontal();
        }

        if (RemoveKey != null)
        {
            MD5DicList.Remove(RemoveKey);
        }
    }

    /// <summary>
    /// 黑名单模式  忽略黑名单列表的后缀文件类型的资源  容易受到一些奇怪的文件影响，暂时放弃，保留处理方式
    /// </summary>
    bool CheckBlackList(string path)
    {
        foreach (var item in BlackList)
        {
            if (path.EndsWith(item))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 白名单模式，只对白名单列表内的文件后缀类型进行处理
    /// </summary>
    bool CheckWirteList(string path)
    {
        foreach (var item in WhiteList)
        {
            if (path.EndsWith(item))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 查看所有文件的后缀类型
    /// </summary>
    void PrintAllFileType()
    {
        if (AssetTypeDicList.Count < 1)
        {
            Debug.LogError("尚未检查资源或者资源文件夹为空");
            return;
        }
        Debug.Log("文件内存在的后缀类型：");
        int index = 0;
        foreach (var item in AssetTypeDicList)
        {
            index++;
            Debug.Log(string.Format("{0}:{1}", index, item.Value));
        }
    }

    /// <summary>
    /// 检查文件类型，记录
    /// </summary>
    void CheckFileType(string Name)
    {
        string type = GetFileType(Name);
        if (AssetTypeDicList.ContainsKey(type))
        {
            return;
        }
        AssetTypeDicList.Add(type, type);
    }

    /// <summary>
    /// 获取文件后缀类型
    /// </summary>
    /// <param name="Name"></param>
    /// <returns></returns>
    string GetFileType(string Name)
    {
        string[] str = Name.Split('.');
        return str[str.Length - 1];
    }


    /// <summary>
    /// 查看依赖丢失的资源 //未完成
    /// </summary>
    void PrintAllFileDependencies()
    {
        if (MD5DicList.Count < 1)
        {
            Debug.LogError("尚未检查资源或者资源文件夹为空");
            return;
        }
        Debug.Log("依赖丢失的文件：");
        int index = 0; //
        foreach (var item in MD5DicList)
        {
            foreach (var item2 in item.Value)
            {
                foreach (var item3 in item2.Dependencies)
                {
                    try
                    {
                        if (!File.Exists(item3))
                        {
                            Debug.LogError(string.Format("{0}丢失依赖：{1}", item2.AssetsPath, item3));
                        }
                    }
                    catch (System.Exception)
                    {
                        Debug.LogError(string.Format("{0}丢失依赖：{1}", item2.AssetsPath, item3));
                        throw;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 单个资源操作
    /// </summary>
    void ChekAAsset()
    {
        string SelectionGuid = Selection.assetGUIDs[0];
        string SelectionPath = AssetDatabase.GUIDToAssetPath(SelectionGuid);
        Debug.Log(string.Format("当前选择的地址是{0},GUID:{1}", SelectionPath, SelectionGuid));
        if (Selection.assetGUIDs.Length != 1 || !SelectionPath.Contains("."))
        {
            Debug.LogError("请选择【一个资源】");
            return;
        }
        AppointMerge(SelectionGuid);
    }

    /// <summary>
    /// 刷新资源文件
    /// </summary>
    void ReLoadAsset()
    {
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
    }

}

/// <summary>
/// 扩展部分  下次在搞了
/// </summary>


public class AssetGUIDTool : EditorWindow
{
    static string GUID = "";

    void OnGUI()
    {
        GUILayout.Space(10);
        GUID = EditorGUILayout.TextField("GUID:", GUID);
        if (GUILayout.Button("查询GUID对应的资源"))
        {
            GUIDToGetAsset();
        }
    }

    void GUIDToGetAsset()
    {
        string AssetName = AssetDatabase.GUIDToAssetPath(GUID);
        if (string.IsNullOrEmpty(AssetName))
        {
            Debug.LogError("没有查询到对应的资源");
            return;
        }
        Debug.Log("GUID:" + AssetName);
    }

}

