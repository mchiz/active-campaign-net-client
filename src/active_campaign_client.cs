using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Mail;
using System.Numerics;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading;
using static ActiveCampaign.ContactInputData;

namespace ActiveCampaign {
    public class Client : IDisposable {
        public Client( string url, string key ) {
            Url = url;

            _httpClient.DefaultRequestHeaders.Add( "Api-Token", key );
        }

        public void Dispose( ) {
            _httpClient.Dispose( );
        }

        public Task< CampaignData[ ] > GetAllCampaignsAsync( System.Action< int, int > onProgressCallback = null, CancellationToken cancellationToken = default ) {
            return ListAllElementsAsync< CampaignData, GetCampaignsDataResponse >( "campaigns", onProgressCallback, cancellationToken );
        }

        public async Task< ContactListStatus[ ] > GetContactListsStatusAsync( int contactId, CancellationToken cancellationToken = default ) {
            string query = Url + $"/contacts/{contactId}/contactLists";

            using var result = await DoGetAsync( query, cancellationToken );
            
            result.EnsureSuccessStatusCode( );

            string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

            var ldr = JsonConvert.DeserializeObject< ContactListStatusResponse >( jsonData, _newtownSoftIgnoreNullValuesSettings );

            return ldr.contactLists;
        }

        public async Task< ContactListStatus > UpdateContactListStatusAsync( int contactId, int listId, ContactStatus status, CancellationToken cancellationToken = default ) {
            var contactList = new {
                sourceid = 0,
                list = listId,
                contact = contactId,
                status = status,
            };

            string query = Url + "/contactLists";
            string content = "{ \"contactList\": " + JsonConvert.SerializeObject( contactList ) + "}";

            using var result = await DoPostAsync( query, content, cancellationToken );
            
            string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

            if( !result.IsSuccessStatusCode )
                throw new UpdateContactListStatusAsync( contactId, listId, status, result.StatusCode, result.ReasonPhrase ?? "" );

            var r = JsonConvert.DeserializeObject< UpdateContactResponse >( jsonData, _newtownSoftIgnoreNullValuesSettings );

            return r.contactList;
        }

