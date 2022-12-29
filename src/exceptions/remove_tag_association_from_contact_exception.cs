using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveCampaign {
    public class RemoveTagAssociationFromContactException : Exception {
        public RemoveTagAssociationFromContactException( int contactId, int tagId, int tagAssociationId, System.Net.HttpStatusCode httpStatusCode, string httpReasonPhrase ) {
            ContactId = contactId;
            TagId = tagId;
            TagAssociationId = tagAssociationId;
            HttpStatusCode = httpStatusCode;
            HttpReasonPhrase = httpReasonPhrase;
        }

        public int ContactId { get; private set; }
        public int TagId { get; private set; }
        public int TagAssociationId { get; private set; }
        public System.Net.HttpStatusCode HttpStatusCode { get; private set; }
        public string HttpReasonPhrase { get; private set; }

        public override string ToString( ) {
            return $"Error removing tag association for contact '{ContactId}'. StatusCode: {HttpStatusCode} Reason: {HttpReasonPhrase}";
        }
    }
}
