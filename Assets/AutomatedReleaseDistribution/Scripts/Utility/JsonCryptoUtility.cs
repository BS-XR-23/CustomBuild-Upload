using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace In.App.Update
{
    public static class JsonCryptoUtility
    {
        private static readonly string encryptionKey = "alpha#@,.!@#4085"; // Replace with a secure key

        /// <summary>
        /// Encrypts a JSON string and writes it to a file.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        /// <param name="jsonData">JSON data to encrypt.</param>
        public static void EncryptJsonToFile(string filePath, string jsonData)
        {
            try
            {
                var key = Encoding.UTF8.GetBytes(encryptionKey);
                var iv = new byte[16]; // Initialization vector (IV), zero-filled for simplicity

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;

                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        using (var writer = new StreamWriter(cryptoStream))
                        {
                            writer.Write(jsonData);
                        }

                        File.WriteAllBytes(filePath, memoryStream.ToArray());
                        Debug.Log($"Encrypted JSON written to: {filePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error encrypting JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Decrypts a JSON file and returns the JSON string.
        /// </summary>
        /// <param name="filePath">Path to the encrypted file.</param>
        /// <returns>The decrypted JSON string.</returns>
        public static string DecryptJsonFromFile(string filePath)
        {
            try
            {
                var key = Encoding.UTF8.GetBytes(encryptionKey);
                var iv = new byte[16]; // Initialization vector (IV), zero-filled for simplicity
                var encryptedData = File.ReadAllBytes(filePath);

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var memoryStream = new MemoryStream(encryptedData))
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cryptoStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error decrypting JSON: {ex.Message}");
                return null;
            }
        }
    }
}