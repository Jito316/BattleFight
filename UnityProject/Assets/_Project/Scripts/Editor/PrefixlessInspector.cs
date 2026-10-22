using UnityEditor;
using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// Inspector の表示名から、コーディングルールの接頭辞 m を外す(mMoveSpeed → 「Move Speed」)。
    /// Unity は m_ は外すが m は外さないため。専用のエディタがあるコンポーネントには使われない(isFallback)。
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
    [CanEditMultipleObjects]
    public class PrefixlessBehaviourInspector : Editor
    {
        public override void OnInspectorGUI() => PrefixlessInspector.Draw(serializedObject);
    }

    [CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
    [CanEditMultipleObjects]
    public class PrefixlessAssetInspector : Editor
    {
        public override void OnInspectorGUI() => PrefixlessInspector.Draw(serializedObject);
    }

    public static class PrefixlessInspector
    {
        public static void Draw(SerializedObject iSerialized)
        {
            iSerialized.Update();
            var aProperty = iSerialized.GetIterator();
            bool aEnterChildren = true;
            while (aProperty.NextVisible(aEnterChildren))
            {
                aEnterChildren = false;
                using (new EditorGUI.DisabledScope(aProperty.propertyPath == "m_Script"))
                {
                    EditorGUILayout.PropertyField(aProperty, new GUIContent(Label(aProperty.name), aProperty.tooltip), true);
                }
            }
            iSerialized.ApplyModifiedProperties();
        }

        /// <summary>表示名。m + 大文字で始まる名前は m を外す</summary>
        public static string Label(string iName)
        {
            if (iName.Length > 1 && iName[0] == 'm' && char.IsUpper(iName[1])) iName = iName.Substring(1);
            return ObjectNames.NicifyVariableName(iName);
        }
    }
}
