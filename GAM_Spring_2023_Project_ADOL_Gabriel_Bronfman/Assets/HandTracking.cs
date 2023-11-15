using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using System.Runtime.InteropServices.ComTypes;
using System.Linq.Expressions;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;

public class HandTracking : MonoBehaviour
{
    public GameObject sphereMarker;

    public ScreenRender screen_source;
   
    GameObject palmObject;
    GameObject leftPalmObject;


    GameObject rightThumb;
    GameObject rightIndex;

    MixedRealityPose pose;
    MixedRealityPose leftPose;

    MixedRealityPose rightIndexPose;
    MixedRealityPose rightThumbPose;

    StreamWriter streamWriter;

    public int frame_width_;
    public int frame_height_;
    public Mat recvd_img_;
    public byte[] img_buffer_;

    double[] jointAngles = new double[3];
    string positionTotal = string.Empty;
    string orientationTotal = string.Empty;
    string leftPositionTotal = string.Empty;

    double groundDistance = 0;

    //int timeStep = 0;
   // int lines = 0;
    float lse = .01F;
    float l1 = .0285F;
    float l2 = .0904F;
    float l3 = .14561F;

    string currentPosition = string.Empty;
    string currentOrientation = string.Empty;

    double[] joint_angles = { 0, 0, 0, 0, 0, 0, 0, 0 };
    byte[] inBuffer;
    int count = 0, interval = 1;
    float[] head_location = new float[3];

    TcpClient tcp_client;
    TcpListener tcp_listnener = new TcpListener(IPAddress.Parse("127.0.0.1"), 12345);
    // server starts

    // Start is called before the first frame update

    public void PlaceObject(Vector3 location)
    {
        groundDistance = location.y;
    }

    void Start()
    {
        frame_height_ = 180;
        frame_width_ = 320;
        recvd_img_ = new Mat(frame_height_, frame_width_, CvType.CV_8UC4);
        this.img_buffer_ = new byte[this.frame_height_ * this.frame_width_ * 4];

        palmObject = Instantiate(sphereMarker, this.transform);
        leftPalmObject = Instantiate(sphereMarker, this.transform);

        rightThumb = Instantiate(sphereMarker, this.transform);
        rightIndex = Instantiate(sphereMarker, this.transform);

        tcp_listnener.Start();


        print("Waiting for the client");
        tcp_client = tcp_listnener.AcceptTcpClient(); // check the client's access
        print("Client is connected");
        //screen_source.GetComponent<Renderer>().enabled = true;
        //StreamReader stream_reader = new StreamReader(tcp_client.GetStream());  // Connect to read stream
        //StreamWriter stream_writer = new StreamWriter(tcp_client.GetStream());  // Connect to write stream
        //stream_writer.AutoFlush = true;  // flush?

        //joint_angles = { 0, 0, 0, 0, 0, 0, 0, 0 }; // make 8 elements of double array
        // right arm (sh1, sh2, elbow), left arm, head pan, tilt
        inBuffer = new byte[joint_angles.Length * sizeof(double)];


    }

