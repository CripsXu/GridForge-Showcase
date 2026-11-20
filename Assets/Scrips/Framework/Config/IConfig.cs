using System.Threading.Tasks;

public interface IConfig
{
    Task InitializeAsync(ResourceManager resource);

    void Dispose();
}