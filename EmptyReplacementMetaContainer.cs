#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace Gongo.Squeak
{
    public class SqueakMetaContainer : MonoBehaviour
    {
        private static readonly Random random = new Random();
        
        public SqueakMeta meta;
        
        public Transform[] foundToReplace;
        public bool triedToFind;

        public bool HasFoundReplacables => foundToReplace != null && foundToReplace.Length > 0;

        public GameObject RandomPrefab
        {
            get
            {
                GameObject foundPrefab = null;
                var iteration = 0;
                while (foundPrefab == null && iteration < 100)
                {
                    iteration++;
                    foundPrefab = meta.PrefabList[random.Next(0, meta.PrefabList.Length)];
                }

                if (foundPrefab == null) throw new Exception("Couldn't find random prefab - are all of them null?");
                return foundPrefab;
            }
        }
    }
}
#endif