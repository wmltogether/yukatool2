﻿using System;
using System.Collections.Generic;
using System.Linq;
using Yuka.Cli.Commands;
using Yuka.Cli.Util;
using Yuka.Util;

namespace Yuka.Cli {
	public abstract class Command {

		public readonly CommandParameters Parameters;
		public readonly string[] Arguments;
		public abstract string Name { get; }
		public abstract string[] Description { get; }

		public abstract (string syntax, string description)[] Usage { get; }
		public abstract (char shorthand, string name, string fallback, string description)[] Flags { get; }

		protected Command(CommandParameters parameters) {
			Parameters = parameters;
			Arguments = parameters.Arguments.Skip(1).ToArray();

			// TODO parse config flags (--config-<key>="value")
			// TODO individual FormatPreferences for different file types (eg. preserve png format in "pack" command)
		}

		public abstract bool Execute();

		#region Helpers

		protected void Wait(bool fallback) {
			if(Parameters.GetBool("wait", 'w', fallback)) {
				Console.ReadLine();
			}
		}

		protected void Log(string text) {
			if(!Parameters.GetBool("quiet", 'q', false) && Parameters.GetBool("verbose", 'v', false)) {
				Output.WriteLineColored(text);
			}
		}

		protected void Success(string text = "Done.") {
			if(!Parameters.GetBool("quiet", 'q', false)) {
				Output.WriteLine(text, ConsoleColor.Green);
			}
		}

		protected void Error(string text) {
			if(!Parameters.GetBool("quiet", 'q', false)) {
				Output.WriteLine(text, ConsoleColor.Red);
			}
		}

		#endregion

		#region Static

		private static readonly Dictionary<string, Func<CommandParameters, Command>> CommandFactories = new Dictionary<string, Func<CommandParameters, Command>> {
			{"help", parameters => new HelpCommand(parameters) },
			{"copy", parameters => new CopyCommand(parameters) },
			{"pack", parameters => new PackCommand(parameters) },
			{"unpack", parameters => new UnpackCommand(parameters) },
		};

		public static Command[] AvailableCommands => CommandFactories.Values.Select(factory => factory(CommandParameters.Empty)).ToArray();

		public static Command CreateFromName(string name, CommandParameters parameters) {
			return CommandFactories.TryGet(name, _ => null)(parameters);
		}

		public static bool TryRun(string[] cliArgs) {
			var commandLine = CommandParameters.ParseArguments(cliArgs);
			if(commandLine.Arguments.Count == 0) return false;

			var command = CreateFromName(commandLine.Arguments[0], commandLine);
			if(command == null) return false;

			command.Execute();
			return true;
		}

		#endregion
	}
}
