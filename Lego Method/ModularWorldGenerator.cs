using System;
using System.Collections;
using System.Linq;
using System.Xml;
using UnityEngine;
using System.Collections.Generic;
using ProBuilder2.Common;
using Random = UnityEngine.Random;

/**
* Dungeon generation Manager.
*/ 

public class ModularWorldGenerator : MonoBehaviour
{
    public GameObject DungeonGameObject { get; private set; }
    
    public GameObject[] ModulePrefabs;
    public GameObject DoorPrefab;
    public GameObject RootModule;
    public GameObject EntranceModule;
    public GameObject ExitModule;
    public int Iterations = 5;
    public float GenerationDelay = 5f;
    public int DungeonCanvasStep = 25;
    public Camera MapCamera;
    public bool UseDelay = false;
    public List<CameraPortal> Portals;

    private Rect DungeonDimensions;
    private ActivationBox PlayerStartBox;
    private ActivationBox PlayerExitBox;
    private Vector3 DungeonMidPoint;

    private void Start()
    {
        //Portals = new List<CameraPortal>();
        StartCoroutine(GenerateNewDungeonCo());
    }

    private IEnumerator GenerateNewDungeonCo()
    {
        WaitForSeconds delay = null;
        if(UseDelay)
            delay = new WaitForSeconds(GenerationDelay);

        DungeonGameObject = new GameObject("Dungeon");
        //Get the starting module and orient it
        var startModule = ProBuilder.Instantiate(RootModule, transform.position, transform.rotation).GetComponent<Module>();
        startModule.transform.parent = DungeonGameObject.transform;
        //Get all the exits of the starting module
        var baseModuleExits = new List<ModuleConnector>(startModule.GetExits());
        //Iterate through the number of iterations
        //Only allow "Open" modules for the initial iterations
        for (int iteration = 0; iteration < Iterations; iteration++)
        {
            var newExits = new List<ModuleConnector>();
            var randomizedModules = new List<GameObject>();
            //Loop through all the basemodule exits
            foreach (var baseModuleExit in baseModuleExits)
            {
                yield return delay;
                //loop through the random list of available modules, until there is a valid candidate
                randomizedModules.Clear();
                GetRandomizedModuleList(randomizedModules, ModulePrefabs, "Open");

                GameObject newModuleGameObject = null;
                if (!CreateNewModule(baseModuleExit, randomizedModules, newExits, out newModuleGameObject))
                    SealExitWithADoor(baseModuleExit);
            }

            baseModuleExits = newExits;
        }

        StartCoroutine(ApplyFinalIterationCo(baseModuleExits));
    }

    private IEnumerator ApplyFinalIterationCo(List<ModuleConnector> finalIterationExits)
    {
        /**
         * - Loop through all the modules where the exits are still open
         * - Choose two open exits randomly, and create an "Entrance" and an "Exit" modules
         * - Seal off the rest of the exits with CLOSED rooms, if possible.
         * 
         * - If an ENTRANCE and an EXIT were not possible to create, then we have to recreate the maze
         */

        //Final iteration modules, are the modules that don't have completely connected exits
        WaitForSeconds delay = null;
        if (UseDelay)
            delay = new WaitForSeconds(GenerationDelay);

        if (finalIterationExits.Count() < 2)
        {
            Debug.Log("Reseting dungeon. Not enough final iteration exits");
            ResetDungeon();
            yield break;
        }

        //Get two random numbers for the iteration modules
        //Initialize the entrance as soon as possible, but put some space between it and the exit
        var entranceIdx = 0;
        var exitIdx = Random.Range(1, finalIterationExits.Count());
        bool startSearchingForEntrance = false;
        bool startSearchingForExit = false;
        GameObject newModuleGameObject = null;

        //Loop through all the basemodule exits
        for (var k = 0; k < finalIterationExits.Count(); k++)
        {
            var finalIterationExit = finalIterationExits[k];

            if (k == entranceIdx)
                startSearchingForEntrance = true;
            else if (k == exitIdx)
                startSearchingForExit = true;

            //Check if it is possible to create an entrance, if not, try an exit, if not try a Closed module, if not, then just seal it off
            List<GameObject> availableModulePrefabs = new List<GameObject>();
            //Trying an entrance
            if (startSearchingForEntrance)
            {
                availableModulePrefabs.Clear();
                availableModulePrefabs.Add(EntranceModule);
                if (CreateNewModule(finalIterationExit, availableModulePrefabs, null, out newModuleGameObject))
                {
                    PlayerStartBox = newModuleGameObject.transform.Find("Player Start").gameObject.GetComponent<ActivationBox>();
                    startSearchingForEntrance = false;
                    yield return delay;
                    continue;
                }
            }
            //Trying an exit
            if (startSearchingForExit)
            {
                availableModulePrefabs.Clear();
                availableModulePrefabs.Add(ExitModule);
                if (CreateNewModule(finalIterationExit, availableModulePrefabs, null, out newModuleGameObject))
                {
                    PlayerExitBox = newModuleGameObject.transform.Find("Player Exit").gameObject.GetComponent<ActivationBox>();
                    startSearchingForExit = false;
                    yield return delay;
                    continue;
                }
            }

            //Trying a closed module
            availableModulePrefabs.Clear();
            GetRandomizedModuleList(availableModulePrefabs, ModulePrefabs, "Closed");
            availableModulePrefabs.Add(ExitModule);

            if (CreateNewModule(finalIterationExit, availableModulePrefabs, null, out newModuleGameObject))
            {
                yield return delay;
                continue;
            }

            //Just seal it with a door
            SealExitWithADoor(finalIterationExit);
        }

        //If we still have these flags open, then reset the dungeon and try again.
        // This is a 1 in a million chance, and will never happen, but still account for it
        if (startSearchingForExit || startSearchingForEntrance)
        {
            Debug.Log("Reseting dungeon. Couldn't generate the entrance or the exit");
            ResetDungeon();
            yield break;
        }

        GenerateMap();
        GameManager.Instance.InitGame(PlayerStartBox.transform.position, PlayerStartBox.transform.rotation);
    }

