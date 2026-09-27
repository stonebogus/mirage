using System.Numerics;
using Box2D.NET;
using Mirage.Physics.Colliders;
using Mirage.Physics.Interfaces;
using Mirage.Physics.Resources;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Mirage.Simulating.Bindings;

internal class BodyBinding
{
    private readonly ISimulatable _simulatable;

    public BodyBinding(B2WorldId world, ISimulatable simulatable)
    {
        _simulatable = simulatable;

        var body = simulatable.Body.Get();
        var position = simulatable.GlobalPosition;

        var definition = b2DefaultBodyDef();

        definition.type = Convert(body.Type.Get());
        definition.position = new B2Vec2(position.X, position.Y);
        definition.rotation = b2MakeRot(simulatable.GlobalRotation);

        var linearVelocity = body.LinearVelocity.Get();

        definition.linearVelocity = new B2Vec2(linearVelocity.X, linearVelocity.Y);

        definition.angularVelocity = body.AngularVelocity.Get();
        definition.linearDamping = body.LinearDamping.Get();
        definition.angularDamping = body.AngularDamping.Get();
        definition.gravityScale = body.GravityScale.Get();

        Body = b2CreateBody(world, definition);

        foreach (var collider in body.Colliders)
            CreateShape(collider);
    }

    public B2BodyId Body { get; }

    private static B2BodyType Convert(BodyType type)
    {
        return type switch
        {
            BodyType.Static => B2BodyType.b2_staticBody,
            BodyType.Kinematic => B2BodyType.b2_kinematicBody,
            BodyType.Dynamic => B2BodyType.b2_dynamicBody,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    private void CreateCircle(CircleCollider collider)
    {
        var material = collider.Material;

        var definition = b2DefaultShapeDef();

        definition.material.friction = material.Friction.Get();
        definition.material.restitution = material.Restitution.Get();

        var circle = new B2Circle(new B2Vec2(0f, 0f), collider.Radius);

        b2CreateCircleShape(Body, definition, circle);
    }

    private void CreateShape(Collider collider)
    {
        switch (collider)
        {
            case CircleCollider circle:
                CreateCircle(circle);
                break;

            case MeshCollider:
                throw new NotSupportedException(
                    "Mesh colliders are not supported by the simulator yet."
                );

            default:
                throw new NotSupportedException(
                    $"Collider type '{collider.GetType().Name}' is not supported."
                );
        }
    }

    public void Destroy()
    {
        b2DestroyBody(Body);
    }

    public void Synchronize()
    {
        var position = b2Body_GetPosition(Body);
        var rotation = b2Body_GetRotation(Body);
        var linearVelocity = b2Body_GetLinearVelocity(Body);
        var angularVelocity = b2Body_GetAngularVelocity(Body);

        _simulatable.GlobalPosition = new Vector2(position.X, position.Y);

        _simulatable.GlobalRotation = b2Rot_GetAngle(rotation);

        var body = _simulatable.Body.Get();

        body.LinearVelocity.Set(new Vector2(linearVelocity.X, linearVelocity.Y));

        body.AngularVelocity.Set(angularVelocity);
    }
}
