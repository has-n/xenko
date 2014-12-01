﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

using SiliconStudio.Core;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Storage;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Shaders.Parser;
using SiliconStudio.Shaders.Utility;
using Encoding = System.Text.Encoding;
using LoggerResult = SiliconStudio.Core.Diagnostics.LoggerResult;

namespace SiliconStudio.Paradox.Shaders.Compiler
{
    /// <summary>
    /// An <see cref="IEffectCompiler"/> which will compile effect into multiple shader code, and compile them with a <see cref="IShaderCompiler"/>.
    /// </summary>
    public class EffectCompiler : EffectCompilerBase
    {
        private bool d3dCompilerLoaded = false;
        private static readonly Object WriterLock = new Object();

        private ShaderMixinParser shaderMixinParser;

        private readonly object shaderMixinParserLock = new object();

        public List<string> SourceDirectories { get; private set; }

        public Dictionary<string, string> UrlToFilePath { get; private set; }

        public EffectCompiler()
        {
            NativeLibrary.PreloadLibrary("d3dcompiler_47.dll");
            SourceDirectories = new List<string>();
            UrlToFilePath = new Dictionary<string, string>();
        }

        public override ObjectId GetShaderSourceHash(string type)
        {
            return GetMixinParser().SourceManager.GetShaderSourceHash(type);
        }

        /// <summary>
        /// Remove cached files for modified shaders
        /// </summary>
        /// <param name="modifiedShaders"></param>
        public override void ResetCache(HashSet<string> modifiedShaders)
        {
            GetMixinParser().DeleteObsoleteCache(modifiedShaders);
        }

        private ShaderMixinParser GetMixinParser()
        {
            lock (shaderMixinParserLock)
            {
                // Generate the AST from the mixin description
                if (shaderMixinParser == null)
                {
                    shaderMixinParser = new ShaderMixinParser();
                    shaderMixinParser.SourceManager.LookupDirectoryList = SourceDirectories; // TODO: temp
                    shaderMixinParser.SourceManager.UrlToFilePath = UrlToFilePath; // TODO: temp
                }
                return shaderMixinParser;
            }
        }

        public override EffectBytecode Compile(ShaderMixinSourceTree mixinTree, CompilerParameters compilerParameters, LoggerResult log)
        {
            // Load D3D compiler dll
            // Note: No lock, it's probably fine if it gets called from multiple threads at the same time.
            if (Platform.IsWindowsDesktop && !d3dCompilerLoaded)
            {
                NativeLibrary.PreloadLibrary("d3dcompiler_47.dll");
                d3dCompilerLoaded = true;
            }

            var shaderMixinSource = mixinTree.Mixin;
            var fullEffectName = mixinTree.GetFullName();
            var usedParameters = mixinTree.UsedParameters;

            // Make a copy of shaderMixinSource. Use deep clone since shaderMixinSource can be altered during compilation (e.g. macros)
            var shaderMixinSourceCopy = new ShaderMixinSource();
            shaderMixinSourceCopy.DeepCloneFrom(shaderMixinSource);
            shaderMixinSource = shaderMixinSourceCopy;

            // Generate platform-specific macros
            var platform = usedParameters.Get(CompilerParameters.GraphicsPlatformKey);
            switch (platform)
            {
                case GraphicsPlatform.Direct3D11:
                    shaderMixinSource.AddMacro("SILICONSTUDIO_PARADOX_GRAPHICS_API_DIRECT3D", 1);
                    shaderMixinSource.AddMacro("SILICONSTUDIO_PARADOX_GRAPHICS_API_DIRECT3D11", 1);
                    break;
                case GraphicsPlatform.OpenGL:
                    shaderMixinSource.AddMacro("SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGL", 1);
                    shaderMixinSource.AddMacro("SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLCORE", 1);
                    break;
                case GraphicsPlatform.OpenGLES:
                    shaderMixinSource.AddMacro("SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGL", 1);
                    shaderMixinSource.AddMacro("SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES", 1);
                    break;
                default:
                    throw new NotSupportedException();
            }

            var parsingResult = GetMixinParser().Parse(shaderMixinSource, shaderMixinSource.Macros.ToArray());

            // Copy log from parser results to output
            CopyLogs(parsingResult, log);

            // Return directly if there are any errors
            if (parsingResult.HasErrors)
            {
                return null;
            }

            // Convert the AST to HLSL
            var writer = new SiliconStudio.Shaders.Writer.Hlsl.HlslWriter {EnablePreprocessorLine = false};
            writer.Visit(parsingResult.Shader);
            var shaderSourceText = writer.Text;

            if (string.IsNullOrEmpty(shaderSourceText))
            {
                log.Error("No code generated for effect [{0}]", fullEffectName);
                return null;
            }

            // -------------------------------------------------------
            // Save shader log
            // TODO: TEMP code to allow debugging generated shaders on Windows Desktop
#if SILICONSTUDIO_PLATFORM_WINDOWS_DESKTOP
            var shaderId = ObjectId.FromBytes(Encoding.UTF8.GetBytes(shaderSourceText));

            var logDir = "log";
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            var shaderSourceFilename = Path.Combine(logDir, "shader_" +  fullEffectName.Replace('.', '_') + "_" + shaderId + ".hlsl");
            lock (WriterLock) // protect write in case the same shader is created twice
            {
                if (!File.Exists(shaderSourceFilename))
                {
                    var builder = new StringBuilder();
                    builder.AppendLine("/***** Used Parameters *****");
                    builder.Append(" * EffectName: ");
                    builder.AppendLine(fullEffectName ?? "");
                    WriteParameters(builder, usedParameters, 0, false);
                    builder.AppendLine(" ***************************/");
                    builder.Append(shaderSourceText);
                    File.WriteAllText(shaderSourceFilename, builder.ToString());
                }
            }
#endif
            // -------------------------------------------------------

            var bytecode = new EffectBytecode { Reflection = parsingResult.Reflection, HashSources = parsingResult.HashSources };

            // Select the correct backend compiler
            IShaderCompiler compiler;
            switch (platform)
            {
                case GraphicsPlatform.Direct3D11:
                    compiler = new Direct3D.ShaderCompiler();
                    break;
                case GraphicsPlatform.OpenGL:
                case GraphicsPlatform.OpenGLES:
                    compiler = new OpenGL.ShaderCompiler();
                    break;
                default:
                    throw new NotSupportedException();
            }

            var shaderStageBytecodes = new List<ShaderBytecode>();

            foreach (var stageBinding in parsingResult.EntryPoints)
            {
                // Compile
                var result = compiler.Compile(shaderSourceText, stageBinding.Value, stageBinding.Key, usedParameters, bytecode.Reflection, shaderSourceFilename);
                result.CopyTo(log);

                if (result.HasErrors)
                {
                    continue;
                }

                // -------------------------------------------------------
                // Append bytecode id to shader log
#if SILICONSTUDIO_PLATFORM_WINDOWS_DESKTOP
                lock (WriterLock) // protect write in case the same shader is created twice
                {
                    if (File.Exists(shaderSourceFilename))
                    {
                        // Append at the end of the shader the bytecodes Id
                        File.AppendAllText(shaderSourceFilename, "\n// {0} {1}".ToFormat(stageBinding.Key, result.Bytecode.Id));
                    }
                }
#endif
                // -------------------------------------------------------

                shaderStageBytecodes.Add(result.Bytecode);

                // When this is a compute shader, there is no need to scan other stages
                if (stageBinding.Key == ShaderStage.Compute)
                    break;
            }

            // In case of Direct3D, we can safely remove reflection data as it is entirely resolved at compile time.
            if (platform == GraphicsPlatform.Direct3D11)
            {
                CleanupReflection(bytecode.Reflection);
            }

            bytecode.Stages = shaderStageBytecodes.ToArray();
            return bytecode;
        }

