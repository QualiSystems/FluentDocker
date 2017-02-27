namespace Ductus.FluentDocker.Model.Builders
{
    public sealed class ExecCommand
    {
        public string Command { get; set; }
        public string Options { get; set; } = "";
        public string Args { get; set; } = "";
        public bool ThrowError { get; set; } = true;
    }
}