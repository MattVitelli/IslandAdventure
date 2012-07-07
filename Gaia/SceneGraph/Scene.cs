using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;

using JigLibX.Physics;
using JigLibX.Collision;
using JigLibX.Geometry;

using Gaia.Core;
using Gaia.Rendering.RenderViews;
using Gaia.SceneGraph.GameEntities;
using Gaia.Resources;
using Gaia.Game;

namespace Gaia.SceneGraph
{
    public class Scene
    {
        public SortedList<string, Entity> Entities = new SortedList<string, Entity>();
        PriorityQueue<int, RenderView> RenderViews = new PriorityQueue<int, RenderView>();

        public Light MainLight; //Our sunlight
        
        public RenderView MainCamera;

        public Camera MainPlayer;

        public Terrain MainTerrain;

        public RoundManager RoundSystem;

        BoundingBox sceneDimensions;

        PhysicsSystem world;
       
        public Scene()
        {
            InitializeScene();
            //LoadScene("Level1.lvl");
            //MainCamera = (Camera)FindEntity("MainCamera");
        }

        public Vector3 GetMainLightDirection()
        {
            if (MainLight != null)
                return MainLight.Transformation.GetPosition();

            return Vector3.Up;
        }

        public PhysicsSystem GetPhysicsEngine()
        {
            return world;
        }

        public BoundingBox GetSceneDimensions()
        {
            return sceneDimensions;
        }

        string GetNextAvailableName(string name)
        {
            int count = 0;
            string newName = name;
            while (Entities.ContainsKey(newName))
            {
                count++;
                newName = name + count;
            }
            return newName;
        }

        public void AddEntity(string name, Entity entity)
        {
            string newName = GetNextAvailableName(name);
            Entities.Add(newName, entity);
            entity.OnAdd(this);
        }

        public string RenameEntity(string oldName, string newName)
        {
            if (Entities.ContainsKey(oldName))
            {
                Entity ent = Entities[oldName];
                Entities.Remove(oldName);
                newName = GetNextAvailableName(newName);
                Entities.Add(newName, ent);
                return newName;
            }

            return oldName;
        }

        public Entity FindEntity(string name)
        {
            if (Entities.ContainsKey(name))
                return Entities[name];

            return null;
        }

        public void RemoveEntity(string name)
        {
            Entities.Remove(name);
        }

        public void RemoveEntity(Entity entity)
        {
            int index = Entities.IndexOfValue(entity);
            if (index >= 0)
            {
                Entities.Values[index].OnDestroy();
                Entities.RemoveAt(index);
            }
        }

        public void AddRenderView(RenderView view)
        {
            RenderViews.Enqueue(view, (int)view.GetRenderType());
        }

        public void RemoveRenderView(RenderView view)
        {
            RenderViews.RemoveAt((int)view.GetRenderType(), view);
        }

        void Initialize()
        {
            for (int i = 0; i < Entities.Count; i++)
            {
                Entities.Values[i].OnAdd(this);
            }
        }

        public void Destroy()
        {
            for (int i = 0; i < Entities.Count; i++)
            {
                Entities.Values[i].OnDestroy();
            }
        }

        void DetermineSceneDimensions()
        {
            sceneDimensions.Max = Vector3.One * float.NegativeInfinity;
            sceneDimensions.Min = Vector3.One * float.PositiveInfinity;
            for (int i = 0; i < Entities.Count; i++)
            {
                BoundingBox bounds = Entities.Values[i].Transformation.GetBounds();
                sceneDimensions.Min = Vector3.Min(sceneDimensions.Min, bounds.Min);
                sceneDimensions.Max = Vector3.Max(sceneDimensions.Max, bounds.Max);
            }
        }

        void CreateTeams()
        {
            Tank playerTank = new Tank();
            playerTank.SetEnabled(true);
            playerTank.SetControllable(true);
            Entities.Add("player", playerTank);
        }

        void InitializePhysics()
        {
            world = new PhysicsSystem();
            //physicSystem.CollisionSystem = new CollisionSystemGrid(32, 32, 32, 30, 30, 30);
            //physicSystem.CollisionSystem = new CollisionSystemBrute();
            world.CollisionSystem = new CollisionSystemSAP();

            world.EnableFreezing = true;
            world.SolverType = PhysicsSystem.Solver.Normal;
            world.CollisionSystem.UseSweepTests = true;
            world.Gravity = new Vector3(0, -10, 0);//PhysicsHelper.GravityEarth, 0);
            world.NumCollisionIterations = 16;
            world.NumContactIterations = 16;
            world.NumPenetrationRelaxtionTimesteps = 30;// 15;
        }

        public void ResetScene()
        {
            for (int i = 0; i < Entities.Count; i++)
                Entities.Values[i].OnDestroy();
            Entities.Clear();
            RenderViews.Clear();
            InitializePhysics();
        }

