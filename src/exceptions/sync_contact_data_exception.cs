namespace ActiveCampaign {
    public class SyncContactDataException : Exception {
        public SyncContactDataException( string emailAddress, ContactInputData data, System.Net.HttpStatusCode httpStatusCode, string httpReasonPhrase ) {
            EmailAddress = emailAddress;
            ContactInputData = data;
            HttpStatusCode = httpStatusCode;
            HttpReasonPhrase = httpReasonPhrase;
        }

        public string EmailAddress { get; private set; }
        public ContactInputData ContactInputData { get; private set; }        
        public System.Net.HttpStatusCode HttpStatusCode { get; private set; }
        public string HttpReasonPhrase { get; private set; }

        public override string ToString( ) {
            return $"Error syncing a contact with email address '{EmailAddress}' and data '{ContactInputData.ToString( )}'. StatusCode: {HttpStatusCode} Reason: {HttpReasonPhrase}";
        }
    }
}