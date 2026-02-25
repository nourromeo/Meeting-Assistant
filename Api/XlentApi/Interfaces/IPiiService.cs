namespace XlentApi.Interfaces
{
    public interface IPiiService
    {
        Task<string> RedactPiiAsync(string text);
        string RedactPiiRegex(string text);
    }
}
