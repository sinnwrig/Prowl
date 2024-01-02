﻿using HexaEngine.ImPlotNET;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Microsoft.VisualBasic;
using Prowl.Icons;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;


// Partially based on https://github.com/suzuke/JitterPhysicsForUnity

namespace Prowl.Runtime.Components
{
    /// <summary> A GameObject Component that describes a Dynamic or Static Physical Rigidbody </summary>
    [RequireComponent(typeof(Transform))]
    [AddComponentMenu($"{FontAwesome6.HillRockslide}  Physics/{FontAwesome6.Cubes}  Rigidbody")]
    public class Rigidbody : MonoBehaviour
    {
        private PhysicalSpace space;
        public PhysicalSpace Space {
            get => space ??= Physics.DefaultSpace;
            set {
                space = value;
                if (Body != null) {
                    space.world.Remove(Body);
                    Body = null;
                }
            }
        }


        internal Jitter2.Dynamics.RigidBody Body { get; private set; }

        private List<Shape> shapes;
        private List<Shape> Shapes => shapes ??= CreateShapes();

        [SerializeField] private bool affectedByGravity = true;
        public bool AffectedByGravity {
            get => affectedByGravity;
            set {
                affectedByGravity = value;
                Body.AffectedByGravity = value;
            }
        }

        [SerializeField] private bool isStatic;
        public bool IsStatic {
            get => isStatic;
            set {
                isStatic = value;
                Body.IsStatic = value;
            }
        }

        [SerializeField] private bool isKinematic;
        public bool IsKinematic {
            get => isKinematic;
            set => isKinematic = value;
        }

        public bool IsActive {
            get => Body.IsActive;
            set => Body.SetActivationState(value);
        }

        [SerializeField] private bool speculativeContacts;
        public bool SpeculativeContacts {
            get => speculativeContacts;
            set {
                speculativeContacts = value;
                Body.EnableSpeculativeContacts = value;
            }
        }

        [SerializeField]
        private float mass;
        public float Mass {
            get => mass;
            set {
                mass = value;
                Body.SetMassInertia(mass);
            }
        }

        public Vector3 Torque => new(Body.Torque.X, Body.Torque.Y, Body.Torque.Z);

        public Vector3 Force {
            get => new(Body.Force.X, Body.Force.Y, Body.Force.Z);
            set => Body.Force = value;
        }

        public Vector3 Velocity {
            get => new(Body.Velocity.X, Body.Velocity.Y, Body.Velocity.Z);
            set => Body.Velocity = value;
        }

        public Vector3 AngularVelocity {
            get => new(Body.AngularVelocity.X, Body.AngularVelocity.Y, Body.AngularVelocity.Z);
            set => Body.AngularVelocity = value;
        }

        public List<Collision> Contacts => Body.Contacts.Select(x => new Collision(this, x)).ToList();
        public List<Rigidbody> TouchingBodies => Body.Connections.Select(x => (x.Tag as Rigidbody)!).ToList();


        public void AddForce(Vector3 force) => Body.AddForce(force);

        public void AddForceAtPosition(Vector3 force, Vector3 position) => Body.AddForce(force, position);

        public Vector3 GetPointVelocity(Vector3 point) => Body.Velocity + JVector.Cross(Body.AngularVelocity, point - Body.Position);

        public void SetTransform(Vector3 position, Quaternion rotation)
        {
            SetPosition(position);
            SetRotation(rotation);
        }

        public void SetPosition(Vector3 position) => Body.Position = position;

        public void SetRotation(Quaternion rotation) => Body.Orientation = JMatrix.CreateFromQuaternion(rotation);

        public void Refresh()
        {
            if(isKinematic || mass > 0)
                Mass = isKinematic ? 1e6f : mass;
            IsStatic = isStatic;
            AffectedByGravity = isKinematic ? false : affectedByGravity;
            SpeculativeContacts = speculativeContacts;
            shapes = CreateShapes();

            IsActive = true;

            SetPosition(GameObject.Transform.GlobalPosition);
            SetRotation(GameObject.Transform.GlobalOrientation);
        }

        public void RefreshShape()
        {
            // TODO: Causes Crash, Shapes get cached and dont appear to support being registered/attached to a body a second time
            //Body.ClearShapes(false);
            //shapes = CreateShapes();
            //Body.AddShape(Shapes);
        }

        private List<Shape> CreateShapes()
        {
            var colliders = GetComponentsInChildren<Collider>().ToList();

            List<Shape> allShapes = new();
            foreach (var collider in colliders) {
                if (collider.GameObject.Transform!.InstanceID == GameObject.Transform!.InstanceID)
                    allShapes.AddRange(collider.Shape);
                else
                    allShapes.AddRange(collider.CreateTransformedShape(this));
            }

            return allShapes;
        }

