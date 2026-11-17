using System;
using System.Collections.Generic;
using UnityEngine;

namespace HollowKeep.Level.Data
{
    [Serializable]
    public class WaveData
    {
        [Tooltip("Duração da janela de tempo da onda (em segundos), sobre a qual as porcentagens das subondas são calculadas.")]
        [Min(1f)]
        [SerializeField] private float duration = 45f;

        [Tooltip("Lista de subondas pertencentes a esta onda.")]
        [SerializeField] private List<SubWaveData> subwaves = new();

        public float Duration => duration;
        public IReadOnlyList<SubWaveData> Subwaves => subwaves;

        public int CalculateTotalEnemies()
        {
            int total = 0;
            if (subwaves == null) return 0;
            foreach (var sub in subwaves)
            {
                if (sub.SpawnGroups == null) continue;
                foreach (var group in sub.SpawnGroups)
                {
                    total += group.Quantity;
                }
            }
            return total;
        }
    }
}
