// Builds the pooling template for one accent particle prefab, shared by PrefabVfxSpawner (one-shots and missiles) and
// StatusAuraPresenter (looping auras). The prefab is instantiated under an inactive holder, so none of its own scripts
// ever wake; optionally every script is then removed (Polygon Arsenal's demo helpers and sound spawners) and its
// lights and particle light modules turned off, so no pooled copy carries them; finally PrefabVfxInstance makes its
// particle systems safe to pool. Runs only while pools are built, never per frame; its scratch lists are reused.
using System.Collections.Generic;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    public static class PrefabVfxTemplates
    {
        private static readonly List<MonoBehaviour> Scripts = new List<MonoBehaviour>(8);
        private static readonly List<Light> Lights = new List<Light>(4);
        private static readonly List<ParticleSystem> Systems = new List<ParticleSystem>(8);

        public static PrefabVfxInstance Create(GameObject prefab, Transform inactiveHolder, bool stripScripts,
            bool disableLights)
        {
            GameObject templateObject = Object.Instantiate(prefab, inactiveHolder, false);
            templateObject.name = prefab.name;

            if (stripScripts)
            {
                StripScripts(templateObject);
            }

            if (disableLights)
            {
                DisableLights(templateObject);
            }

            var template = templateObject.AddComponent<PrefabVfxInstance>();
            template.Initialize(prefab);
            return template;
        }

        private static void StripScripts(GameObject templateObject)
        {
            Scripts.Clear();
            templateObject.GetComponentsInChildren(true, Scripts);
            for (int i = Scripts.Count - 1; i >= 0; i--)
            {
                if (Scripts[i] != null)
                {
                    Object.DestroyImmediate(Scripts[i]);
                }
            }

            Scripts.Clear();
        }

        private static void DisableLights(GameObject templateObject)
        {
            Lights.Clear();
            templateObject.GetComponentsInChildren(true, Lights);
            for (int i = 0; i < Lights.Count; i++)
            {
                Lights[i].enabled = false;
            }

            Lights.Clear();

            Systems.Clear();
            templateObject.GetComponentsInChildren(true, Systems);
            for (int i = 0; i < Systems.Count; i++)
            {
                ParticleSystem.LightsModule lights = Systems[i].lights;
                lights.enabled = false;
            }

            Systems.Clear();
        }
    }
}
