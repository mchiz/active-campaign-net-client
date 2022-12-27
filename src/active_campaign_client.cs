using ActiveCampaignNetClient;
using Newtonsoft.Json;
using System.Net.Mail;
using System.Threading;
using System.Xml.XPath;

namespace ActiveCampaign {
    public class Client : IDisposable {
        public Client( string url, string key ) {
            _url = url + "/api/3";

            _httpClient.DefaultRequestHeaders.Add( "Api-Token", key );
        }

        public void Dispose( ) {
            _httpClient.Dispose( );
        }

        public async Task< TagData? > GetTagId( string tagName, CancellationToken cancellationToken = default ) {
            string query = _url + "/tags?search=" + Uri.EscapeDataString( tagName );

            using var result = await DoGetAsync( query, cancellationToken );
            
            string jsonData = await result.Content.ReadAsStringAsync( );

            var tdr = JsonConvert.DeserializeObject< TagsDataResponse >( jsonData );

            if( tdr.meta.total == 0 )
                return null;

            return tdr.tags[ 0 ];
        }

        public async Task< ContactData[ ] > GetContactsByTagId( int tagId, ContactStatus status, CancellationToken cancellationToken = default ) {
            return await GetContactsByTagId( tagId, status, null, cancellationToken );
        }

        public async Task< ContactData[ ] > GetContactsByTagId( int tagId, ContactStatus status, DateRange? dateRange, CancellationToken cancellationToken = default ) {
            const int limit = 100;

            string dateFilter = "";

            if( dateRange.HasValue ) {
                var f = dateRange.Value.Start;
                var t = dateRange.Value.End;

                string dateBefore = $"{t.Year}/{t.Month}/{t.Day}T{t.Hour}:{t.Minute}:{t.Second}-00:00";
                string dateAfter = $"{f.Year}/{f.Month}/{f.Day}T{f.Hour}:{f.Minute}:{f.Second}-00:00";

                dateFilter = $"&filters[created_before]={Uri.EscapeDataString( dateBefore )}&filters[created_after]={Uri.EscapeDataString( dateAfter )}";
            }

            int offset = 0;

            var o = new List< ContactData >( );

            do {
                string query = _url + "/contacts" + $"?limit={limit}&offset={offset}" + dateFilter;
            
                query += $"&tagid={tagId}";
                query += $"&status={( int )status}";

                using var result = await DoGetAsync( query, cancellationToken );
            
                string jsonData = await result.Content.ReadAsStringAsync( );

                var cdr = JsonConvert.DeserializeObject< ContactDataResponse >( jsonData );

                foreach( var cd in cdr.contacts )
                    o.Add( cd );

                if( cdr.contacts.Length < limit )
                    break;

                offset += limit;

            } while( true );

            return o.ToArray( );
        }

        public async Task< ContactData > AddContact( string emailAddress, CancellationToken cancellationToken = default ) {
            var contact = new {
                email = emailAddress,
            };

            string query = _url + "/contacts";
            string content = "{ \"contact\": " + JsonConvert.SerializeObject( contact ) + "}";

            using var result = await DoPostAsync( query, content, cancellationToken );

            if( !result.IsSuccessStatusCode )
                throw new AddContactException( emailAddress, result.StatusCode, result.ReasonPhrase ?? "" );

            string jsonData = await result.Content.ReadAsStringAsync( );

            var acr = JsonConvert.DeserializeObject< AddContactResponse >( jsonData );

            return acr.contact;
        }

        public async Task< ContactData[ ] > SearchContactByEmailAddress( string emailAddress, CancellationToken cancellationToken = default ) {
            string query = _url + "/contacts?email=" + Uri.EscapeDataString( emailAddress );

            using var result = await DoGetAsync( query, cancellationToken );
            
            if( !result.IsSuccessStatusCode )
                throw new SearchContactByEmailAddressException( emailAddress, result.StatusCode, result.ReasonPhrase ?? "" );

            string jsonData = await result.Content.ReadAsStringAsync( );

            var cdr = JsonConvert.DeserializeObject< ContactDataResponse >( jsonData );

            return cdr.contacts;
        }

        public async Task AddTagToContact( int id, int tagId, CancellationToken cancellationToken = default ) {
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

        public async Task< bool > RemoveTagFromContact( int id, int tagId, CancellationToken cancellationToken = default ) {
            var contactTag = new {
                contact = id,
                tag = tagId,
            };

            string getTagAssociationIdQuery = _url + $"/contacts/{id}/contactTags";

            using var listResult = await DoGetAsync( getTagAssociationIdQuery, cancellationToken );

            if( !listResult.IsSuccessStatusCode )
                throw new ListTagAssociationException( id, listResult.StatusCode, listResult.ReasonPhrase ?? "" );

            var jsonData = await listResult.Content.ReadAsStringAsync( );

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

        async Task< HttpResponseMessage > DoGetAsync( string query, CancellationToken cancellationToken = default ) {
            try {
                await _semaphore.WaitAsync( );

                await WaitForActiveCampaignAccess( cancellationToken );

                var result = await _httpClient.GetAsync( query, cancellationToken );

                _lastAccessTimeStamp = System.DateTime.Now;

                return result;

            } finally {
                _semaphore.Release( );

            }
        }

        async Task< HttpResponseMessage > DoPostAsync( string query, string content, CancellationToken cancellationToken = default ) {
            try {
                await _semaphore.WaitAsync( );

                await WaitForActiveCampaignAccess( cancellationToken );

                using var c = new StringContent( content );

                var result = await _httpClient.PostAsync( query, c, cancellationToken );

                _lastAccessTimeStamp = System.DateTime.Now;

                return result;

            } finally {
                _semaphore.Release( );

            }

        }

        async Task< HttpResponseMessage > DoDeleteAsync( string query, CancellationToken cancellationToken = default ) {
            try {
                await _semaphore.WaitAsync( );

                await WaitForActiveCampaignAccess( cancellationToken );

                var result = await _httpClient.DeleteAsync( query,cancellationToken );

                _lastAccessTimeStamp = System.DateTime.Now;

                return result;

            } finally {
                _semaphore.Release( );

            }

        }

        async Task WaitForActiveCampaignAccess( CancellationToken cancellationToken ) {
            var diff = System.DateTime.Now - _lastAccessTimeStamp;

            if( diff.TotalMilliseconds < _delayBetweenQueries ) {
                int remaining = _delayBetweenQueries - ( int )diff.TotalMilliseconds;
                
                await Task.Delay( remaining, cancellationToken );
            }
        }

        struct AddContactResponse {
            public ContactData contact;
        }

        struct ContactDataResponse {
            public string [ ]scoreValues;
            public ContactData [ ]contacts;
        }

        struct TagsDataResponse {
            public struct Meta {
                public int total;
            }

            public TagData [ ]tags;
            public Meta meta;
        }

        struct GetContactTagsDataResponse {
            public struct TagAssociationData {
                public int contact;
                public int tag;
                public int id;
            }

            public TagAssociationData [ ]contactTags;
        }

        // ActiveCampaign imposes a limit of 5 requests per second
        // This is expressed in milliseconds
        const int _delayBetweenQueries = 1000 / 5 + 1;

        HttpClient _httpClient = new HttpClient( );

        string _url;

        static System.DateTime _lastAccessTimeStamp;
        static SemaphoreSlim _semaphore = new SemaphoreSlim( 1 );
    }
}
