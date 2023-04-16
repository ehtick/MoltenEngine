﻿using System.Reflection;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using DxcBuffer = Silk.NET.Direct3D.Compilers.Buffer;

namespace Molten.Graphics.Dxc
{
    public unsafe class DxcCompiler : ShaderCompiler
    {
        // For reference or help see the following:
        // See: https://github.com/microsoft/DirectXShaderCompiler/blob/master/include/dxc/dxcapi.h
        // See: https://posts.tanki.ninja/2019/07/11/Using-DXC-In-Practice/
        // See: https://simoncoenen.com/blog/programming/graphics/DxcCompiling

        internal static readonly Guid CLSID_DxcLibrary= new Guid(0x6245d6af, 0x66e0, 0x48fd, 
            new byte[] {0x80, 0xb4, 0x4d, 0x27, 0x17, 0x96, 0x74, 0x8c});

        internal static readonly Guid CLSID_DxcUtils = CLSID_DxcLibrary;

        internal static readonly Guid CLSID_DxcCompilerArgs = new Guid(0x3e56ae82, 0x224d, 0x470f,
            new byte[] { 0xa1, 0xa1, 0xfe, 0x30, 0x16, 0xee, 0x9f, 0x9d });

        internal static readonly Guid CLSID_DxcCompiler = new Guid(0x73e22d93U, (ushort)0xe6ceU, (ushort)0x47f3U, 
            0xb5, 0xbf, 0xf0, 0x66, 0x4f, 0x39, 0xc1, 0xb0 );

        internal static readonly Guid CLSID_DxcContainerReflection = new Guid(0xb9f54489, 0x55b8, 0x400c,
            0xba, 0x3a, 0x16, 0x75, 0xe4, 0x72, 0x8b, 0x91);
       
        IDxcCompiler3* _compiler;
        IDxcUtils* _utils;
        Dictionary<DxcCompilerArg, string> _baseArgs;
        Dictionary<ShaderSource, DxcBuffer> _sourceBlobs;

        /// <summary>
        /// Creates a new instance of <see cref="DxcCompiler"/>.
        /// </summary>
        /// <param name="renderer">The renderer which owns the compiler.</param>
        /// <param name="includePath">The default path for engine/game HLSL include files.</param>
        /// <param name="includeAssembly"></param>
        public DxcCompiler(RenderService renderer, string includePath, Assembly includeAssembly) : 
            base(renderer, includePath, includeAssembly)
        {
            _sourceBlobs = new Dictionary<ShaderSource, DxcBuffer>();
            _baseArgs = new Dictionary<DxcCompilerArg, string>();

            Dxc = DXC.GetApi();
            _utils = CreateDxcInstance<IDxcUtils>(CLSID_DxcUtils, IDxcUtils.Guid);
            _compiler = CreateDxcInstance<IDxcCompiler3>(CLSID_DxcCompiler, IDxcCompiler3.Guid);
        }

        /// <summary>
        /// Adds a DXC compiler argument that is included with every DXC shader compilation call of the current <see cref="DxcCompiler"/> instance.
        /// </summary>
        /// <param name="arg">The DXC argument to add.</param>
        /// <param name="value">The argument value to add, or null of none. Default value is null.</param>
        public void AddBaseArg(DxcCompilerArg arg, string value = null)
        {
            if (_baseArgs.ContainsKey(arg))
                _baseArgs[arg] = value;
            else
                _baseArgs.Add(arg, value);
        }

        protected override void OnDispose()
        {
            SilkUtil.ReleasePtr(ref _compiler);
            SilkUtil.ReleasePtr(ref _utils);
            Dxc.Dispose();
        }

        private T* CreateDxcInstance<T>(Guid clsid, Guid iid) 
            where T : unmanaged
        {
            void* ppv = null;
            HResult result = Dxc.CreateInstance(&clsid, &iid, ref ppv);
            return (T*)ppv;
        }

