#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Gongo.EmptyReplacement
{
    [Serializable]
    public class EmptyReplacementMeta : ScriptableObject
    {
        public EmptyReplacementMode ReplacementMode;

        public string OutputName;
        public string StartsWithMatch;
        [SerializeReference]
        public GameObject[] PrefabList;
        [SerializeReference]
        public GameObject LastExportedObject; // used only when trying to modify a prefab

        public bool UpdateOnReload;
        
        public bool CopyName, CopyAngles, CopyScale, HasRedo;

        public EmptyReplacementMeta Clone()
        {
            return Instantiate(this);
        }

        public static EmptyReplacementMeta Create()
        {
            var meta = CreateInstance<EmptyReplacementMeta>();
            meta.CopyName = true;
            meta.CopyAngles = true;
            meta.CopyScale = true;
            meta.PrefabList = Array.Empty<GameObject>();
            meta.ReplacementMode = EmptyReplacementMode.INSTANCE;
            meta.OutputName = "Prefab";

            return meta;
        }
    }

    public enum EmptyReplacementMode
    {
        // instance = replace all GameObject instances matching a certain name with prefabs
        // vertex = replace all mesh faces with prefabs 
        INSTANCE, FACE
    }
}
#endif