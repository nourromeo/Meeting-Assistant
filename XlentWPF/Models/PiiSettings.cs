namespace XlentWPF.Models
{
    public class PiiSettings
    {
        public bool RedactPersonnummer { get; set; } = true;
        public bool RedactNames { get; set; } = true;
        public bool RedactEmails { get; set; } = true;
        public bool RedactPhoneNumbers { get; set; } = true;
        public bool RedactAddress { get; set; } = true;
        public bool RedactOrganizations { get; set; } = false; 
    }
}