    /**
     * The only scenario where CreateNewModule would return false, is if there was no way to generate a module, so we have to seal it off with a door
     */
    private bool CreateNewModule(ModuleConnector baseExit, List<GameObject> availableModulePrefabs, List<ModuleConnector> newExits, out GameObject moduleGameObject)
    {
        bool foundExit = false;
        bool foundModule = false;
        ModuleConnector newModuleExit = null;
        ModuleConnector[] newModuleExits = null;
        moduleGameObject = null;
        Module newModule = null;

        foreach (var newModulePrefab in availableModulePrefabs)
        {
            //Create the new module prefab
            newModule = ProBuilder.Instantiate(newModulePrefab, Vector3.zero, Quaternion.identity).GetComponent<Module>();
            newModule.transform.parent = DungeonGameObject.transform;
            newModuleExits = newModule.GetExits();
            Util.ShuffleArray(newModuleExits);

            for (var i = 0; i < newModuleExits.Count(); i++)
            {
                newModuleExit = newModuleExits[i];

                //Match the exits
                MatchExits(baseExit, newModuleExit);

                //Letting a full frame go by so we can do a collision check
                if (CollisionCheck(newModule, baseExit))
                {
                    foundModule = true;
                    foundExit = true;
                    break;
                }
            }

            //We've tried every exit combination with this module, get a new module
            if (!foundExit)
            {
                Destroy(newModule.gameObject);
                continue;
            }

            //if we've reached here, we have found a suitable exit with a suitable module, or we have not found anything suitable.
            break;
        }

        //We haven't found anything useful. Just make a door, and find another baseModuleExit
        if (!foundModule)
            return false;

        //the new module was received. All is good. Connect with the base module exit. Find another baseModuleExit to connect with
        baseExit.ConnectWith(newModuleExit);
        newModuleExit.ConnectWith(baseExit);

        if(newExits != null)
            newExits.AddRange(newModuleExits.Where(e => e != newModuleExit));

        CameraPortal cameraPortal = (CameraPortal) newModule.gameObject.GetComponentInChildren(typeof (CameraPortal));
        if (cameraPortal != null)
            Portals.Add(cameraPortal);

        moduleGameObject = newModule.gameObject;
        return true;
    }

    private void SealExitWithADoor(ModuleConnector baseExit)
    {
        //Debug.Log("Couldn't find any possible combination. Sealing off the exit with a door");
        var newModule = ProBuilder.Instantiate(DoorPrefab.gameObject, Vector3.zero, Quaternion.identity).GetComponent<Module>();
        newModule.transform.parent = DungeonGameObject.transform;
        var doorExit = newModule.GetExits()[0];
        MatchExits(baseExit, doorExit);

        baseExit.ConnectWith(doorExit);
        doorExit.ConnectWith(baseExit);
    }

    private void ResetDungeon()
    {
        Debug.Log("Reseting Dungeon");
        Destroy(DungeonGameObject);
        DungeonGameObject = null;

        StopCoroutine(GenerateNewDungeonCo());
        StopCoroutine(ApplyFinalIterationCo(null));
        StartCoroutine(GenerateNewDungeonCo());
    }

