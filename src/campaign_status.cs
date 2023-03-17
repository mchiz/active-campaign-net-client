using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveCampaign {
    public enum CampaignStatus {
        Draft     = 0,
        Scheduled = 1,
        Sending   = 2,
        Paused    = 3,
        Stopped   = 4,
        Completed = 5,
    }
}
