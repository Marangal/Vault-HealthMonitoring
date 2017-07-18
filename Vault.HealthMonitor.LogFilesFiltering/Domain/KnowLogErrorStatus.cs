using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vault.HealthMonitor.LogFilesFiltering.Configuration;

namespace Vault.HealthMonitor.LogFilesFiltering.Domain
{
    public class KnowLogErrorStatus
    {
        public KnowLogError KnowLogError { get; private set; }
        public int Occurences { get; private set; }
        public bool TooManyOccurences { get; private set; }

        public KnowLogErrorStatus(KnowLogError knowLogError)
        {
            this.KnowLogError = knowLogError;
            this.Occurences = 0;
            this.TooManyOccurences = false;
        }

        public void IncreaseOccurences()
        {
            Occurences++;
            if (Occurences > KnowLogError.DangerWhenMoreThenXOccurences)
                TooManyOccurences = true;
        }
    }
}
