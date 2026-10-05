// Builds the preview scene from nothing: a camera framed like the web build's
// stage, the CastStage with its shaders assigned (so they ship in a build), and
// the preview UI. Run it from Tools > Magic Circle > Create Preview Scene.

using MagicCircleSim.Preview;
using MagicCircleSim.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MagicCircleSim.EditorTools
{
    public static class PreviewSceneBuilder
    {
        public const string ScenePath = "Assets/Scene/MagicCirclePreview.unity";
        const string ShaderFolder = "Assets/Scripts/MagicCircle/View/Shaders/";
        const float FovDeg = 50;

        [MenuItem("Tools/Magic Circle/Create Preview Scene")]
        public static void CreatePreviewScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = new GameObject("Stage Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = PreviewSkin.Hex("#0b0d14");
            camera.fieldOfView = FovDeg;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200;
            // Behind and above the caster, looking forward (+Z) over the spell's area.
            camera.transform.position = new Vector3(0, 11, -13);
            camera.transform.LookAt(new Vector3(0, 0, 7));

            var stage = new GameObject("Cast Stage").AddComponent<CastStage>();
            stage.Configure(
                AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "MagicCircleUnlit.shader"),
                AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "MagicCircleParticles.shader"),
                camera);

            var preview = new GameObject("Magic Circle Preview").AddComponent<MagicCirclePreview>();
            preview.Configure(stage, camera);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = preview.gameObject;
            Debug.Log($"Magic Circle preview scene saved to {ScenePath}. Press Play to build and cast circles.");
        }
    }
}
