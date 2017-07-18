using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Vault.HealthMonitor.LogFilesFiltering.Domain;
using Windows.Storage;

namespace Vault.HealthMonitor.LogFilesFiltering.Configuration
{
    public static class ConfigSerialization
    {
        public static async Task<int> Serialize<T>(string filePath, T model)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            StorageFolder folder = null;
            string dirName = Path.GetDirectoryName(filePath);
            folder = await StorageFolder.GetFolderFromPathAsync(dirName);
            Utf8StringWriter stringWriter = new Utf8StringWriter();
            
            serializer.Serialize(stringWriter, model);
            string content = stringWriter.ToString();
            StorageFile file = await folder.CreateFileAsync(Path.GetFileName(filePath), CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, content);
            return content.Length / 1024;
        }

        public static async Task<T> Deserialize<T>(string filePath)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);

            string content = await FileIO.ReadTextAsync(file);
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            return (T)serializer.Deserialize(new StringReader(content));
        }
    }
}
