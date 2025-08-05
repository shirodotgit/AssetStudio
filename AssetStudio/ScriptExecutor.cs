using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace AssetStudio
{
    public class ScriptExecutor
    {
        private ScriptState scriptState;
        private readonly AssetsManager assetsManager;
        private readonly ILogger logger;

        public ScriptExecutor(AssetsManager assetsManager, ILogger logger)
        {
            this.assetsManager = assetsManager;
            this.logger = logger;
        }

        public async Task<ScriptExecutionResult> ExecuteScriptAsync(string scriptPath)
        {
            try
            {
                if (!File.Exists(scriptPath))
                {
                    return new ScriptExecutionResult
                    {
                        Success = false,
                        ErrorMessage = $"Script file not found: {scriptPath}"
                    };
                }

                string scriptContent = File.ReadAllText(scriptPath);
                return await ExecuteScriptContentAsync(scriptContent, scriptPath);
            }
            catch (Exception ex)
            {
                return new ScriptExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Error executing script: {ex.Message}"
                };
            }
        }

        public async Task<ScriptExecutionResult> ExecuteScriptContentAsync(string scriptContent, string scriptName = "Script")
        {
            try
            {
                var scriptOptions = ScriptOptions.Default
                    .AddReferences(typeof(AssetsManager).Assembly)
                    .AddReferences(typeof(System.Console).Assembly)
                    .AddReferences(typeof(System.Collections.Generic.List<>).Assembly)
                    .AddReferences(typeof(System.Linq.Enumerable).Assembly)
                    .AddImports("System")
                    .AddImports("System.Collections.Generic")
                    .AddImports("System.Linq")
                    .AddImports("System.IO")
                    .AddImports("AssetStudio");

                var globals = new ScriptGlobals
                {
                    AssetsManager = assetsManager,
                    Logger = logger,
                    Console = new ScriptConsole(logger)
                };
                scriptState = await CSharpScript.RunAsync(scriptContent, scriptOptions, globals);
                return new ScriptExecutionResult
                {
                    Success = true,
                    ReturnValue = scriptState.ReturnValue,
                    Message = "Script executed successfully"
                };
            }
            catch (CompilationErrorException ex)
            {
                return new ScriptExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Compilation error: {string.Join("\n", ex.Diagnostics)}"
                };
            }
            catch (Exception ex)
            {
                return new ScriptExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Runtime error: {ex.Message}"
                };
            }
        }

        public async Task<ScriptExecutionResult> ContinueScriptAsync(string scriptContent)
        {
            if (scriptState == null)
            {
                return new ScriptExecutionResult
                {
                    Success = false,
                    ErrorMessage = "No previous script state to continue from"
                };
            }

            try
            {
                scriptState = await scriptState.ContinueWithAsync(scriptContent);
                return new ScriptExecutionResult
                {
                    Success = true,
                    ReturnValue = scriptState.ReturnValue,
                    Message = "Script continued successfully"
                };
            }
            catch (Exception ex)
            {
                return new ScriptExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Error continuing script: {ex.Message}"
                };
            }
        }

        public void ResetScriptState()
        {
            scriptState = null;
        }
    }

    public class ScriptGlobals
    {
        public AssetsManager AssetsManager { get; set; }
        public ILogger Logger { get; set; }
        public ScriptConsole Console { get; set; }
    }

    public class ScriptConsole
    {
        private readonly ILogger logger;

        public ScriptConsole(ILogger logger)
        {
            this.logger = logger;
        }

        public void WriteLine(string message)
        {
            logger.Log(LoggerEvent.Info, message);
        }

        public void WriteLine(object obj)
        {
            logger.Log(LoggerEvent.Info, obj?.ToString() ?? "null");
        }

        public void Write(string message)
        {
            logger.Log(LoggerEvent.Info, message);
        }

        public void Write(object obj)
        {
            logger.Log(LoggerEvent.Info, obj?.ToString() ?? "null");
        }
    }

    public class ScriptExecutionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public object ReturnValue { get; set; }
    }
} 