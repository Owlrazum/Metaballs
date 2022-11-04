using UnityEditor;

[CustomEditor(typeof(MetaballMeshes))]
public class MetaballMeshesInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("The order of materials determines mapping to \nmetaballColors Enum inside code", MessageType.Info);
        base.OnInspectorGUI();
    }
}