        /// <summary>Compiles HLSL source code and outputs the result. Returns true if successful, or false if there were errors.</summary>
        /// <param name="entryPoint"></param>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        /// 
        public override bool CompileSource(string entryPoint, ShaderType type, 
            ShaderCompilerContext context, out ShaderCodeResult result)
        {
            // Since it's not possible to have two functions in the same file with the same name, we'll just check if
            // a shader with the same entry-point name is already loaded in the context.
            if (!context.Shaders.TryGetValue(entryPoint, out result))
            {
                DxcArgumentBuilder args = new DxcArgumentBuilder(context);
                ShaderReflection reflection = null;

                foreach (DxcCompilerArg arg in _baseArgs.Keys)
                {
                    string argVal = _baseArgs[arg];
                    if (string.IsNullOrWhiteSpace(argVal))
                        args.Set(arg);
                    else
                        args.Set(arg, argVal);
                }

                args.SetEntryPoint(entryPoint);
                args.SetShaderProfile(ShaderModel.Model6_0, type);

                string[] argArray = args.GetArgsArray();

                Guid dxcResultGuid = IDxcResult.Guid;
                void* ptrResult;

                DxcBuffer srvBuffer = BuildSource(context.Source, NativeStringEncoding.LPStr);
                HResult hResult = (HResult)Native->Compile(srvBuffer, argArray, (uint)argArray.Length, null, &dxcResultGuid, &ptrResult);

                IDxcResult* dxcResult = (IDxcResult*)ptrResult;
                IDxcBlob* byteCode = null;
                IDxcBlob* pdbData = null;
                string pdbPath = "";

                // List all available outputs
                uint numOutputs = dxcResult->GetNumOutputs();
                context.AddDebug($"{numOutputs} DXC outputs found: ");
                for(uint i = 0; i < numOutputs; i++)
                {
                    OutKind kind = dxcResult->GetOutputByIndex(i);
                    context.AddDebug($"\t{kind}");
                }

                // Now retrieve the outputs we care about.
                for (uint i = 0; i < numOutputs; i++)
                {
                    OutKind kind = dxcResult->GetOutputByIndex(i);
                    switch (kind)
                    {
                        case OutKind.Errors:
                            LoadErrors(context, dxcResult);
                            break;

                        case OutKind.Pdb:
                            LoadPdbData(context, dxcResult, ref pdbData, ref pdbPath);
                            break;

                        case OutKind.Reflection:
                            reflection = BuildReflection(context, dxcResult);
                            break;

                        case OutKind.Object:
                            // Same as calling dxcResult->GetResult(&byteCode);
                            if (GetDxcOutput(context, OutKind.Object, dxcResult, ref byteCode))
                            {
                                // Store shader object. Do we flag errors as warnings if we have successfully output bytecode?
                            }
                            break;
                    }
                }

                SilkUtil.ReleasePtr(ref pdbData);

                if (context.HasErrors)
                    return false;
                
                result = new ShaderCodeResult(reflection, byteCode, byteCode->GetBufferSize());
                context.Shaders.Add(entryPoint, result);
            }

            return true;
        }

        private void LoadPdbData(ShaderCompilerContext context, IDxcResult* dxcResult, ref IDxcBlob* data, ref string pdbPath)
        {
            IDxcBlobUtf16* pPdbPath = null;

            if (GetDxcOutput(context, OutKind.Pdb, dxcResult, ref data, &pPdbPath))
            {
                pdbPath = pPdbPath->GetStringPointerS();
                nuint dataSize = data->GetBufferSize();
                context.AddDebug($"\t Loaded DXC PDB data -- Bytes: {dataSize} -- Path: {pdbPath}");

                SilkUtil.ReleasePtr(ref pPdbPath);
            }
        }

