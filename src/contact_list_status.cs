using Newtonsoft.Json;

namespace ActiveCampaign {
    public struct ContactListStatus {
        [ JsonProperty( "firstName" ) ]   public string FirstName;
        [ JsonProperty( "lastName" ) ]    public string LastName;
        [ JsonProperty( "contact" ) ]     public int Contact;
        [ JsonProperty( "id" ) ]     public int Id;
        [ JsonProperty( "list" ) ]        public int listId;
        [ JsonProperty( "email" ) ]       public string Email;
        [ JsonProperty( "sdate" ) ]       public DateTime SubscribedTimeStamp;
        [ JsonProperty( "udate" ) ]       public DateTime UpdatedTimeStamp;
        [ JsonProperty( "unsubreason" ) ] public string UnsubscribeReason;
        [ JsonProperty( "sourceid" ) ]    public int SourceId;
        [ JsonProperty( "sync" ) ]        public int Sync;
        [ JsonProperty( "responder" ) ]   public int Responder;
    }
}
