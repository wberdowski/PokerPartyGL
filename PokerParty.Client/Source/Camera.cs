using OpenTK.Mathematics;

namespace PokerParty.Client
{
    public class Camera
    {
        public Vector3 Position;
        public Vector3 Front;
        public Vector3 Up;
        public float DepthNear;
        public float DepthFar;
        public float FieldOfView;
        public Matrix4 View;
        public Matrix4 Projection;
        public Matrix4 ProjectionUI;
        public Box2i Bounds;

        public Camera(Vector3 position, Vector3 front, Vector3 up, float depthNear, float depthFar, float fieldOfView)
        {
            Position = position;
            Front = front;
            Up = up;
            DepthNear = depthNear;
            DepthFar = depthFar;
            FieldOfView = fieldOfView;

            UpdateMatrix();
        }

        public void UpdateMatrix()
        {
            View = Matrix4.LookAt(Position, Position + Front, Up);
            Projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FieldOfView), Bounds.Size.X / (float)Bounds.Size.Y, DepthNear, DepthFar);
            ProjectionUI = Matrix4.CreateOrthographic(Bounds.Size.X, Bounds.Size.Y, DepthNear, DepthFar);
        }
    }
}
