using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class PlayerRecord
{
    public string playerName;
    public int score;
    public float time;
    public int characterId;
    public string date;
    public bool uploaded;
}

[Serializable]
public class RankingData
{
    public List<PlayerRecord> topScores = new List<PlayerRecord>();
    public List<PlayerRecord> topTimes = new List<PlayerRecord>();
}

public static class RankingManager
{
    private static string Path => Application.persistentDataPath + "/ranking.dat";
    
    public static RankingData LoadRanking()
    {
        if (!File.Exists(Path)) return new RankingData();
        string json = File.ReadAllText(Path);
        return JsonUtility.FromJson<RankingData>(json);
    }

    public static void SaveRanking(RankingData data)
    {
        File.WriteAllText(Path, JsonUtility.ToJson(data, true));
    }

    // CORRECCIÓN: Ahora la función recibe 4 argumentos, incluyendo el string playerName
    public static void RegistrarPartidaCompletada(string playerName, int score, float time, int characterId)
    {
        RankingData data = LoadRanking();
        
        PlayerRecord newRecord = new PlayerRecord
        {
            playerName = playerName, // Asignamos el nombre guardado
            score = score,
            time = time,
            characterId = characterId,
            date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            uploaded = false
        };

        bool requiresUpload = false;

        // --- LÓGICA DE MAX SCORE ---
        data.topScores.Add(newRecord);
        data.topScores = data.topScores.OrderByDescending(r => r.score).ToList();
        
        int scoreRank = data.topScores.IndexOf(newRecord);
        if (scoreRank < 3) requiresUpload = true; // Top 3 local -> Subir
        
        if (data.topScores.Count > 10) data.topScores.RemoveAt(10); // Mantener top 10

        // --- LÓGICA DE MEJOR TIEMPO ---
        PlayerRecord timeRecord = JsonUtility.FromJson<PlayerRecord>(JsonUtility.ToJson(newRecord));
        data.topTimes.Add(timeRecord);
        data.topTimes = data.topTimes.OrderBy(r => r.time).ToList(); // Menor tiempo es mejor
        
        int timeRank = data.topTimes.IndexOf(timeRecord);
        if (timeRank < 3) requiresUpload = true;
        
        if (data.topTimes.Count > 10) data.topTimes.RemoveAt(10);

        SaveRanking(data);

        // Si calificó para el Top 3 local, intentamos enviarlo
        if (requiresUpload)
        {
            IntentarSubirRecordsPendientes();
        }
    }

    public static void IntentarSubirRecordsPendientes()
    {
        RankingData data = LoadRanking();
        bool changed = false;

        foreach (var record in data.topScores.Concat(data.topTimes).Where(r => !r.uploaded))
        {
            // Aquí harás tu llamada HTTP (UnityWebRequest) al Backend en la nube.
            // Si es exitoso: record.uploaded = true; changed = true;
            Debug.Log($"Enviando al backend... Jugador: {record.playerName}, Score: {record.score}, Time: {record.time}");
        }

        if (changed) SaveRanking(data);
    }
}