namespace ActiveCampaign {
    public class ListTagAssociationException : Exception {
        public ListTagAssociationException( int contactId, System.Net.HttpStatusCode httpStatusCode, string reasonPhrase ) {
            ContactId = contactId;
            HttpStatusCode = httpStatusCode;
            HttpReasonPhrase = reasonPhrase;
        }

        public int ContactId { get; private set; }
        public System.Net.HttpStatusCode HttpStatusCode { get; private set; }
        public string HttpReasonPhrase { get; private set; }

        public override string ToString( ) {
            return $"Could not list tag associations for contact '{ContactId}'. StatusCode {HttpStatusCode} Reason: {HttpReasonPhrase}";
        }
    }
}