        private static void CopyLogs(SiliconStudio.Shaders.Utility.LoggerResult inputLog, LoggerResult outputLog)
        {
            foreach (var inputMessage in inputLog.Messages)
            {
                var logType = LogMessageType.Info;
                switch (inputMessage.Level)
                {
                    case ReportMessageLevel.Error:
                        logType = LogMessageType.Error;
                        break;
                    case ReportMessageLevel.Info:
                        logType = LogMessageType.Info;
                        break;
                    case ReportMessageLevel.Warning:
                        logType = LogMessageType.Warning;
                        break;
                }
                var outputMessage = new LogMessage(inputMessage.Span.ToString(), logType, string.Format(" {0}: {1}", inputMessage.Code, inputMessage.Text));
                outputLog.Log(outputMessage);
            }
            outputLog.HasErrors = inputLog.HasErrors;
        }

        private static void WriteParameters(StringBuilder builder, ParameterCollection parameters, int indent, bool isArray)
        {
            var indentation = "";
            for (var i = 0; i < indent - 1; ++i)
                indentation += "    ";
            var first = true;
            foreach (var usedParam in parameters)
            {
                builder.Append(" * ");
                builder.Append(indentation);
                if (isArray && first)
                {
                    builder.Append("  - ");
                    first = false;
                }
                else if (indent > 0)
                    builder.Append("    ");
                
                if (usedParam.Key == null)
                    builder.Append("NullKey");
                else
                    builder.Append(usedParam.Key);
                builder.Append(": ");
                if (usedParam.Value == null)
                    builder.AppendLine("NullValue");
                else
                {
                    if (usedParam.Value is ParameterCollection)
                    {
                        WriteParameters(builder, usedParam.Value as ParameterCollection, indent + 1, false);
                    }
                    else if (usedParam.Value is ParameterCollection[])
                    {
                        var collectionArray = (ParameterCollection[])usedParam.Value;
                        foreach (var collection in collectionArray)
                            WriteParameters(builder, collection, indent + 1, true);
                    }
                    else if (usedParam.Value is Array)
                    {
                        builder.AppendLine(string.Join(", ", (Array)usedParam.Value));
                    }
                    else
                    {
                        builder.AppendLine(usedParam.Value.ToString());
                    }
                }
            }
        }

        private static void CleanupReflection(EffectReflection reflection)
        {
            for (int i = reflection.ConstantBuffers.Count - 1; i >= 0; i--)
            {
                var cBuffer = reflection.ConstantBuffers[i];
                if (cBuffer.Stage == ShaderStage.None)
                {
                    reflection.ConstantBuffers.RemoveAt(i);
                }
            }

            for (int i = reflection.ResourceBindings.Count - 1; i >= 0; i--)
            {
                var resourceBinding = reflection.ResourceBindings[i];
                if (resourceBinding.Stage == ShaderStage.None)
                {
                    reflection.ResourceBindings.RemoveAt(i);
                }
            }
        }
    }
}