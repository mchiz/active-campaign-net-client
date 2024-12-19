using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ActiveCampaign {
    public struct ContactCustomFieldData {
        [ JsonProperty( "contact" ) ] public int Contact;
        [ JsonProperty( "id" ) ] public int Id;
        [ JsonProperty( "field" ) ] public int FieldId;
        [ JsonProperty( "value" ) ] public string Value;
        [ JsonProperty( "cdate" ) ] public DateTime CreationDate;
        [ JsonProperty( "udate" ) ] public DateTime UpdateDate;

    }
}
