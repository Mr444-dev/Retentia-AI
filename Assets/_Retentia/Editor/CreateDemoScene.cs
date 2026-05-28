using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Retentia.Core;

namespace Retentia.Editor
{
    public class CreateDemoScene
    {
        [MenuItem("Retentia AI/Create Super Demo Scene")]
        public static void CreateScene()
        {
            // Create a new empty scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create Main Camera
            GameObject cameraObj = new GameObject("Main Camera");
            Camera camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.2f, 0.2f, 0.2f); // Dark gray
            cameraObj.tag = "MainCamera";
            cameraObj.transform.position = new Vector3(0, 1, -10);

            // Create Directional Light
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Create Retentia Manager
            GameObject retentiaManagerObj = new GameObject("RetentiaManager");
            retentiaManagerObj.AddComponent<RetentiaBootstrapper>();
            retentiaManagerObj.AddComponent<RetentiaDemoSimulator>();

            // Save the scene
            string scenePath = "Assets/_Retentia/Demos/RetentiaSuperDemo.unity";
            
            // Ensure Demos folder exists
            if (!AssetDatabase.IsValidFolder("Assets/_Retentia/Demos"))
            {
                AssetDatabase.CreateFolder("Assets/_Retentia", "Demos");
            }

            bool saved = EditorSceneManager.SaveScene(newScene, scenePath);
            
            if (saved)
            {
                Debug.Log("Successfully created Retentia Super Demo scene at: " + scenePath);
                EditorUtility.DisplayDialog("Retentia AI", "Gotowe! Zbudowałem scenę testową.\n\nZnajdziesz ją w: Assets/_Retentia/Demos/RetentiaSuperDemo.unity", "OK");
            }
            else
            {
                Debug.LogError("Failed to save the scene.");
            }
        }
    }
}
