using System;

namespace GitHub.Unity
{
    [Serializable]
    public struct GitCountObjects
    {
        public static GitCountObjects Default = new GitCountObjects();

        public int objects;
        public int kilobytes;

        public GitCountObjects(int objects, int kilobytes)
        {
            this.objects = objects;
            this.kilobytes = kilobytes;
        }

        public int Objects => objects;

        public int Kilobytes => kilobytes;
    }
}