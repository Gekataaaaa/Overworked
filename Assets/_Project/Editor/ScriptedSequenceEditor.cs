using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Инспектор ScriptedSequence: список шагов с подписями («3. Message: F3_Max_01»),
    /// кнопки ↑ ↓ ✕ у каждого шага и «Add step» с меню всех типов шагов (наследники SequenceStep).
    /// В Play Mode подсвечивает, что сценарий идёт.
    /// </summary>
    [CustomEditor(typeof(ScriptedSequence))]
    public class ScriptedSequenceEditor : Editor
    {
        static Type[] stepTypes;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var seq = (ScriptedSequence)target;

            if (Application.isPlaying)
                EditorGUILayout.HelpBox(seq.IsPlaying ? "Идёт" : seq.HasPlayed ? "Проигран" : "Не запускался", MessageType.None);

            DrawPropertiesExcluding(serializedObject, "m_Script", "steps", "onFinished");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Шаги", EditorStyles.boldLabel);
            var steps = serializedObject.FindProperty("steps");
            for (int i = 0; i < steps.arraySize; i++)
            {
                if (DrawStep(steps, i)) break; // список изменился — дорисуем в следующем кадре
            }

            if (GUILayout.Button("Add step")) ShowAddMenu(steps);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onFinished"));

            if (Application.isPlaying && GUILayout.Button("Play (отладка)")) seq.Play();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>Рисует шаг; true — если шаг удалён или перемещён.</summary>
        static bool DrawStep(SerializedProperty steps, int i)
        {
            var element = steps.GetArrayElementAtIndex(i);
            var step = element.managedReferenceValue as SequenceStep;
            string title = $"{i + 1}. {(step != null ? step.Describe() : "(пустой шаг)")}";

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, title, true);
            GUI.enabled = i > 0;
            if (GUILayout.Button("↑", GUILayout.Width(24))) { steps.MoveArrayElement(i, i - 1); return End(); }
            GUI.enabled = i < steps.arraySize - 1;
            if (GUILayout.Button("↓", GUILayout.Width(24))) { steps.MoveArrayElement(i, i + 1); return End(); }
            GUI.enabled = true;
            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                // Для managed reference DeleteArrayElementAtIndex сразу удаляет элемент (не обнуляет, как у объектов).
                steps.DeleteArrayElementAtIndex(i);
                return End();
            }
            EditorGUILayout.EndHorizontal();

            if (element.isExpanded && step != null)
            {
                EditorGUI.indentLevel++;
                var it = element.Copy();
                var end = element.GetEndProperty();
                bool enter = true;
                while (it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end))
                {
                    EditorGUILayout.PropertyField(it, true);
                    enter = false;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            return false;
        }

        static bool End()
        {
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return true;
        }

        void ShowAddMenu(SerializedProperty steps)
        {
            stepTypes ??= TypeCache.GetTypesDerivedFrom<SequenceStep>()
                .Where(t => !t.IsAbstract && !t.IsGenericType)
                .OrderBy(t => t.Name)
                .ToArray();

            var menu = new GenericMenu();
            foreach (var type in stepTypes)
            {
                var t = type;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(t.Name.Replace("Step", ""))), false, () =>
                {
                    serializedObject.Update();
                    int i = steps.arraySize;
                    steps.InsertArrayElementAtIndex(i);
                    var element = steps.GetArrayElementAtIndex(i);
                    element.managedReferenceValue = Activator.CreateInstance(t);
                    element.isExpanded = true;
                    serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }
    }
}