        public async Task< ListData? > GetListIdAsync( string listName, CancellationToken cancellationToken = default ) {
            string query = Url + "/lists?filters[name]=" + Uri.EscapeDataString( listName );

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

        public async Task< ContactData[ ] > GetContactsByListIdAsync( int listId, DateRange dateRange, ContactStatus status, GetContactsOptions options = null, CancellationToken cancellationToken = default ) {
            return await RetreiveContactsAsync( dateRange, status, $"&listid={listId}", options, cancellationToken );
        }

        public async Task< CustomFieldData[ ] > ListAllCustomFieldsAsync( System.Action< int, int > onProgressCallback = null, CancellationToken cancellationToken = default ) {
            return await ListAllElementsAsync< CustomFieldData, GetCustomFieldsResponse >( "fields", onProgressCallback, cancellationToken );
        }

        public Task< ContactCustomFieldData[ ] > RetreiveAllContactsCustomFieldsValuesAsync( CancellationToken cancellationToken = default ) {
            return RetreiveAllContactsCustomFieldsValuesAsync( null, cancellationToken );
        }

        public async Task< ContactCustomFieldData[ ] > RetreiveAllContactsCustomFieldsValuesAsync( ActiveCampaign.GetContactsOptions options = null, CancellationToken cancellationToken = default ) {
            return await ListAllElementsAsync< ContactCustomFieldData, GetAllContactsCustomFieldsResponse >( "fieldValues", options != null ? options.RaiseProcessedEvent : null, cancellationToken );
        }

        public async Task< ContactCustomFieldData[ ] > RetreiveContactCustomFieldsValuesAsync( int contactId, CancellationToken cancellationToken = default ) {
            string query = Url + $"/contacts/{contactId}/fieldValues";

            using var result = await DoGetAsync( query, cancellationToken );

            string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

            var r = JsonConvert.DeserializeObject< GetContactCustomFieldsResponse >( jsonData );

            return r.fieldValues;
        }

        public Task< ListData[ ] > ListAllListsAsync( System.Action< int, int > onProgressCallback = null, CancellationToken cancellationToken = default ) {
            return ListAllElementsAsync< ListData, ListsDataResponse >( "lists", onProgressCallback, cancellationToken );
        }

        public Task< TagData[ ] > ListAllTagsAsync( System.Action< int, int > onProgressCallback = null, CancellationToken cancellationToken = default ) {
            return ListAllElementsAsync< TagData, TagsDataResponse >( "tags", onProgressCallback, cancellationToken );
        }

        public async Task< TagData? > GetTagIdAsync( string tagName, CancellationToken cancellationToken = default ) {
            string query = Url + "/tags?search=" + Uri.EscapeDataString( tagName );

            using var result = await DoGetAsync( query, cancellationToken );
            
            string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

            var tdr = JsonConvert.DeserializeObject< TagsDataResponse >( jsonData );

            if( tdr.meta.total == 0 )
                return null;

            return tdr.tags[ 0 ];
        }

        public Task< int > GetContactsCountByTagIdAsync( int tagId, ContactStatus status, GetContactsOptions options = null, CancellationToken cancellationToken = default ) {
            return GetContactsCountByTagIdAsync( tagId, status, null, options, cancellationToken );
        }

        public Task< int > GetContactsCountByTagIdAsync( int tagId, ContactStatus status, DateRange? dateRange, GetContactsOptions options = null, CancellationToken cancellationToken = default ) {
            return RetreiveContactsCountAsync( dateRange, status, $"&tagid={tagId}", options, cancellationToken );
        }

        public async Task< ContactData[ ] > GetContactsByTagIdAsync( int tagId, ContactStatus status, GetContactsOptions options = null, CancellationToken cancellationToken = default ) {
            return await GetContactsByTagIdAsync( tagId, status, null, options, cancellationToken );
        }

        public async Task< ContactData[ ] > GetContactsByTagIdAsync( int tagId, ContactStatus status, DateRange? dateRange, GetContactsOptions options = null, CancellationToken cancellationToken = default ) {
            return await RetreiveContactsAsync( dateRange, status, $"&tagid={tagId}", options, cancellationToken );
        }

        public async Task< int[ ] > GetContactTagsAsync( int contactId, CancellationToken cancellationToken = default ) {
            string query = $"{Url}/contacts/{contactId}/contactTags";

            using var result = await DoGetAsync( query, cancellationToken );
            
            //if( !result.IsSuccessStatusCode )
            //    throw new AddContactException( emailAddress, result.StatusCode, result.ReasonPhrase ?? "" );

            string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

            var o = JsonConvert.DeserializeObject< GetContactTagsResponse >( jsonData );

            if( o.contactTags.Length == 0 )
                return new int[ 0 ];

            int [ ]output = new int[ o.contactTags.Length ];
            
            for( int i = 0; i < o.contactTags.Length; ++i ) {
                var c = o.contactTags[ i ];

                output[ i ] = c.tag;
            }

            return output;
        }

        public async Task< ContactData > AddContactAsync( string emailAddress, CancellationToken cancellationToken = default ) {
            var contact = new {
                email = emailAddress,
            };

            string query = Url + "/contacts";
            string content = "{ \"contact\": " + JsonConvert.SerializeObject( contact ) + "}";

            using var result = await DoPostAsync( query, content, cancellationToken );

            if( !result.IsSuccessStatusCode )
                throw new AddContactException( emailAddress, result.StatusCode, result.ReasonPhrase ?? "" );

            string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

            var acr = JsonConvert.DeserializeObject< AddOrSyncContactResponse >( jsonData );

            return acr.contact;
        }

        public async Task DeleteContactAsync( int contactId, CancellationToken cancellationToken = default ) {
            string query = $"{Url}/contacts/{contactId}";

            using var result = await DoDeleteAsync( query, cancellationToken );

            //if( !removeResult.IsSuccessStatusCode )
            //    throw new RemoveTagAssociationFromContactException( id, tagId, ta.id, removeResult.StatusCode, removeResult.ReasonPhrase ?? "" );

        }

        public async Task< ContactData > SyncContactDataAsync( string emailAddress, ContactInputData data, CancellationToken cancellationToken = default ) {
            string firstName =   data.FirstName   != null ? $"\"firstName\":\"{data.FirstName}\"{( data.LastName != null ? "," : "" )}" : "";
            string lastName    = data.LastName    != null ? $"\"lastName\":\"{data.LastName}\"{( data.Phone != null ? "," : "" )}"      : "";
            string phone       = data.Phone       != null ? $"\"phone\":\"{data.Phone}\"{( data.FieldValues != null ? "," : "" )}"      : "";
            string fieldValues = data.FieldValues != null ? $"\"fieldValues\":{JsonConvert.SerializeObject( data.FieldValues )}"         : "";

            string content = $"{{ \"contact\": {{ \"email\":\"{emailAddress}\",{firstName}{lastName}{phone}{fieldValues} }} }}";

            string query = Url + "/contact/sync";

            using var result = await DoPostAsync( query, content, cancellationToken );

            if( !result.IsSuccessStatusCode )
                throw new SyncContactDataException( emailAddress, data, result.StatusCode, result.ReasonPhrase ?? "" );

            string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

            var acr = JsonConvert.DeserializeObject< AddOrSyncContactResponse >( jsonData );

            return acr.contact;
        }

        public async Task< ContactData[ ] > SearchContactByEmailAddressAsync( string emailAddress, CancellationToken cancellationToken = default ) {
            string query = Url + "/contacts?email=" + Uri.EscapeDataString( emailAddress );

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

            string query = Url + "/contactTags";
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

            string getTagAssociationIdQuery = Url + $"/contacts/{id}/contactTags";

            using var listResult = await DoGetAsync( getTagAssociationIdQuery, cancellationToken );

            if( !listResult.IsSuccessStatusCode )
                throw new ListTagAssociationException( id, listResult.StatusCode, listResult.ReasonPhrase ?? "" );

            var jsonData = await listResult.Content.ReadAsStringAsync( cancellationToken );

            var gctdr = JsonConvert.DeserializeObject< GetContactTagsDataResponse >( jsonData );

            int index = Array.FindIndex( gctdr.contactTags, p => p.tag == tagId );

            if( index == -1 )
                return false;

            var ta = gctdr.contactTags[ index ];

            string removeTagAssociationQuery = Url + $"/contactTags/{ta.id}";

            using var removeResult = await DoDeleteAsync( removeTagAssociationQuery, cancellationToken );

            if( !removeResult.IsSuccessStatusCode )
                throw new RemoveTagAssociationFromContactException( id, tagId, ta.id, removeResult.StatusCode, removeResult.ReasonPhrase ?? "" );

            return true;
        }

        async Task< TElement[ ] > ListAllElementsAsync< TElement, TResponseData >( string queryTag, System.Action< int, int > onProgressCallback, CancellationToken cancellationToken = default ) where TResponseData : ResponseListData< TElement > {
            const int limit = 100;

            int offset = 0;
            int totalProcessed = 0;

            TElement[ ]? o = null;

            async Task Process( ) {
                do {
                    int myOffset = offset;
                    offset += limit;

                    string query = $"{Url}/{queryTag}?offset={myOffset}&limit={limit}";
            
                    System.Diagnostics.Debug.WriteLine( query );

                    using var result = await DoGetAsync( query, cancellationToken );

                    result.EnsureSuccessStatusCode( );

                    string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

                    var response = JsonConvert.DeserializeObject< TResponseData >( jsonData );

                    if( response == null || response.Elements.Length == 0 )
                        break;

                    if( o == null ) o = new TElement[ response.Total ];

                    System.Array.Copy( response.Elements, 0, o, myOffset, response.Elements.Length );

                    totalProcessed += response.Elements.Length;

                    onProgressCallback?.Invoke( totalProcessed, response.Total );

                    if( response.Elements.Length < limit )
                        break;

                } while( true );
            }

            var tasks = new List< Task >( );

            for( int i = 0; i < 6; ++i ) {
                var task = Process( );
                tasks.Add( task );
            }

            await Task.WhenAll( tasks );

            if( o == null )
                return new TElement[ 0 ];

            return o;
        }

        async Task< int > RetreiveContactsCountAsync( DateRange? dateRange, ContactStatus status, string querySuffix, GetContactsOptions options = null, CancellationToken cancellationToken = default ) {
            string dateFilter = "";

            if( dateRange.HasValue ) {
                var f = dateRange.Value.Start.ToUniversalTime( );
                var t = dateRange.Value.End.ToUniversalTime( );

                string dateBefore = $"{t.Year}/{t.Month}/{t.Day}T{t.Hour}:{t.Minute}:{t.Second}-00:00";
                string dateAfter = $"{f.Year}/{f.Month}/{f.Day}T{f.Hour}:{f.Minute}:{f.Second}-00:00";

                dateFilter = $"?filters[created_before]={Uri.EscapeDataString( dateBefore )}&filters[created_after]={Uri.EscapeDataString( dateAfter )}";
            }

            string query = Url + $"/contacts{dateFilter}";
            
            query += $"&status={( int )status}";
            query += querySuffix;

            using var result = await DoGetAsync( query, cancellationToken );
                
            result.EnsureSuccessStatusCode( );

            string jsonData = await result.Content.ReadAsStringAsync( cancellationToken );

            var cdr = JsonConvert.DeserializeObject< ContactDataResponse >( jsonData );

            return cdr.meta.total;
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
                    string query = Url + "/contacts" + $"?limit={limit}&offset={offset}" + dateFilter;
                    offset += limit;
            
                    query += $"&status={( int )status}";
                    query += querySuffix;

                    System.Diagnostics.Debug.WriteLine( query );

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

            do {
                try {
                    var result = await _httpClient.GetAsync( query, cancellationToken );

                    if( !result.IsSuccessStatusCode ) {
                        result.Dispose( );

                        await Task.Delay( 1000, cancellationToken );

                        continue;
                    }

                    return result;

                } catch( TaskCanceledException ex ) when ( ex.InnerException is TimeoutException ) {

                } catch( OperationCanceledException ) { 
                    throw;

                }

            } while( true );
        }

        async Task< HttpResponseMessage > DoPostAsync( string query, string content, CancellationToken cancellationToken = default ) {
            await _activityLimiter.WaitAsync( cancellationToken );

            using var c = new StringContent( content );

            do {
                try {
                    var result = await _httpClient.PostAsync( query, c, cancellationToken );

                    if( !result.IsSuccessStatusCode ) {
                        result.Dispose( );

                        await Task.Delay( 1000, cancellationToken );

                        continue;
                    }

                    return result;

                } catch( TaskCanceledException ex ) when ( ex.InnerException is TimeoutException ) {

                } catch( OperationCanceledException ) { 
                    throw;

                }

            } while( true );
        }

        async Task< HttpResponseMessage > DoDeleteAsync( string query, CancellationToken cancellationToken = default ) {
            await _activityLimiter.WaitAsync( cancellationToken );

            do {
                try {
                    var result = await _httpClient.DeleteAsync( query,cancellationToken );

                    if( !result.IsSuccessStatusCode ) {
                        result.Dispose( );

                        await Task.Delay( 1000, cancellationToken );

                        continue;
                    }

                    return result;

                } catch( TaskCanceledException ex ) when ( ex.InnerException is TimeoutException ) {

                } catch( OperationCanceledException ) { 
                    throw;

                }

            } while( true );
        }

        interface ResponseListData< T > {
            int Total { get; }
            T [ ]Elements { get; }
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

        struct TagsDataResponse : ResponseListData< TagData > {
            int ResponseListData< TagData >.Total => meta.total;
            TagData [ ]ResponseListData< TagData >.Elements => tags;

            public struct Meta {
                public int total;
            }

            public TagData [ ]tags;
            public Meta meta;

        }

        struct ListsDataResponse : ResponseListData< ListData > {
            int ResponseListData< ListData >.Total => meta.total;
            ListData [ ]ResponseListData< ListData >.Elements => lists;

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

        struct GetCampaignsDataResponse : ResponseListData< CampaignData > {
            int ResponseListData< CampaignData >.Total => meta.total;
            CampaignData [ ]ResponseListData< CampaignData >.Elements => campaigns;

            public struct Meta {
                public int total;
            }

            public CampaignData [ ]campaigns;
            public Meta meta;
        }

        struct UpdateContactResponse {
            public ContactListStatus contactList; 

        }

        HttpClient _httpClient = new HttpClient( );

        public string Url {
            get => _url;
            set {
                _url = value + "/api/3";
            }
        }
        string _url;

        struct GetCustomFieldsResponse : ResponseListData< CustomFieldData > {
            int ResponseListData< CustomFieldData >.Total => meta.total;
            CustomFieldData [ ]ResponseListData< CustomFieldData >.Elements => fields;

            public struct Meta {
                public int total;
            }

            public CustomFieldData [ ]fields;
            public Meta meta;
        }

        struct GetAllContactsCustomFieldsResponse : ResponseListData< ContactCustomFieldData > {
            int ResponseListData< ContactCustomFieldData >.Total => meta.total;

            ContactCustomFieldData [ ]ResponseListData< ContactCustomFieldData >.Elements => fieldValues;

            public struct Meta {
                public int total;
            }

            public ContactCustomFieldData [ ]fieldValues;
            public Meta meta;
        }

        struct GetContactCustomFieldsResponse {
            public ContactCustomFieldData [ ]fieldValues; 
        }

        struct GetContactTagsResponse {
            public struct Contact {
                public int contact;
                public int tag;
                public DateTime cdate;
                public DateTime created_timestamp;
                public DateTime updated_timestamp;
            }

            public Contact [ ]contactTags;
        }

        public string Key {
            set {
                _httpClient.DefaultRequestHeaders.Remove( "Api-Token" );
                _httpClient.DefaultRequestHeaders.Add( "Api-Token", value );
            }
        }

        readonly JsonSerializerSettings _newtownSoftIgnoreNullValuesSettings = new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        // ActiveCampaign imposes a limit of 5 requests per second
        static ConcurrentLimitedTimedAccessResourceGuard _activityLimiter = new ConcurrentLimitedTimedAccessResourceGuard( 5, 1000 );
    }
}
