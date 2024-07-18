﻿using Prowl.Runtime.NodeSystem;
using Prowl.Runtime.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Prowl.Runtime.RenderPipelines
{
    [CreateAssetMenu("GraphRenderPipeline")]
    public class RenderPipeline : NodeGraph
    {
        public override string[] NodeCategories => [
            "General",
            "Flow Control",
            "Operations",
            "Rendering",
        ];

        public override (string, Type)[] NodeTypes => [
            ("Parameter", typeof(ParameterNode)),
        ];

        private List<NodeRenderTexture> rts = [];

        public Vector2 Resolution { get; private set; }
        public Camera.CameraData CurrentCamera { get; private set; }
        public RenderingContext Context { get; private set; }

        public NodeRenderTexture Target { get; internal set; }

        private Material blitMat;

        public NodeRenderTexture GetRT(RenderTextureDescription desc, RTBuffer[] colorFormats)
        {
            NodeRenderTexture rt = new(RenderTexture.GetTemporaryRT(desc), colorFormats);
            rts.Add(rt);
            return rt;
        }

        public void InitializeResources()
        {
        }

        public void Render(RenderingContext context, Camera.CameraData[] cameras)
        {
            // Create and schedule a command to clear the current render target
            var rootBuffer = new CommandBuffer();
            rootBuffer.SetRenderTarget(context.TargetTexture);
            rootBuffer.ClearRenderTarget(context.TargetTexture.ColorBuffers.Length > 0, context.TargetTexture.DepthBuffer != null, Color.black);

            context.ExecuteCommandBuffer(rootBuffer);

            Context = context;

            foreach (var cam in cameras)
            {
                try
                {
                    // Get Width and Height an the target RenderTexture
                    var target = context.TargetTexture;
                    uint width = context.TargetTexture.Width;
                    uint height = context.TargetTexture.Height;

                    if (cam.Target.IsAvailable)
                    {
                        target = cam.Target.Res!;
                        width = cam.Target.Res!.Width;
                        height = cam.Target.Res!.Height;
                    }

                    context.SetupTargetCamera(cam, width, height);

                    // Update the value of built-in shader variables, based on the current Camera
                    Target = new NodeRenderTexture(target);
                    Resolution = new Vector2(width, height);
                    CurrentCamera = cam;


                    var cmd = CommandBufferPool.Get("Camera Buffer");
                    cmd.SetRenderTarget(Target.RenderTexture);
                    if (cam.DoClear)
                        cmd.ClearRenderTarget(Target.HasColors, Target.HasDepth, cam.ClearColor);
                    context.ExecuteCommandBuffer(cmd);

                    var pipelineNode = GetNodes<OnPipelineNode>().FirstOrDefault(n => n.Name == context.PipelineName);
                    if(pipelineNode == null)
                    {
                        //Debug.LogError($"Pipeline Node {context.PipelineName} not found!");
                        return;
                    }

                    pipelineNode.Execute(null);

                    CommandBufferPool.Release(cmd);

                }
                catch (Exception e)
                {
                    Debug.LogError($"Error rendering camera: {e.Message}");
                }
                finally
                {
                    // Release all Temp Render Textures back into the RT Pool
                    foreach (var rt in rts)
                    {
                        rt.HasBeenReleased = true;
                        RenderTexture.ReleaseTemporaryRT(rt.RenderTexture);
                    }
                    rts.Clear();
                }
            }

            // Instruct the graphics API to perform all scheduled commands
            context.Submit();
        }

        public void ReleaseResources()
        {
        }
    }
}
