using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CanvasToolMenu
{
    [MenuItem("MT Tools/Canvas control")]
    static void CanvasToolWindow()
    {
        EditorWindow.GetWindow<CanvasCon>("Canvas control tool").Show();
    }
}

public class CanvasCon : EditorWindow
{
    static int CanvasNum = 0;           //变化量
    static GameObject obj = null;

    void OnGUI()
    {
        if (GUILayout.Button("选中预制体"))
        {
            SelectGameobject();
        }
        if (obj)
        {
            GUILayout.Label("预制体：" + obj.name);
        }

        CanvasNum = EditorGUILayout.IntField("变化量:", CanvasNum);

        if (GUILayout.Button("初始化(按顺序调整为0,1,2,3,4,5)"))
        {
            Debug.Log("执行初始化");
            ResetCanvas(obj);
        }

        if (GUILayout.Button("调整(集体增减)"))
        {
            Debug.Log("执行调整增减");
            AddCanvas(obj, CanvasNum);
        }

        if (GUILayout.Button("调整间隔(放大层级之间的间隔 )"))
        {
            Debug.Log("执行调整间隔");
            EnlargeCanvas(obj, CanvasNum);
        }
    }

    void SelectGameobject()
    {
        obj = Selection.gameObjects[0];
        try
        {
            Debug.Log(obj.name);
        }
        catch (System.Exception)
        {
            Debug.LogError("Dont Find This GameObject !!!");
            throw;
        }
    }

    void AddCanvas(GameObject obj, int num)
    {
        Canvas[] canvasComps = obj.transform.GetComponentsInChildren<Canvas>(true);
        Renderer[] particleComps = obj.transform.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i <= canvasComps.Length - 1; i++)
        {
            Debug.Log(canvasComps[i].transform.name + ":" + canvasComps[i].sortingOrder + "→" + (canvasComps[i].sortingOrder + num));
            canvasComps[i].sortingOrder += num;
        }
        for (int i = 0; i <= particleComps.Length - 1; i++)
        {
            Debug.Log(particleComps[i].transform.name + ":" + particleComps[i].sortingOrder + "→" + (particleComps[i].sortingOrder + num));
            particleComps[i].sortingOrder += num;
        }
    }

    /// <summary>
    /// 层级重置  归于1,2,3,4,5,6 最低排序设置
    /// </summary>
    /// <param name="obj"></param>
    void ResetCanvas(GameObject obj)
    {
        Canvas[] canvasComps = obj.transform.GetComponentsInChildren<Canvas>(true);
        Renderer[] particleComps = obj.transform.GetComponentsInChildren<Renderer>(true);
        List<int> canvasList = new List<int>();
        for (int i = 0; i <= canvasComps.Length - 1; i++)
        {
            if (canvasList.Contains(canvasComps[i].sortingOrder) == false)
            {
                canvasList.Add(canvasComps[i].sortingOrder);
            }
        }
        for (int i = 0; i <= particleComps.Length - 1; i++)
        {
            if (canvasList.Contains(particleComps[i].sortingOrder) == false)
            {
                canvasList.Add(particleComps[i].sortingOrder);
            }
        }

        canvasList = Sort(canvasList);
        for (int i = 0; i <= canvasComps.Length - 1; i++)
        {
            for (int j = 0; j < canvasList.Count; j++)
            {
                if (canvasComps[i].sortingOrder == canvasList[j])
                {
                    int newSortingOrder = j + 1;
                    Debug.Log(canvasComps[i].transform.name + ":" + canvasComps[i].sortingOrder + "→" + newSortingOrder);
                    canvasComps[i].sortingOrder = newSortingOrder;
                    break;
                }
            }
        }
        for (int i = 0; i <= particleComps.Length - 1; i++)
        {
            for (int j = 0; j < canvasList.Count; j++)
            {
                if (particleComps[i].sortingOrder == canvasList[j])
                {
                    int newSortingOrder = j + 1;
                    Debug.Log(particleComps[i].transform.name + ":" + particleComps[i].sortingOrder + "→" + j);
                    particleComps[i].sortingOrder = j;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 放大层级之间的间隔   eg:1,2 →→→ 1,5
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="num"></param>
    void EnlargeCanvas(GameObject obj, int num)
    {
        Canvas[] canvasComps = obj.transform.GetComponentsInChildren<Canvas>(true);
        Renderer[] particleComps = obj.transform.GetComponentsInChildren<Renderer>(true);
        List<int> canvasList = new List<int>();
        List<int> canvasList_New = new List<int>();
        for (int i = 0; i <= canvasComps.Length - 1; i++)
        {
            if (canvasList.Contains(canvasComps[i].sortingOrder) == false)
            {
                canvasList.Add(canvasComps[i].sortingOrder);
            }
        }
        for (int i = 0; i <= particleComps.Length - 1; i++)
        {
            if (canvasList.Contains(particleComps[i].sortingOrder) == false)
            {
                canvasList.Add(particleComps[i].sortingOrder);
            }
        }

        canvasList = Sort(canvasList);

        canvasList_New.Add(canvasList[0]);
        for (int i = 1; i <= canvasList.Count - 1; i++)
        {
            canvasList_New.Add(canvasList[i] + (num * i));
        }

        for (int i = 0; i < canvasList.Count; i++)
        {
            for (int j = 0; j < canvasComps.Length; j++)
            {
                if (canvasList[i] == canvasComps[j].sortingOrder)
                {
                    Debug.Log(canvasComps[j].transform.name + ":" + canvasComps[j].sortingOrder + "→" + canvasList_New[i]);
                    canvasComps[j].sortingOrder = canvasList_New[i];
                }
            }
        }

        for (int i = 0; i < canvasList.Count; i++)
        {
            for (int j = 0; j < particleComps.Length; j++)
            {
                if (canvasList[i] == particleComps[j].sortingOrder)
                {
                    Debug.Log(particleComps[j].transform.name + ":" + particleComps[j].sortingOrder + "→" + canvasList_New[i]);
                    particleComps[j].sortingOrder = canvasList_New[i];
                }
            }
        }

    }

    List<int> Sort(List<int> canvasList)
    {
        for (int i = 0; i <= canvasList.Count - 1; i++)
        {
            for (int j = 0; j <= canvasList.Count - 1; j++)
            {
                if (canvasList[i] <= canvasList[j])
                {
                    int item = canvasList[i];
                    canvasList[i] = canvasList[j];
                    canvasList[j] = item;
                }
            }
        }
        return canvasList;
    }

}
