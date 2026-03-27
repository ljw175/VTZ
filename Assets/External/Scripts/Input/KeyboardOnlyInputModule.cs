using UnityEngine.EventSystems;

public class KeyboardOnlyInputModule : StandaloneInputModule
{
    public override void Process()
    {
        bool usedEvent = SendUpdateEventToSelectedObject();
        if (!usedEvent)
            SendMoveEventToSelectedObject();
        SendSubmitEventToSelectedObject();
    }
}