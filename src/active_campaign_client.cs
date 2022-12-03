using Newtonsoft.Json;
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

        public async Task< TagData? > GetTagId( string tagName ) {
            return await GetTagId( tagName, null );
        }

        public async Task< TagData? > GetTagId( string tagName, CancellationToken? cancellationToken ) {
            string query = _url + "/tags?search=" + Uri.EscapeDataString( tagName );

            using var result = await DoGetAsync( query, cancellationToken );
            
            string jsonData = await result.Content.ReadAsStringAsync( );

            var tdr = JsonConvert.DeserializeObject< TagsDataRespone >( jsonData );

            if( tdr.meta.total == 0 )
                return null;

            return tdr.tags[ 0 ];
        }

        public async Task< ContactData[ ] > GetContactsByTagId( int tagId, ContactStatus status ) {
            return await GetContactsByTagId( tagId, status, null, null );
        }

        public async Task< ContactData[ ] > GetContactsByTagId( int tagId, ContactStatus status, DateRange? dateRange ) {
            return await GetContactsByTagId( tagId, status, dateRange, null );
        }

        public async Task< ContactData[ ] > GetContactsByTagId( int tagId, ContactStatus status, CancellationToken? cancellationToken ) {
            return await GetContactsByTagId( tagId, status, null, cancellationToken );
        }

        public async Task< ContactData[ ] > GetContactsByTagId( int tagId, ContactStatus status, DateRange? dateRange, CancellationToken? cancellationToken ) {
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

        public async Task< ContactData > AddContact( string emailAddress, CancellationToken? cancellationToken ) {
            var contact = new {
                email = emailAddress,
            };

            string query = _url + "/contacts";
            string content = "{ \"contact\": " + JsonConvert.SerializeObject( contact ) + "}";

            using var result = await DoPostAsync( query, content, cancellationToken );

            if( !result.IsSuccessStatusCode )
                throw new Exception( $"Error creating a new contact with email address '{emailAddress}'. Reason: {result.ReasonPhrase}" );

            string jsonData = await result.Content.ReadAsStringAsync( );

            var acr = JsonConvert.DeserializeObject< AddContactResponse >( jsonData );

            return acr.contact;
        }

        public async Task< ContactData? > SearchContactByEmailAddress( string emailAddress, CancellationToken? cancellationToken ) {
            string query = _url + "/contacts?email=" + Uri.EscapeDataString( emailAddress );

            using var result = await DoGetAsync( query, cancellationToken );
            
            string jsonData = await result.Content.ReadAsStringAsync( );

            var cdr = JsonConvert.DeserializeObject< ContactDataResponse >( jsonData );

            if( cdr.contacts.Length == 0 )
                return null;

            if( cdr.contacts.Length > 1 )
                throw new Exception( $"More than one result has been found searching for a contact with email address '{emailAddress}'. Received data: {jsonData}" );

            return cdr.contacts[ 0 ];
        }

        public async Task< bool > AddTagToContact( int id, int tagId ) {
            return await AddTagToContact( id, tagId );
        }

        public async Task AddTagToContact( int id, int tagId, CancellationToken? cancellationToken ) {
            var contactTag = new {
                contact = id,
                tag = tagId,
            };

            string query = _url + "/contactTags";
            string content = "{ \"contactTag\": " + JsonConvert.SerializeObject( contactTag ) + "}";

            using var result = await DoPostAsync( query, content, cancellationToken );

            if( !result.IsSuccessStatusCode )
                throw new Exception( $"Error adding tag '{tagId}' to contact '{id}'. Reason: {result.ReasonPhrase}" );
        }

        async Task< HttpResponseMessage > DoGetAsync( string query, CancellationToken? cancellationToken ) {
            try {
                await _semaphore.WaitAsync( );

                await WaitForActiveCampaignAccess( cancellationToken );

                var result = cancellationToken.HasValue ?
                    await _httpClient.GetAsync( query, cancellationToken.Value ) :
                    await _httpClient.GetAsync( query );

                _lastAccessTimeStamp = System.DateTime.Now;

                return result;

            } finally {
                _semaphore.Release( );

            }
        }

        async Task< HttpResponseMessage > DoPostAsync( string query, string content, CancellationToken? cancellationToken ) {
            try {
                await _semaphore.WaitAsync( );

                await WaitForActiveCampaignAccess( cancellationToken );

                using var c = new StringContent( content );

                var result = cancellationToken.HasValue ?
                    await _httpClient.PostAsync( query, c, cancellationToken.Value ) :
                    await _httpClient.PostAsync( query, c );

                _lastAccessTimeStamp = System.DateTime.Now;

                return result;

            } finally {
                _semaphore.Release( );

            }

        }

        async Task WaitForActiveCampaignAccess( CancellationToken? cancellationToken ) {
            var diff = System.DateTime.Now - _lastAccessTimeStamp;

            if( diff.TotalMilliseconds < _delayBetweenQueries ) {
                int remaining = _delayBetweenQueries - ( int )diff.TotalMilliseconds;
                
                if( cancellationToken.HasValue ) {
                    await Task.Delay( remaining, cancellationToken.Value );

                } else {
                    await Task.Delay( remaining );

                }
            }
        }

        struct AddContactResponse {
            public ContactData contact;
        }

        struct ContactDataResponse {
            public string [ ]scoreValues;
            public ContactData [ ]contacts;
        }

        struct TagsDataRespone {
            public struct Meta {
                public int total;
            }

            public TagData [ ]tags;
            public Meta meta;
        }

        // We have to wait 250 ms between each call
        const int _delayBetweenQueries = 251;

        HttpClient _httpClient = new HttpClient( );

        string _url;

        static System.DateTime _lastAccessTimeStamp;
        static SemaphoreSlim _semaphore = new SemaphoreSlim( 1 );
    }
}
