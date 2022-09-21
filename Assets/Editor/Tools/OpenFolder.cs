using System.Diagnostics;
using UnityEditor;
using UnityEngine;

public class OpenFolderMenu
{
    /// <summary>
    /// 打开 Data Path 文件夹。
    /// </summary>
    [MenuItem("MT Tools/Open Folder/Data Path")]
    public static void OpenFolderDataPath()
    {
        OpenFolder.Execute(Application.dataPath);
    }

    /// <summary>
    /// 打开 Persistent Data Path 文件夹。
    /// </summary>
    [MenuItem("MT Tools/Open Folder/Persistent Data Path")]
    public static void OpenFolderPersistentDataPath()
    {
        OpenFolder.Execute(Application.persistentDataPath);
    }

    /// <summary>
    /// 打开 Streaming Assets Path 文件夹。
    /// </summary>
    [MenuItem("MT Tools/Open Folder/Streaming Assets Path")]
    public static void OpenFolderStreamingAssetsPath()
    {
        OpenFolder.Execute(Application.streamingAssetsPath);
    }

    /// <summary>
    /// 打开 Temporary Cache Path 文件夹。
    /// </summary>
    [MenuItem("MT Tools/Open Folder/Temporary Cache Path")]
    public static void OpenFolderTemporaryCachePath()
    {
        OpenFolder.Execute(Application.temporaryCachePath);
    }

}

/// <summary>
/// 打开文件夹相关的实用函数。
/// </summary>
public static class OpenFolder
{
        /// <summary>
        /// 打开指定路径的文件夹。
        /// </summary>
        /// <param name="folder">要打开的文件夹的路径。</param>
        public static void Execute(string folder)
        {
            folder = string.Format("\"{0}\"", folder);
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    Process.Start("Explorer.exe", folder.Replace('/', '\\'));
                    break;
                case RuntimePlatform.OSXEditor:
                    Process.Start("open", folder);
                    break;
                default:
                    UnityEngine.Debug.LogError(string.Format("Not support open folder on '{0}' platform.", Application.platform.ToString()));
                    throw null;
            }
        }
    }

