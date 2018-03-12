﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Yuka.Cli.Util;
using Yuka.IO;
using Yuka.Util;

namespace Yuka.Cli.Commands {
	public class CopyCommand : Command {

		public CopyCommand(CommandParameters parameters) : base(parameters) { }

		public override string Name => "copy";

		public override string[] Description => new[] {
			"Copies files from one place to another while optionally converting them in some way.",
			"\aacopy\a- forms the basis of most other commands."
		};

		public override (string syntax, string description)[] Usage => new[] {
			("source", "Applies the selected conversion too the \abspecified files\a-"),
			("source destination", "Copies all files from \absource\a- to \abdestination\a- while applying the selected conversion"),
			("source destination filter1 filter2 ...", "Copies all files matching at least one \abfilter\a- from \absource\a- to \abdestination\a- while applying the selected conversion"),
			("", "Specify all parameters with flags")
		};

		// reminder: when changing these, also change all inheriting classes
		public override (char shorthand, string name, string fallback, string description)[] Flags => new[] {
			('s', "source", null, "Source location"),
			('d', "destination", null, "Destination location"),
			('f', "format", "keep", "The preferred output format (valid values: \abkeep\a-, \abpacked\a-, \abunpacked\a-)"),
			('r', "raw", null, "Short form of \ac--format=keep\a-, overwrites the format flag if set"),
			('m', "move", "false", "Delete each file after successfully copying it"),
			('o', "overwrite", "false", "Delete existing destination archive/folder"),
			('q', "quiet", null, "Disable user-friendly output"),
			('v', "verbose", null, "Whether to enable detailed output"),
			('w', "wait", null, "Whether to wait after the command finished")
		};

		public override bool Execute() {
			var (sourcePath, destinationPath, filters) = GetPaths();
			var (formatPreference, rawCopy, deleteAfterCopy) = GetCopyModes();

			int fileCount;
			using(var sourceFs = OpenFileSystem(sourcePath)) {
				bool overwriteExisting = Parameters.GetBool("overwrite", 'o', false);
				using(var destinationFs = OpenOrCreateFileSystem(destinationPath, sourceFs is SingleFileSystem, overwriteExisting)) {

					// collect files
					var files = new List<string>();
					foreach(string filter in filters) {
						files.AddRange(sourceFs.GetFiles(filter));
					}

					// Call copy loop
					fileCount = CopyFiles(sourceFs, destinationFs, files.Distinct(), formatPreference, rawCopy, deleteAfterCopy);
				}
			}

			Success($"Successfully copied {fileCount} files");
			Wait(false);
			return true;
		}


		protected virtual (string sourcePath, string destinationPath, string[] filters) GetPaths() {
			string sourcePath, destinationPath;
			string[] filters = { "*.*" };

			switch(Arguments.Length) {
				case 0:
					sourcePath = Parameters.GetString("source", 's', null);
					destinationPath = Parameters.GetString("destination", 'd', sourcePath.WithoutExtension());
					break;
				case 1:
					sourcePath = Arguments[0];
					destinationPath = Parameters.GetString("destination", 'd', sourcePath.WithoutExtension());
					break;
				case 2:
					sourcePath = Arguments[0];
					destinationPath = Arguments[1];
					break;
				default:
					sourcePath = Arguments[0];
					destinationPath = Arguments[1];
					filters = Arguments.Skip(2).ToArray();
					break;
			}

			return (sourcePath, destinationPath, filters);
		}

		protected virtual void CheckPaths(string sourcePath, string destinationPath) {
			if(sourcePath == null) {
				throw new ArgumentNullException(nameof(sourcePath), "No source path specified");
			}
			if(destinationPath == null) {
				throw new ArgumentNullException(nameof(destinationPath), "No destination path specified");
			}
		}