        protected override unsafe ShaderReflection BuildReflection(ShaderCompilerContext context, void* ptrData)
        {
            IDxcResult* dxcResult = (IDxcResult*)ptrData;
            IDxcBlob* outData = null;
            DxcBuffer* reflectionBuffer = null;
            void* pReflection = null;
            Guid guidReflection = CLSID_DxcContainerReflection;

            if (GetDxcOutput(context, OutKind.Reflection, dxcResult, ref outData))
            {
                _utils->CreateReflection(reflectionBuffer, ref guidReflection, ref pReflection);

                nuint dataSize = outData->GetBufferSize();
                context.AddDebug($"\t Loaded DXC container reflection data -- Bytes: {dataSize}");
            }

            IDxcContainerReflection* containerReflection = (IDxcContainerReflection*)pReflection;
            ShaderReflection r = new ShaderReflection();

            // TODO populate reflection

            return r;
        }

        private void LoadErrors(ShaderCompilerContext context, IDxcResult* dxcResult)
        {
            IDxcBlobEncoding* pErrorBlob = null;
            dxcResult->GetErrorBuffer(&pErrorBlob);

            void* ptrErrors = pErrorBlob->GetBufferPointer();
            nuint numBytes = pErrorBlob->GetBufferSize();
            string strErrors = SilkMarshal.PtrToString((nint)ptrErrors, NativeStringEncoding.UTF8);

            string[] errors = strErrors.Split('\r', '\n', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < errors.Length; i++)
                context.AddError(errors[i]);

            SilkUtil.ReleasePtr(ref pErrorBlob);
        }

        /// <summary>
        /// Retrieves the debug PDB data from a shader compilation result (<see cref="IDxcResult"/>).
        /// </summary>
        /// <param name="result"></param>
        /// <param name="outData"></param>
        /// <param name="outPath"></param>
        private bool GetDxcOutput(ShaderCompilerContext context, OutKind outputType, IDxcResult* dxcResult,
            ref IDxcBlob* outData, IDxcBlobUtf16** outPath = null)
        {
            if (dxcResult->HasOutput(outputType) == 0)
            {
                context.AddError($"Unable to retrieve '{outputType}' data: Not found");
                return false;
            }

            void* pData = null;
            IDxcBlobUtf16* pDataPath = null;
            Guid iid = IDxcBlob.Guid;
            dxcResult->GetOutput(outputType, &iid, &pData, outPath);
            outData = (IDxcBlob*)pData;
            return true;
        }

        /// <summary>
        /// (Re)builds the HLSL source code in the current <see cref="HlslSource"/> instance. 
        /// This generates a (new) <see cref="DxcBuffer"/> object.
        /// </summary>
        /// <param name="compiler"></param>
        /// <returns></returns>
        internal DxcBuffer BuildSource(ShaderSource source, NativeStringEncoding nativeEncoding)
        {
            if(!_sourceBlobs.TryGetValue(source, out DxcBuffer buffer))
            {
                Encoding encoding = Encoding.UTF8; // CodePagesEncodingProvider.Instance.GetEncoding(1252); // Ansi codepage
                buffer = new DxcBuffer();
                void* ptrSource = EngineUtil.StringToPtr(source.SourceCode, encoding, out ulong numBytes); // (void*)SilkMarshal.StringToPtr(source.SourceCode, encoding);
                //uint numBytesSilk = (uint)SilkMarshal.GetMaxSizeOf(source.SourceCode, nativeEncoding);

                buffer = new DxcBuffer()
                {
                    Ptr = ptrSource, 
                    Size = (nuint)numBytes, 
                    Encoding = 0
                };

                _sourceBlobs.Add(source, buffer);
            }

            return buffer;
        }

        public override ShaderIOStructure BuildIO(ShaderCodeResult result, ShaderType sType, ShaderIOStructureType type)
        {
            throw new NotImplementedException();
        }

        public override bool BuildStructure(ShaderCompilerContext context, HlslShader shader, ShaderCodeResult result, ShaderComposition composition)
        {
            throw new NotImplementedException();
        }

        public override unsafe void* BuildShader(HlslPass parent, ShaderType type, void* byteCode)
        {
            throw new NotImplementedException();
        }

        internal DXC Dxc { get; }

        internal IDxcCompiler3* Native => _compiler;

        internal IDxcUtils* Utils => _utils;
    }
}
