using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveCampaign {
    public struct ContactData {
        [ JsonProperty( "email" ) ]                 public string Email;
        [ JsonProperty( "id" ) ]                    public int Id;
        [ JsonProperty( "created_utc_timestamp" ) ] public DateTime CreatedUTCTimeStamp;
        [ JsonProperty( "updated_utc_timestamp" ) ] public DateTime UpdateUTCTimeStamp;
    }

}
