using BepInEx;
using MoreSuits;
#if DEBUG
using System;
using System.Collections;
using UnityEngine;
#endif

namespace MoreSuitsPages
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    [BepInDependency("x753.More_Suits")]
    class MoreSuitsPagesPlugin : BaseUnityPlugin
    {
        internal const string MOD_GUID = "gay.tigers.moresuitspages";
        internal const string MOD_NAME = "MoreSuitsPages";
        internal const string MOD_VERSION = "1.0.0";

        private static Pages _pages = new Pages();

        private void Awake()
        {
            if (!_pages.Loaded) return;
            SuitSorter.RegisterSorter("pages", _pages);
#if DEBUG
            StartCoroutine(c());
#endif
        }

        private void Update()
        {
            if(!_pages.Loaded) return;
            _pages.Update();
        }
        
#if DEBUG
        private IEnumerator c()
        {
            SuitSorter none;
            bool s = SuitSorter.TryGetSorterByIdentifier("none", out none);
            if (!s) yield break;
            while (true)
            {
                SuitSorter.CurrentSorter = SuitSorter.CurrentSorter == _pages ? none : _pages;
                yield return new WaitForSeconds(10);
            }
        }
#endif
    }
}