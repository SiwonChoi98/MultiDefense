using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditorInternal;

/// <summary>
/// Hero_Scriptable 대한 내용을 edit 할 수 있게 만듬
/// </summary>
[CustomEditor(typeof(Hero_Scriptable))]
public class HeroEditor : Editor
{
    /// <summary>
    /// unity editor는 빌드 전 editor script 폴더에 넣어주면 되고, editor상에서는 폴더 안에 없어도 됨
    /// </summary>
    ///

    private ReorderableList effectTypeList;
    private void OnEnable()
    {
        SerializedProperty debuffTypeProperty = serializedObject.FindProperty("effectType");
        effectTypeList = new ReorderableList(serializedObject, debuffTypeProperty, true, true, true, true);

        effectTypeList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Hero Debuff Types");
        };

        //리스트 요소의 높이를 설정하는 함수
        effectTypeList.elementHeightCallback = (index) =>
        {
            SerializedProperty element = debuffTypeProperty.GetArrayElementAtIndex(index);
            SerializedProperty parametersProp = element.FindPropertyRelative("parameters");

            float baseHeight = EditorGUIUtility.singleLineHeight + 6.0f;
            float paramHeight = parametersProp.arraySize * (EditorGUIUtility.singleLineHeight + 4.0f);
            return baseHeight + paramHeight + 10.0f;
        };
        
        //리스트의 각 요소를 그리는 콜백 함수
        effectTypeList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = debuffTypeProperty.GetArrayElementAtIndex(index);
            rect.y += 2;

            SerializedProperty effectTypeProp = element.FindPropertyRelative("DebuffType");
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                effectTypeProp, new GUIContent("Debuff Type"));

            DebuffType debuffType = (DebuffType)effectTypeProp.enumValueIndex;
            SerializedProperty parametersProp = element.FindPropertyRelative("parameters");

            rect.y += EditorGUIUtility.singleLineHeight + 4.0f;

            switch (debuffType)
            {
                case DebuffType.Slow:
                    parametersProp.arraySize = 3;
                    
                    //첫번째 필드
                    DrawParameterField(rect, "Chance:", parametersProp,0);
                    rect.y += EditorGUIUtility.singleLineHeight + 4;
                    //두번째 필드
                    DrawParameterField(rect, "Slow Amount:", parametersProp,1);
                    rect.y += EditorGUIUtility.singleLineHeight + 4;
                    //세번째 필드
                    DrawParameterField(rect, "Duration (seconds):", parametersProp,2);
                    break;
                case DebuffType.Stun:
                    parametersProp.arraySize = 2;
                    
                    DrawParameterField(rect, "Chance:", parametersProp,0);
                    rect.y += EditorGUIUtility.singleLineHeight + 4;
                    DrawParameterField(rect, "Duration (seconds):", parametersProp,1);
                    break;

            }
        };
        
        //리스트에 추가버튼을 눌렀을 때 발생할 거
        effectTypeList.onAddCallback = (ReorderableList list) =>
        {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.InsertArrayElementAtIndex(index); //새로운 요소 추가

            SerializedProperty newElement = list.serializedProperty.GetArrayElementAtIndex(index);
            newElement.FindPropertyRelative("DebuffType").enumValueIndex = 0;
            newElement.FindPropertyRelative("parameters").arraySize = 0;
        };

        //리스트에 빼기버튼을 눌렀을 때 발생할 거
        effectTypeList.onRemoveCallback = (ReorderableList list) =>
        {
            if (EditorUtility.DisplayDialog("Remove Effect", "Are you sure you want to remove this effect?", "Yes",
                    "No"))
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
            }
        };
    }

    private void DrawParameterField(Rect rect, string label, SerializedProperty parametersProp, int index)
    {
        EditorGUI.LabelField(new Rect(rect.x, rect.y, 120, EditorGUIUtility.singleLineHeight),
            label);

        //첫번째 필드
        parametersProp.GetArrayElementAtIndex(index).floatValue =
            EditorGUI.FloatField(new Rect(rect.x + 130.0f, rect.y, 100, EditorGUIUtility.singleLineHeight),
                parametersProp.GetArrayElementAtIndex(index).floatValue);
    }
    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("Name"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ATK"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ATK_Speed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Range"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Animator"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rare"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bullet"));
        
        EditorGUILayout.Space(20f);
        
        effectTypeList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}
