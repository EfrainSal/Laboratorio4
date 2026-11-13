using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

public static class SaveSystem
{
#if UNITY_EDITOR
    public static string RutaDePruebas { get; set; }
#endif
    private static string SavePath
    {
        get
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(RutaDePruebas)) return RutaDePruebas;
#endif
            return Path.Combine(Application.persistentDataPath, "run_suspend.dat");
        }
    }
    private static string TempPath => SavePath + ".tmp";

    // Llave AES-128 (16 bytes). Es ofuscación contra edición casual, no seguridad real.
    private static readonly byte[] Key = System.Text.Encoding.UTF8.GetBytes("T8q#4w9Z!m2X@y7L");
    private const int IvSize = 16;

    public static bool HayGuardado() => File.Exists(SavePath);

    public static void Borrar()
    {
        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(TempPath)) File.Delete(TempPath);
        }
        catch (Exception e)
        {
            Debug.LogWarning("No se pudo borrar el guardado: " + e.Message);
        }
    }

    /// <returns>true si el guardado se escribió correctamente.</returns>
    public static bool Guardar(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data);
            byte[] encrypted = Encrypt(json);

            // Escritura segura: primero a un temporal y luego se reemplaza el archivo real.
            // Si el juego se cierra a mitad de escritura, el guardado anterior no queda corrupto.
            File.WriteAllBytes(TempPath, encrypted);
            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(TempPath, SavePath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Error guardando la partida: " + e.Message);
            return false;
        }
    }

    public static SaveData Cargar()
    {
        if (!HayGuardado()) return null;

        try
        {
            byte[] encrypted = File.ReadAllBytes(SavePath);
            string json = Decrypt(encrypted);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (CryptographicException e)
        {
            Debug.LogWarning("El archivo de guardado estaba corrupto y ha sido borrado: " + e.Message);
            Borrar();
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError("Error crítico leyendo el guardado: " + e.Message);
            Borrar();
            return null;
        }
    }

    // El IV es distinto en cada guardado y viaja al inicio del archivo
    private static byte[] Encrypt(string plainText)
    {
        using Aes aes = Aes.Create();
        aes.Key = Key;
        aes.GenerateIV();

        using MemoryStream ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
        using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (StreamWriter sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return ms.ToArray();
    }

    private static string Decrypt(byte[] fileBytes)
    {
        if (fileBytes == null || fileBytes.Length <= IvSize)
            throw new CryptographicException("Archivo de guardado demasiado corto.");

        byte[] iv = new byte[IvSize];
        Array.Copy(fileBytes, 0, iv, 0, IvSize);

        using Aes aes = Aes.Create();
        aes.Key = Key;
        aes.IV = iv;

        using ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using MemoryStream ms = new MemoryStream(fileBytes, IvSize, fileBytes.Length - IvSize);
        using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using StreamReader sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}