    private void GenerateMap()
    {
        /**
         * Get the mid point of the maze, which will act as the middle point of the map camera
         * Get the width and height of the maze, which will be used to approximate a ratio for the camera's distance from the camera
         * Adjust the camera based on the previous two points
         */

        //First round (MinX)
        var startingPoint = new Vector3(0, 5, -1000);
        for (int x = DungeonCanvasStep; ; x += DungeonCanvasStep)
        {
            startingPoint.x = x;

            //Debug.DrawLine(startingPoint, startingPoint + Vector3.forward * 3000, Color.cyan, 9999);
            if (Physics.Raycast(startingPoint, Vector3.forward, 3000))
                continue;

            DungeonDimensions.xMax = x;
            break;
        }

        //Second round (MaxX)
        startingPoint.x = 0;
        startingPoint.z = -1000;
        for (int x = DungeonCanvasStep; ; x -= DungeonCanvasStep)
        {
            startingPoint.x = x;

            //Debug.DrawLine(startingPoint, startingPoint + Vector3.forward * 3000, Color.blue, 9999);
            if (Physics.Raycast(startingPoint, Vector3.forward, 3000))
                continue;

            DungeonDimensions.xMin = x;
            break;
        }

        //Third round (MinZ)
        startingPoint.x = -1000;
        startingPoint.z = 0;
        for (int z = DungeonCanvasStep; ; z -= DungeonCanvasStep)
        {
            startingPoint.z = z;

            //Debug.DrawLine(startingPoint, startingPoint + Vector3.right * 3000, Color.magenta, 9999);
            if (Physics.Raycast(startingPoint, Vector3.right, 3000))
                continue;

            DungeonDimensions.yMin = z;
            break;
        }

        //Fourth round (MaxZ)
        startingPoint.x = -1000;
        startingPoint.z = 0;
        for (int z = DungeonCanvasStep; ; z += DungeonCanvasStep)
        {
            startingPoint.z = z;

            //Debug.DrawLine(startingPoint, startingPoint + Vector3.right * 3000, Color.red, 9999);
            if (Physics.Raycast(startingPoint, Vector3.right, 3000))
                continue;

            DungeonDimensions.yMax = z;
            break;
        }

        DungeonDimensions.width = (DungeonDimensions.xMax - DungeonDimensions.xMin);
        DungeonDimensions.height = (DungeonDimensions.yMax - DungeonDimensions.yMin);
        DungeonMidPoint = new Vector3(DungeonDimensions.xMin + (DungeonDimensions.width * .5f), 0, DungeonDimensions.yMin + (DungeonDimensions.height * .5f));

        if (DungeonDimensions.width >= DungeonDimensions.height)
        {
            MapCamera.transform.rotation = Quaternion.Euler(MapCamera.transform.rotation.eulerAngles.x, 0f,
                MapCamera.transform.rotation.eulerAngles.z);
        }
        else
        {
            MapCamera.transform.rotation = Quaternion.Euler(MapCamera.transform.rotation.eulerAngles.x, 90f,
                MapCamera.transform.rotation.eulerAngles.z);
        }

        //Adjust the camera accordingly
        MapCamera.transform.position = new Vector3(DungeonMidPoint.x, 50, DungeonMidPoint.z);
    }

    private bool CollisionCheck(Module newModule, ModuleConnector exit)
    {
        var bounds = newModule.GetComponentInChildren<Collider>().bounds;
        //Make an overlap sphere to get all the nearby Modules
        var radius = Mathf.Max(bounds.size.x, bounds.size.z) * 4.0f;
        //var drawRadius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f;
        //DebugDraw.DrawSphere(bounds.center, drawRadius, new Color(0f, .3f, .8f));
        Collider[] hitColliders = Physics.OverlapSphere(bounds.center, radius);

        var fatherObject = newModule.BaseModuleConnector.transform.parent.gameObject;
        //Debug.Log(newModule.gameObject + ": father: " + fatherObject);

        foreach (Collider c in hitColliders)
        {
            var possibleColliderObject = c.transform.parent.gameObject;
            if (possibleColliderObject.tag != "Module" || possibleColliderObject == fatherObject || possibleColliderObject == newModule.gameObject)
                continue;

            //Debug.Log(newModule.gameObject + ": possible collider: " + possibleColliderObject);
            if (bounds.Intersects(c.bounds))
            {
                //Debug.Log("Collision failed with : " + c.gameObject);
                return false;
            }
        }

        return true;
    }

    private void MatchExits(ModuleConnector oldExit, ModuleConnector newExit)
    {
        var newModule = newExit.transform.parent;
        var forwardVectorToMatch = -oldExit.transform.forward;
        var correctiveRotation = Azimuth(forwardVectorToMatch) - Azimuth(newExit.transform.forward);
        newModule.RotateAround(newExit.transform.position, Vector3.up, correctiveRotation);
        var correctiveTranslation = oldExit.transform.position - newExit.transform.position;
        newModule.transform.position += correctiveTranslation;

        newExit.transform.parent.GetComponent<Module>().SetBaseModule(oldExit);
    }

    private static float Azimuth(Vector3 vector)
    {
        return Vector3.Angle(Vector3.forward, vector) * Mathf.Sign(vector.x);
    }

    private static void GetRandomizedModuleList(List<GameObject> list, GameObject[] moduleObjects, string newTag)
    {
        foreach (var moduleObject in moduleObjects)
        {
            var module = moduleObject.GetComponent<Module>();
            if (module.Tags.Contains(newTag))
                list.Add(moduleObject);
        }

        Util.ShuffleList(list);
    }
}