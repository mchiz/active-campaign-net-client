using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Threading;

namespace ActiveCampaign {
    public class Client : IDisposable {
        public Client( string url, string key ) {
            _url = url + "/api/3";

            _httpClient.DefaultRequestHeaders.Add( "Api-Token", key );
        }

        public void Dispose( ) {
            _httpClient.Dispose( );
        }

        public async Task< CampaignData[ ] > GetAllCampaignsAsync( GetCampaignsOptions options = null, CancellationToken cancellationToken = default ) {
            CampaignData [ ]o = null;

            int processedCampaignsCount = 0;

            do {
                string query = _url + $"/campaigns/?limit=100&offset={processedCampaignsCount}";

                using var result = await DoGetAsync( query, cancellationToken );
            
                string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

                var cdr = JsonConvert.DeserializeObject< GetCampaignsDataResponse >( jsonData, _newtownSoftIgnoreNullValuesSettings );

                if( o == null )
                    o = new CampaignData[ cdr.meta.total ];

                System.Array.Copy( cdr.campaigns, 0, o, processedCampaignsCount, cdr.campaigns.Length );

                processedCampaignsCount += cdr.campaigns.Length;

                if( options != null ) options.RaiseProcessedEvent( processedCampaignsCount, cdr.meta.total );

                if( processedCampaignsCount == cdr.meta.total )
                    break;

            } while( true );
            
            if( o == null )
                return new CampaignData[ ] { };

            return o;
        }

        public async Task< ContactListStatus[ ] > GetContactListsStatusAsync( int contactId, CancellationToken cancellationToken ) {
            string query = _url + $"/contacts/{contactId}/contactLists";

            using var result = await DoGetAsync( query, cancellationToken );
            
            result.EnsureSuccessStatusCode( );

            string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

            var ldr = JsonConvert.DeserializeObject< ContactListStatusResponse >( jsonData, _newtownSoftIgnoreNullValuesSettings );

            return ldr.contactLists;
        }

        public async Task UpdateContactListStatusAsync( int contactId, int listId, ContactStatus status, CancellationToken cancellationToken = default ) {
            var contactList = new {
                sourceid = 0,
                list = listId,
                contact = contactId,
                status = status,
            };

            string query = _url + "/contactLists";
            string content = "{ \"contactList\": " + JsonConvert.SerializeObject( contactList ) + "}";

            using var result = await DoPostAsync( query, content, cancellationToken );

            if( !result.IsSuccessStatusCode )
                throw new UpdateContactListStatusAsync( contactId, listId, status, result.StatusCode, result.ReasonPhrase ?? "" );
        }

        public async Task< ListData? > GetListIdAsync( string listName, CancellationToken cancellationToken = default ) {
            string query = _url + "/lists?filters[name]=" + Uri.EscapeDataString( listName );

            using var result = await DoGetAsync( query, cancellationToken );
            
            string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

            var ldr = JsonConvert.DeserializeObject< ListsDataResponse >( jsonData );

            if( ldr.meta.total >= 1 )
                return ldr.lists[ 0 ];
            
            return null;
        }

        public async Task< ContactData[ ] > GetContactsByListIdAsync( int listId, ContactStatus status, GetContactsOptions options = null, CancellationToken cancellationToken = default ) {
            return await RetreiveContactsAsync( null, status, $"&listid={listId}", options, cancellationToken );
        }

        public async Task< TagData[ ] > ListAllTagsAsync( CancellationToken cancellationToken = default ) {
            TagData [ ]o = null;

            int i = 0;

            do {
                string query = _url + $"/tags?offset={i}";

                using var result = await DoGetAsync( query, cancellationToken );
            
                string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

                var tdr = JsonConvert.DeserializeObject< TagsDataResponse >( jsonData );

                if( o == null ) o = new TagData[ tdr.meta.total ];

                System.Array.Copy( tdr.tags, 0, o, i, tdr.tags.Length );

                i += tdr.tags.Length;

                if( i == o.Length )
                    return o;

            } while( true );
        }

        public async Task< TagData? > GetTagIdAsync( string tagName, CancellationToken cancellationToken = default ) {
            string query = _url + "/tags?search=" + Uri.EscapeDataString( tagName );

            using var result = await DoGetAsync( query, cancellationToken );
            
            string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

            var tdr = JsonConvert.DeserializeObject< TagsDataResponse >( jsonData );

            if( tdr.meta.total == 0 )
                return null;

            return tdr.tags[ 0 ];
        }

