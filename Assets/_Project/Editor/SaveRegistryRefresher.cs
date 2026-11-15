using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Держит Resources/SaveRegistry в актуальном виде: после каждой перекомпиляции собирает все ObjectiveData
    /// и MessageData проекта. Пересохраняет ассет, только если список изменился. Вручную — Overworked → P1 → Refresh Save Registry.
    /// </summary>
    [InitializeOnLoad]
    public static class SaveRegistryRefresher
    {
        const string AssetPath = "Assets/_Project/Data/Resources/SaveRegistry.asset";

        static SaveRegistryRefresher() => EditorApplication.delayCall += () => Refresh(false);

        [MenuItem("Overworked/P1/Refresh Save Registry")]
        public static void RefreshMenu() => Refresh(true);

        static void Refresh(bool log)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var objectives = Find<ObjectiveData>();
            var messages = Find<MessageData>();

            var reg = AssetDatabase.LoadAssetAtPath<SaveRegistry>(AssetPath);
            if (reg == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Data/Resources"))
                    AssetDatabase.CreateFolder("Assets/_Project/Data", "Resources");
                reg = ScriptableObject.CreateInstance<SaveRegistry>();
                AssetDatabase.CreateAsset(reg, AssetPath);
            }
            else if (reg.Objectives.SequenceEqual(objectives) && reg.Messages.SequenceEqual(messages))
            {
                if (log) Debug.Log("[Setup] SaveRegistry уже актуален");
                return;
            }
            reg.Objectives = objectives;
            reg.Messages = messages;
            EditorUtility.SetDirty(reg);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] SaveRegistry: заданий {objectives.Length}, сообщений {messages.Length}");
        }

        static T[] Find<T>() where T : Object =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/_Project/Data" })
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .OrderBy(a => a.name)
                .ToArray();
    }
}
