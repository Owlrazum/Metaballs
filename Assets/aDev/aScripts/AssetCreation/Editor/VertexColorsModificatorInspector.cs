using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VertexColorsModificator))]
public class VertexColorsModificatorInspector : Editor
{
    public override void OnInspectorGUI()
    {
        Debug.Log("XXX");
        base.OnInspectorGUI();

        VertexColorsModificator modificator = (VertexColorsModificator)target;
        if (GUILayout.Button("EditVertexColors"))
        {
            Mesh mesh = modificator.EditVertexColors();
            if (!modificator.IsSharedMesh)
            {
                AssetDatabase.CreateAsset(mesh, "Assets/aDev/aMeshes/" + modificator.AssetName + ".mesh");
            }
        }
    }

    
}