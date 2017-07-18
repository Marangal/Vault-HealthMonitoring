using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vault.HealthMonitor.LogFilesFiltering.Domain
{
    public class LineGroup
    {
        private const string ErrorGroupPrefix = "Exception: ";
        public DateTime LogDate { get; private set; }
        public bool HasErrorException { get; private set; }
        public List<string> Lines { get; private set; }
        public List<Configuration.KnowLogError> FoundErrors { get; private set; }
        private List<Configuration.KnowLogError> knownLogErrors = null;
        public LineGroup(DateTime logDate, List<string> lines, List<Configuration.KnowLogError> knownLogErrors)
        {
            this.LogDate = logDate;
            this.knownLogErrors = knownLogErrors;
            this.Lines = lines;
            this.HasErrorException = false;
            this.FoundErrors = SearchForKnownErrors();
        }

        private List<Configuration.KnowLogError> SearchForKnownErrors()
        {
            List<Configuration.KnowLogError> foundErrors = new List<Configuration.KnowLogError>();

            foreach(var line in Lines)
            {
                if (line.StartsWith(ErrorGroupPrefix))
                    HasErrorException = true;

                foreach (var knownLogError in knownLogErrors)
                {
                    if (line.Contains(knownLogError.ContainsValue))
                        foundErrors.Add(knownLogError);
                }
            }

            return foundErrors;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach(var line in Lines)
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }
    }
}
