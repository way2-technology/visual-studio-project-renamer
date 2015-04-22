using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using NUnit.Framework;

namespace Way2.ProjectRenamer.Tests {
    [TestFixture]
    internal class ProjectRenamerTests {
        private ProjectRenamer _projectRenamer;
        private MockFileSystem _mockFileSystem;

        private const string BasePath = @"C:\foo\bar";
        private const string SlnPath = @"C:\foo\bar\pasta\fake.sln";
        private const string OriginalProjPath = @"C:\foo\bar\pasta\way2.proj1\way2.proj1.csproj";
        private const string NovoProjPath = @"C:\foo\bar\pasta\way2.proj2\way2.proj2.csproj";
        private const string AssemblyInfoPath = @"C:\foo\bar\pasta\way2.proj1\Properties\AssemblyInfo.cs";
        private const string NovoAssemblyInfoPath = @"C:\foo\bar\pasta\way2.proj2\Properties\AssemblyInfo.cs";

        private const string SlnContent = @"
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""way2.proj1"", ""way2.proj1\way2.proj1.csproj"", ""{A9444A77-69FA-43B1-A321-835DDD8D1D8F}""
EndProject
";

        private const string SlnResult = @"
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""way2.proj2"", ""way2.proj2\way2.proj2.csproj"", ""{A9444A77-69FA-43B1-A321-835DDD8D1D8F}""
EndProject
";

        private const string ProjContent = @"
<Project>
<RootNamespace>way2.proj1</RootNamespace>
<AssemblyName>way2.proj1</AssemblyName>
</Project>
";

        private const string ProjResult = @"
<Project>
<RootNamespace>way2.proj2</RootNamespace>
<AssemblyName>way2.proj2</AssemblyName>
</Project>
";

