namespace WOC;

public class ResponseBodyAsJson(byte[] fileContents, string fileName, string contentType)
{
    public string FileName { get; set; } = fileName;
    public string ContentType { get; set; } = contentType;
    public string FileContents { get; set; } = Convert.ToBase64String((byte[])fileContents);
}