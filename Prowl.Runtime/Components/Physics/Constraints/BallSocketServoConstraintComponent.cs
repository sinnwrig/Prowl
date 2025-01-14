﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using BepuPhysics.Constraints;

namespace Prowl.Runtime;


[AddComponentMenu($"{Prowl.Icons.FontAwesome6.HillRockslide}  Physics/{Prowl.Icons.FontAwesome6.Joint}  Constraints/{Prowl.Icons.FontAwesome6.Circle}  Ball Socket Servo")]
public sealed class BallSocketServoConstraintComponent : TwoBodyConstraintComponent<BallSocketServo>
{
    [SerializeField, HideInInspector] private Vector3 _localOffsetA;
    [SerializeField, HideInInspector] private Vector3 _localOffsetB;

    [SerializeField, HideInInspector] private float _servoMaximumSpeed = 10;
    [SerializeField, HideInInspector] private float _servoBaseSpeed = 1;
    [SerializeField, HideInInspector] private float _servoMaximumForce = 1000;

    [SerializeField, HideInInspector] private float _springFrequency = 35;
    [SerializeField, HideInInspector] private float _springDampingRatio = 5;

    [ShowInInspector]
    public Vector3 LocalOffsetA
    {
        get => _localOffsetA;
        set
        {
            _localOffsetA = value;
            ConstraintData?.TryUpdateDescription();
        }
    }

    [ShowInInspector]
    public Vector3 LocalOffsetB
    {
        get => _localOffsetB;
        set
        {
            _localOffsetB = value;
            ConstraintData?.TryUpdateDescription();
        }
    }

    [ShowInInspector]
    public float ServoMaximumSpeed
    {
        get
        {
            return _servoMaximumSpeed;
        }
        set
        {
            _servoMaximumSpeed = value;
            ConstraintData?.TryUpdateDescription();
        }
    }

    [ShowInInspector]
    public float ServoBaseSpeed
    {
        get
        {
            return _servoBaseSpeed;
        }
        set
        {
            _servoBaseSpeed = value;
            ConstraintData?.TryUpdateDescription();
        }
    }

    [ShowInInspector]
    public float ServoMaximumForce
    {
        get
        {
            return _servoMaximumForce;
        }
        set
        {
            _servoMaximumForce = value;
            ConstraintData?.TryUpdateDescription();
        }
    }

    [ShowInInspector]
    public float SpringFrequency
    {
        get
        {
            return _springFrequency;
        }
        set
        {
            _springFrequency = value;
            ConstraintData?.TryUpdateDescription();
        }
    }

    [ShowInInspector]
    public float SpringDampingRatio
    {
        get
        {
            return _springDampingRatio;
        }
        set
        {
            _springDampingRatio = value;
            ConstraintData?.TryUpdateDescription();
        }
    }

    internal override BallSocketServo CreateConstraint()
    {
        return new BallSocketServo
        {
            LocalOffsetA = _localOffsetA,
            LocalOffsetB = _localOffsetB,
            ServoSettings = new ServoSettings
            {
                MaximumSpeed = _servoMaximumSpeed,
                BaseSpeed = _servoBaseSpeed,
                MaximumForce = _servoMaximumForce,
            },
            SpringSettings = new SpringSettings(_springFrequency, _springDampingRatio)
        };
    }
}
