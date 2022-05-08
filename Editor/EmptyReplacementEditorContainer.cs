using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gongo.EmptyReplacement.Editor
{

    public static class Language
    {
        public static readonly GUIContent TARGET_CONTENT = new GUIContent("Target Object");
        public static readonly GUIContent AUTOUPDATE_CONTENT =
            new GUIContent("Auto Update", "Automatically update when reimporting");
        public static readonly GUIContent PREFAB_LIST_CONTENT = new GUIContent("Prefab List");
        
        public static readonly GUIContent STARTS_WITH_NAME_CONTENT =
            new GUIContent("Starts with matcher", "The text the game objects must start with");
        public static readonly GUIContent OUTPUT_NAME_CONTENT =
            new GUIContent("Output Name", "The name the instantiated prefabs will have");

        public static readonly GUIContent FOUND_CONTENT =
            new GUIContent("Found transforms", "This list of Transform's will be replaced");
        public static readonly GUIContent FOUND_TIP_CONTENT =
            new GUIContent("You can remove transforms from this list for them to not be replaced");
        public static readonly GUIContent COPY_NAME_CONTENT =
            new GUIContent("Copy Name", "Copies name from original transform");
        public static readonly GUIContent COPY_SCALE_CONTENT =
            new GUIContent("Copy Scale", "Copies Scale from original transform");
        public static readonly GUIContent COPY_ROTATION_CONTENT =
            new GUIContent("Copy Rotation", "Copies Rotation from original transform");
    }
    
    [CustomEditor(typeof(EmptyReplacementMetaContainer))]
    public class EmptyReplacementMetaContainerEditor : UnityEditor.Editor
    {
        private EmptyReplacementMetaContainer _container;
        

        [MenuItem("GameObject/Blender Empty Replace", false, -100)]
        static void OnSelectObject()
        {
            DoOpen(Selection.activeGameObject);
        }

        private static void DoOpen(GameObject target)
        {
            if (Selection.activeGameObject.transform.parent != null)
            {
                EditorUtility.DisplayDialog("Invalid Target",
                    "Cannot do replacements with an object that isn't at the root of the scene", "Ok");
                return;
            }

            if (target.GetComponent<EmptyReplacementMetaContainer>() == null)
            {
                var container = target.AddComponent<EmptyReplacementMetaContainer>();
                container.meta = EmptyReplacementMeta.Create();
            }

            Selection.activeGameObject = target;
        }
        
        public static void AttemptRedo(EmptyReplacementMetaContainer metaContainer)
        {
            var meta = metaContainer.meta;
            if (meta.ReplacementMode == EmptyReplacementMode.FACE)
            {
                var filter = metaContainer.GetComponent<MeshFilter>();
                if(filter != null && filter.sharedMesh != null) DoFaceInstantiates(metaContainer, filter.sharedMesh);
            }
            if (!meta.HasRedo || meta.LastExportedObject == null) return;
            
            FindToReplace(metaContainer);
            DoReplacements(metaContainer);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Blender Empty Replacer", EditorStyles.boldLabel);
            if (_container == null)
            {
                _container = (EmptyReplacementMetaContainer)target;
            }

            if (_container.meta == null)
            {
                EditorGUILayout.HelpBox(new GUIContent("Meta in container null"));
                return;
            }

            var meta = _container.meta;

            meta.UpdateOnReload = EditorGUILayout.Toggle(Language.AUTOUPDATE_CONTENT, meta.UpdateOnReload);

            if (meta.UpdateOnReload && (meta.LastExportedObject == null || (!meta.HasRedo && meta.ReplacementMode == EmptyReplacementMode.INSTANCE)))
            {
                var unavailableReason = meta.LastExportedObject == null ? "Unknown last exported object" : "Need to do 1 replacement first";
                EditorGUILayout.HelpBox(new GUIContent($"Auto Update Unavailable: {unavailableReason}"));
            }
        
            var metaSo = new SerializedObject(meta);

            using (new EditorGUI.DisabledGroupScope(true))
            {
                var prefabProp = metaSo.FindProperty(nameof(meta.PrefabList));

                EditorGUILayout.PropertyField(prefabProp, Language.PREFAB_LIST_CONTENT, true);
            }

            if (GUILayout.Button("Edit Prefabs"))
            {
                PrefabSelector.OpenPrefabSelector(_container);
                return;
            }

            using (new EditorGUI.DisabledGroupScope(_container.HasFoundReplacables))
            {
                EditorGUI.BeginChangeCheck();
                meta.ReplacementMode = (EmptyReplacementMode) EditorGUILayout.EnumPopup("Replace Mode", meta.ReplacementMode);
                if (meta.ReplacementMode == EmptyReplacementMode.INSTANCE)
                {
                    meta.StartsWithMatch =
                        EditorGUILayout.TextField(Language.STARTS_WITH_NAME_CONTENT, meta.StartsWithMatch);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    meta.HasRedo = false;
                }
            }

            MeshFilter filter = null;

            string error = null;
            if (string.IsNullOrEmpty(meta.StartsWithMatch) && meta.ReplacementMode == EmptyReplacementMode.INSTANCE)
            {
                error = "Missing \"Starts with matcher\"";
            } else if (meta.ReplacementMode == EmptyReplacementMode.FACE &&
                ((filter = _container.GetComponent<MeshFilter>()) == null || filter.sharedMesh == null))
            {
                error = "Missing valid mesh on target";
            } else if (meta.PrefabList.Count(prefab => prefab != null) < 1)
            {
                error = "Need at least 1 prefab";
            } else if (meta.PrefabList.Any(obj => obj != null && PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.NotAPrefab))
            {
                error = "One of the prefabs aren't a prefab";
            } else if (meta.PrefabList.Any(obj => obj != null && PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.MissingAsset))
            {
                error = "One of the prefabs is missing an asset";
            }


            using (new EditorGUI.DisabledGroupScope(error != null))
            {
                if (meta.ReplacementMode == EmptyReplacementMode.INSTANCE)
                {
                    var newError = RenderInstanceReplace();
                    if (newError != null)
                    {
                        error = newError;
                    }
                } else if (meta.ReplacementMode == EmptyReplacementMode.FACE)
                {
                    RenderFaceReplace(filter);
                }
            }

            if (error != null)
            {
                EditorGUILayout.HelpBox(new GUIContent(error));
            }
        }

        private void RenderFaceReplace(MeshFilter filter)
        {
            var meta = _container.meta;
            if (filter == null || filter.sharedMesh == null)
            {
                return;
            }
            var mesh = filter.sharedMesh;
            EditorGUILayout.HelpBox(new GUIContent($"{mesh.triangles.Length / 3} prefabs will be instantiated"));
            meta.OutputName = EditorGUILayout.TextField(Language.OUTPUT_NAME_CONTENT, meta.OutputName);
            if (GUILayout.Button("Instantiate"))
            {
                DoFaceInstantiates(_container, mesh);
            }
        }

        private static void DoFaceInstantiates(EmptyReplacementMetaContainer container, Mesh mesh)
        {
            var baseTransform = CreateBaseObject(container);
            var triangles = mesh.triangles;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var trans = container.transform;
            var originalPos = trans.position;
            var originalRotation = trans.rotation;
            trans.position = Vector3.zero;
            trans.rotation = Quaternion.identity;
            
            var beginTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var displayedProgress = false;
            var matrix = new Matrix4x4();
            var vertPairs = new[] { new[]{ 1, 0, 2 }, new[]{ 2, 0, 1 }, new[]{ 1, 2, 0 } };
           var verts = new Vector3[3];
            
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var vert0 = trans.TransformPoint(vertices[triangles[i + 0]]);
                var vert1 = trans.TransformPoint(vertices[triangles[i + 1]]);
                var vert2 = trans.TransformPoint(vertices[triangles[i + 2]]);

                var norm0 = normals[triangles[i + 0]];
                var norm1 = normals[triangles[i + 1]];
                var norm2 = normals[triangles[i + 2]];

                var guessedNormals = (norm0 + norm1 + norm2) / 3.0f;
                guessedNormals.Normalize();

                var faceNormal = (vert0 + vert1 + vert2) / 3.0f;

                if (i % 300 == 0) // modulus must be multiple of 3
                {
                    var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (currentTime - beginTime >= 100 && !displayedProgress)
                    {
                        displayedProgress = true;
                        Debug.Log($"Taking a long time, showing progress for user");
                        
                    }
                    if (displayedProgress)
                    {
                        EditorUtility.DisplayProgressBar("Instantiating Prefabs", "Placing prefabs in scene, " + (i / 3) + "/" + (triangles.Length / 3), i * 1.0f / triangles.Length);
                    }
                }

                

                var instantiated = PrefabUtility.InstantiatePrefab(container.RandomPrefab, baseTransform);
                if (instantiated is GameObject prefabObj)
                {
                    verts[0] = vert0;
                    verts[1] = vert1;
                    verts[2] = vert2;
                    var largestPairIndex = 0;
                    var largestMagnitude = 0.0f;
                    for (var pairIndex = 0; pairIndex < vertPairs.Length; pairIndex++)
                    {
                        // the vert pairs is [first index to check, second index to check, the odd one out]
                        var pairs = vertPairs[pairIndex];
                        var magnitude = (verts[pairs[0]] - verts[pairs[1]]).magnitude;
                        if (magnitude >= largestMagnitude)
                        {
                            largestPairIndex = pairIndex;
                            largestMagnitude = magnitude;
                        }
                    }

                    var pair = vertPairs[largestPairIndex];
                    var largest0 = verts[pair[0]];
                    var largest1 = verts[pair[1]];
                    var origin = verts[pair[2]];

                    var unitVector0 = (largest0 - origin).normalized;
                    var unitVector1 = (largest1 - origin).normalized;
                    var unitVector2 = guessedNormals;
                    
                    matrix.SetColumn(0, unitVector0);
                    matrix.SetColumn(1, unitVector2);
                    matrix.SetColumn(2, unitVector1);
                    var prefTrans = prefabObj.transform;
                    prefTrans.localPosition = faceNormal;
                    var pos = prefTrans.position;
                    
                    matrix.SetColumn(3, new Vector4(pos.x, pos.y, pos.z, 1));
                    
                    if (matrix.ValidTRS())
                    {
                        prefTrans.rotation = matrix.rotation;
                    }

                    prefTrans.name = container.meta.OutputName;
                }
            }
            trans.position = originalPos;
            trans.rotation = originalRotation;

            if (displayedProgress)
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private string RenderInstanceReplace()
        {
            string error = null;
            var meta = _container.meta;
            var metaSo = new SerializedObject(meta);
            
            if (_container.HasFoundReplacables)
            {
                var so = new SerializedObject(_container);
            
                var foundProp = so.FindProperty(nameof(_container.foundToReplace));
                EditorGUILayout.PropertyField(foundProp, Language.FOUND_CONTENT, true);

                metaSo.ApplyModifiedProperties();

                meta.CopyName = EditorGUILayout.Toggle(Language.COPY_NAME_CONTENT, meta.CopyName);
                meta.CopyAngles = EditorGUILayout.Toggle(Language.COPY_ROTATION_CONTENT, meta.CopyAngles);
                meta.CopyScale = EditorGUILayout.Toggle(Language.COPY_SCALE_CONTENT, meta.CopyScale);

                EditorGUILayout.HelpBox(Language.FOUND_TIP_CONTENT);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Confirm Replacements"))
                    {
                        DoReplacements();
                    }
                    else if (GUILayout.Button("Cancel"))
                    {
                        _container.foundToReplace = null;
                        _container.triedToFind = false;
                    }
                }
            } else  {
                GUILayout.BeginHorizontal();
                if (!_container.triedToFind || _container.HasFoundReplacables)
                {
                    if (GUILayout.Button(meta.LastExportedObject != null && meta.HasRedo ? "Edit Replace Config" : "Find To Replace"))
                    {
                        FindToReplace();
                    }
                }
                else
                {
                    error = "Couldn't find anything matching that name";
                    if (GUILayout.Button("Okay"))
                    {
                        _container.triedToFind = false;
                    }
                }

                if (meta.LastExportedObject != null && meta.HasRedo && GUILayout.Button("Redo Replacements"))
                {
                    FindToReplace();
                    DoReplacements();
                }
                GUILayout.EndHorizontal();
            }

            return error;
        }

        private void FindToReplace()
        {
            FindToReplace(_container);
        }
        
        private static void FindToReplace(EmptyReplacementMetaContainer container)
        {
            //var rootIsPrefab = PrefabUtility.IsPartOfAnyPrefab(_targetObject);
            container.foundToReplace = container.gameObject.GetComponentsInChildren<Transform>(true)
                .Where(t =>
                {
                    return t.name.StartsWith(container.meta.StartsWithMatch) && t != container.transform;
                    // if (!found) return false;
                    //
                    // // only allow root to be a prefab, ignore any sub-prefabs in the object
                    // var outermostPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);
                    // if (outermostPrefab == null)
                    // {
                    //     return true;
                    // }
                    // if (!rootIsPrefab)
                    // {
                    //     return false;
                    // }
                    //
                    // return outermostPrefab == _targetObject;

                })
                .ToArray();
            container.triedToFind = true;
        }

        private static void CopyTransforms(EmptyReplacementMeta meta, Transform targetTransform, Transform copySource)
        {
            targetTransform.localPosition = copySource.localPosition;
                
            if(meta.CopyAngles) targetTransform.localEulerAngles = copySource.localEulerAngles;
            if(meta.CopyScale) targetTransform.localScale = copySource.localScale;
            if (meta.CopyName) targetTransform.name = copySource.name;
        }

        private void DoReplacements()
        {
            DoReplacements(_container);
        }

        private static Transform CreateBaseObject(EmptyReplacementMetaContainer container)
        {
            if (container.meta.LastExportedObject != null)
            {
                DestroyImmediate(container.meta.LastExportedObject);
            }

            var newObj = new GameObject();
                
            container.meta.LastExportedObject = newObj;
            var newObjTransform = newObj.transform;
            CopyTransforms(container.meta, newObjTransform, container.transform);
                
            newObjTransform.name = container.name + "_EmptyReplace";

            return newObjTransform;
        }

        private static void DoReplacements(EmptyReplacementMetaContainer container)
        {
            var prefabMode = PrefabUtility.IsPartOfAnyPrefab(container.gameObject);
            var transformOrigin = container.transform;
            if (prefabMode)
            {
                transformOrigin = CreateBaseObject(container);
            }
            foreach (var transform in container.foundToReplace)
            {
                var transformRawParent = transform.parent;
                var transformParent = prefabMode
                    ? GetOrCreatePath(transformRawParent, transformOrigin)
                    : transformRawParent;

                var instantiated = PrefabUtility.InstantiatePrefab(container.RandomPrefab, transformParent);
                if (instantiated is GameObject prefabObj)
                {
                    var prefTrans = prefabObj.transform;
                    CopyTransforms(container.meta, prefTrans, transform);
                }

                if (!prefabMode)
                {
                    // allow Ctrl+Z to undo the prefab being instantiated
                    Undo.RegisterCreatedObjectUndo(instantiated, "Created prefab");
                    // allow Ctrl+Z to undo the object being destroyed
                    Undo.DestroyObjectImmediate(transform.gameObject);
                }
            }
            // allow Ctrl+Z to undo _foundToReplace being set to null
            Undo.RegisterCompleteObjectUndo(container, "Update found to replace");
            container.foundToReplace = null;
            container.meta.HasRedo = true;
            container.triedToFind = false;
        }

        private static Transform GetOrCreatePath(Transform copyTransform, Transform newOriginTransform)
        {
            var requiredPaths = new List<string>();
            var currentTransform = copyTransform;

            while (currentTransform != null)
            {
                requiredPaths.Add(currentTransform.name);
                currentTransform = currentTransform.parent;
            }

            requiredPaths.Reverse();
            requiredPaths.RemoveAt(0);

            if (requiredPaths.Count > 0)
            {
                var currentParent = newOriginTransform;
                foreach (var requiredPath in requiredPaths)
                {
                    var subTransform = currentParent.Find(requiredPath);
                    if (subTransform == null)
                    {
                        var newObject = new GameObject(requiredPath);
                        newObject.transform.parent = currentParent;
                        subTransform = newObject.transform;
                    }

                    currentParent = subTransform;
                }

                return currentParent;
            }

            return newOriginTransform;
        }
        // public override void OnInspectorGUI()
        // {
        //     var metaContainer = ((EmptyReplacementMetaContainer)target);
        //     var so = new SerializedObject(metaContainer);
        //     EditorGUILayout.PropertyField(so.FindProperty(nameof(metaContainer.meta)), true);
        //
        //     so.ApplyModifiedProperties();
        //     // give the option to create the asset if the user deleted it
        //     if (metaContainer.meta == null)
        //     {
        //         if (GUILayout.Button("Create Asset"))
        //         {
        //             Undo.RegisterCompleteObjectUndo(metaContainer, "Updating meta");
        //             metaContainer.meta = EmptyReplacementMeta.Create();
        //         }
        //         return;
        //     }
        //
        //     var assetFileExists = !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(metaContainer.meta));
        //     
        //     GUILayout.BeginHorizontal();
        //     
        //     // show an "open" button that'll be a shortcut to open the Editor Window
        //     if (GUILayout.Button("Open Empty Replacer")) EmptyReplacementEditor.DoOpen(metaContainer.gameObject);
        //     if (metaContainer.meta.HasRedo && GUILayout.Button("Redo Replacements"))
        //     {
        //         
        //     }
        //
        //     // only show export button if the asset doesn't exist as a file
        //     if (!assetFileExists && GUILayout.Button("Export Asset"))
        //     {
        //         var fileName = $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}_Meta.asset";
        //         var path = EditorUtility.SaveFilePanel("Save meta for EmptyReplacement",
        //             "Assets/",
        //             fileName,
        //             "asset"
        //         );
        //         if(path.Length == 0) return;
        //         
        //         // can't undo CreateAsset
        //         AssetDatabase.CreateAsset(metaContainer.meta, path);
        //     }
        //     GUILayout.EndHorizontal();
        // }
    }

    public class PrefabSelector : EditorWindow
    {
        private EmptyReplacementMetaContainer _metaContainer;

        public static void OpenPrefabSelector(EmptyReplacementMetaContainer metaContainer)
        {
            var window = (PrefabSelector) GetWindow(typeof (PrefabSelector));
            window.autoRepaintOnSceneChange = true;
            window.Show();

            window.titleContent = new GUIContent("Prefab Selection");
            window._metaContainer = metaContainer;
        }

        private void OnGUI()
        {
            if (_metaContainer == null)
            {
                Close();
                return;
            }
            var metaSo = new SerializedObject(_metaContainer.meta);

            var prefabProp = metaSo.FindProperty(nameof(_metaContainer.meta.PrefabList));

            EditorGUILayout.PropertyField(prefabProp, Language.PREFAB_LIST_CONTENT, true);
            metaSo.ApplyModifiedProperties();
            if (GUILayout.Button("Done"))
            {
                Selection.activeGameObject = _metaContainer.gameObject;
                Close();
            }
        }
    }
}