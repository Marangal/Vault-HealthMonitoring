using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using System.Collections;

namespace Vault.HealthMonitor.LogFilesFiltering.BL
{
    class FileToFilterFactory
    {
        public static async Task<Domain.FileToFilter[]> CreateFilesToFilterAsync(IReadOnlyList<StorageFile> files, Configuration.FilterConfiguration filterConfiguration)
        {
            List<Task<Domain.FileToFilter>> tasks = new List<Task<Domain.FileToFilter>>();
            List<StorageFile> notSupportedFiles = new List<StorageFile>();
            foreach (var file in files)
            {
                try
                {
                    Configuration.FileTypeConfiguration fileTypeConfiguration = filterConfiguration.FindByFileName(file.Name);
                    tasks.Add(CreateFileToFilterAsync(file, fileTypeConfiguration));
                } catch (NotSupportedException)
                {
                    notSupportedFiles.Add(file);
                }
            }
            if (notSupportedFiles.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach(var file in notSupportedFiles)
                {
                    sb.AppendLine($" {file.Name} - {file.Path}");
                }
                var errorDialog = new MessageDialog($"{sb.ToString()}", $"Following files are not supported by the configuration");
                errorDialog.Commands.Add(new UICommand { Label = "Close", Id = 0 });
                await errorDialog.ShowAsync();
            }

            Domain.FileToFilter[] filesToFilter = await Task.WhenAll(tasks);
            return filesToFilter;
        }

        private static async Task<Domain.FileToFilter> CreateFileToFilterAsync(StorageFile file, Configuration.FileTypeConfiguration fileTypeConfiguration)
        {
            string[] lines = await ReadLinesAsync(file);
            Domain.FileToFilter fileToFilter = new Domain.FileToFilter(file, lines, fileTypeConfiguration);
            return fileToFilter;

        }

        private static async Task<string[]> ReadLinesAsync(StorageFile file)
        {
            List<string> lines = new List<string>();
            string nextLine;
            using (StreamReader sr = new StreamReader(await file.OpenStreamForReadAsync()))
            {
                while ((nextLine = await sr.ReadLineAsync()) != null)
                {
                    lines.Add(nextLine);
                }
            }
            return lines.ToArray();
        }
    }
}
