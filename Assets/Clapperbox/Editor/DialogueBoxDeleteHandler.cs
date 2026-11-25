#if UNITY_EDITOR
using System.Linq;
using DefaultNamespace;
using UnityEditor;
using UnityEngine;

namespace Clapperbox.Editor
{
    /*
     * Deleting a dialogue box prefab in DialogueBoxes/ also deletes the screenplay it uses (ScreenPlays/<Name>_Screenplay.asset),
     * so a box you no longer want doesn't leave its dialogue data behind.
     *  - The screenplay is kept when another prefab still uses it.
     *  - Character data is never deleted: characters are shared by many screenplays.
     *  - The csv is never deleted: it is the user's source file.
     * Only deletes made inside Unity are seen; deleting the prefab in Finder skips this.
     */
    public class DialogueBoxDeleteHandler : AssetModificationProcessor
    {
        private const string DialogueBoxesFolder = "Assets/Clapperbox/DialogueBoxes";

        private static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
        {
            // Never blocks the delete; it only looks up the screenplay before the prefab is gone
            if (!path.StartsWith(DialogueBoxesFolder + "/") || !path.EndsWith(".prefab")) return AssetDeleteResult.DidNotDelete;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var manager = prefab != null ? prefab.GetComponentInChildren<DialogueManager>(true) : null;
            if (manager == null) return AssetDeleteResult.DidNotDelete;

            // screenPlay is a private [SerializeField], so it is read through SerializedObject
            var screenPlay = new SerializedObject(manager).FindProperty("screenPlay")?.objectReferenceValue as ScreenPlayData;
            if (screenPlay == null) return AssetDeleteResult.DidNotDelete;
            string screenPlayPath = AssetDatabase.GetAssetPath(screenPlay);

            string otherUser = AssetDatabase.FindAssets("t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p != path)
                .FirstOrDefault(p => AssetDatabase.GetDependencies(p, false).Contains(screenPlayPath));
            if (otherUser != null)
            {
                Debug.Log($"[Clapperbox] Kept {screenPlayPath}: it is still used by {otherUser}.");
                return AssetDeleteResult.DidNotDelete;
            }

            // Deleting another asset while Unity is mid-delete isn't safe, so it waits until this delete is done
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<ScreenPlayData>(screenPlayPath) == null) return;
                if (AssetDatabase.DeleteAsset(screenPlayPath))
                    Debug.Log($"[Clapperbox] Deleted {screenPlayPath} with its dialogue box {path}.");
            };
            return AssetDeleteResult.DidNotDelete;
        }
    }
}
#endif
