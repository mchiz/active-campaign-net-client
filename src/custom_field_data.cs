using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveCampaign {
    public struct CustomFieldData {
        [ JsonProperty( "title" ) ] public string Title;
        [ JsonProperty( "descript" ) ] public string Description;
        [ JsonProperty( "type" ) ] public string Type;
        [ JsonProperty( "perstag" ) ] public string PersistentTag;
        [ JsonProperty( "id" ) ] public int Id;
    }
}
