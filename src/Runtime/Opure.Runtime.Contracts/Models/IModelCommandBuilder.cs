namespace Opure.Runtime.Contracts.Models;

public interface IModelCommandBuilder
{
    ModelProcessConfiguration Build(string modelPath, ModelRequest request);
}
