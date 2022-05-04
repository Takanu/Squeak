#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Gongo.Squeak
{
    [Serializable]
    public class SqueakMeta : ScriptableObject
    {
        public SqueakMode ReplacementMode;

        public string OutputName;
        public string StartsWithMatch;
        [SerializeReference]
        public GameObject[] PrefabList;
        [SerializeReference]
        public GameObject LastExportedObject; // used only when trying to modify a prefab

        public bool UpdateOnReload;
        
        public bool CopyName, CopyAngles, CopyScale, HasRedo;

        public SqueakMeta Clone()
        {
            return Instantiate(this);
        }

        public static SqueakMeta Create()
        {
            var meta = CreateInstance<SqueakMeta>();
            meta.CopyName = true;
            meta.CopyAngles = true;
            meta.CopyScale = true;
            meta.PrefabList = Array.Empty<GameObject>();
            meta.ReplacementMode = SqueakMode.INSTANCE;
            meta.OutputName = "Prefab";

            return meta;
        }
    }

    public enum SqueakMode
    {
        // instance = replace all GameObject instances matching a certain name with prefabs
        // vertex = replace all mesh faces with prefabs 
        INSTANCE, FACE
    }
}
#endif