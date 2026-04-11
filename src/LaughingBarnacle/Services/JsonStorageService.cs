namespace LaughingBarnacle.Services;

public class JsonStorageService
{
    public string? JsonContent { get; private set; }
    public string? FileName { get; private set; }

    public void Store(string content, string fileName)
    {
        JsonContent = content;
        FileName = fileName;
    }

    public void Clear()
    {
        JsonContent = null;
        FileName = null;
    }
}
