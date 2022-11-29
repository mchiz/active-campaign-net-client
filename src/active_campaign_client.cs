using Newtonsoft.Json;

namespace ActiveCampaign {
    public class Client : IDisposable {
        public enum ContactStatus {
            Any = -1,
            Unconfirmed = 0,
            Active = 1,
            Unsubscribed = 2,
            Bounced = 3,
        }

        public struct ContactData {
            [ JsonProperty( "email" ) ] public string Email;
            [ JsonProperty( "id" ) ]    public int Id;
        }

        public struct TagData {
            [ JsonProperty( "tagType" ) ]          public string TagType;
            [ JsonProperty( "description" ) ]      public string Description;
            [ JsonProperty( "id" ) ]               public int Id;
            [ JsonProperty( "subscriber_count" ) ] public int SubscriberCount;
        }

        public struct DateRange {
            public DateRange( DateTime start, DateTime end ) {
                if( start > end )
                    throw new ArgumentException( "The start date cannot be set after end date", "start" );

                _start = start;
                _end = end;
            }
            
            public DateTime Start { get => _start; }
            public DateTime End { get => _end; }

            DateTime _start;
            DateTime _end;
        }

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

            await Wait( );

            using var result = cancellationToken.HasValue ?
                await _httpClient.GetAsync( query, cancellationToken.Value ) :
                await _httpClient.GetAsync( query );

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

                await Wait( );

                using var result = cancellationToken.HasValue ?
                    await _httpClient.GetAsync( query, cancellationToken.Value ) :
                    await _httpClient.GetAsync( query );

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

            var json = "{ \"contact\": " + JsonConvert.SerializeObject( contact ) + "}";
            using var content = new StringContent( json );

            await Wait( );

            string query = _url + "/contacts";

            using var result = cancellationToken.HasValue ?
                await _httpClient.PostAsync( query, content, cancellationToken.Value ) :
                await _httpClient.PostAsync( query, content );

            if( !result.IsSuccessStatusCode )
                throw new Exception( $"Error creating a new contact with email address '{emailAddress}'. Reason: {result.ReasonPhrase}" );

            string jsonData = await result.Content.ReadAsStringAsync( );

            var acr = JsonConvert.DeserializeObject< AddContactResponse >( jsonData );

            return acr.contact;
        }

        public async Task< ContactData? > SearchContactByEmailAddress( string emailAddress, CancellationToken? cancellationToken ) {
            string query = _url + "/contacts?email=" + Uri.EscapeDataString( emailAddress );

            await Wait( );

            using var result = cancellationToken.HasValue ?
                await _httpClient.GetAsync( query, cancellationToken.Value ) :
                await _httpClient.GetAsync( query );

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

            var json = "{ \"contactTag\": " + JsonConvert.SerializeObject( contactTag ) + "}";
            using var content = new StringContent( json );

            await Wait( );

            string query = _url + "/contactTags";

            using var result = cancellationToken.HasValue ?
                await _httpClient.PostAsync( query, content, cancellationToken.Value ) :
                await _httpClient.PostAsync( query, content );

            if( !result.IsSuccessStatusCode )
                throw new Exception( $"Error adding tag '{tagId}' to contact '{id}'. Reason: {result.ReasonPhrase}" );
        }

        async Task Wait( ) {
            var diff = System.DateTime.Now - _lastAccessTimeStamp;

            if( diff.TotalMilliseconds < _delayBetweenQueries ) {
                int remaining = _delayBetweenQueries - ( int )diff.TotalMilliseconds;
                await Task.Delay( remaining );
            }

            _lastAccessTimeStamp = System.DateTime.Now;
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

        System.DateTime _lastAccessTimeStamp;
        HttpClient _httpClient = new HttpClient( );

        string _url;
    }
}
