using System;
using System.Collections;
using System.Collections.Generic;
using CodingConnected.TraCI.NET;
using CodingConnected.TraCI.NET.Types;
using UnityEngine;

using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using CodingConnected.TraCI.NET.Commands;
using Color = UnityEngine.Color;
using Object = System.Object;

public class Traci_one : MonoBehaviour
{

    public Light ttLight;
    public GameObject tLight;
    public Material roadmat;
    public List<string> vehicleidlist;
    public List<GameObject> carlist;
    public GameObject NPCVehicle;
    public TraCIClient client;
    private List<string> tlightids;
    private int phaser;
    private List<traLights> listy;
    public Dictionary<string, List<traLights>> dicti;


    // Start is called before the first frame update

    void Start()
    {



        client = new TraCIClient();
        client.Connect("127.0.0.1", 4001); //connects to SUMO simulation

        tlightids = client.TrafficLight.GetIdList().Content; //all traffic light IDs in the simulation
        client.Gui.TrackVehicle("View #0", "0");
        client.Gui.SetZoom("View #0", 1200); //tracking the player vehicle



        createTLS();
        client.Control.SimStep();
        client.Control.SimStep();//making sure vehicle is loaded in

        client.Vehicle.SetSpeed("0", 0); //stops SUMO controlling player vehicle



    }

    private void OnApplicationQuit()
    {
        client.Control.Close();//terminates the connection upon ending of the scene
    }

    // Update is called once per frame
    private void FixedUpdate()
    {


        var newvehicles = client.Simulation.GetDepartedIDList("0").Content; //new vehicles this step
        var vehiclesleft = client.Simulation.GetArrivedIDList("0").Content; //vehicles that have left the simulation



        //if any vehicles have left the scene they are removed and destroyed
        for (int j = 0; j < vehiclesleft.Count; j++)
        {
            GameObject toremove = GameObject.Find(vehiclesleft[j]);

            if (toremove)
            {

                carlist.Remove(toremove);
                Destroy(toremove);


            }

        }


        for (int carid = 1; carid < carlist.Count; carid++)
        {



            var carpos = client.Vehicle.GetPosition(carlist[carid].name).Content; //gets position of NPC vehicle

            carlist[carid].transform.position = new Vector3((float)carpos.X, 1.33f, (float)carpos.Y);



            var newangle = client.Vehicle.GetAngle(carlist[carid].name).Content; //gets angle of NPC vehicle
            carlist[carid].transform.rotation = Quaternion.Euler(0f, (float)newangle, 0f);



        }

        for (int i = 0; i < newvehicles.Count; i++)
        {






            var newcarposition = client.Vehicle.GetPosition(newvehicles[i]).Content; //gets position of new vehicle



            GameObject newcar = GameObject.Instantiate(NPCVehicle); //creates the vehicle GameObject
            newcar.transform.position = new Vector3((float)newcarposition.X, 1.33f,
                (float)newcarposition.Y);//maps its initial position
            var newangle = client.Vehicle.GetAngle(newvehicles[i]).Content;
            newcar.transform.rotation = Quaternion.Euler(0f, (float)newangle, 0f);//maps initial angle

            newcar.name = newvehicles[i];//object name the same as SUMO simulation version
            carlist.Add(newcar);

        }
        var currentphase = client.TrafficLight.GetCurrentPhase("42443658");
        client.Control.SimStep();
        //checks traffic light's phase to see if it has changed
        if (client.TrafficLight.GetCurrentPhase("42443658").Content != currentphase.Content)
        {
            changeTrafficLights();
        }







    }



    //Changes traffic lights to their next phase
    void changeTrafficLights()
    {
        for (int i = 0; i < tlightids.Count; i++)
        {
            //for each traffic light value of a junctions name
            for (int k = 0; k < dicti[tlightids[i]].Count; k++)
            {

                var newstate = client.TrafficLight.GetState(tlightids[i]).Content;
                var lightchange = dicti[tlightids[i]][k]; //retrieves traffic light object from list

                var chartochange = newstate[lightchange.index].ToString();//traffic lights new state based on its index
                if (lightchange.isdual == false)
                {
                    lightchange.changeState(chartochange.ToLower());//single traffic light change
                }
                else
                {
                    lightchange.changeStateDual(chartochange.ToLower());//dual traffic light change
                }

            }
        }

    }


