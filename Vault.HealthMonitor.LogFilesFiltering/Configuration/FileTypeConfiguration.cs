using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Vault.HealthMonitor.LogFilesFiltering.Configuration
{
    public class FileTypeConfiguration
    {
        [XmlAttribute("FileNamePrefix")]
        public string FileNamePrefix { get; set; }

        [XmlArray("StartLineGroups")]
        [XmlArrayItem("StartLineGroup")]
        public List<string> StartLineGroups { get; set; }
        

        [XmlArray("LogErrors")]
        [XmlArrayItem("LogError")]
        public List<KnowLogError> KnownLogErrors { get; set; }
        public FileTypeConfiguration()
        {
            KnownLogErrors = new List<KnowLogError>();
        }
        
    }
}
