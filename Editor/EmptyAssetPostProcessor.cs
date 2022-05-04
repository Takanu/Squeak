using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gongo.Squeak.Editor
{
    public class EmptyAssetPostProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var metaContainers =
                Resources.FindObjectsOfTypeAll(typeof(SqueakMetaContainer)) as SqueakMetaContainer[];
            if(metaContainers == null) return;
            
            // metaContainers contain ALL metaContainers, including ones that are in the Project and not the Scne
            foreach (var metaContainer in metaContainers)
            {
                // filter out only metaContainers in the Scene
                if (!EditorUtility.IsPersistent(metaContainer.transform.root.gameObject) &&
                    !(metaContainer.hideFlags == HideFlags.NotEditable ||
                      metaContainer.hideFlags == HideFlags.HideAndDontSave))
                {
                    var obj = metaContainer.gameObject;
                    
                    // only do auto reload if it's a prefab
                    if (PrefabUtility.IsPartOfAnyPrefab(obj))
                    {
                        var parentObject = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                        var path = AssetDatabase.GetAssetPath(parentObject);
                        
                        // check if this prefab was reloaded
                        if (importedAssets.Contains(path))
                        {
                            var updateWaiter = new OneUpdateWaiter();
                            updateWaiter.MetaContainer = metaContainer;
                            EditorApplication.update += updateWaiter.Update;
                        }

                    }
                }
            }
        }
    }

    public class OneUpdateWaiter
    {
        public SqueakMetaContainer MetaContainer;
        public void Update()
        {
            var meta = MetaContainer.meta;
            if (meta != null && meta.UpdateOnReload && meta.LastExportedObject != null && (meta.HasRedo || meta.ReplacementMode == SqueakMode.FACE ))
            {
                                
                SqueakMetaContainerEditor.AttemptRedo(MetaContainer);
                Debug.Log("Auto reloading " + MetaContainer.name);
            }
            EditorApplication.update -= Update;
        }
    }
}