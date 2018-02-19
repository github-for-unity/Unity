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

        private readonly float totalAnimationTime = 2f;
        private float currentRotation;
        private float startTime;
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
            startTime = currentTime;
        }

        public void Stop()
        {
            started = false;
        }

        public float Rotate(float currentTime)
        {
            var elapsed = Mathf.Repeat(currentTime - startTime, totalAnimationTime);
            currentRotation = Linear(elapsed, 360f, totalAnimationTime);
            currentRotation = Mathf.Repeat(currentRotation, 360f);
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

        private void PushRotation(float rotation, Vector2 center)
        {
            rotations.Push(new Rotation(rotation, center));
            GUIUtility.RotateAroundPivot(rotation, center);
        }

        private void PopRotation()
        {
            var rot = rotations.Pop();
            GUIUtility.RotateAroundPivot(-rot.rotation, rot.center);
        }

        private float ExpoEase(float currentTime, float end, float duration)
        {
            //Debug.LogFormat("ExpoEase: {0} {1}", currentTime, duration);
            currentTime /= duration / 2;
            if (currentTime < 1) return duration / 2 * Mathf.Pow(2, 10 * (currentTime - 1));
            currentTime--;
            return end / 2 * (-Mathf.Pow(2, -10 * currentTime) + 2);
        }
        private float SinEase(float currentTime, float end, float duration)
        {
            return -end / 2 * (Mathf.Cos(Mathf.PI * currentTime / duration) - 1);
        }

        private float SinEaseIn(float t, float c, float d)
        {
            //Debug.LogFormat("SinEaseIn: {0} {1}", t, d);

            return -c * Mathf.Cos(t / d * (Mathf.PI / 2)) + c;
        }
        private float SinEaseOut(float t, float c, float d)
        {
            //Debug.LogFormat("SinEaseOut: {0} {1}", t, d);
            return c * Mathf.Sin(t / d * (Mathf.PI / 2));
        }

        private float Linear(float t, float c, float d)
        {
            //Debug.LogFormat("Linear: {0} {1}", t, d);
            return c * t / d;
        }

        private float CubicIn(float t, float c, float d)
        {
            //Debug.LogFormat("CubicIn: {0} {1}", t, d);
            t /= d;
            return c * t * t * t;
        }

        private float CubicOut(float t, float c, float d)
        {
            //Debug.LogFormat("CubicOut: {0} {1}", t, d);
            t /= d;
            t--;
            return c * (t * t * t + 1);
        }

        private float CubicInOut(float t, float c, float d)
        {
            t /= d / 2;
            if (t < 1) return c / 2 * t * t * t;
            t -= 2;
            return c / 2 * (t * t * t + 2);
        }

        private float QuintInOut(float t, float c, float d)
        {
            t /= d / 2;
            if (t < 1) return c / 2 * t * t * t * t * t;
            t -= 2;
            return c / 2 * (t * t * t * t * t + 2);
        }
    }
}