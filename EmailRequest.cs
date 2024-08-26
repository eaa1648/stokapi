public class EmailRequest
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public IList<IFormFile> Attachments { get; set; } // New property for attachments
}
