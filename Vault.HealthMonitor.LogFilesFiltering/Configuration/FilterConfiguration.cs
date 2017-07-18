using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Vault.HealthMonitor.LogFilesFiltering.Configuration
{
    public class FilterConfiguration
    {

        [XmlArray("FileTypes")]
        [XmlArrayItem("FileType")]
        public List<FileTypeConfiguration> FileTypeConfigurations { get; set; }
        public FilterConfiguration()
        {
            FileTypeConfigurations = new List<FileTypeConfiguration>();
        }
        
        public FileTypeConfiguration FindByFileName(string fileName)
        {
            foreach(var fileTypeConfiguration in FileTypeConfigurations)
            {
                if (fileName.StartsWith(fileTypeConfiguration.FileNamePrefix))
                    return fileTypeConfiguration;
            }
            throw new NotSupportedException("File is not supported and has no fileTypeConfiguration.");
        }

        public bool Validate(out string errorMessage)
        {
            errorMessage = "";
            if (FileTypeConfigurations.Count == 0)
            {
                errorMessage = $"No fileTypeConfigurations found.";
                return false;
            }



            // all errors must contain values
            foreach (var fileTypeConfiguration in FileTypeConfigurations)
            {
                if (String.IsNullOrEmpty(fileTypeConfiguration.FileNamePrefix))
                {
                    errorMessage = $"FileNamePrefix cannot be empty";
                    return false;
                }

                if (fileTypeConfiguration.StartLineGroups.Count == 0)
                {
                    errorMessage = $"StartLineGroups cannot be empty";
                    return false;
                }
                else
                {
                    foreach(var startLineGroup in fileTypeConfiguration.StartLineGroups)
                    {
                        if (String.IsNullOrEmpty(startLineGroup))
                        {
                            errorMessage = $"StartLineGroup cannot be empty";
                            return false;
                        }
                    }

                    if (fileTypeConfiguration.KnownLogErrors.Count != 0)
                    {
                        foreach (var knownLogError in fileTypeConfiguration.KnownLogErrors)
                        {
                            if (String.IsNullOrEmpty(knownLogError.ContainsValue))
                            {
                                errorMessage = $"ContainsValue cannot be empty";
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}
