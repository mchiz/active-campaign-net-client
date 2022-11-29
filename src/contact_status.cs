using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveCampaign {
    public enum ContactStatus {
        Any = -1,
        Unconfirmed = 0,
        Active = 1,
        Unsubscribed = 2,
        Bounced = 3,
    }
}