    // Creates the TLS for of all junctions in the SUMO simulation

    void createTLS()
    {
        dicti = new Dictionary<string, List<traLights>>(); //the dictionary to hold each junctions traffic lights
        for (int ids = 0; ids < tlightids.Count; ids++)
        {
            List<traLights> traLightslist = new List<traLights>();
            int numconnections = 0;  //The index that represents the traffic light's state value
            var newjunction = GameObject.Find(tlightids[ids]); //the traffic light junction
            for (int i = 0; i < newjunction.transform.childCount; i++)
            {
                bool isdouble = false;
                var trafficlight = newjunction.transform.GetChild(i);//the next traffic light in the junction
                //Checks if the traffic light has more than 3 lights
                if (trafficlight.childCount > 3)
                {
                    isdouble = true;
                }
                Light[] newlights = trafficlight.GetComponentsInChildren<Light>();//list of light objects belonging to
                                                                                  //the traffic light
                                                                                  //Creation of the traffic light object, with its junction name, list of lights, index in the junction
                                                                                  //and if it is a single or dual traffic light
                traLights newtraLights = new traLights(newjunction.name, newlights, numconnections, isdouble);
                traLightslist.Add(newtraLights);
                var linkcount = client.TrafficLight.GetControlledLinks(newjunction.name).Content.NumberOfSignals;
                var laneconnections = client.TrafficLight.GetControlledLinks(newjunction.name).Content.Links;
                if (numconnections + 1 < linkcount - 1)
                {
                    numconnections++;//index increases
                    //increases index value until the next lane is reached
                    while ((laneconnections[numconnections][0] == laneconnections[numconnections - 1][0] || isdouble) &&
                           numconnections < linkcount - 1)
                    {
                        //if the next lane is reached but the traffic light is a dual lane, continue until the
                        //lane after is reached
                        if (laneconnections[numconnections][0] != laneconnections[numconnections - 1][0] && isdouble)
                        {
                            isdouble = false;
                        }
                        numconnections++;
                    }
                }
            }
            dicti.Add(newjunction.name, traLightslist);
        }
        changeTrafficLights(); //displays the initial state of all traffic lights
        print(550.4 + +0.54 + 776.4);
    }

    // 设置所有左转为绿灯，直行为红灯的方法
    public void SetLeftTurnsToGreen()
    {
        // 遍历所有交通灯
        foreach (string tlightId in tlightids)
        {
            try
            {
                // 获取该交通灯控制的所有连接
                var controlledLinks = client.TrafficLight.GetControlledLinks(tlightId).Content;
                
                // 创建转向类型分析结果数组
                var linkDirections = new TurnDirection[controlledLinks.NumberOfSignals];
                
                // 分析每个信号控制的连接的转向类型
                for (int i = 0; i < controlledLinks.NumberOfSignals; i++)
                {
                    var link = controlledLinks.Links[i];
                    if (link.Length >= 2 && !string.IsNullOrEmpty(link[0]) && !string.IsNullOrEmpty(link[1]))
                    {
                        // 获取进入车道和目标车道
                        string incomingLane = link[0];
                        string targetLane = link[1];
                        
                        // 分析转向类型
                        linkDirections[i] = DetermineTurnDirection(incomingLane, targetLane);
                        Debug.Log($"Traffic light {tlightId}, link {i}: {incomingLane} -> {targetLane}, direction: {linkDirections[i]}");
                    }
                    else
                    {
                        linkDirections[i] = TurnDirection.Unknown;
                    }
                }
                
                // 根据分析结果创建信号灯状态字符串
                string stateString = GenerateStateString(linkDirections);
                Debug.Log($"Setting traffic light {tlightId} to state: {stateString}");
                
                // 设置信号灯状态
                SetTrafficLightProgram(tlightId, stateString);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing traffic light {tlightId}: {ex.Message}");
            }
        }
        
        // 更新Unity场景中的交通灯显示
        changeTrafficLights();
    }

