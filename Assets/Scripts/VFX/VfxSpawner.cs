using UnityEngine;

namespace HollowKeep.VFX
{
    /// <summary>
    /// Instancia efeitos de partículas one-shot garantindo reprodução e autodestruição.
    /// </summary>
    public static class VfxSpawner
    {
        public static ParticleSystem Spawn(ParticleSystem prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            ParticleSystem instance = Object.Instantiate(prefab, position, rotation);
            ParticleSystem.MainModule main = instance.main;
            if (!main.loop && main.stopAction == ParticleSystemStopAction.None)
            {
                main.stopAction = ParticleSystemStopAction.Destroy;
            }

            instance.Play(true);
            return instance;
        }
    }
}
