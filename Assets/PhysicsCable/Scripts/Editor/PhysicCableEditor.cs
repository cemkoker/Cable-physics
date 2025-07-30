using UnityEngine;
using UnityEditor;

namespace HPhysic
{
    [CustomEditor(typeof(PhysicCable))]
    public class PhysicCableEditor : Editor
    {
        private PhysicCable cable;
        
        private void OnEnable()
        {
            cable = (PhysicCable)target;
        }

        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            
            // Add custom buttons section
            EditorGUILayout.LabelField("Cable Controls", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // Add Point button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Add Point", GUILayout.Height(30)))
            {
                cable.AddPoint();
                EditorUtility.SetDirty(cable);
            }
            
            // Remove Point button
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Remove Point", GUILayout.Height(30)))
            {
                cable.RemovePoint();
                EditorUtility.SetDirty(cable);
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Update Points button
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Update Points", GUILayout.Height(25)))
            {
                cable.UpdatePoints();
                EditorUtility.SetDirty(cable);
            }
            
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(5);
            
            // Display current number of points
            EditorGUILayout.LabelField($"Current Points: {cable.NumberOfPoints}", EditorStyles.helpBox);
            
            // Warning messages
            if (cable.NumberOfPoints < 2)
            {
                EditorGUILayout.HelpBox("Cable should have at least 2 points for proper functionality.", MessageType.Warning);
            }
            
            if (cable.NumberOfPoints > 20)
            {
                EditorGUILayout.HelpBox("Too many points may impact performance.", MessageType.Info);
            }
        }
    }
}