    // Update is called once per frame
    void Update()
    {
        Vector3 eulerAngles = Camera.main.transform.rotation.eulerAngles;
        if (eulerAngles[1] > 240)
        {
            eulerAngles[1] = eulerAngles[1] - 360;
        }
        if (eulerAngles[0] > 240)
        {
            eulerAngles[0] = eulerAngles[0] - 360;
        }

        joint_angles[6] = -(eulerAngles[1] * 3.14 / 180) % (2 * Math.PI); // Pan
        joint_angles[7] = (-eulerAngles[0] * 3.14 / 180) % (2 * Math.PI); // Tilt
        palmObject.GetComponent<Renderer>().enabled = false;
        leftPalmObject.GetComponent<Renderer>().enabled = false;

        head_location[0] = Camera.main.transform.position[0];
        head_location[1] = Camera.main.transform.position[1];
        head_location[2] = Camera.main.transform.position[2];


        screen_source.transform.position = Camera.main.transform.position;
        screen_source.transform.rotation = Camera.main.transform.rotation;
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, Handedness.Right, out pose)){
            palmObject.GetComponent<Renderer>().enabled = true;
            palmObject.transform.position = pose.Position;
            
            jointAngles = inverseKinematicsRightArm(palmObject.transform.position[0] - head_location[0], palmObject.transform.position[1] - head_location[1], palmObject.transform.position[2] - head_location[2]);
            currentPosition = joint_angles[0].ToString("F2")  + " " + joint_angles[1].ToString("F2") + " " + joint_angles[2].ToString("F2") + " " + joint_angles[3].ToString("F2") + " " + joint_angles[4].ToString("F2") + " " + joint_angles[5].ToString("F2") + " " + joint_angles[6].ToString("F2") + " " + joint_angles[7].ToString("F2")  +"\n";
            positionTotal = positionTotal + currentPosition;
            joint_angles[0] = jointAngles[0];
            joint_angles[2] = jointAngles[1];
            joint_angles[4] = jointAngles[2];
        } 
        else
        {
           using (StreamWriter streamWriter = new StreamWriter(UnityEngine.Application.dataPath + "/logs/" + "pose_estimation_right_hand.txt"))
           {
               streamWriter.WriteLine(positionTotal);
           }


        }

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, Handedness.Left, out leftPose))
        {
            leftPalmObject.GetComponent<Renderer>().enabled = true;
            leftPalmObject.transform.position = leftPose.Position;
            jointAngles = inverseKinematicsLeftArm(leftPalmObject.transform.position[0] - head_location[0], leftPalmObject.transform.position[1] - head_location[1], leftPalmObject.transform.position[2] - head_location[2]);
            
            //for (int i = 0; i < 3; i++)
            //{
            //    jointAngles[i] = 180.0 * jointAngles[i] / 3.141592;
            //}
            currentPosition = leftPalmObject.transform.position[0].ToString("F2") + ", " + leftPalmObject.transform.position[1].ToString("F2") + ", " + leftPalmObject.transform.position[2].ToString("F2") + "\n";
            joint_angles[1] = jointAngles[0];
            joint_angles[3] = jointAngles[1];
            joint_angles[5] = jointAngles[2];
            leftPositionTotal = leftPositionTotal + joint_angles[1].ToString("F2") + ", " + joint_angles[0].ToString("F2") + "\n";

        }
        else
        {
            using (StreamWriter leftStreamWriter = new StreamWriter(UnityEngine.Application.dataPath + "/logs/" + "pose_estimation_left_hand.txt"))
            {
                leftStreamWriter.WriteLine(leftPositionTotal);
            }
        }

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Right, out rightIndexPose) && HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Right, out rightThumbPose)){
            rightIndex.transform.position = rightIndexPose.Position;
            rightThumb.transform.position = rightThumbPose.Position;
            double distance = Math.Sqrt(Math.Pow(rightIndex.transform.position[0] - rightThumb.transform.position[0], 2) + Math.Pow(rightIndex.transform.position[1] - rightThumb.transform.position[1], 2) + Math.Pow(rightIndex.transform.position[2] - rightThumb.transform.position[2], 2));
            if (distance < .015)
            {
                UnityEngine.Debug.Log("I am in grasp mode");
            }
        }
        


        Buffer.BlockCopy(joint_angles, 0, inBuffer, 0, inBuffer.Length);
        if (!tcp_client.Connected)
        {
            //Console.Write("Client is disconnected");
            //tcp_client.GetStream().Close();
            tcp_client.Client.Close();
            tcp_client.Client.Dispose();

            Console.WriteLine("Waiting for the client");
            tcp_client = tcp_listnener.AcceptTcpClient(); // check the client's access
            Console.WriteLine("Client is connected");
            for (int i = 0; i < 8; i++)
            {
                joint_angles[i] = 0;
            }
            count = 0;
            interval = 1;
        }

        try
        {
            tcp_client.Client.Send(inBuffer);
        }
        catch (SocketException e)
        {
            Console.WriteLine("Client is disconnected");
        }

        this.receiveCameraImage();
        this.screen_source.DisplayReceivedImage(this.recvd_img_);
        print("Current height: " + (Math.Abs(head_location[1] - groundDistance)));



    }

    double[] inverseKinematicsRightArm(float CameraX, float CameraY, float CameraZ)
    {
        double q1, q2, q3, D;
        double x, y, z;

        double lambda = .41;

        

        x = lambda * (-CameraY - .2794);
        y = lambda * (CameraZ + .1524);
        z = lambda * (CameraX - .1878);
        

        q1 = Math.Atan2(y, x);
        D = Math.Sqrt(Math.Pow(Math.Sqrt(Math.Pow(y,2) + Math.Pow(x,2)) - lse,2) + Math.Pow((z - l1),2));
        //print((Math.Pow(l2, 2) + Math.Pow(l3, 2) - Math.Pow(D, 2)) / (2 * l3 * l2));
        q3 = Math.PI - Math.Acos((Math.Pow(l2,2) + Math.Pow(l3,2) - Math.Pow(D,2)) / (2 * l3 * l2));
        q2 = -(Math.Atan2(Math.Sqrt(Math.Pow(y,2) + Math.Pow(x,2)) - lse, z - l1) - Math.Atan2(l3 * Math.Sin(q3), l2 + l3 * Math.Cos(q3)));


        q1 = Math.Clamp(q1, -(Math.PI) / 2, (Math.PI) / 2);
        q2 = Math.Clamp(q2, -(Math.PI) / 2, (Math.PI) / 2);
        q3 = Math.Clamp(q3, -(Math.PI) / 2, (Math.PI) / 2);
        return new double [] { q1, q2, q3 };
    }

    double[] inverseKinematicsLeftArm(float CameraX, float CameraY, float CameraZ)
    {
        double q1, q2, q3, D;
        double x, y, z;

        double lambda = .41;



        x = lambda * (-CameraY - .2794);
        y = lambda * -(-CameraZ - .1524);
        z = lambda * (-CameraX - .1578);


        q1 = Math.Atan2(y,x);
        D = Math.Sqrt(Math.Pow(Math.Sqrt(Math.Pow(y, 2) + Math.Pow(x, 2)) - lse, 2) + Math.Pow((z - l1), 2));
        //print((Math.Pow(l2, 2) + Math.Pow(l3, 2) - Math.Pow(D, 2)) / (2 * l3 * l2));
        q3 = Math.PI - Math.Acos((Math.Pow(l2, 2) + Math.Pow(l3, 2) - Math.Pow(D, 2)) / (2 * l3 * l2));
        
        q2 = -Math.Atan2(Math.Sqrt(Math.Pow(y, 2) + Math.Pow(x, 2)) - lse, z - l1) + Math.Atan2(l3 * Math.Sin(q3), l2 + l3 * Math.Cos(q3));

        q1 = Math.Clamp(q1, -(Math.PI) / 2, (Math.PI) / 2);
        q2 = Math.Clamp(q2, -(Math.PI) / 2, (Math.PI) / 2);
        q3 = Math.Clamp(q3, -(Math.PI) / 2, (Math.PI) / 2);

        q1 = -q1;
        q2 = -q2;
        q3 = -q3;



        return new double[] { q1, q2, q3 };
    }

    public void receiveCameraImage()
    {

        int recvd_length = 0;
        try
        {
            recvd_length = tcp_client.Client.Receive(this.img_buffer_);
        }
        catch (SocketException e)
        {
            Console.WriteLine(e.ToString());
            
        }
        IntPtr dataPtr = new IntPtr((long)recvd_img_.dataAddr());

        
        Marshal.Copy(img_buffer_, 0, dataPtr, frame_width_ * frame_height_ * 4);
        Console.WriteLine("img received: " + recvd_length.ToString());

    }

    static int GetArrayLength(IntPtr arrayPointer)
    {
        // Iterate through the array until a null value (0) is encountered
        int arrayLength = 0;
        while (Marshal.ReadByte(arrayPointer, arrayLength) != 0)
        {
            arrayLength++;
        }

        return arrayLength;
    }
}
