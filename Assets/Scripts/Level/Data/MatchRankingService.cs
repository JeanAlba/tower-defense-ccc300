using System;
using System.Collections.Generic;
using UnityEngine;

namespace HollowKeep.Level.Data
{
    /// <summary>
    /// Gerencia e persiste localmente (PlayerPrefs) o ranking de melhores tempos de conclusão por fase.
    /// </summary>
    public static class MatchRankingService
    {
        private const string PrefsKeyPrefix = "HollowKeep_Ranking_v1_";
        public const int MaxEntriesPerLevel = 5;

        [Serializable]
        public struct RankingEntry
        {
            public float completionTimeSeconds;
            public string difficultyName;
            public int remainingLives;
            public string recordedAt;
        }

        [Serializable]
        private class RankingListWrapper
        {
            public List<RankingEntry> entries = new();
        }

        /// <summary>
        /// Registra uma vitória no ranking da fase informada e retorna a colocação obtida (1 = recorde, 0 = fora do top).
        /// </summary>
        public static int RecordVictory(string levelId, float completionTimeSeconds, string difficultyName, int remainingLives)
        {
            string normalizedLevelId = NormalizeLevelId(levelId);
            float clampedTime = Mathf.Max(1f, completionTimeSeconds);
            List<RankingEntry> entries = LoadEntries(normalizedLevelId);

            RankingEntry newEntry = new()
            {
                completionTimeSeconds = clampedTime,
                difficultyName = string.IsNullOrWhiteSpace(difficultyName) ? "Normal" : difficultyName,
                remainingLives = Mathf.Max(1, remainingLives),
                recordedAt = DateTime.Now.ToString("dd/MM HH:mm")
            };

            int insertIndex = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                if (clampedTime < entries[i].completionTimeSeconds)
                {
                    insertIndex = i;
                    break;
                }
            }

            entries.Insert(insertIndex, newEntry);
            if (entries.Count > MaxEntriesPerLevel)
            {
                entries.RemoveRange(MaxEntriesPerLevel, entries.Count - MaxEntriesPerLevel);
            }

            SaveEntries(normalizedLevelId, entries);
            return insertIndex < MaxEntriesPerLevel ? (insertIndex + 1) : 0;
        }

        public static IReadOnlyList<RankingEntry> GetTopEntries(string levelId, int maxCount = 3)
        {
            List<RankingEntry> entries = LoadEntries(NormalizeLevelId(levelId));
            if (entries.Count > maxCount)
            {
                entries.RemoveRange(maxCount, entries.Count - maxCount);
            }
            return entries;
        }

        public static string FormatVictorySummary(string levelId, string stageTitle, float completionTimeSeconds, int rankPosition, int topCount = 3)
        {
            string timeFormatted = FormatTime(completionTimeSeconds);
            string badge = rankPosition == 1
                ? " <color=#7A2E0E>[NOVO RECORDE!]</color>"
                : (rankPosition > 1 ? $" <color=#4E331B>[#{rankPosition} MELHOR TEMPO]</color>" : string.Empty);

            string headerTitle = string.IsNullOrWhiteSpace(stageTitle) ? NormalizeLevelId(levelId) : stageTitle.ToUpperInvariant();
            IReadOnlyList<RankingEntry> entries = GetTopEntries(levelId, topCount);

            System.Text.StringBuilder sb = new();
            sb.AppendLine($"<b>TEMPO FINAL: {timeFormatted}</b>{badge}");
            sb.AppendLine($"<size=82%>RANKING · {headerTitle}</size>");

            for (int i = 0; i < topCount; i++)
            {
                if (i < entries.Count)
                {
                    RankingEntry e = entries[i];
                    sb.AppendLine($"<size=85%>{i + 1}º  <b>{FormatTime(e.completionTimeSeconds)}</b>  ·  {e.difficultyName} ({e.remainingLives} vidas)</size>");
                }
                else
                {
                    sb.AppendLine($"<size=85%>{i + 1}º  --:--  ·  ---</size>");
                }
            }

            return sb.ToString().TrimEnd();
        }

        public static string FormatMenuLeaderboardSummary()
        {
            System.Text.StringBuilder sb = new();
            AppendLevelLine(sb, "Level_01", "FASE 1");
            AppendLevelLine(sb, "Level_02", "FASE 2");
            return sb.ToString().TrimEnd();
        }

        private static void AppendLevelLine(System.Text.StringBuilder sb, string levelId, string label)
        {
            IReadOnlyList<RankingEntry> entries = GetTopEntries(levelId, 3);
            if (entries.Count == 0)
            {
                sb.AppendLine($"{label}: Sem registros concluídos");
                return;
            }

            sb.Append($"{label}: ");
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) sb.Append("  |  ");
                sb.Append($"{i + 1}º {FormatTime(entries[i].completionTimeSeconds)} ({entries[i].difficultyName})");
            }
            sb.AppendLine();
        }

        public static string FormatTime(float totalSeconds)
        {
            int clamped = Mathf.Max(0, Mathf.CeilToInt(totalSeconds));
            int minutes = clamped / 60;
            int seconds = clamped % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        private static string NormalizeLevelId(string levelId)
        {
            return string.IsNullOrWhiteSpace(levelId) ? "Level_01" : levelId.Trim();
        }

        private static List<RankingEntry> LoadEntries(string normalizedLevelId)
        {
            string key = PrefsKeyPrefix + normalizedLevelId;
            if (!PlayerPrefs.HasKey(key))
            {
                return new List<RankingEntry>();
            }

            string json = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<RankingEntry>();
            }

            try
            {
                RankingListWrapper wrapper = JsonUtility.FromJson<RankingListWrapper>(json);
                return wrapper?.entries ?? new List<RankingEntry>();
            }
            catch
            {
                return new List<RankingEntry>();
            }
        }

        private static void SaveEntries(string normalizedLevelId, List<RankingEntry> entries)
        {
            RankingListWrapper wrapper = new() { entries = entries };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(PrefsKeyPrefix + normalizedLevelId, json);
            PlayerPrefs.Save();
        }
    }
}
