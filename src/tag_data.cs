using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveCampaign {
    public struct TagData {
        [ JsonProperty( "tagType" ) ]          public string TagType;
        [ JsonProperty( "description" ) ]      public string Description;
        [ JsonProperty( "id" ) ]               public int Id;
        [ JsonProperty( "subscriber_count" ) ] public int SubscriberCount;
    }

}
