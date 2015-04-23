using System;
using System.IO.Abstractions;
using System.Linq;

namespace Way2.ProjectRenamer {
    public class Program {
        private static void Main(string[] args) {
            var projectRenamer = new ProjectRenamer(new FileSystem());

            var dontAsk = false;
            if (args.Length > 0 && args[0] == "-s") {
                dontAsk = true;
                args = args.Skip(1).ToArray();
            }

            if (!projectRenamer.ParseArgs(args)) {
                PrintUtilizacao();
                return;
            }

            Console.WriteLine("Nome Original: '{0}'", projectRenamer.NomeOriginal);
            Console.WriteLine("Nome Novo    : '{0}'", projectRenamer.NomeNovo);
            Console.WriteLine("Relative Path: '{0}'", projectRenamer.RelativePath);

            if (!dontAsk) {
                Console.WriteLine("Faz um backup antes de renomear. Quer continuar? (s/n)");
                if (Console.ReadLine().Trim() != "s") {
                    return;
                }
            }

            if (!projectRenamer.VerificaSuposicoes()) {
                Console.WriteLine("Falhou verificação das suposições: {0}", projectRenamer.ReasonForFailure);
                return;
            }

            if (!projectRenamer.Renomear()) {
                Console.WriteLine("Falhou durante o processo de renomear. Utiliza o backup.");
                return;
            }

            Console.WriteLine("Sucesso!");
            if (!dontAsk) {
                Console.ReadLine();
            }
        }

        private static void PrintUtilizacao() {
            Console.WriteLine(@"
Utilização:
Way2.ProjectRenamer.exe [Caminho Relativa]\[Nome Original] [Nome Novo]
Nome Original: Nome original do projeto
Nome Novo: Nome novo do projeto
Caminho Relativa (opçional): localizaçaõ do solução

Suposições:
1. [Nome Original] e [Nome Novo] saõ nomes de arquivos validos
2. [Caminho Relativa] existe
3. Tem um arquivo .sln na pasta [Caminho Relativa] ou pasta atual
4. O projeto está localizado em pasta [Caminho Relativa]\[Nome Original]
5. O arquivo do projeto tem nome [Nome Original].proj
6. Existe um arquivo [Caminho Relativa]\[Nome Original]\Properties\AssemblyInfo.cs
7. A solução contem um projeto com nome [Nome Original]
8. A 'RootNamespace' do projeto é [Nome Original]
9. A 'AssemblyName' do projeto é [Nome Original]
10. No AssemblyInfo, a 'AssemblyTitle' é [Nome Original]
11. No AssemblyInfo, a 'AssemblyProduct' é [Nome Original]

Depois de validar as suposições, vai:
1. Renomear a pasta do projeto ao [Nome Novo]
2. Renomear a arquivo do projeto ao [Nome Novo].proj
3. Vai renomear 'RootNamespace', 'AssemblyName', 'AssemblyTitle', e 'AssemblyProduct' ao [Nome Novo]
4. Vai atualizar o nome e pasta no arquivo do solução

Os Namespaces do source code não vai ser renomeado. Faz isso com Resharper depois.
");
        }
    }
}