		protected virtual (FormatPreference formatPreference, bool rawCopy, bool deleteAfterCopy) GetCopyModes() {

			string format = Parameters.GetString("format", 'f', "keep").ToLower();
			bool rawCopy = Parameters.GetBool("raw", 'r', false);
			bool deleteAfterCopy = Parameters.GetBool("move", 'm', false);

			switch(format) {
				case "keep":
					return (new FormatPreference(null, FormatType.None), rawCopy: true, deleteAfterCopy);
				case "packed":
					return (new FormatPreference(null, FormatType.Packed), rawCopy, deleteAfterCopy);
				case "unpacked":
					return (new FormatPreference(null, FormatType.Unpacked), rawCopy, deleteAfterCopy);
				default:
					throw new ArgumentOutOfRangeException(nameof(format), format, "Format must be one of the following: keep, packed, unpacked");
			}
		}

		protected virtual int CopyFiles(FileSystem sourceFs, FileSystem destinationFs, IEnumerable<string> files, FormatPreference formatPreference, bool rawCopy, bool deleteAfterCopy) {
			bool verbose = Parameters.GetBool("verbose", 'v', false);
			int fileCount = 0;

			// main loop
			foreach(string file in files) {
				if(rawCopy) {
					if(verbose) Output.WriteLine($"Copying {file}", ConsoleColor.Green);
					using(var sourceStream = sourceFs.OpenFile(file)) {
						using(var destinationStream = destinationFs.CreateFile(file)) {
							sourceStream.CopyTo(destinationStream);
							destinationStream.Flush();
						}
					}
				}
				else {
					// skip auxiliary files (csv, frm, ani, etc...)
					var fileFormat = Format.ForFile(sourceFs, file);
					var fileCategory = fileFormat.GetFileCategory(sourceFs, file);
					if(fileCategory != FileCategory.Primary) {
						if(verbose) Output.WriteLine($"Skipping {file} (file category: {fileCategory})", ConsoleColor.Yellow);
						continue;
					}

					var obj = FileReader.DecodeObject(file, sourceFs);
					if(verbose) Output.WriteLine($"Decoded {file} to {obj.GetType().Name}", ConsoleColor.Green);
					FileWriter.EncodeObject(obj, file, destinationFs, formatPreference);

					if(deleteAfterCopy) {
						// delete auxiliary files (csv, frm, ani, etc...)
						foreach(string secondaryFile in fileFormat.GetSecondaryFiles(sourceFs, file)) {
							if(verbose) Output.WriteLine($"Deleting {secondaryFile}", ConsoleColor.Red);
							sourceFs.DeleteFile(secondaryFile);
						}
						if(verbose) Output.WriteLine($"Deleting {file}", ConsoleColor.Red);
						sourceFs.DeleteFile(file);
					}

					fileCount++;
				}
			}

			return fileCount;
		}

		// TODO move these to somewhere else

		public static FileSystem OpenFileSystem(string path) {
			if(Directory.Exists(path)) {
				return FileSystem.FromFolder(path);
			}

			if(File.Exists(path)) {
				try {
					return FileSystem.FromArchive(path);
				}
				catch(Exception) {
					return FileSystem.FromFile(path);
				}
			}

			throw new FileNotFoundException(path);
		}

		public static FileSystem CreateFileSystem(string path, bool singleFile, bool deleteExisting = false) {
			// TODO YkcFormat
			if(path.EndsWith(".ykc")) {
				if(deleteExisting && File.Exists(path)) File.Delete(path);
				return FileSystem.NewArchive(path);
			}
			if(singleFile) {
				if(deleteExisting && File.Exists(path)) File.Delete(path);
				return FileSystem.NewFile(path);
			}
			if(deleteExisting && Directory.Exists(path)) File.Delete(path);
			return FileSystem.NewFolder(path);
		}

		public static FileSystem OpenOrCreateFileSystem(string path, bool singleFile, bool overwriteExisting) {
			if(overwriteExisting) return CreateFileSystem(path, singleFile, true);

			try {
				return OpenFileSystem(path);
			}
			catch(FileNotFoundException) {
				return CreateFileSystem(path, singleFile);
			}
		}
	}
}