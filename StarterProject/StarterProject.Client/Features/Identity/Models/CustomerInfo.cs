namespace StarterProject.Client.Features.Identity.Models
{
    public class CustomerInfo
    {
        public string PartitaIVA { get; set; } = string.Empty;
        public string RagioneSociale { get; set; }
        public string Nome { get; set; }
        public string Cognome { get; set; }
        public string Indirizzo { get; set; }
        public string CAP { get; set; }
        public string Comune { get; set; }
        public string Provincia { get; set; }
        public string Note { get; set; } = string.Empty;

        public string FullName => $"{Nome} {Cognome}";
    }
}
