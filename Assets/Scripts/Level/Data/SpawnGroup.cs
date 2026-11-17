using System;
using UnityEngine;
using HollowKeep.Enemies;

namespace HollowKeep.Level.Data
{
    [Serializable]
    public class SpawnGroup
    {
        [Tooltip("Prefab do inimigo a ser instanciado. Aceita apenas prefabs que herdam de Enemy.")]
        [SerializeField] private Enemy enemyPrefab;

        [Tooltip("Quantidade de inimigos deste tipo a serem instanciados.")]
        [Min(1)]
        [SerializeField] private int quantity = 5;

        [Tooltip("Intervalo em segundos entre o spawn de cada inimigo deste grupo.")]
        [Min(0.05f)]
        [SerializeField] private float interval = 1f;

        [Tooltip("Identificador tipado da rota que este grupo seguirá.")]
        [SerializeField] private PathIdentifier targetPath;

        public Enemy EnemyPrefab => enemyPrefab;
        public int Quantity => quantity;
        public float Interval => interval;
        public PathIdentifier TargetPath => targetPath;

        public float TotalSpawnTime => (quantity > 0) ? (quantity - 1) * interval : 0f;
    }
}
