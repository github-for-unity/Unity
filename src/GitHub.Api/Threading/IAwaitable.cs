namespace GitHub.Unity
{
    interface IAwaitable
    {
        IAwaiter GetAwaiter();
    }
}