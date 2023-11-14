namespace OpenUp.Interpreter.Environment
{
    public interface IEnvironmentOption
    {
        string   Id          { get; }
        string Version { get; }
        string   Name        { get; }
        string[] PrefabPaths { get; }
    }
}