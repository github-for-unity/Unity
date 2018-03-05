using UnityEngine;
using System.Collections.Generic;

namespace GitHub.Unity
{
    class Spinner
    {
        struct Rotation
        {
            public float rotation;
            public Vector2 center;
            public Rotation(float rotation, Vector2 center)
            {
                this.rotation = rotation;
                this.center = center;
            }

            public override string ToString()
            {
                return string.Format("rotation:{0} center:{1}:{2}", rotation, center.x, center.y);
            }
        }

        private float speed = 120f;
        private float currentRotation;
        private float lastTime;
        private bool started;
        private float centerX;
        private float centerY;
        private Vector2 center;
        private Vector2 codeIconTopLeft;
        private Vector2 mergeIconTopLeft;
        private Vector2 rocketIconTopLeft;
        private Rect backgroundRect;
        private Rect outsideRect;
        private Rect insideRect;
        private Vector2 rocketIconCenter;
        private Vector2 codeIconCenter;
        private Vector2 mergeIconCenter;

        private Texture2D Inside { get { return Styles.SpinnerInside; } }
        private Texture2D Outside { get { return Styles.SpinnerOutside; } }
        private Texture2D Code { get { return Styles.Code; } }
        private Texture2D Merge { get { return Styles.Merge; } }
        private Texture2D Rocket { get { return Styles.Rocket; } }
        private Stack<Rotation> rotations = new Stack<Rotation>();

        public Spinner()
        {
        }

        public void Start(float currentTime)
        {
            if (started)
                return;
            started = true;
        }

        public void Stop()
        {
            started = false;
        }

        public float Rotate(float currentTime)
        {
            var deltaTime = currentTime - lastTime;
            currentRotation += deltaTime * speed * ((Mathf.Sin(currentTime * 1.2f)) + 2);
            currentRotation = Mathf.Repeat(currentRotation, 360f);
            lastTime = currentTime;
            return currentRotation;
        }

        public Rect Layout(Rect rect)
        {
            var width = rect.width;
            var height = rect.height;
            var x = rect.x;
            var y = rect.y;
            centerX = width / 2 + x;
            centerY = height / 2 + y;
            center = new Vector2(centerX, centerY);
            outsideRect = new Rect(centerX - Outside.width / 2, centerY - Outside.height / 2 - 11, Outside.width, Outside.height);
            codeIconTopLeft = new Vector2(centerX - Code.width / 2 - 0.6f, centerY - Code.height / 2 - 45);
            mergeIconTopLeft = new Vector2(centerX - Merge.width / 2 + 38, centerY - Merge.height / 2 + 24);
            rocketIconTopLeft = new Vector2(centerX - Rocket.width / 2 - 39, centerY - Rocket.height / 2 + 24);
            backgroundRect = new Rect(x, y, width, height);
            insideRect = new Rect(centerX - Inside.width / 2, centerY - Inside.height / 2, Inside.width, Inside.height);
            codeIconCenter = new Vector2(codeIconTopLeft.x + Code.width / 2 - .1f, codeIconTopLeft.y + Code.height / 2 + 0.1f);
            rocketIconCenter = new Vector2(rocketIconTopLeft.x + Rocket.width / 2, rocketIconTopLeft.y + Rocket.height / 2);
            mergeIconCenter = new Vector2(mergeIconTopLeft.x + Merge.width / 2, mergeIconTopLeft.y + Merge.height / 2);
            return outsideRect;
        }

        public void Render()
        {
            var matrix = GUI.matrix;

            // draw the background
            GUI.DrawTexture(backgroundRect, Utility.GetTextureFromColor(new Color(0.2f, 0.2f, 0.2f, 0.9f)));

            // draw the center
            GUI.DrawTexture(insideRect, Inside);

            // draw the outside ring, rotated
            PushRotation(currentRotation, center);
            GUI.DrawTexture(outsideRect, Outside);
            PopRotation();

            // draw the code icon inside the ring unrotated
            PushRotation(-currentRotation, codeIconCenter);
            PushRotation(currentRotation, center);
            GUI.DrawTexture(new Rect(codeIconTopLeft.x, codeIconTopLeft.y, Code.width, Code.height), Code);
            PopRotation();
            PopRotation();

            // draw the merge icon inside the ring unrotated
            PushRotation(-currentRotation, mergeIconCenter);
            PushRotation(currentRotation, center);
            GUI.DrawTexture(new Rect(mergeIconTopLeft.x, mergeIconTopLeft.y, Merge.width, Merge.height), Merge);
            PopRotation();
            PopRotation();

            // draw the rocket icon inside the ring unrotated
            PushRotation(-currentRotation, rocketIconCenter);
            PushRotation(currentRotation, center);
            GUI.DrawTexture(new Rect(rocketIconTopLeft.x, rocketIconTopLeft.y, Rocket.width, Rocket.height), Rocket);
            PopRotation();
            PopRotation();

            GUI.matrix = matrix;
        }

        private void PushRotation(float rotation, Vector2 rotCenter)
        {
            rotations.Push(new Rotation(rotation, rotCenter));
            GUIUtility.RotateAroundPivot(rotation, rotCenter);
        }

        private void PopRotation()
        {
            var rot = rotations.Pop();
            GUIUtility.RotateAroundPivot(-rot.rotation, rot.center);
        }
    }
}