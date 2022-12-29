namespace ActiveCampaign {
    public class SearchContactByEmailAddressException : Exception {
        public SearchContactByEmailAddressException( string emailAddress, System.Net.HttpStatusCode httpStatusCode, string httpReasonPhrase ) {
            EmailAddress = emailAddress;
            HttpStatusCode = httpStatusCode;
            HttpReasonPhrase = httpReasonPhrase;
        }

        public string EmailAddress { get; private set; }
        public System.Net.HttpStatusCode HttpStatusCode { get; private set; }
        public string HttpReasonPhrase { get;private set; }

        public override string ToString( ) {
            return $"Error while searching a contact with email address '{EmailAddress}'. ";
        }
    }
}
