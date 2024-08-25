using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;

public class ScriptableObjectTest
{
    //Asset猟周隠贋揃抄
    private const string assetPath = "Assets/Resources/Asset/";

    [MenuItem("MyTools/ScriptableObjectTest")]
    public static void CreateTestAsset()
    {
        //幹秀方象
        TestData testData = ScriptableObject.CreateInstance<TestData>();
        //験峙
        testData.testName = "name";
        testData.level = 1;

        //殊臥隠贋揃抄
        if (!Directory.Exists(assetPath))
            Directory.CreateDirectory(assetPath);

        //評茅圻嗤猟周・伏撹仟猟周
        string fullPath = assetPath + "/" + "TestData.asset";
        UnityEditor.AssetDatabase.DeleteAsset(fullPath);
        UnityEditor.AssetDatabase.CreateAsset(testData, fullPath);
        UnityEditor.AssetDatabase.Refresh();
    }
}

//霞編方象窃
[CreateAssetMenu(fileName = "TestData", menuName = "Create ScriptableObject : TestData", order = 1)]
//窃兆嚥C#猟周兆匯岷
public class TestData : ScriptableObject
{
    public string testName;
    public int level;
}

#endif