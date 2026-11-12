using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

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
    
    private const string ApiUrl = "https://laboratorio4api.onrender.com/api/administracion/ranking";
    private const string ApiKey = "bd225f21e673bdac15dda936ff91d82e"; 

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

    public static void RegistrarPartidaCompletada(string playerName, int score, float time, int characterId)
    {
        RankingData data = LoadRanking();
        
        int apiCharacterId = characterId <= 0 ? 1 : characterId;
        if (apiCharacterId == 1 && characterId == 1) apiCharacterId = 2; 

        PlayerRecord newRecord = new PlayerRecord
        {
            playerName = playerName,
            score = score,
            time = time,
            characterId = apiCharacterId,
            date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            uploaded = false
        };

        bool requiresUpload = false;

        // --- LÓGICA DE MAX SCORE ---
        data.topScores.Add(newRecord);
        data.topScores = data.topScores.OrderByDescending(r => r.score).ToList();
        
        if (data.topScores.IndexOf(newRecord) < 3) requiresUpload = true; 
        if (data.topScores.Count > 10) data.topScores.RemoveAt(10); 

        // --- LÓGICA DE MEJOR TIEMPO ---
        PlayerRecord timeRecord = JsonUtility.FromJson<PlayerRecord>(JsonUtility.ToJson(newRecord));
        data.topTimes.Add(timeRecord);
        data.topTimes = data.topTimes.OrderBy(r => r.time).ToList(); 
        
        if (data.topTimes.IndexOf(timeRecord) < 3) requiresUpload = true;
        if (data.topTimes.Count > 10) data.topTimes.RemoveAt(10);

        SaveRanking(data);

        // Intento inmediato de subida al terminar la run
        if (requiresUpload)
        {
            IntentarSubirRecordsPendientes();
        }
    }

    // Al ser un método estático y público, el MainMenuController puede llamarlo libremente
    public static async void IntentarSubirRecordsPendientes()
    {
        RankingData data = LoadRanking();
        bool changed = false;

        // Filtramos los pendientes agrupando por nombre y fecha para evitar mandar el clon de 'topTimes'
        var recordsPendientes = data.topScores.Concat(data.topTimes)
            .Where(r => !r.uploaded)
            .GroupBy(r => new { r.playerName, r.date })
            .Select(g => g.First())
            .ToList();

        if (recordsPendientes.Count == 0) return; // Nada que subir

        foreach (var record in recordsPendientes)
        {
            Debug.Log($"Intentando sincronizar con backend... Jugador: {record.playerName}, Score: {record.score}");

            string json = JsonUtility.ToJson(record);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest request = new UnityWebRequest(ApiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-API-Key", ApiKey);
                request.timeout = 90;

                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success && request.responseCode == 201)
                {
                    Debug.Log($"Sincronización exitosa para {record.playerName}");
                    
                    // Actualizamos el estado "uploaded" a true en ambas listas físicas si existen
                    foreach(var match in data.topScores.Where(r => r.date == record.date && r.playerName == record.playerName))
                        match.uploaded = true;
                    foreach(var match in data.topTimes.Where(r => r.date == record.date && r.playerName == record.playerName))
                        match.uploaded = true;

                    changed = true;
                }
                else
                {
                    // Si falla (sin internet, timeout, etc.), se queda en false. Se reintentará luego.
                    Debug.LogWarning($"Fallo en sincronización. Se reintentará después. HTTP: {request.responseCode}");
                }
            }
        }

        // Solo guardamos en disco si logramos subir algo y cambiamos su estado a true
        if (changed) 
        {
            SaveRanking(data);
        }
    }
}