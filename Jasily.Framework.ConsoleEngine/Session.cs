using Jasily.Framework.ConsoleEngine.Commands;
using Jasily.Framework.ConsoleEngine.IO;
using Jasily.Framework.ConsoleEngine.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jasily.Framework.ConsoleEngine
{
    public sealed class Session : IOutput, IDisposable
    {
        private readonly List<CommandLine> historys = new List<CommandLine>();

        public Session(JasilyConsoleEngine engine)
        {
            this.Engine = engine;
        }

        public JasilyConsoleEngine Engine { get; }

        public Dictionary<string, object> State { get; } = new Dictionary<string, object>();

        public IApplicationDescription Description { get; set; }

        public void ShowDescription()
        {
            var desc = this.Description;
            if (desc != null)
            {
                this.WriteLine($"{desc.ApplicationName} {desc.Version}");
                this.WriteLine(desc.Copyright);
                this.WriteLine();
                this.WriteLine(desc.Description);
            }
        }

        public void Help()
            => this.Execute(this.Engine.GetCommandMember(z => z.Helper), CommandLine.Empty);

        public void Help(CommandLine commandLine)
            => this.Execute(this.Engine.GetCommandMember(z => z.Helper), commandLine);

        public void Execute(string command)
        {
            var commandLine = new CommandLine(command);
            this.historys.Add(commandLine);
            commandLine.Parse(
                this.Engine.GetCommandMember(z => z.CommandParser),
                this.Engine.GetCommandMember(z => z.CommandParameterParser));

            if (commandLine.CommandBlock == null)
            {
                this.Execute(this.Engine.GetCommandMember(z => z.NoneInput), commandLine);
                this.Execute(this.Engine.GetCommandMember(z => z.Helper), commandLine);
            }
            else
            {
                var mapper = this.GetCommandMapper(commandLine);
                if (mapper != null)
                {
                    this.Execute(mapper, commandLine);
                    return;
                }
                this.Execute(this.Engine.GetCommandMember(z => z.Helper), commandLine);
            }
        }

        public CommandMapper GetCommandMapper(CommandLine commandLine)
        {
            if (commandLine.CommandBlock == null) return null;
            return this.Engine.MapperManager.GetCommand(commandLine);
        }

        private void Execute(CommandMapper mapper, CommandLine command)
        {
            var obj = mapper.CommandClassBuilder.Build();
            var executor = mapper.ExecutorBuilder.CreateExecutor(obj);
            foreach (var kvp in command.Parameters)
            {
                var r = executor.SetParameter(kvp.Key, kvp.Value, this.Engine.MapperManager.GetAgent());
                if (r.HasError)
                {
                    this.WriteLine(r);
                    return;
                }
            }
            var missing = executor.GetMissingParameters().ToArray();
            if (missing.Length != 0)
            {
                this.Engine.GetCommandMember(z => z.MissingParametersFormater)
                    .Format(this.Engine.GetCommandMember(z => z.Output), mapper, missing,
                        this.Engine.GetCommandMember(z => z.CommandParameterParser));
                this.Help(command);
                return;
            }

            executor.Execute(this, command);
        }

        private void Execute(ICommand command, CommandLine commandLine)
        {
            command.Execute(this, commandLine);
        }

        public void Dispose()
        {

        }

        public void Write(string value)
            => this.Engine.GetCommandMember(z => z.Output).Write(value);

        public void WriteLine(string line = null)
            => this.Engine.GetCommandMember(z => z.Output).WriteLine(line ?? string.Empty);
    }
}