        public void SaveScene(XmlWriter writer)
        {
            writer.WriteStartElement("Scene");
            for (int i = 0; i < Entities.Count; i++)
            {
                string key = Entities.Keys[i];
                writer.WriteStartElement(Entities[key].GetType().FullName);
                writer.WriteStartAttribute("name");
                writer.WriteValue(key);
                writer.WriteEndAttribute();
                Entities.Values[i].OnSave(writer);
                writer.WriteEndElement();
                writer.WriteWhitespace("\n");
            }
            writer.WriteEndElement();
        }

        public void LoadScene(string filename)
        {
            ResetScene();
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);
            XmlNodeList roots = doc.GetElementsByTagName("Scene");
            foreach (XmlNode root in roots)
            {
                foreach(XmlNode node in root.ChildNodes)
                {
                    Type entityType = Type.GetType(node.Name);
                    if (entityType.IsSubclassOf(typeof(Entity)))
                    {
                        Entity entity = (Entity)entityType.GetConstructors()[0].Invoke(null);
                        entity.OnLoad(node);
                        Entities.Add(node.Attributes["name"].Value, entity); 
                    }
                }
            }

            Initialize();
        }

        void InitializeScene()
        {
            ResetScene();

            Entities.Add("Sky", new Sky());
            MainLight = new Sunlight();
            MainTerrain = new TerrainVoxel();
            //MainTerrain = new TerrainHeightmap("Textures/HeightMap/heightmap.png", 0, 0.5f);
            MainPlayer = new Camera();
            
            Entities.Add("MainCamera", MainPlayer);
            Entities.Add("Terrain", MainTerrain);
            Entities.Add("Light", MainLight);
            Entities.Add("Plane", new Model("Plane"));
            Entities.Add("AmbientLight", new Light(LightType.Ambient, new Vector3(0.15f, 0.35f, 0.55f), Vector3.Zero, false));
            Entities.Add("TestTree", new Model("Cecropia"));
            Entities["TestTree"].Transformation.SetPosition(Vector3.Forward * 10.0f);
            Entities.Add("TestTree2", new Model("JungleOverhang"));
            Entities["TestTree2"].Transformation.SetPosition(Vector3.Forward * 10.0f + Vector3.Right * 7.6f);
            
            for (int i = 0; i < 250; i++)
            {
                Model tree = new Model("Cecropia");
                NormalTransform transform = new NormalTransform();
                tree.Transformation = transform;
                Vector3 pos = Vector3.Zero;
                Vector3 normal = Vector3.Up;
                MainTerrain.GenerateRandomTransform(RandomHelper.RandomGen, out pos, out normal);
                transform.ConformToNormal(normal);
                transform.SetPosition(pos);
                AddEntity("Tree", tree);
            }
            
            //Entities.Add("Grass", new GrassPlacement());
            /*
            Model testGeom = new Model("test_level");
            testGeom.Transformation.SetPosition(Vector3.Up*20.0f);
            Entities.Add("scene_geom1", testGeom);

            Model testGeom2 = new Model("cgWarehouse002story");
            testGeom2.Transformation.SetPosition(Vector3.Up * 21.0f);
            Entities.Add("scene_geom2", testGeom2);
            */
            Chest weaponCrate = new Chest("Weapon Box", "WeaponBox");
            weaponCrate.Transformation.SetPosition(Vector3.Up * 30.0f);
            Entities.Add("weaponCrate", weaponCrate);
            //CreateTeams();          
            
            //Entities.Add("Light2", new Light(LightType.Directional, new Vector3(0.2797f, 0.344f, 0.43f), Vector3.Up, false));

            Initialize();
            
        }

        public void Update()
        {
            DetermineSceneDimensions();

            for (int i = 0; i < Entities.Count; i++)
            {
                Entities.Values[i].OnUpdate();       
            }


            float timeStep = Time.GameTime.ElapsedTime;
            if (timeStep < 1.0f / 60.0f)
                world.Integrate(timeStep);
            else
                world.Integrate(1.0f / 60.0f);
        }

        public void Render()
        {
            if (RenderViews.Count == 0)
                return;

            int renderViewCount = RenderViews.Count;
            RenderView[] views = new RenderView[renderViewCount];
            
            for (int i = 0; i < renderViewCount; i++)
            {
                RenderView renderView = RenderViews.ExtractMin();
                for (int j = 0; j < Entities.Count; j++)
                {
                    Entities.Values[j].OnRender(renderView);
                }
                views[i] = renderView;
            }
            for (int i = 0; i < views.Length; i++)
            {
                views[i].Render();
                RenderViews.Enqueue(views[i], (int)views[i].GetRenderType());
            }
        }
    }
}