        public async Task< ContactData[ ] > GetContactsByTagIdAsync( int tagId, ContactStatus status, GetContactsOptions options = null, CancellationToken cancellationToken = default ) {
            return await GetContactsByTagIdAsync( tagId, status, null, options, cancellationToken );
        }

        public async Task< ContactData[ ] > GetContactsByTagIdAsync( int tagId, ContactStatus status, DateRange? dateRange, GetContactsOptions options = null, CancellationToken cancellationToken = default ) {
            return await RetreiveContactsAsync( dateRange, status, $"&tagid={tagId}", options, cancellationToken );
        }

        public async Task< ContactData > AddContactAsync( string emailAddress, CancellationToken cancellationToken = default ) {
            var contact = new {
                email = emailAddress,
            };

            string query = _url + "/contacts";
            string content = "{ \"contact\": " + JsonConvert.SerializeObject( contact ) + "}";

            using var result = await DoPostAsync( query, content, cancellationToken );

            if( !result.IsSuccessStatusCode )
                throw new AddContactException( emailAddress, result.StatusCode, result.ReasonPhrase ?? "" );

            string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

            var acr = JsonConvert.DeserializeObject< AddOrSyncContactResponse >( jsonData );

            return acr.contact;
        }

        public async Task< ContactData > SyncContactDataAsync( string emailAddress, ContactInputData data, CancellationToken cancellationToken = default ) {
            string firstName =   data.FirstName   != null ? $"\"firstName\":\"{data.FirstName}\"{( data.LastName != null ? "," : "" )}" : "";
            string lastName    = data.LastName    != null ? $"\"lastName\":\"{data.LastName}\"{( data.Phone != null ? "," : "" )}"      : "";
            string phone       = data.Phone       != null ? $"\"phone\":\"{data.Phone}\"{( data.FieldValues != null ? "," : "" )}"      : "";
            string fieldValues = data.FieldValues != null ? $"\"fieldValues\":{JsonConvert.SerializeObject( data.FieldValues )}"         : "";

            string content = $"{{ \"contact\": {{ \"email\":\"{emailAddress}\",{firstName}{lastName}{phone}{fieldValues} }} }}";

            string query = _url + "/contact/sync";

            using var result = await DoPostAsync( query, content, cancellationToken );

            if( !result.IsSuccessStatusCode )
                throw new SyncContactDataException( emailAddress, data, result.StatusCode, result.ReasonPhrase ?? "" );

            string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

            var acr = JsonConvert.DeserializeObject< AddOrSyncContactResponse >( jsonData );

            return acr.contact;
        }

        public async Task< ContactData[ ] > SearchContactByEmailAddressAsync( string emailAddress, CancellationToken cancellationToken = default ) {
            string query = _url + "/contacts?email=" + Uri.EscapeDataString( emailAddress );

            using var result = await DoGetAsync( query, cancellationToken );
            
            if( !result.IsSuccessStatusCode )
                throw new SearchContactByEmailAddressException( emailAddress, result.StatusCode, result.ReasonPhrase ?? "" );

            string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

            var cdr = JsonConvert.DeserializeObject< ContactDataResponse >( jsonData );

            return cdr.contacts;
        }

        public async Task AddTagToContactAsync( int id, int tagId, CancellationToken cancellationToken = default ) {
            var contactTag = new {
                contact = id,
                tag = tagId,
            };

            string query = _url + "/contactTags";
            string content = "{ \"contactTag\": " + JsonConvert.SerializeObject( contactTag ) + "}";

            using var result = await DoPostAsync( query, content, cancellationToken );

            if( !result.IsSuccessStatusCode )
                throw new AddTagToContactException( id, tagId, result.StatusCode, result.ReasonPhrase ?? "" );
        }

        public async Task< bool > RemoveTagFromContactAsync( int id, int tagId, CancellationToken cancellationToken = default ) {
            var contactTag = new {
                contact = id,
                tag = tagId,
            };

            string getTagAssociationIdQuery = _url + $"/contacts/{id}/contactTags";

            using var listResult = await DoGetAsync( getTagAssociationIdQuery, cancellationToken );

            if( !listResult.IsSuccessStatusCode )
                throw new ListTagAssociationException( id, listResult.StatusCode, listResult.ReasonPhrase ?? "" );

            var jsonData = await listResult.Content.ReadAsStringAsync( cancellationToken );

            var gctdr = JsonConvert.DeserializeObject< GetContactTagsDataResponse >( jsonData );

            int index = Array.FindIndex( gctdr.contactTags, p => p.tag == tagId );

            if( index == -1 )
                return false;

            var ta = gctdr.contactTags[ index ];

            string removeTagAssociationQuery = _url + $"/contactTags/{ta.id}";

            using var removeResult = await DoDeleteAsync( removeTagAssociationQuery, cancellationToken );

            if( !removeResult.IsSuccessStatusCode )
                throw new RemoveTagAssociationFromContactException( id, tagId, ta.id, removeResult.StatusCode, removeResult.ReasonPhrase ?? "" );

            return true;
        }

