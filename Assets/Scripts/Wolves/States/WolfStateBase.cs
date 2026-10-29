/// <summary>
/// Base de los estados del lobo. Cada estado decide sus propias transiciones.
/// </summary>
public abstract class WolfStateBase : State<WolfAgent.States>
{
    protected readonly WolfAgent Wolf;

    protected WolfStateBase(WolfAgent wolf)
    {
        Wolf = wolf;
    }
}
