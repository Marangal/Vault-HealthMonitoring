using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Vault.HealthMonitor.LogFilesFiltering.Domain
{
    public class FileToFilter
    {
        private const int minLengthOfDateField = 8;
        private const int maxLengthOfDateField = 10;
        private const int minLengthOfTimeField = 7;
        private const int maxLengthOfTimeField = 8;

        private readonly Configuration.FileTypeConfiguration fileTypeConfiguration = null;
        public readonly StorageFile File;
        private readonly string[] Lines;
        private readonly LineGroup[] LineGroups;
        private readonly List<KnowLogErrorStatus> knownLogErrorStates = null;

        public FileToFilter(StorageFile file, string[] lines, Configuration.FileTypeConfiguration fileTypeConfiguration)
        {
            this.knownLogErrorStates = new List<KnowLogErrorStatus>();
            this.fileTypeConfiguration = fileTypeConfiguration;
            this.File = file;
            this.Lines = lines;
            
            foreach (var knownLogError in fileTypeConfiguration.KnownLogErrors)
            {
                knownLogErrorStates.Add(new KnowLogErrorStatus(knownLogError));
            }

            this.LineGroups = CreateLineGroups();
            this.CollectectOccurences();
        }

        private LineGroup[] CreateLineGroups()
        {
            List<LineGroup> lineGroups = new List<LineGroup>();

            List<string> lineCollection = new List<string>();
            foreach (var line in Lines)
            {
                DateTime? dateValue;
                if (NewLineGroup(line, out dateValue))
                {
                    if (lineCollection.Count > 0)
                    {
                        lineGroups.Add(new LineGroup(dateValue.Value, lineCollection, fileTypeConfiguration.KnownLogErrors));
                    }
                    lineCollection = new List<string>();
                    lineCollection.Add(line);
                } else
                {
                    lineCollection.Add(line);
                }
            }
            return lineGroups.ToArray();
        }
        private bool NewLineGroup(string line, out DateTime? date)
        {
            try
            {
                date = null;
                string dateField = ExtractDateField(line);
                if (String.IsNullOrEmpty(dateField))
                    return false;
                string timeField = ExtractTimeField(line, dateField);
                if (String.IsNullOrEmpty(timeField))
                    return false;

                string dateTimeField = dateField + " " + timeField;

                foreach (var startLineGroup in fileTypeConfiguration.StartLineGroups)
                {
                    DateTime dateValue;
                    bool success = DateTime.TryParseExact(dateTimeField, startLineGroup, new CultureInfo("en-US"), DateTimeStyles.None, out dateValue);
                    if (success)
                    {
                        date = dateValue;
                        return true;
                    }
                }
            }catch(Exception)
            {
                throw;
            }
            return false;
        }

        private string ExtractDateField(string line)
        {
            if (line.Length >= minLengthOfDateField)
            {
                if (!line.Contains(' '))
                    return String.Empty;
                else
                {
                    int lengthOfDateField = line.IndexOf(' ');
                    if (lengthOfDateField < minLengthOfDateField || lengthOfDateField > maxLengthOfDateField)
                        return String.Empty;
                    else
                    {
                        return line.Substring(0, lengthOfDateField);
                    }
                }
            }
            else
                return String.Empty;
        }

        private string ExtractTimeField(string line, string dateField)
        {
            int lengthDateField = dateField.Length;
            int startIndexOfTimeField = lengthDateField + 1;

            string partWithoutDate = "";
            if (line.Length >= startIndexOfTimeField + 1)
            {
                partWithoutDate = line.Substring(startIndexOfTimeField);
            }

            if (partWithoutDate.Length >= minLengthOfTimeField)
            {
                if (!partWithoutDate.Contains(' '))
                    return String.Empty;
                else
                {
                    int lengthOfTimeField = partWithoutDate.IndexOf(' ');
                    if (lengthOfTimeField < minLengthOfTimeField || lengthOfTimeField > maxLengthOfTimeField)
                        return String.Empty;
                    else
                    {
                        return partWithoutDate.Substring(0, lengthOfTimeField);
                    }
                }
            }
            else
                return String.Empty;
        }

        public IEnumerable<LineGroup> GetImportantLineGroups()
        {
            List<LineGroup> importantLineGroups = new List<LineGroup>();
            foreach (var LineGroup in LineGroups)
            {
                if (LineGroup.FoundErrors.Count == 0 && LineGroup.HasErrorException)
                {
                    LineGroup.Lines.Insert(0, "*************** UNKNOWN EXCEPTION FOUND ********************");
                    importantLineGroups.Add(LineGroup);
                }
                else
                {
                    foreach (var foundError in LineGroup.FoundErrors)
                    {
                        KnowLogErrorStatus knownLogErrorStatus = knownLogErrorStates.Single(error => error.KnowLogError == foundError);
                        if (knownLogErrorStatus.TooManyOccurences)
                        {
                            LineGroup.Lines.Insert(0, $"*************** KNOW EXCEPTION {knownLogErrorStatus.Occurences} TIMES FOUND, ONLY {knownLogErrorStatus.KnowLogError.DangerWhenMoreThenXOccurences} ALLOWED ********************");
                            importantLineGroups.Add(LineGroup);
                        }
                    }
                }
            }
            return importantLineGroups;
        }

        private void CollectectOccurences()
        {
            foreach(var lineGroup in LineGroups)
            {
                foreach(var foundError in lineGroup.FoundErrors)
                {
                    var knownLogErrorStatus = knownLogErrorStates.Single(status => status.KnowLogError.ContainsValue == foundError.ContainsValue);
                    knownLogErrorStatus.IncreaseOccurences();
                }
            }
        }
    }
}
