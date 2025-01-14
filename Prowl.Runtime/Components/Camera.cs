﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;

using Prowl.Icons;
using Prowl.Runtime.RenderPipelines;

namespace Prowl.Runtime;

[AddComponentMenu($"{FontAwesome6.Tv}  Rendering/{FontAwesome6.Camera}  Camera")]
[ExecuteAlways]
public class Camera : MonoBehaviour
{
    public LayerMask LayerMask = LayerMask.Everything;

    public bool DoClear = true;
    public Color ClearColor = new Color(0f, 0f, 0f, 1f);
    public float FieldOfView = 60f;
    public float OrthographicSize = 0.5f;
    public int DrawOrder = 0;
    public Rect Viewrect = new Rect(0, 0, 1, 1);
    public float NearClip = 0.01f;
    public float FarClip = 1000f;

    public float RenderScale = 1.0f;

    public enum ProjectionType { Perspective, Orthographic }
    public ProjectionType projectionType = ProjectionType.Perspective;


    public AssetRef<RenderTexture> Target;
    public AssetRef<RenderPipeline> Pipeline;


    public Ray ScreenPointToRay(Vector2 screenPoint, Vector2 screenScale)
    {
        // Normalize screen coordinates to [-1, 1]
        Vector2 ndc = new Vector2(
            (screenPoint.x / screenScale.x) * 2.0f - 1.0f,
            1.0f - (screenPoint.y / screenScale.y) * 2.0f
        );

        // Create the near and far points in NDC
        Vector4 nearPointNDC = new Vector4(ndc.x, ndc.y, 0.0f, 1.0f);
        Vector4 farPointNDC = new Vector4(ndc.x, ndc.y, 1.0f, 1.0f);

        // Calculate the inverse view-projection matrix
        Matrix4x4 viewProjectionMatrix = GetViewMatrix() * GetProjectionMatrix(screenScale);
        Matrix4x4.Invert(viewProjectionMatrix, out Matrix4x4 inverseViewProjectionMatrix);

        // Unproject the near and far points to world space
        Vector4 nearPointWorld = Vector4.Transform(nearPointNDC, inverseViewProjectionMatrix);
        Vector4 farPointWorld = Vector4.Transform(farPointNDC, inverseViewProjectionMatrix);

        // Perform perspective divide
        nearPointWorld /= nearPointWorld.w;
        farPointWorld /= farPointWorld.w;

        // Create the ray
        Vector3 rayOrigin = new Vector3(nearPointWorld.x, nearPointWorld.y, nearPointWorld.z);
        Vector3 rayDirection = Vector3.Normalize(new Vector3(farPointWorld.x, farPointWorld.y, farPointWorld.z) - rayOrigin);

        return new Ray(rayOrigin, rayDirection);
    }


    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookToLeftHanded(Transform.position, Transform.forward, Transform.up);
    }


    public Matrix4x4 GetProjectionMatrix(Vector2 resolution)
    {
        if (projectionType == ProjectionType.Orthographic)
            return Matrix4x4.CreateOrthographic(OrthographicSize, OrthographicSize, NearClip, FarClip);

        return System.Numerics.Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(FieldOfView.ToRad(), (float)(resolution.x / resolution.y), NearClip, FarClip).ToDouble();
    }
}
