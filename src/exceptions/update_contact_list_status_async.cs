namespace ActiveCampaign {
    public class UpdateContactListStatusAsync : Exception {
        public UpdateContactListStatusAsync( int contactId, int listId, ContactStatus status, System.Net.HttpStatusCode httpStatusCode, string httpReasonPhrase ) {
            ContactId = contactId;
            ListId = listId;
            Status = status;
            HttpStatusCode = httpStatusCode;
            HttpReasonPhrase = httpReasonPhrase;
        }

        public int ContactId  { get; private set; }
        public int ListId  { get; private set; }
        public ContactStatus Status  { get; private set; }
        public System.Net.HttpStatusCode HttpStatusCode { get; private set; }
        public string HttpReasonPhrase { get; private set; }

        public override string ToString( ) {
            return $"Error updating contact list status for contact id '{ContactId}, list id {ListId} and status {Status.ToString( )}'. StatusCode: {HttpStatusCode} Reason: {HttpReasonPhrase}";
        }
    }
}