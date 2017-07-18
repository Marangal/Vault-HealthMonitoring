using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Vault.HealthMonitor.LogFilesFiltering.Configuration
{
   
    public class KnowLogError
    {
        [XmlAttribute("ContainsValue")]
        public string ContainsValue { get; set; }

        [XmlAttribute("DangerWhenMoreThenXOccurences")]
        public int DangerWhenMoreThenXOccurences { get; set; }
    }
}