    // 根据进入车道和目标车道判断转向类型
    private TurnDirection DetermineTurnDirection(string incomingLane, string targetLane)
    {
        try
        {
            // 获取车道的形状（坐标点序列）
            var incomingShape = client.Lane.GetShape(incomingLane).Content;
            var targetShape = client.Lane.GetShape(targetLane).Content;
            
            if (incomingShape.Count < 2 || targetShape.Count < 2)
            {
                return TurnDirection.Unknown;
            }
            
            // 获取进入车道的末端方向向量
            var inDirection = new Vector2(
                (float)(incomingShape[incomingShape.Count - 1].X - incomingShape[incomingShape.Count - 2].X),
                (float)(incomingShape[incomingShape.Count - 1].Y - incomingShape[incomingShape.Count - 2].Y)
            ).normalized;
            
            // 获取目标车道的起始方向向量
            var outDirection = new Vector2(
                (float)(targetShape[1].X - targetShape[0].X),
                (float)(targetShape[1].Y - targetShape[0].Y)
            ).normalized;
            
            // 计算夹角的余弦值
            float cosAngle = Vector2.Dot(inDirection, outDirection);
            
            // 计算叉积来判断转向方向（左/右）
            float crossProduct = inDirection.x * outDirection.y - inDirection.y * outDirection.x;
            
            // 判断转向类型
            // cosAngle接近1表示直行，接近0或为负表示转弯
            // crossProduct为正表示左转，为负表示右转
            if (cosAngle > 0.866f) // cos(30°) ≈ 0.866，夹角小于30°视为直行
            {
                return TurnDirection.Straight;
            }
            else if (crossProduct > 0)
            {
                return TurnDirection.Left;
            }
            else
            {
                // 根据题目要求，没有右转信号灯，暂时归类为Unknown
                return TurnDirection.Unknown;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error determining turn direction: {ex.Message}");
            return TurnDirection.Unknown;
        }
    }

    // 生成信号灯状态字符串
    private string GenerateStateString(TurnDirection[] directions)
    {
        var stateChars = new char[directions.Length];
        
        for (int i = 0; i < directions.Length; i++)
        {
            switch (directions[i])
            {
                case TurnDirection.Left:
                    stateChars[i] = 'G'; // 左转设为绿灯（大写G表示常亮）
                    break;
                case TurnDirection.Straight:
                    stateChars[i] = 'R'; // 直行设为红灯（大写R表示常亮）
                    break;
                default:
                    stateChars[i] = 'r'; // 其他情况默认为红灯
                    break;
            }
        }
        
        return new string(stateChars);
    }

    // 设置交通灯程序
    private void SetTrafficLightProgram(string tlightId, string stateString)
    {
        try
        {
            // 创建一个新的自定义程序
            var programLogic = new TrafficLightLogic
            {
                ProgramID = "custom_program",
                Type = 0, // 固定时间控制
                CurrentPhaseIndex = 0,
                Phases = new List<TrafficLightPhase>
                {
                    new TrafficLightPhase
                    {
                        Duration = 999999, // 非常长的持续时间，实现"常亮"效果
                        State = stateString
                    }
                }
            };
            
            // 设置信号灯程序
            client.TrafficLight.SetCompleteRedYellowGreenDefinition(tlightId, programLogic);
            
            // 切换到新程序
            client.TrafficLight.SetProgram(tlightId, "custom_program");
            
            // 设置相位索引为0（我们只有一个相位）
            client.TrafficLight.SetPhase(tlightId, 0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error setting traffic light program for {tlightId}: {ex.Message}");
        }
    }









}
