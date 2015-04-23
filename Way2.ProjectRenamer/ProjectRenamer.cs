using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;

namespace Way2.ProjectRenamer {
    public class ProjectRenamer {
        public string NomeOriginal { get; private set; }
        public string NomeNovo { get; private set; }
        public string RelativePath { get; private set; }

        public string FullPath { get; private set; }
        public string SlnPath { get; private set; }
        public string OriginalProjPath { get; private set; }
        public string NovoProjPath { get; private set; }
        public string AssemblyInfoPath { get; private set; }

        public string ReasonForFailure { get; private set; }

        public const string SlnProjectRegexKey = @"Project\(""{........-....-....-....-............}""\) = ""{nome}"", ""{nome}\\{nome}\.csproj"", ""{........-....-....-....-............}""";
        public const string RootNamespaceKey = "<RootNamespace>{nome}</RootNamespace>";
        public const string AssemblyNameKey = "<AssemblyName>{nome}</AssemblyName>";
        public const string AssemblyTitleKey = "[assembly: AssemblyTitle(\"{nome}\")]";
        public const string AssemblyProductKey = "[assembly: AssemblyProduct(\"{nome}\")]";
        public const string ToReplace = "{nome}";

        private readonly IFileSystem _fileSystem;

        public ProjectRenamer(IFileSystem fileSystem) {
            _fileSystem = fileSystem;
        }

        public bool ParseArgs(string[] args) {
            if (args.Length != 2) {
                return false;
            }

            try {
                var splitPath = args[0].Split('\\');
                NomeOriginal = splitPath.Last();
                NomeNovo = args[1];
                RelativePath = splitPath.Length > 1 ? String.Join("\\", splitPath.Take(splitPath.Length - 1)) : ".";
                return true;
            }
            catch {
                return false;
            }
        }

        public bool VerificaSuposicoes() {
            try {
                VerificaSuposcao(RelativePath.IndexOfAny(Path.GetInvalidPathChars()) == -1, String.Format("Relative path is not valid: {0}", RelativePath));
                VerificaSuposcao(NomeOriginal.IndexOfAny(Path.GetInvalidFileNameChars()) == -1, String.Format("Original project name is not valid: {0}", NomeOriginal));
                VerificaSuposcao(NomeNovo.IndexOfAny(Path.GetInvalidFileNameChars()) == -1, String.Format("New project name is not valid: {0}", NomeNovo));

                FullPath = _fileSystem.Path.GetFullPath(RelativePath);
                VerificaSuposcao(_fileSystem.Directory.Exists(FullPath), String.Format("Solution directory does not exist: {0}", FullPath));

                SlnPath = GetUnicoSln();
                VerificaSuposcao(SlnPath != null, "Solution directory does not contain exactly 1 .sln file.");

                OriginalProjPath = Path.Combine(FullPath, NomeOriginal, NomeOriginal + ".csproj");
                VerificaSuposcao(_fileSystem.File.Exists(OriginalProjPath), String.Format("Project file does not exist: {0}", OriginalProjPath));

                var outputProjectPath = Path.Combine(FullPath, NomeNovo);
                VerificaSuposcao(!_fileSystem.Directory.Exists(outputProjectPath), String.Format("Output project folder already exists: {0}", outputProjectPath));

                NovoProjPath = Path.Combine(FullPath, NomeNovo, NomeNovo + ".csproj");
                VerificaSuposcao(!_fileSystem.File.Exists(NovoProjPath), String.Format("Output project file already exists: {0}", NovoProjPath));

                AssemblyInfoPath = Path.Combine(FullPath, NomeOriginal, "Properties", "AssemblyInfo.cs");
                VerificaSuposcao(_fileSystem.File.Exists(AssemblyInfoPath), String.Format("AssemblyInfo file does not exist: {0}", AssemblyInfoPath));

                VerificaSuposcao(FileContainsRegex(SlnPath, SlnProjectRegexKey.Replace(ToReplace, Regex.Escape(NomeOriginal))), String.Format("Solution file does not reference project: {0}", SlnPath));

                VerificaSuposcao(FileContainsText(OriginalProjPath, RootNamespaceKey.Replace(ToReplace, NomeOriginal)), String.Format("Project file has an unexpected RootNamespace: {0}", OriginalProjPath));
                VerificaSuposcao(FileContainsText(OriginalProjPath, AssemblyNameKey.Replace(ToReplace, NomeOriginal)), String.Format("Project file has an unexpected AssemblyName: {0}", OriginalProjPath));

                VerificaSuposcao(FileContainsText(AssemblyInfoPath, AssemblyTitleKey.Replace(ToReplace, NomeOriginal)), String.Format("AssemblyInfo file has an unexpected AssemblyTitle: {0}", AssemblyInfoPath));
                VerificaSuposcao(FileContainsText(AssemblyInfoPath, AssemblyProductKey.Replace(ToReplace, NomeOriginal)), String.Format("AssemblyInfo file has an unexpected AssemblyProject: {0}", AssemblyInfoPath));
                return true;
            }
            catch {
                return false;
            }
        }

