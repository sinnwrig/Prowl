// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Veldrid;


namespace Prowl.Runtime
{
    public struct ShaderPipelineDescription : IEquatable<ShaderPipelineDescription>
    {
        public ShaderPass pass;
        public ShaderVariant variant;

        public OutputDescription? output;

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(pass, variant, output);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not ShaderPipelineDescription other)
                return false;

            return Equals(other);
        }

        public bool Equals(ShaderPipelineDescription other)
        {
            return
                pass == other.pass &&
                variant == other.variant &&
                output.Equals(other.output);
        }
    }

    public sealed partial class ShaderPipeline : IDisposable
    {
        public static readonly FrontFace FrontFace = FrontFace.Clockwise;

        public readonly ShaderVariant shader;

        public readonly ShaderSetDescription shaderSet;
        public readonly ResourceLayout resourceLayout;

        private Dictionary<string, uint> semanticLookup;
        private Dictionary<string, uint> bufferLookup;

        private byte bufferCount;

        public ShaderUniform[] Uniforms => shader.Uniforms;

        private Veldrid.GraphicsPipelineDescription description;

        private static readonly int pipelineCount = 20; // 20 possible combinations (5 topologies, 2 fill modes, 2 scissor modes)
        private Pipeline[] pipelines;


        public Pipeline GetPipeline(PolygonFillMode fill, PrimitiveTopology topology, bool scissor)
        {
            int index = (int)topology * 4 + (int)fill * 2 + (scissor ? 0 : 1);

            if (pipelines[index] == null)
            {
                description.RasterizerState.ScissorTestEnabled = scissor;
                description.RasterizerState.FillMode = fill;
                description.PrimitiveTopology = topology;

                pipelines[index] = Graphics.Factory.CreateGraphicsPipeline(description);
            }

            return pipelines[index];
        }


        public ShaderPipeline(ShaderPipelineDescription description)
        {
            this.shader = description.variant;

            ShaderDescription[] shaderDescriptions = shader.GetProgramsForBackend();

            // Create shader set description
            Veldrid.Shader[] shaders = new Veldrid.Shader[shaderDescriptions.Length];

            this.semanticLookup = new();

            for (int shaderIndex = 0; shaderIndex < shaders.Length; shaderIndex++)
                shaders[shaderIndex] = Graphics.Factory.CreateShader(shaderDescriptions[shaderIndex]);

            VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[shader.VertexInputs.Length];

            for (int inputIndex = 0; inputIndex < vertexLayouts.Length; inputIndex++)
            {
                VertexInput input = shader.VertexInputs[inputIndex];

                // Add in_var_ to match reflected name in SPIRV-Cross generated GLSL.
                vertexLayouts[inputIndex] = new VertexLayoutDescription(
                    new VertexElementDescription("in_var_" + input.semantic, input.format, VertexElementSemantic.TextureCoordinate));

                semanticLookup[input.semantic] = (uint)inputIndex;
            }

            this.shaderSet = new ShaderSetDescription(vertexLayouts, shaders);

            // Create resource layout and uniform lookups
            this.bufferLookup = new();

            ResourceLayoutDescription layoutDescription = new ResourceLayoutDescription(
                new ResourceLayoutElementDescription[Uniforms.Length]);

            for (ushort uniformIndex = 0; uniformIndex < Uniforms.Length; uniformIndex++)
            {
                ShaderUniform uniform = Uniforms[uniformIndex];
                ShaderStages stages = shader.UniformStages[uniformIndex];

                layoutDescription.Elements[uniform.binding] =
                    new ResourceLayoutElementDescription(uniform.name, uniform.kind, stages);

                if (uniform.kind != ResourceKind.UniformBuffer)
                    continue;

                bufferLookup[uniform.name] = Pack(uniformIndex, bufferCount);
                bufferCount++;
            }

            this.resourceLayout = Graphics.Factory.CreateResourceLayout(layoutDescription);

            this.pipelines = new Pipeline[pipelineCount];

            RasterizerStateDescription rasterizerState = new(
                description.pass.CullMode,
                PolygonFillMode.Solid,
                FrontFace,
                description.pass.DepthClipEnabled,
                false
            );

            this.description = new(
                description.pass.Blend,
                description.pass.DepthStencilState,
                rasterizerState,
                PrimitiveTopology.LineList,
                shaderSet,
                [resourceLayout],
                description.output ?? Graphics.ScreenTarget.OutputDescription);
        }


        private static BindableResource GetBindableResource(ShaderUniform uniform, out DeviceBuffer? buffer)
        {
            buffer = null;

            if (uniform.kind == ResourceKind.TextureReadOnly)
                return Texture2D.Empty.TextureView;

            if (uniform.kind == ResourceKind.TextureReadWrite)
                return Texture2D.EmptyRW.TextureView;

            if (uniform.kind == ResourceKind.Sampler)
                return Graphics.Device.PointSampler;

            if (uniform.kind == ResourceKind.StructuredBufferReadOnly)
                return GraphicsBuffer.Empty.Buffer;

            if (uniform.kind == ResourceKind.StructuredBufferReadWrite)
                return GraphicsBuffer.EmptyRW.Buffer;

            uint bufferSize = (uint)Math.Ceiling(uniform.size / (double)16) * 16;
            buffer = Graphics.Factory.CreateBuffer(new BufferDescription(bufferSize, BufferUsage.UniformBuffer | BufferUsage.DynamicWrite));

            return buffer;
        }


        public BindableResourceSet CreateResources()
        {
            DeviceBuffer[] boundBuffers = new DeviceBuffer[bufferCount];
            BindableResource[] boundResources = new BindableResource[Uniforms.Length];
            byte[][] intermediateBuffers = new byte[bufferCount][];

            for (int i = 0, b = 0; i < Uniforms.Length; i++)
            {
                boundResources[Uniforms[i].binding] = GetBindableResource(Uniforms[i], out DeviceBuffer? buffer);

                if (buffer != null)
                {
                    boundBuffers[b] = buffer;
                    intermediateBuffers[b] = new byte[buffer.SizeInBytes];

                    b++;
                }
            }

            ResourceSetDescription setDescription = new ResourceSetDescription(resourceLayout, boundResources);
            BindableResourceSet resources = new BindableResourceSet(this, setDescription, boundBuffers, intermediateBuffers);

            return resources;
        }


        public bool GetBuffer(string name, out ushort uniform, out ushort buffer)
        {
            uniform = 0;
            buffer = 0;

            if (bufferLookup.TryGetValue(name, out uint packed))
            {
                Unpack(packed, out uniform, out buffer);
                return true;
            }

            return false;
        }


        private static uint Pack(ushort a, ushort b)
            => ((uint)a << 16) | b;

        private static void Unpack(uint packed, out ushort a, out ushort b)
            => (a, b) = ((ushort)(packed >> 16), (ushort)(packed & ushort.MaxValue));


        public void BindVertexBuffer(CommandList list, string semantic, DeviceBuffer buffer, uint offset = 0)
        {
            if (semanticLookup.TryGetValue(semantic, out uint location))
                list.SetVertexBuffer(location, buffer, offset);
        }


        public void Dispose()
        {
            for (int i = 0; i < shaderSet.Shaders.Length; i++)
                shaderSet.Shaders[i]?.Dispose();

            for (int i = 0; i < pipelines.Length; i++)
                pipelines[i]?.Dispose();

            resourceLayout?.Dispose();
        }
    }
}
