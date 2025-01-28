namespace Application.Models.Audits
{
    public class DeepValidation
    {
        public long Id { get; set; }

        /// <summary>
        /// primary key of one of the Ref. tables
        /// </summary>
        public required int EntityId { get; set; }

        /// <summary>
        /// type could be Account Role or Contact
        /// </summary>
        public required string Type { get; set; }

        public required string Reason { get; set; }

        public DateTime CreationDate { get; set; }
    }
}
