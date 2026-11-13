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
    public string recordId;
    public string playerName;
    public int score;
    public float time;
    public int characterId; // IDs del contrato API: Jason=1, Milena=2.
    public string date;
    public bool uploaded;
    public bool victory;
}

[Serializable]
public class RankingData
{
    public int version;
    public List<PlayerRecord> topScores = new List<PlayerRecord>();
    public List<PlayerRecord> topTimes = new List<PlayerRecord>();
    public List<PlayerRecord> pendingUploads = new List<PlayerRecord>();
}

public static class RankingManager
{
    private const string ApiUrl = "https://laboratorio4api.onrender.com/api/administracion/ranking";
    private const string ApiKey = "bd225f21e673bdac15dda936ff91d82e";
    private static Task sincronizacion = Task.CompletedTask;
    public static string UltimoError { get; private set; }
    public static long UltimoEstadoHttp { get; private set; }
    public static bool Sincronizando => !sincronizacion.IsCompleted;
#if UNITY_EDITOR
    public static string RutaDePruebas;
    public static string ApiDePruebas;
    public static bool SuspenderSincronizacionAutomatica;
#endif
    private static string Ruta
    {
        get
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(RutaDePruebas)) return RutaDePruebas;
#endif
            return System.IO.Path.Combine(Application.persistentDataPath, "ranking.dat");
        }
    }

    public static RankingData LoadRanking() => Cargar(Ruta);
    private static RankingData Cargar(string ruta)
    {
        if (!File.Exists(ruta)) return new RankingData { version = 2 };
        RankingData data;
        try
        {
            data = JsonUtility.FromJson<RankingData>(File.ReadAllText(ruta));
            if (data == null) throw new FormatException("Ranking vacío");
        }
        catch (Exception e)
        {
            string copia = ruta + ".corrupto";
            if (!File.Exists(copia)) File.Copy(ruta, copia);
            Debug.LogWarning("No se pudo leer el ranking local; se conservó una copia. " + e.GetType().Name);
            return new RankingData { version = 2 };
        }
        data.topScores ??= new List<PlayerRecord>();
        data.topTimes ??= new List<PlayerRecord>();
        data.pendingUploads ??= new List<PlayerRecord>();
        if (data.version < 2)
        {
            var grupos = data.topScores.Concat(data.topTimes).Where(r => r != null)
                .GroupBy(r => new { r.playerName, r.date, r.score, r.time, r.characterId });
            foreach (var grupo in grupos)
            {
                string id = Guid.NewGuid().ToString("N");
                bool subido = grupo.Any(r => r.uploaded);
                foreach (var r in grupo) { r.recordId = id; r.uploaded = subido; r.victory = true; }
                if (!subido) data.pendingUploads.Add(grupo.First());
            }
            data.version = 2;
            Guardar(ruta, data);
        }
        return data;
    }

    public static void SaveRanking(RankingData data) => Guardar(Ruta, data);
    private static void Guardar(string ruta, RankingData data)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ruta));
        string temporal = ruta + ".tmp";
        File.WriteAllText(temporal, JsonUtility.ToJson(data, true));
        // Windows puede mantener un bloqueo breve (indexación/antivirus) sobre el destino.
        // Reintentar el mismo temporal evita perder una partida por ese bloqueo transitorio.
        for (int intento = 0; ; intento++)
        {
            try
            {
                if (File.Exists(ruta)) File.Replace(temporal, ruta, ruta + ".bak");
                else File.Move(temporal, ruta);
                break;
            }
            catch (IOException) when (intento < 3)
            {
                System.Threading.Thread.Sleep(10 * (intento + 1));
            }
        }
    }

    public static void RegistrarPartidaCompletada(string nombre, int puntos, float segundos, int personaje)
        => RegistrarPartida(nombre, puntos, segundos, personaje, true);
    public static void RegistrarPartida(string nombre, int puntos, float segundos, int personaje, bool victoria)
    {
        try
        {
            var data = LoadRanking();
            var record = new PlayerRecord
            {
                recordId = Guid.NewGuid().ToString("N"),
                playerName = string.IsNullOrWhiteSpace(nombre) ? "Desconocido" : nombre.Trim(),
                score = Mathf.Max(0, puntos), time = Mathf.Max(0, segundos), characterId = personaje == 0 ? 1 : 2,
                date = DateTime.UtcNow.ToString("O"), victory = victoria
            };
            data.topScores.Add(record);
            data.topScores = data.topScores.OrderByDescending(r => r.score).ThenBy(r => r.date).Take(10).ToList();
            // Las derrotas cuentan en puntuación, nunca como victorias rápidas.
            if (victoria) data.topTimes.Add(record);
            data.topTimes = data.topTimes.OrderBy(r => r.time).Take(10).ToList();
            data.pendingUploads.Add(record);
            SaveRanking(data);
            IntentarSubirRecordsPendientes();
        }
        catch (Exception e) { Debug.LogError("No se pudo guardar la puntuación local: " + e.Message); }
    }

    public static async void IntentarSubirRecordsPendientes()
    {
#if UNITY_EDITOR
        if (SuspenderSincronizacionAutomatica) return;
#endif
        await SincronizarAsync();
    }

    public static Task SincronizarAsync()
    {
        if (Sincronizando) return sincronizacion;
        string url = ApiUrl;
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(ApiDePruebas)) url = ApiDePruebas;
#endif
        return sincronizacion = Sincronizar(Ruta, url);
    }

    [Serializable] private class RankingRequest
    {
        public string playerName;
        public int score;
        public float time;
        public int characterId;
        public string date;
        public bool uploaded;
    }

    private static async Task Sincronizar(string ruta, string url)
    {
        UltimoError = null;
        try
        {
            while (true)
            {
                var record = Cargar(ruta).pendingUploads.FirstOrDefault();
                if (record == null) return;
                var payload = new RankingRequest
                {
                    playerName = record.playerName, score = record.score, time = record.time,
                    characterId = record.characterId, date = record.date, uploaded = true
                };
                using var request = new UnityWebRequest(url, "POST");
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-API-Key", ApiKey);
                request.timeout = 30;
                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();
                UltimoEstadoHttp = request.responseCode;
                if (request.result != UnityWebRequest.Result.Success || request.responseCode < 200 || request.responseCode >= 300)
                {
                    UltimoError = "HTTP " + request.responseCode + ": " + request.error;
                    Debug.LogWarning("Ranking pendiente de sincronización. " + UltimoError);
                    return;
                }
                // Volver a leer: otras partidas pueden haberse guardado durante el envío.
                var actual = Cargar(ruta);
                foreach (var r in actual.topScores.Concat(actual.topTimes).Where(r => r.recordId == record.recordId)) r.uploaded = true;
                actual.pendingUploads.RemoveAll(r => r.recordId == record.recordId);
                Guardar(ruta, actual);
            }
        }
        catch (Exception e)
        {
            UltimoError = e.Message;
            Debug.LogWarning("Ranking conservado localmente: " + e.GetType().Name);
        }
    }
}
