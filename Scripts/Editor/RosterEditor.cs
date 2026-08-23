using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WeaponMaster.EditorTools
{
    /// <summary>
    /// 로스터 인스펙터에 "폴더 다시 스캔" 버튼을 추가하는 공통 로직.
    /// </summary>
    // WeaponRosterEditor/AchievementRosterEditor/AugmentRosterEditor가 완전히 동일한 스캔 로직을 갖고 있어 통합했다.
    // 스캔은 버튼을 누른 순간에만 일어나고(런타임 스캔 없음), 결과로 리스트를 통째로 교체한다 - 수동으로 항목을 뺀 적이 없다는 전제라 병합 로직 없이 단순하게 둔다.
    // [CustomEditor]는 제네릭 클래스에 달면 유니티가 인식하지 못하므로, 이 베이스가 아니라 각 구체 서브클래스에 달아야 한다.
    public abstract class RosterEditor<TRoster, TDefinition> : Editor
        where TRoster : UnityEngine.Object
        where TDefinition : UnityEngine.Object
    {
        protected abstract string FolderPath { get; }
        protected abstract string FieldName { get; }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button($"{FolderPath} 폴더 다시 스캔"))
            {
                Rescan((TRoster)target);
            }
        }

        private void Rescan(TRoster roster)
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(TDefinition).Name}", new[] { FolderPath });
            var found = new List<TDefinition>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TDefinition definition = AssetDatabase.LoadAssetAtPath<TDefinition>(path);
                if (definition != null) found.Add(definition);
            }

            found.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            var serializedRoster = new SerializedObject(roster);
            SerializedProperty prop = serializedRoster.FindProperty(FieldName);
            prop.arraySize = found.Count;
            for (int i = 0; i < found.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
            }
            serializedRoster.ApplyModifiedProperties();
        }
    }
}
