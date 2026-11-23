using System.Collections.Generic;
using UnityEngine;

namespace HollowKeep.Level.Data
{
    [CreateAssetMenu(fileName = "Level_New", menuName = "HollowKeep/Level/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Identificação")]
        [Tooltip("Identificador único da fase (ex: Level_01).")]
        [SerializeField] private string levelId = "Level_01";

        [Header("Briefing da Fase")]
        [Tooltip("Título exibido no popup pré-batalha (ex: FASE 1).")]
        [SerializeField] private string stageTitle = "";

        [TextArea(3, 5)]
        [Tooltip("Descrição temática exibida no popup pré-batalha.")]
        [SerializeField] private string stageDescription = "";

        [Tooltip("Índice da miniatura cartográfica exibida no popup (0 = Fase 1, 1 = Fase 2).")]
        [Min(0)]
        [SerializeField] private int mapPreviewVariant = 0;

        [Header("Economia e Ritmo")]
        [Tooltip("Ouro inicial fornecido ao jogador ao iniciar a fase.")]
        [Min(0)]
        [SerializeField] private int startingGold = 100;

        [Tooltip("Tempo em segundos de descanso/planejamento entre o término de uma onda e o início da próxima.")]
        [Min(0f)]
        [SerializeField] private float timeBetweenWaves = 15f;

        [Header("Ondas")]
        [Tooltip("Lista sequencial e ordenada de ondas que compõem este nível.")]
        [SerializeField] private List<WaveData> waves = new();

        public string LevelId => levelId;
        public string StageTitle => stageTitle;
        public string StageDescription => stageDescription;
        public int MapPreviewVariant => mapPreviewVariant;
        public int StartingGold => startingGold;
        public float TimeBetweenWaves => timeBetweenWaves;
        public IReadOnlyList<WaveData> Waves => waves;
        public int TotalWaves => waves != null ? waves.Count : 0;
    }
}
