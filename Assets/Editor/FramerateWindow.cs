using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class FramerateWindow : EditorWindow
    {
        [SerializeField] private int framerate;
        
        [MenuItem("Tools/Framerate")]
        static void ShowWindow()
        {
            GetWindow<FramerateWindow>().Show();
        }

        private void Awake()
        {
            framerate = Application.targetFrameRate;
        }

        private void OnGUI()
        {
            var so = new SerializedObject(this);
            var prop = so.FindProperty(nameof(framerate));
            
            EditorGUILayout.PropertyField(prop);

            so.ApplyModifiedProperties();

            Application.targetFrameRate = framerate;
        }
    }
}
