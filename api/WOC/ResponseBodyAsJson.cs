namespace WOC;

public class ResponseBodyAsJson
{
    public ResponseBodyAsJson(byte[] fileContents, string fileName, string contentType)
    {
        FileContents = Convert.ToBase64String(fileContents);
        FileName = fileName;
        ContentType = contentType;
    }

    public string FileName { get; set; }
    public string ContentType { get; set; }
    public string FileContents { get; set; }
}