        #region Prowl Methods
        public override void OnValidate() { if (Application.isPlaying) Refresh(); }

        private void OnEnable()
        {
            Body = Space.world.CreateRigidBody();
            Body.AddShape(Shapes);
            Body.Tag = this;
            Body.AffectedByGravity = AffectedByGravity;
            Body.IsStatic = IsStatic;

            if (isKinematic) {
                Body.AffectedByGravity = false;
                Body.SetMassInertia(1e6f);
            } else if (Mass > 0) Body.SetMassInertia(Mass);
            else Body.SetMassInertia();

            SetPosition(GameObject.Transform.GlobalPosition);
            SetRotation(GameObject.Transform.GlobalOrientation);
        }

        private void OnDisable()
        {
            Space.world.Remove(Body);
            Body = null;
        }

        private void LateUpdate()
        {
            GameObject.Transform.GlobalPosition = Body.Position;
            GameObject.Transform.GlobalOrientation = JQuaternion.CreateFromMatrix(Body.Orientation);
        }

        #endregion
    }

    public abstract class Collider : MonoBehaviour
    {
        private List<Shape> shape;
        public List<Shape> Shape => shape ??= CreateShapes();
        public void OnEnable() => GetComponentInParent<Rigidbody>()?.RefreshShape();
        public void OnDisable() => GetComponentInParent<Rigidbody>()?.RefreshShape();
        public abstract List<Shape> CreateShapes();
        public virtual List<TransformedShape> CreateTransformedShape(Rigidbody body)
        {
            // Transform is guranteed to exist, since a Collider must be On or under a Rigidbody which requires a Transform
            // COllider is on a child without a transform the Parent transform of the Rigidbody is used via Inheritance
            // This is fine since we use Global positions here, since the Body and Collider share a transform their global positions are identical
            // Results in no offsets being applied to the shape
            var position = GameObject.Transform!.GlobalPosition - body.GameObject.Transform!.GlobalPosition;
            var rotation = Quaternion.RotateTowards(body.GameObject.Transform!.GlobalOrientation, GameObject.Transform!.GlobalOrientation, 360);

            var invRotation = Quaternion.Inverse(body.GameObject.Transform!.GlobalOrientation);
            rotation = invRotation * rotation;
            position = Vector3.Transform(position, invRotation);
            var jRot = JMatrix.CreateFromQuaternion(rotation);

            return Shape.Select(x => new TransformedShape(x, position, jRot)).ToList();
        }
    }

    [AddComponentMenu($"{FontAwesome6.HillRockslide}  Physics/{FontAwesome6.Box}  Box Collider")]
    public class BoxCollider : Collider
    {
        public Vector3 size = Vector3.One;

        public override List<Shape> CreateShapes() => [ new BoxShape(size * GameObject.Transform!.Scale) ];
        public override void OnValidate()
        {
            (Shape[0] as BoxShape).Size = size * GameObject.Transform!.Scale;
            Shape[0].UpdateShape();
            GetComponentInParent<Rigidbody>().IsActive = true;
        }

        public void DrawGizmosSelected()
        {
            Gizmos.Matrix = Matrix4x4.CreateScale(size * 1.0025f) * GameObject.Transform!.GlobalCamRelative;
            Gizmos.Cube(Color.yellow);
        }
    }

    [AddComponentMenu($"{FontAwesome6.HillRockslide}  Physics/{FontAwesome6.Circle}  Sphere Collider")]
    public class SphereCollider : Collider
    {
        public float radius = 1f;
        public override List<Shape> CreateShapes() => [ new SphereShape(radius * (float)GameObject.Transform!.Scale.x) ];
        public override void OnValidate()
        {
            (Shape[0] as SphereShape).Radius = radius * (float)GameObject.Transform!.Scale.x;
            Shape[0].UpdateShape();
            GetComponentInParent<Rigidbody>().IsActive = true;
        }

        public void DrawGizmosSelected()
        {
            var mat = Matrix4x4.Identity;
            mat = Matrix4x4.Multiply(mat, Matrix4x4.CreateScale((radius * (float)GameObject.Transform!.Scale.x) * 1.0025f));
            mat = Matrix4x4.Multiply(mat, Matrix4x4.CreateTranslation(GameObject.Transform!.GlobalPosition - (Camera.Current.GameObject.Transform?.GlobalPosition ?? Vector3.Zero)));
            Gizmos.Matrix = mat;
            Gizmos.Sphere(Color.yellow);
        }
    }

