using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(SButton))]
public class SButtonEditor : ButtonEditor
{
    SerializedProperty onButtonDown;
    SerializedProperty onButtonUp;
    SerializedProperty onButtonEnter;
    SerializedProperty onButtonExit;

    protected override void OnEnable()
    {
        base.OnEnable();
        onButtonDown = serializedObject.FindProperty("OnButtonDown");
        onButtonUp = serializedObject.FindProperty("OnButtonUp");
        onButtonEnter = serializedObject.FindProperty("OnButtonEnter");
        onButtonExit = serializedObject.FindProperty("OnButtonExit");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pointer Events", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(onButtonDown);
        EditorGUILayout.PropertyField(onButtonUp);
        EditorGUILayout.PropertyField(onButtonEnter);
        EditorGUILayout.PropertyField(onButtonExit);

        serializedObject.ApplyModifiedProperties();
    }
}