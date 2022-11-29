using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveCampaign {
    public struct DateRange {
        public DateRange( DateTime start, DateTime end ) {
            if( start > end )
                throw new ArgumentException( "The start date cannot be set after end date", "start" );

            _start = start;
            _end = end;
        }
            
        public DateTime Start { get => _start; }
        public DateTime End { get => _end; }

        DateTime _start;
        DateTime _end;
    }

}