        private const string AssemblyInfoContent = @"
[assembly: AssemblyTitle(""way2.proj1"")]
[assembly: AssemblyDescription("""")]
[assembly: AssemblyProduct(""way2.proj1"")]
";

        private const string AssemblyInfoResult = @"
[assembly: AssemblyTitle(""way2.proj2"")]
[assembly: AssemblyDescription("""")]
[assembly: AssemblyProduct(""way2.proj2"")]
";

        [TestFixtureSetUp]
        public void TestFixtureSetUp() { }

        [TestFixtureTearDown]
        public void TestFixtureTearDown() { }

        [SetUp]
        public void SetUp() {
            _mockFileSystem = new MockFileSystem(null, BasePath);
            _mockFileSystem.AddFile(SlnPath, new MockFileData(SlnContent));
            _mockFileSystem.AddFile(OriginalProjPath, new MockFileData(ProjContent));
            _mockFileSystem.AddFile(AssemblyInfoPath, new MockFileData(AssemblyInfoContent));
            _projectRenamer = new ProjectRenamer(_mockFileSystem);
        }

        private void SetDefaultArgs() {
            SetArgs(@"pasta\way2.proj1", "way2.proj2");
        }

        private void SetArgs(string arg1, string arg2) {
            var args = new[] { arg1, arg2 };
            Assert.IsTrue(_projectRenamer.ParseArgs(args));
        }

        [Test]
        public void ParseArgs_Com2Args_PegaNomeOriginal() {
            var args = new[] { "proj1", "proj2" };
            _projectRenamer.ParseArgs(args);
            Assert.AreEqual("proj1", _projectRenamer.NomeOriginal);
        }


        [Test]
        public void ParseArgs_Com2Args_PegaNomeNovo() {
            var args = new[] { "proj1", "proj2" };
            _projectRenamer.ParseArgs(args);
            Assert.AreEqual("proj2", _projectRenamer.NomeNovo);
        }


        [Test]
        public void ParseArgs_SemReativePath_CreieRelativePath() {
            var args = new[] { "proj1", "proj2" };
            _projectRenamer.ParseArgs(args);
            Assert.AreEqual(@".", _projectRenamer.RelativePath);
        }

        [Test]
        public void ParseArgs_ComFacilReativePath_PegaRelativePath() {
            var args = new[] { @"relpath\proj1", "proj2" };
            _projectRenamer.ParseArgs(args);
            Assert.AreEqual(@"relpath", _projectRenamer.RelativePath);
        }

        [Test]
        public void ParseArgs_ComComplexoReativePath_PegaRelativePath() {
            var args = new[] { @"C:\abc\def\relpath\proj1", "proj2" };
            _projectRenamer.ParseArgs(args);
            Assert.AreEqual(@"C:\abc\def\relpath", _projectRenamer.RelativePath);
        }

        [Test]
        public void ParseArgs_Com2Args_VoltaTrue() {
            var args = new[] { @"proj1", "proj2" };
            Assert.IsTrue(_projectRenamer.ParseArgs(args));
        }

        [Test]
        public void ParseArgs_Com1Args_VoltaTrue() {
            var args = new[] { @"proj1" };
            Assert.IsFalse(_projectRenamer.ParseArgs(args));
        }

        [Test]
        public void VerificaSuposicoes_TodosPassam_EhTrue() {
            SetDefaultArgs();
            Assert.IsTrue(_projectRenamer.VerificaSuposicoes());
        }

        [Test]
        public void VerificaSuposicoes_RelativePathInvalida_EhFalse() {
            SetArgs(@"pasta|errado\proj1", "proj2");
            Assert.IsFalse(_projectRenamer.VerificaSuposicoes());
        }

        [Test]
        public void VerificaSuposicoes_NomeOriginalInvalida_EhFalse() {
            SetArgs(@"pasta\pr|oj1", "proj2");
            Assert.IsFalse(_projectRenamer.VerificaSuposicoes());
        }

        [Test]
        public void VerificaSuposicoes_NomeNovoInvalida_EhFalse() {
            SetArgs(@"pasta\proj1", "pro|j2");
            Assert.IsFalse(_projectRenamer.VerificaSuposicoes());
        }

        [Test]
        public void VerificaSuposicoes_CreieFullPath() {
            SetDefaultArgs();
            _projectRenamer.VerificaSuposicoes();
            Assert.AreEqual(BasePath + @"\pasta", _projectRenamer.FullPath);
        }

        [Test]
        public void VerificaSuposicoes_FullPathNaoExiste_EhFalse() {
            SetArgs(@"outropasta\proj1", "proj2");
            Assert.IsFalse(_projectRenamer.VerificaSuposicoes());
        }

        [Test]
        public void VerificaSuposicoes_SemSolucao_EhFalse() {
            SetDefaultArgs();
            _mockFileSystem.RemoveFile(SlnPath);
            Assert.IsFalse(_projectRenamer.VerificaSuposicoes());
        }

        [Test]
        public void VerificaSuposicoes_ComDoisSolucoes_EhFalse() {
            SetDefaultArgs();
            _mockFileSystem.AddFile(@"C:\foo\bar\pasta\fake2.sln", new MockFileData(""));
            Assert.IsFalse(_projectRenamer.VerificaSuposicoes());
        }

        [Test]
        public void VerificaSuposicoes_ComUmSolucoa_ColocaSlnPath() {
            SetDefaultArgs();
            _projectRenamer.VerificaSuposicoes();
            Assert.AreEqual(SlnPath, _projectRenamer.SlnPath);
        }

        [Test]
        public void VerificaSuposicoes_SemProjetoOriginal_EhFalse() {
            SetDefaultArgs();
            _mockFileSystem.RemoveFile(OriginalProjPath);
            Assert.IsFalse(_projectRenamer.VerificaSuposicoes());
        }

        [Test]
        public void VerificaSuposicoes_ComProjetoOriginal_ColocaOriginalProjPath() {
            SetDefaultArgs();
            _projectRenamer.VerificaSuposicoes();
            Assert.AreEqual(OriginalProjPath, _projectRenamer.OriginalProjPath);
        }

        [Test]
        public void VerificaSuposicoes_ComPastaDoProjetoNovo_EhFalse() {
            SetDefaultArgs();
            _mockFileSystem.AddDirectory(@"C:\foo\bar\pasta\way2.proj2");
            Assert.IsFalse(_projectRenamer.VerificaSuposicoes());
        }

        [Test]
        public void VerificaSuposicoes_ComProjetoNovoExiste_EhFalse() {
            SetDefaultArgs();
            _mockFileSystem.AddFile(NovoProjPath, new MockFileData(""));
            Assert.IsFalse(_projectRenamer.VerificaSuposicoes());
        }

        [Test]
        public void VerificaSuposicoes_ComProjetoNovo_ColocaNovoProjPath() {
            SetDefaultArgs();
            _projectRenamer.VerificaSuposicoes();            
            Assert.AreEqual(NovoProjPath, _projectRenamer.NovoProjPath);
        }

        [Test]
        public void VerificaSuposicoes_SemAssemblyInfo_EhFalse() {
            SetDefaultArgs();
            _mockFileSystem.RemoveFile(AssemblyInfoPath);
            Assert.IsFalse(_projectRenamer.VerificaSuposicoes());
        }

        [Test]
        public void VerificaSuposicoes_ComAssemblyInfo_ColocaAssemblyInfoPath() {
            SetDefaultArgs();
            _projectRenamer.VerificaSuposicoes();
            Assert.AreEqual(AssemblyInfoPath, _projectRenamer.AssemblyInfoPath);
        }

        [Test]
        public void VerificaSuposicoes_FaultaProjDefinicaoNoSln_EhFalse() {
            SetDefaultArgs();
            _mockFileSystem.RemoveFile(SlnPath);
            _mockFileSystem.AddFile(SlnPath, new MockFileData("Project EndProject"));
            Assert.IsFalse(_projectRenamer.VerificaSuposicoes());
        }

        [Test]
        public void VerificaSuposicoes_FaultaRootNamespace_EhFalse() {
            SetDefaultArgs();
            _mockFileSystem.RemoveFile(OriginalProjPath);
            _mockFileSystem.AddFile(OriginalProjPath, new MockFileData("<Project></Project>"));
            Assert.IsFalse(_projectRenamer.VerificaSuposicoes());
        }

        [Test]
        public void VerificaSuposicoes_FaultaAssemblyName_EhFalse() {
            SetDefaultArgs();
            _mockFileSystem.RemoveFile(OriginalProjPath);
            _mockFileSystem.AddFile(OriginalProjPath, new MockFileData("<Project></Project>"));
            Assert.IsFalse(_projectRenamer.VerificaSuposicoes());
        }

        [Test]
        public void VerificaSuposicoes_FaultaAssemblyTitle_EhFalse() {
            SetDefaultArgs();
            _mockFileSystem.RemoveFile(AssemblyInfoPath);
            _mockFileSystem.AddFile(AssemblyInfoPath, new MockFileData("[assembly: AssemblyDescription(\"\")]"));
            Assert.IsFalse(_projectRenamer.VerificaSuposicoes());
        }

        [Test]
        public void VerificaSuposicoes_FaultaProductKey_EhFalse() {
            SetDefaultArgs();
            _mockFileSystem.RemoveFile(AssemblyInfoPath);
            _mockFileSystem.AddFile(AssemblyInfoPath, new MockFileData("[assembly: AssemblyDescription(\"\")]"));
            Assert.IsFalse(_projectRenamer.VerificaSuposicoes());
        }

        [Test]
        public void Renomear_ComSucceso_EhTrue() {
            SetDefaultArgs();
            _projectRenamer.VerificaSuposicoes();
            Assert.IsTrue(_projectRenamer.Renomear());
        }

        [Test]
        public void Renomear_AssemblyInfoFile_ReescreveCerto() {
            SetDefaultArgs();
            _projectRenamer.VerificaSuposicoes();
            _projectRenamer.Renomear();
            Assert.AreEqual(AssemblyInfoResult, _mockFileSystem.File.ReadAllText(NovoAssemblyInfoPath));
        }

        [Test]
        public void Renomear_ProjectFile_ReescreveCerto() {
            SetDefaultArgs();
            _projectRenamer.VerificaSuposicoes();
            _projectRenamer.Renomear();
            Assert.AreEqual(ProjResult, _mockFileSystem.File.ReadAllText(NovoProjPath));
        }

        [Test]
        public void Renomear_ProjectFile_RenomeCerto() {
            SetDefaultArgs();
            _projectRenamer.VerificaSuposicoes();
            _projectRenamer.Renomear();
            Assert.IsTrue(_mockFileSystem.File.Exists(NovoProjPath));
        }

        [Test]
        public void Renomear_ProjectDir_RenomeCerto() {
            SetDefaultArgs();
            _projectRenamer.VerificaSuposicoes();
            _projectRenamer.Renomear();
            Assert.IsTrue(_mockFileSystem.Directory.Exists(@"C:\foo\bar\pasta\way2.proj2"));
        }

        [Test]
        public void Renomear_SlnFile_ReescreveCerto() {
            SetDefaultArgs();
            _projectRenamer.VerificaSuposicoes();
            _projectRenamer.Renomear();
            Assert.AreEqual(SlnResult, _mockFileSystem.File.ReadAllText(SlnPath));
        }
    }
}