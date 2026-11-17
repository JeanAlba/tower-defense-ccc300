using System;
using System.Collections.Generic;
using UnityEngine;

namespace HollowKeep.Level.Data
{
    [Serializable]
    public class SubWaveData
    {
        [Tooltip("Porcentagem da duração da onda (0% a 100%) no momento em que esta subonda dispara.")]
        [Range(0f, 100f)]
        [SerializeField] private float wavePercentage = 0f;

        [Tooltip("Grupos de inimigos que iniciarão o spawn nesta subonda.")]
        [SerializeField] private List<SpawnGroup> spawnGroups = new();

        public float WavePercentage => wavePercentage;
        public IReadOnlyList<SpawnGroup> SpawnGroups => spawnGroups;
    }
}
