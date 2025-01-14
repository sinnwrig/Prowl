// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;

using Veldrid;

#pragma warning disable

namespace Prowl.Runtime
{
    public class BindableResourceSet : IDisposable
    {
        public ShaderPipeline Pipeline { get; private set; }

        public ResourceSetDescription description;
        private ResourceSet resources;

        private DeviceBuffer[] uniformBuffers;
        private byte[][] intermediateBuffers;


        public BindableResourceSet(ShaderPipeline pipeline, ResourceSetDescription description, DeviceBuffer[] buffers, byte[][] intermediate)
        {
            this.Pipeline = pipeline;
            this.description = description;
            this.uniformBuffers = buffers;
            this.intermediateBuffers = buffers.Select(x => new byte[x.SizeInBytes]).ToArray();
        }


        public void Bind(CommandList list, PropertyState state)
        {
            bool recreateResourceSet = false | (resources == null);

            for (int i = 0; i < Pipeline.Uniforms.Length; i++)
            {
                ShaderUniform uniform = Pipeline.Uniforms[i];

                switch (uniform.kind)
                {
                    case ResourceKind.UniformBuffer:
                        UpdateBuffer(list, uniform.name, state);
                        break;

                    case ResourceKind.StructuredBufferReadOnly:
                        GraphicsBuffer buffer = state._buffers.GetValueOrDefault(uniform.name, null) ?? GraphicsBuffer.Empty;

                        if (!buffer.Buffer.Usage.HasFlag(BufferUsage.StructuredBufferReadOnly))
                            buffer = GraphicsBuffer.EmptyRW;

                        UpdateResource(buffer.Buffer, uniform.binding, ref recreateResourceSet);
                        break;

                    case ResourceKind.StructuredBufferReadWrite:
                        GraphicsBuffer rwbuffer = state._buffers.GetValueOrDefault(uniform.name, null) ?? GraphicsBuffer.EmptyRW;

                        if (!rwbuffer.Buffer.Usage.HasFlag(BufferUsage.StructuredBufferReadWrite))
                            rwbuffer = GraphicsBuffer.EmptyRW;

                        UpdateResource(rwbuffer.Buffer, uniform.binding, ref recreateResourceSet);
                        break;

                    case ResourceKind.TextureReadOnly:
                        Texture texture = GetTexture(uniform.name, state, TextureUsage.Sampled, Texture2D.EmptyWhite);
                        UpdateResource(texture.TextureView, uniform.binding, ref recreateResourceSet);
                        break;

                    case ResourceKind.TextureReadWrite:
                        Texture rwtexture = GetTexture(uniform.name, state, TextureUsage.Storage, Texture2D.EmptyRW);
                        UpdateResource(rwtexture.TextureView, uniform.binding, ref recreateResourceSet);
                        break;

                    case ResourceKind.Sampler:
                        Texture stexture = GetTexture(SliceSampler((uniform.name)), state, TextureUsage.Sampled, Texture2D.EmptyWhite);
                        UpdateResource(stexture.Sampler.InternalSampler, uniform.binding, ref recreateResourceSet);
                        break;
                }
            }

            if (recreateResourceSet)
            {
                resources?.Dispose();
                resources = Graphics.Factory.CreateResourceSet(description);
            }

            list.SetGraphicsResourceSet(0, resources);
        }


        private Texture GetTexture(string name, PropertyState state, TextureUsage usage, Texture defaultTex)
        {
            AssetRef<Texture> textureRes = state._values.GetValueOrDefault(name, default).texture ?? defaultTex;

            Texture texture = textureRes.Res ?? defaultTex;

            if (!texture.Usage.HasFlag(usage))
                return defaultTex;

            return texture;
        }


        private void UpdateResource(BindableResource newResource, uint binding, ref bool wasChanged)
        {
            if (description.BoundResources[binding].Resource != newResource.Resource)
            {
                wasChanged |= true;
                description.BoundResources[binding] = newResource;
            }
        }


        public bool UpdateBuffer(CommandList list, string ID, PropertyState state)
        {
            if (!Pipeline.GetBuffer(ID, out ushort uniformIndex, out ushort bufferIndex))
                return false;

            ShaderUniform uniform = Pipeline.Uniforms[uniformIndex];
            DeviceBuffer buffer = uniformBuffers[bufferIndex];
            byte[] tempBuffer = intermediateBuffers[bufferIndex];

            for (int i = 0; i < uniform.members.Length; i++)
            {
                ShaderUniformMember member = uniform.members[i];

                if (state._values.TryGetValue(member.name, out Property value))
                {
                    if (value.type != member.type || value.texture != null)
                        continue;

                    if (member.arrayStride <= 0)
                    {
                        Buffer.BlockCopy(value.data, 0, tempBuffer, (int)member.bufferOffsetInBytes, Math.Min((int)member.size, value.data.Length));
                        continue;
                    }

                    uint destStride = member.arrayStride;
                    uint srcStride = Math.Min(destStride, (uint)value.width * value.height);
                    uint destLength = member.size / member.arrayStride;

                    for (int j = 0; j < Math.Min(destLength, value.arraySize); i++)
                    {
                        Buffer.BlockCopy(value.data, (int)(j * srcStride), tempBuffer, (int)(member.bufferOffsetInBytes + (j * destStride)), (int)srcStride);
                    }
                }
            }

            list.UpdateBuffer(buffer, 0, tempBuffer);

            return true;
        }


        private static string SliceSampler(string name)
        {
            const string prefix = "sampler";

            if (name.StartsWith(prefix, StringComparison.Ordinal))
                return name.Substring(prefix.Length);

            return name;
        }


        public void Dispose()
        {
            resources.Dispose();

            for (int i = 0; i < uniformBuffers.Length; i++)
                uniformBuffers[i].Dispose();
        }
    }
}
