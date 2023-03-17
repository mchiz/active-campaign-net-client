using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveCampaign {
    public class GetContactsOptions {
        public event System.Action< int, int > OnProcessed = delegate { };

        internal void RaiseProcessedEvent( int processed, int total ) {
            OnProcessed( processed, total );
        }
    }
}
