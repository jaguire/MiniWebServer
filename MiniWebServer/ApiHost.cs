using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using Nano.Web.Core;
using Serilog;

namespace MiniWebServer
{
    public static class ApiHost
    {
        public static void Init(NanoConfiguration config, string webRoot)
        {
            InitApiExplorer(webRoot);

            var apiPath = $"{webRoot}\\Api";
            if (!Directory.Exists(apiPath))
                return;

            var source = Directory.GetFiles(apiPath, "*.cs");
            var assembly = Compile(source);
            if (assembly == null)
                return;

            foreach (var module in assembly.GetModules())
                foreach (var type in module.GetTypes())
                {
                    config.AddMethods(type);
                    Log.Information("API: {0}", type.Name);
                    foreach (var method in type.GetMethods().TakeWhile(x => x.IsPublic && x.IsStatic))
                        Log.Information("  /api/{0}/{1}", type.Name, method.Name);
                }
        }

        private static void InitApiExplorer(string webRoot)
        {
            var apiExplorerPath = $"{webRoot}\\ApiExplorer";
            var apiExplorerFile = $"{apiExplorerPath}\\index.html";
            if (File.Exists(apiExplorerFile))
                return;

            Log.Information("Creating {apiExplorerFile}", apiExplorerFile);
            var html = GetResource("ApiExplorer.html");
            Directory.CreateDirectory(apiExplorerPath);
            File.WriteAllText(apiExplorerFile, html);
        }

        private static string GetResource(string name)
        {
            var assembly = typeof(Program).Assembly;
            using (var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{name}"))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        private static Assembly Compile(string[] files)
        {
            var parameters = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
                TreatWarningsAsErrors = false,
                CompilerOptions = "/optimize"
            };

            parameters.ReferencedAssemblies.AddRange(new[]
            {
                "Microsoft.CSharp.dll",
                "System.dll",
                "System.Core.dll",
                "MiniWebServer.exe"
            });
            var pattern = new Regex(@"\/\/ ?ref (.+\.(dll|exe))", RegexOptions.Compiled);
            var references = files.SelectMany(File.ReadAllLines)
                                  .Select(x => pattern.Match(x)?.Groups[1].Value)
                                  .Where(x => !string.IsNullOrWhiteSpace(x))
                                  .ToArray();
            parameters.ReferencedAssemblies.AddRange(references);

            var provider = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });
            var results = provider.CompileAssemblyFromFile(parameters, files);

            if (!results.Errors.HasErrors)
                return results.CompiledAssembly;

            var text = results.Errors.Cast<CompilerError>()
                              .Aggregate("Compiler error: ", (current, ce) => current + $"\r\n  {Path.GetFileName(ce.FileName)}({ce.Line}) : {ce.ErrorText}");
            Log.Error(text);
            return null;
        }
    }
}