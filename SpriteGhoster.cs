using System;
using System.Collections;
using System.Collections.Generic;
using NibletUtility;
using UnityEngine;
using NibletUtility;

/*namespace NibletRPG.Effects
{
    
}*/
public class SpriteGhoster : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteSource;

    public int poolSize = 100;
    private bool isPrimed = false;

    public bool onByDefault = false;
    public float ghostRate = 5;
    public Color defaultColor = Color.white;
    public float defaultFadeSpeed = 3;
    [Tooltip("Set this in inspector otherwise the ghost effect will not work")]
    [SerializeField] private AnimationCurve fadeSpeedOverTime;
    
    private float intervalTimer;
    private bool isGhosting = false;
    private float ghostingTime;

    private Transform afterImageContainer;
    private List<SpriteRenderer> activeAfterImages = new List<SpriteRenderer>();
    private List<Color> afterImageColors = new List<Color>();
    private List<float> afterImageFadeSpeeds = new List<float>();

    private Queue<GameObject> afterImages = new Queue<GameObject>();

    private List<int> itemsToRemove = new List<int>();

    public bool shouldCopySortingOrder = true;
    public bool shouldCopyMaterial = false;

    public bool primeOnStart = true;
    [Tooltip(
        "When true, all pool objects are destroyed when no after images remain which clears up the inspector. The priming" +
        " function will be called again when ghosting starts up again, but this may cause" +
        " a spiked frame rate drop as all pool objects will need to be created in a single frame. " +
        "When false, the 'isPrimed' variable will never be false after the first use." +
        " Recommended to be set to false for release builds.")]
    public bool shouldUnloadUponNoGhosts = false;

    private void Start()
    {
        if (spriteSource == null)
        {
            spriteSource = GetComponent<SpriteRenderer>();
        }
        
        if(primeOnStart) PrimeGhoster();

        if (onByDefault)
        {
            StartGhosting();
        }
    }

    void PrimeGhoster()
    {
        afterImageContainer = new GameObject().transform;
        afterImageContainer.name = gameObject.name + "_afterImages";
        //Initialise
        for (int i = 0; i < poolSize; i++)
        {
            GameObject newAfterImage = new GameObject();
            newAfterImage.transform.parent = afterImageContainer;
            newAfterImage.AddComponent<SpriteRenderer>();
            newAfterImage.SetActive(false);
            newAfterImage.name = gameObject.name + "_afterImage_" + i;
            afterImages.Enqueue(newAfterImage);
        }

        isPrimed = true;
    }

    void UnloadGhoster()
    {
        //Removes all objects 
        isGhosting = false;
        Destroy(afterImageContainer.gameObject);
        activeAfterImages.Clear();
        afterImageColors.Clear();
        afterImageFadeSpeeds.Clear();

        isPrimed = false;
    }

    void Update()
    {

        //Handles after image creation
        if (isGhosting)
        {
            ghostingTime += Time.deltaTime;

            intervalTimer += Time.deltaTime;
            if (intervalTimer >= 1/ghostRate)
            {
                CreateNewGhostSprite();
                intervalTimer = 0;
            }
        }

        //Controls after image fades
        for (int i = 0; i < activeAfterImages.Count; i++)
        {
            Color currentAfterImageCol = afterImageColors[i];
            afterImageColors[i] = Vector4.MoveTowards(currentAfterImageCol,
                new Color(currentAfterImageCol.r, currentAfterImageCol.g, currentAfterImageCol.b, 0),
                afterImageFadeSpeeds[i] * Time.deltaTime);
            activeAfterImages[i].color = afterImageColors[i];

            if (activeAfterImages[i].color.a <= 0)
            {
                itemsToRemove.Add(i);
            }
        }

        //removes after images at 0 alpha
        if (itemsToRemove.Count != 0)
        {
            foreach (var item in itemsToRemove)
            {
                afterImages.Enqueue(activeAfterImages[item].gameObject);
                activeAfterImages[item].gameObject.SetActive(false);

                activeAfterImages.Remove(activeAfterImages[item]);
                afterImageColors.RemoveIndexItem(item);
                afterImageFadeSpeeds.RemoveIndexItem(item);
            }
            
            itemsToRemove.Clear();
            
            //Unloads ghoster when no more after images exist, if allowed
            if (shouldUnloadUponNoGhosts)
            {
                if (activeAfterImages.Count == 0)
                {
                    UnloadGhoster();
                }
            }
        }
    }

    public void StartGhosting()
    {
        if(!isPrimed) PrimeGhoster();   //Sets up object pool if not set up already
        
        afterImageContainer.position = transform.position;
        isGhosting = true;
        ghostingTime = 0;

        intervalTimer = 0;
    }

    public void StopGhosting()
    {
        isGhosting = false;
    }

    void CreateNewGhostSprite()
    {
        if(afterImages.Count == 0) return;  //break function if no ghosts left in queue
        GameObject newObject = afterImages.Dequeue();           //get afterImage from queue

        if (newObject == null) return; //null check
        
        newObject.transform.position = transform.position;      //set afterImage position
        newObject.transform.rotation = transform.rotation;      //set afterImage position
        newObject.transform.localScale = transform.localScale;      //set afterImage position
        SpriteRenderer afterImageSprite = newObject.GetComponent<SpriteRenderer>();     //gets sprend of image
        afterImageSprite.sprite = spriteSource.sprite;          //set afterImage sprite
        afterImageSprite.color = defaultColor;
        if (shouldCopySortingOrder) afterImageSprite.sortingLayerID = spriteSource.sortingLayerID;
        if (shouldCopySortingOrder) afterImageSprite.sortingOrder = spriteSource.sortingOrder;
        if (shouldCopyMaterial) afterImageSprite.material = spriteSource.material;
        
        activeAfterImages.Add(newObject.GetComponent<SpriteRenderer>());
        afterImageColors.Add(defaultColor);
        afterImageFadeSpeeds.Add(defaultFadeSpeed * fadeSpeedOverTime.Evaluate(ghostingTime));
        
        newObject.SetActive(true);
        
    }
}