        async Task< ContactData[ ] > RetreiveContactsAsync( DateRange? dateRange, ContactStatus status, string querySuffix, GetContactsOptions options = null, CancellationToken cancellationToken = default ) {
            const int limit = 100;

            string dateFilter = "";

            if( dateRange.HasValue ) {
                var f = dateRange.Value.Start.ToUniversalTime( );
                var t = dateRange.Value.End.ToUniversalTime( );

                string dateBefore = $"{t.Year}/{t.Month}/{t.Day}T{t.Hour}:{t.Minute}:{t.Second}-00:00";
                string dateAfter = $"{f.Year}/{f.Month}/{f.Day}T{f.Hour}:{f.Minute}:{f.Second}-00:00";

                dateFilter = $"&filters[created_before]={Uri.EscapeDataString( dateBefore )}&filters[created_after]={Uri.EscapeDataString( dateAfter )}";
            }

            int offset = 0;
            int totalProcessed = 0;

            var o = new List< ContactData >( );

            async Task Process( ) {
                do {
                    string query = _url + "/contacts" + $"?limit={limit}&offset={offset}" + dateFilter;
                    offset += limit;
            
                    query += $"&status={( int )status}";
                    query += querySuffix;

                    using var result = await DoGetAsync( query, cancellationToken );
                
                    result.EnsureSuccessStatusCode( );

                    string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

                    var cdr = JsonConvert.DeserializeObject< ContactDataResponse >( jsonData );

                    if( cdr.contacts.Length > 0 ) {
                        o.AddRange( cdr.contacts );

                        totalProcessed += cdr.contacts.Length;

                        if( options != null ) options.RaiseProcessedEvent( totalProcessed, cdr.meta.total );
                    }

                    if( cdr.contacts.Length < limit )
                        break;

                } while( true );
            }

            var tasks = new List< Task >( );

            for( int i = 0; i < 6; ++i ) {
                var task = Process( );
                tasks.Add( task );
            }

            await Task.WhenAll( tasks );

            return o.ToArray( );
        }

        async Task< HttpResponseMessage > DoGetAsync( string query, CancellationToken cancellationToken = default ) {
            await _activityLimiter.WaitAsync( cancellationToken );

            return await _httpClient.GetAsync( query, cancellationToken );
        }

        async Task< HttpResponseMessage > DoPostAsync( string query, string content, CancellationToken cancellationToken = default ) {
            await _activityLimiter.WaitAsync( cancellationToken );

            using var c = new StringContent( content );

            return await _httpClient.PostAsync( query, c, cancellationToken );
        }

        async Task< HttpResponseMessage > DoDeleteAsync( string query, CancellationToken cancellationToken = default ) {
            await _activityLimiter.WaitAsync( cancellationToken );

            return await _httpClient.DeleteAsync( query,cancellationToken );
        }

        struct AddOrSyncContactResponse {
            public ContactData contact;
        }

        struct ContactDataResponse {
            public struct Meta {
                public int total;
            }

            public string [ ]scoreValues;
            public ContactData [ ]contacts;
            public Meta meta;
        }

        struct TagsDataResponse {
            public struct Meta {
                public int total;
            }

            public TagData [ ]tags;
            public Meta meta;
        }

        struct ListsDataResponse {
            public struct Meta {
                public int total;
            }

            public ListData [ ]lists; 
            public Meta meta;
        }

        struct ContactListStatusResponse {
            public ContactListStatus [ ] contactLists; 
        }

        struct GetContactTagsDataResponse {
            public struct TagAssociationData {
                public int contact;
                public int tag;
                public int id;
            }

            public TagAssociationData [ ]contactTags;
        }

        struct GetCampaignsDataResponse {
            public struct Meta {
                public int total;
            }

            public CampaignData [ ]campaigns;
            public Meta meta;
        }

        HttpClient _httpClient = new HttpClient( );

        string _url;

        readonly JsonSerializerSettings _newtownSoftIgnoreNullValuesSettings = new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        // ActiveCampaign imposes a limit of 5 requests per second
        static ConcurrentLimitedTimedAccessResourceGuard _activityLimiter = new ConcurrentLimitedTimedAccessResourceGuard( 5, 1000 );
    }
}
