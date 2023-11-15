using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System.Diagnostics;
using UnityEngine;


public class ScreenRender : MonoBehaviour
{

    public GameObject targetObject;
    public Material displayMaterial; // Assign your material in the Unity editor
    public int textureWidth = 320;   // Adjust according to your image dimensions
    public int textureHeight = 180;

    private Texture2D receivedTexture;

    // This method can be called when a new Mat is received over the network
    public void DisplayReceivedImage(Mat receivedMat)
    {
        // Convert Mat to Unity Texture2D
        receivedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
        Utils.matToTexture2D(receivedMat, receivedTexture);
        receivedTexture.Apply();

        // Apply the texture to the display material
        if(targetObject != null)
        {
            Renderer renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = renderer.material;
                if (material != null)
                {
                    material.mainTexture = receivedTexture;
                }
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Check if the required components are assigned
        if (displayMaterial == null)
        {
            UnityEngine.Debug.LogError("DisplayMaterial is not assigned!");
        } else
        {
            targetObject.GetComponent<Renderer>().enabled = true;
        }
    }

    // Implement your logic to get the next frame from the stream
    private Mat GetNextFrameFromStream()
    {
        // This is a placeholder; replace it with your logic to fetch the next frame from the stream
        // For example, if you have a list of Mat frames, you can iterate through them in sequence.
        // Ensure that you handle the looping or fetching logic according to your specific use case.
        Mat nextFrame = new Mat();
        // Implement your logic to get the next frame from the stream
        return nextFrame;
    }
}