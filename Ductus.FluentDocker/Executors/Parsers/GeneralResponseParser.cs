using Ductus.FluentDocker.Model.Containers;

namespace Ductus.FluentDocker.Executors.Parsers
{
    public sealed class GeneralResponseParser : IProcessResponseParser<string>
    {
        public CommandResponse<string> Response { get; private set; }

        public IProcessResponse<string> Process(ProcessExecutionResult response)
        {
            var success = response.ExitCode == 0;
            Response = response.ToResponse(success, response.StdErr, $"ExitCode={response.ExitCode}");

            return this;
        }
    }
}