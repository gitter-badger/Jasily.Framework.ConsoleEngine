namespace Jasily.Framework.ConsoleEngine.Mappers
{
    public interface IParameterMapper : INameMapper
    {
        bool IsOptional { get; }
    }
}