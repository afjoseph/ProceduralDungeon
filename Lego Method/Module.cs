using UnityEngine;

/**
* Each "Module" can be a room, corridor, closed room, anything that is walkable.
* Even a door can be a module that seals off the exit.
*
* A base module is the parent module that this module is connected to.
*/ 

public class Module : MonoBehaviour
{
    public string[] Tags;
    public bool didCollide { get; set; }
    public ModuleConnector BaseModuleConnector { get; private set; }

    public void SetBaseModule(ModuleConnector baseModuleConnector)
    {
        BaseModuleConnector = baseModuleConnector;
    }

    public ModuleConnector[] GetExits()
    {
        return GetComponentsInChildren<ModuleConnector>();
    }
}