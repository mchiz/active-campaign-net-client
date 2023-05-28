using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveCampaign {
    public class ContactInputData {
        public struct FieldValue {
            public int Id;
            public string Value;
        }

        [ JsonProperty( "firstName", DefaultValueHandling = DefaultValueHandling.Ignore ) ] public string FirstName;
        [ JsonProperty( "lastName", DefaultValueHandling = DefaultValueHandling.Ignore ) ] public string LastName;
        [ JsonProperty( "phone", DefaultValueHandling = DefaultValueHandling.Ignore ) ] public string Phone;
        [ JsonProperty( "fieldValues", DefaultValueHandling = DefaultValueHandling.Ignore ) ] public FieldValue [ ]FieldValues;
    }
}
