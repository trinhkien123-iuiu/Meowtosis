#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Meowtosis.Presentation.Editor
{
    // Apply this skin migration to an already-open scene without reloading or saving it.
    [InitializeOnLoad]
    internal static class HomeStartSkinSync
    {
        private const string Folder = "Assets/_Project/Art/Catdom/HomeNavigation/";
        private const string AppliedKey = "Meowtosis.HomeStartSkin.20261112";

        static HomeStartSkinSync()
        {
            EditorApplication.delayCall += ApplyPending;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += ApplyPending;
            };
        }

        private static void ApplyPending()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                SessionState.GetBool(AppliedKey, false)) return;
            if (Apply()) SessionState.SetBool(AppliedKey, true);
        }

        [MenuItem("Tools/Meowtosis/Update Start Button In Open Home")]
        private static void ApplyFromMenu() => Apply();

        private static bool Apply()
        {
            var frame = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "StartWoodFrame.png");
            var face = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "StartGreenFace.png");
            if (frame == null || face == null) return false;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || scene.path != "Assets/_Project/Scenes/Home.unity") continue;
                foreach (var root in scene.GetRootGameObjects())
                foreach (var view in root.GetComponentsInChildren<HomeView>(true))
                {
                    var button = new SerializedObject(view).FindProperty("playButton").objectReferenceValue as Button;
                    if (button == null) continue;
                    var background = button.GetComponent<Image>();
                    if (background == null) continue;
                    var existing = button.transform.Find("CatdomStartGreenFace");
                    if (background.sprite == frame && existing != null &&
                        existing.GetComponent<Image>()?.sprite == face) return true;

                    Undo.IncrementCurrentGroup();
                    var undoGroup = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("Update Catdom Start button");
                    var rect = (RectTransform)button.transform;
                    Undo.RecordObjects(new Object[] { rect, background, button }, "Update Start skin");
                    rect.sizeDelta = new Vector2(520f, 264.08f);
                    background.sprite = frame;
                    background.type = Image.Type.Simple;
                    background.preserveAspect = true;
                    background.color = Color.white;
                    Image graphic;
                    if (existing == null)
                    {
                        var child = new GameObject("CatdomStartGreenFace", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        Undo.RegisterCreatedObjectUndo(child, "Create Start face");
                        child.layer = button.gameObject.layer;
                        child.transform.SetParent(rect, false);
                        graphic = child.GetComponent<Image>();
                    }
                    else
                    {
                        graphic = existing.GetComponent<Image>();
                        if (graphic == null) graphic = Undo.AddComponent<Image>(existing.gameObject);
                        Undo.RecordObjects(new Object[] { graphic, graphic.rectTransform }, "Update Start face");
                    }
                    graphic.transform.SetAsFirstSibling();
                    graphic.rectTransform.anchorMin = graphic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    graphic.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    graphic.rectTransform.anchoredPosition = new Vector2(0f, 7.71f);
                    graphic.rectTransform.sizeDelta = new Vector2(490.96f, 220.52f);
                    graphic.sprite = face;
                    graphic.type = Image.Type.Simple;
                    graphic.preserveAspect = true;
                    graphic.color = Color.white;
                    graphic.raycastTarget = false;
                    button.targetGraphic = graphic;
                    Undo.CollapseUndoOperations(undoGroup);
                    if (!EditorApplication.isPlaying) EditorSceneManager.MarkSceneDirty(scene);
                    SceneView.RepaintAll();
                    Debug.Log("Updated the open Home scene: Catdom green Start button with wood frame.", button);
                    return true;
                }
            }
            return false;
        }
    }
}
#endif