    [AddComponentMenu($"{FontAwesome6.HillRockslide}  Physics/{FontAwesome6.Capsules}  Capsule Collider")]
    public class CapsuleCollider : Collider
    {
        public float radius = 1f;
        public float height = 1f;
        public override List<Shape> CreateShapes() => [ new CapsuleShape(radius, height) ];
        public override void OnValidate()
        {
            (Shape[0] as CapsuleShape).Radius = radius;
            (Shape[0] as CapsuleShape).Length = height;
            Shape[0].UpdateShape();
            GetComponentInParent<Rigidbody>().IsActive = true;
        }
    }

    [AddComponentMenu($"{FontAwesome6.HillRockslide}  Physics/{FontAwesome6.Capsules}  Cylinder Collider")]
    public class CylinderCollider : Collider
    {
        public float radius = 1f;
        public float height = 1f;
        public override List<Shape> CreateShapes() => [ new CylinderShape(radius, height) ];
        public override void OnValidate()
        {
            (Shape[0] as CapsuleShape).Radius = radius;
            (Shape[0] as CapsuleShape).Length = height;
            Shape[0].UpdateShape();
            GetComponentInParent<Rigidbody>().IsActive = true;
        }
    }

    [AddComponentMenu($"{FontAwesome6.HillRockslide}  Physics/{FontAwesome6.Capsules}  Mesh Collider")]
    public class MeshCollider : Collider
    {
        public AssetRef<Mesh> mesh;

        public bool convex = false;

        public enum Approximation
        {
            Level1 = 6,
            Level2 = 7,
            Level3 = 8,
            Level4 = 9,
            Level5 = 10,
            Level6 = 11,
            Level7 = 12,
            Level8 = 13,
            Level9 = 15,
            Level10 = 20,
            Level15 = 25,
            Level20 = 30
        }

        public Approximation convexApprox = Approximation.Level5;

        public override List<Shape> CreateShapes()
        {
            if (mesh.IsAvailable == false) return [new SphereShape(0.001f)]; // Mesh is missing so we create a sphere with a tiny radius to prevent errors

            if (!convex) {
                var indices = mesh.Res.indices;
                var vertices = mesh.Res.vertices;

                List<JTriangle> triangles = new();

                for (int i = 0; i < mesh.Res.triangleCount; i += 3) {
                    JVector v1 = vertices[i + 0].Position.ToDouble();
                    JVector v2 = vertices[i + 1].Position.ToDouble();
                    JVector v3 = vertices[i + 2].Position.ToDouble();
                    triangles.Add(new JTriangle(v1, v2, v3));
                }

                var jtm = new TriangleMesh(triangles);
                List<Shape> shapesToAdd = new();

                for (int i = 0; i < jtm.Indices.Length; i++) {
                    TriangleShape ts = new TriangleShape(jtm, i);
                    shapesToAdd.Add(ts);
                }
                return shapesToAdd;
            } else {
                var points = mesh.Res.vertices.Select(x => (JVector)x.Position.ToDouble());
                return [new PointCloudShape(BuildConvexCloud(points.ToList()))];
            }

        }

        private List<JVector> BuildConvexCloud(List<JVector> pointCloud)
        {
            List<JVector> allIndices = new();

            int steps = (int)convexApprox;

            for (int thetaIndex = 0; thetaIndex < steps; thetaIndex++) {
                // [0,PI]
                float theta = MathF.PI / (steps - 1) * thetaIndex;
                float sinTheta = (float)Math.Sin(theta);
                float cosTheta = (float)Math.Cos(theta);

                for (int phiIndex = 0; phiIndex < steps; phiIndex++) {
                    // [-PI,PI]
                    float phi = (2.0f * MathF.PI) / (steps - 0) * phiIndex - MathF.PI;
                    float sinPhi = (float)Math.Sin(phi);
                    float cosPhi = (float)Math.Cos(phi);

                    JVector dir = new JVector(sinTheta * cosPhi, cosTheta, sinTheta * sinPhi);

                    int index = FindExtremePoint(pointCloud, ref dir);
                    allIndices.Add(pointCloud[index]);
                }
            }

            return allIndices.Distinct().ToList();
        }

        private static int FindExtremePoint(List<JVector> points, ref JVector dir)
        {
            int index = 0;
            float current = float.MinValue;

            JVector point; float value;

            for (int i = 1; i < points.Count; i++) {
                point = points[i];

                value = JVector.Dot(ref point, ref dir);
                if (value > current) { current = value; index = i; }
            }

            return index;
        }

    }
}