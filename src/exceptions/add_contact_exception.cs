namespace ActiveCampaign {
    public class AddContactException : Exception {
        public AddContactException( string emailAddress, System.Net.HttpStatusCode httpStatusCode, string httpReasonPhrase ) {
            EmailAddress = emailAddress;
            HttpStatusCode = httpStatusCode;
            HttpReasonPhrase = httpReasonPhrase;
        }

        public string EmailAddress { get; private set; }
        public System.Net.HttpStatusCode HttpStatusCode { get; private set; }
        public string HttpReasonPhrase { get; private set; }

        public override string ToString( ) {
            return $"Error creating a new contact with email address '{EmailAddress}'. StatusCode: {HttpStatusCode} Reason: {HttpReasonPhrase}";
        }
    }
}