using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveCampaign {
    public struct CampaignData {
        [ JsonProperty( "type" ) ] public string Type;
        [ JsonProperty( "name" ) ] public string Name;
        [ JsonProperty( "sendid" ) ] public int SendId;
        [ JsonProperty( "cdate" ) ] public DateTime CreationDate;
        [ JsonProperty( "sdate" ) ] public DateTime SentDate;
        [ JsonProperty( "send_amt" ) ] public int SendAmt;
        [ JsonProperty( "total_amt" ) ] public int TotalAmt;
        [ JsonProperty( "opens" ) ] public int Opens;
        [ JsonProperty( "uniqueopens" ) ] public int UniqueOpens;
        [ JsonProperty( "linkclicks" ) ] public int LinkClicks;
        [ JsonProperty( "uniquelinkclicks" ) ] public int UniqueLinkClicks;
        [ JsonProperty( "subscriberclicks" ) ] public int SubscriberClicks;
        [ JsonProperty( "unsubscribes" ) ] public int Unsubscribes;
        [ JsonProperty( "id" ) ] public int Id;
        [ JsonProperty( "status" ) ] public CampaignStatus Status;
    }
}
