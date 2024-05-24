namespace BuurtApplicatie.Models
{
    public class AnonymousImage
    {
        public string Id { get; set; }
        
        public byte[] Data { get; set; }

        public string PostId { get; set; }
        public AnonymousPost Post { get; set; }
    }
}