        private void VerificaSuposcao(bool suposcao, string message) {
            if (!suposcao) {
                ReasonForFailure = message;
                throw new InvalidOperationException(message);
            }
        }

        private bool FileContainsRegex(string file, string regexStr) {
            var alltext = _fileSystem.File.ReadAllText(file);
            var regex = new Regex(regexStr);
            return regex.IsMatch(alltext);
        }

        private bool FileContainsText(string file, string text) {
            var alltext = _fileSystem.File.ReadAllText(file);
            return alltext.Contains(text);
        }

        private string GetUnicoSln() {
            var files = _fileSystem.Directory.GetFiles(FullPath, "*.sln");
            if (files.Length != 1) {
                return null;
            }
            return files[0];
        }

        public bool Renomear() {
            try {

                ReplaceTextInFile(AssemblyInfoPath, AssemblyTitleKey.Replace(ToReplace, NomeOriginal), AssemblyTitleKey.Replace(ToReplace, NomeNovo));
                ReplaceTextInFile(AssemblyInfoPath, AssemblyProductKey.Replace(ToReplace, NomeOriginal), AssemblyProductKey.Replace(ToReplace, NomeNovo));

                ReplaceTextInFile(OriginalProjPath, RootNamespaceKey.Replace(ToReplace, NomeOriginal), RootNamespaceKey.Replace(ToReplace, NomeNovo));
                ReplaceTextInFile(OriginalProjPath, AssemblyNameKey.Replace(ToReplace, NomeOriginal), AssemblyNameKey.Replace(ToReplace, NomeNovo));

                var tempProjPath = Path.Combine(FullPath, NomeOriginal, NomeNovo + ".csproj");
                _fileSystem.File.Move(OriginalProjPath, tempProjPath);

                var fromProjDir = Path.Combine(FullPath, NomeOriginal);
                var toProjDir = Path.Combine(FullPath, NomeNovo);
                _fileSystem.Directory.Move(fromProjDir, toProjDir);

                ReplaceRegexInFile(SlnPath, SlnProjectRegexKey.Replace(ToReplace, Regex.Escape(NomeOriginal)), match => match.Value.Replace(NomeOriginal, NomeNovo));

                return true;
            }
            catch {
                return false;
            }
        }

        private void ReplaceTextInFile(string file, string oldText, string newText) {
            var alltext = _fileSystem.File.ReadAllText(file);
            _fileSystem.File.WriteAllText(file, alltext.Replace(oldText, newText));
        }

        private void ReplaceRegexInFile(string file, string oldRegexStr, MatchEvaluator matchEvaluator) {
            var alltext = _fileSystem.File.ReadAllText(file);
            var regex = new Regex(oldRegexStr);
            _fileSystem.File.WriteAllText(file, regex.Replace(alltext, matchEvaluator));
        }
    }
}