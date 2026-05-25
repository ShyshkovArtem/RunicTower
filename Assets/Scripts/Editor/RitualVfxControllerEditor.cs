using UnityEditor;
using UnityEngine;
using RunicTower.Presentation3D;

namespace RunicTower.Editor
{
    [CustomEditor(typeof(RitualVfxController))]
    public sealed class RitualVfxControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Play Debug Table VFX"))
                {
                    RitualVfxController controller = (RitualVfxController)target;
                    controller.PlayDebugTableVfxFromInspector();
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to use the debug VFX test button.",
                    MessageType.Info);
            }
        }
    }
}
