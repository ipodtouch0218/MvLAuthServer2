namespace MvLAuthServer2.Services
{
    public interface IEncryptionService
    {
        string EncryptToBase64(string input);
        string DecryptFromBase64(string input);
    }
}
