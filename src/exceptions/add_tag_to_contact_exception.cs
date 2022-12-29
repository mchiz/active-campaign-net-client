namespace ActiveCampaign {
    public class AddTagToContactException : Exception {
        public AddTagToContactException( int contactId, int tagId, System.Net.HttpStatusCode httpStatusCode, string httpReasonPhrase ) {
            ContactId = contactId;
            TagId = tagId;
            HttpStatusCode = httpStatusCode;
            HttpReasonPhrase = httpReasonPhrase;
        }

        public int ContactId { get; private set; }
        public int TagId { get; private set; }
        public System.Net.HttpStatusCode HttpStatusCode { get; private set; }
        public string HttpReasonPhrase { get; private set; }

        public override string ToString( ) {
            return $"Error adding tag '{TagId}' to contact '{ContactId}'. StatusCode: {HttpStatusCode} Reason: {HttpReasonPhrase}";
        }
    }
}
