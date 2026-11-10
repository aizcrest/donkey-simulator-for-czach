using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DonkeyForest
{
    [Serializable] public class Progress
    {
        public int version = 1, kills, coins, sword = 1, armor = 1, barding = 1, potions = 3;
        public float health = 100, time = 0.25f, x, z;
        public int gemMask, gemsPlaced;
        public bool dragonDefeated;
    }

    public sealed class ForestGame : MonoBehaviour
    {
        public static ForestGame Instance;
        public readonly List<ForestEnemy> enemies = new List<ForestEnemy>();
        public Progress data = new Progress();
        public Transform rider;
        public ForestTerrain Landscape { get; private set; }
        public DragonEncounter Encounter { get; private set; }
        public Vector3 Ground(Vector3 position, float clearance = 0) => Landscape.Ground(position, clearance);
        public float MaxHealth => 90 + data.armor * 10 + data.barding * 10;
        public bool Night => data.time < .22f || data.time > .76f;
        public bool Dead => data.health <= 0;
        public bool Paused { get; private set; }
        public Vector2 moveInput;
        CharacterController controller;
        Camera view;
        Light sun;
        Text status, message, inventory, objective;
        GameObject menu, creditsPanel;
        Transform sword, donkey;
        float attackCooldown, swing, spawnClock, autoSave, messageClock, dodgeCooldown, dodgeTime;
        Vector3 dodgeDirection;
        string notice = "Welcome, rider. Hunt for loot. Zombies arrive after dark.";
        readonly List<Material> mats = new List<Material>();
        readonly Dictionary<Color, Material> palette = new Dictionary<Color, Material>();
        string SavePath => Path.Combine(Application.persistentDataPath, "forest-rider.json");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (!FindObjectOfType<ForestGame>()) new GameObject("Forest Rider").AddComponent<ForestGame>(); }
        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Load();
            BuildWorld(); BuildRider(); BuildUI();
            Encounter=new GameObject("Gems and summoning altar").AddComponent<DragonEncounter>();
            Encounter.Setup(this);
            if(!Encounter.Active)for (int i = 0; i < 14; i++) SpawnEnemy(false);
            Show(notice);
        }
        public Material Mat(Color color)
        {
            if (palette.TryGetValue(color, out var existing)) return existing;
            var m = new Material(Shader.Find("Standard")); m.color = color; mats.Add(m); palette.Add(color,m); return m;
        }
        public Material Glow(Color color)
        {
            var material=Mat(color);material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*.8f);return material;
        }
        public GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material material)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = material;
            var c = g.GetComponent<Collider>(); if(c) Destroy(c);
            return g;
        }
        void BuildWorld()
        {
            UnityEngine.Random.InitState(7419);
            Landscape = new GameObject("Forest landscape").AddComponent<ForestTerrain>();
            Landscape.Generate();
            var bark = Mat(new Color(.28f,.16f,.09f));
            var leaves = Mat(new Color(.12f,.32f,.15f)); var rocks = Mat(new Color(.4f,.43f,.39f));
            var world = new GameObject("Seeded forest").transform;
            for (int i=0;i<230;i++)
            {
                Vector2 p = UnityEngine.Random.insideUnitCircle * 85;
                var position = Ground(new Vector3(p.x,0,p.y));
                if (p.magnitude < 18 || ForestTerrain.PathWeight(p.x,p.y) > .2f || Landscape.Slope(position) > 28) continue;
                float h = UnityEngine.Random.Range(3f,7f);
                var tree = new GameObject("Pine").transform; tree.SetParent(world); tree.position = position;
                tree.gameObject.layer = ForestTerrain.SceneryLayer;
                Shape("Trunk",PrimitiveType.Cylinder,tree,new Vector3(0,h*.35f,0),new Vector3(.45f,h*.35f,.45f),bark);
                Shape("Lower canopy",PrimitiveType.Sphere,tree,new Vector3(0,h*.65f,0),new Vector3(3.5f,h*.6f,3.5f),leaves);
                Shape("Upper canopy",PrimitiveType.Sphere,tree,new Vector3(.15f,h*.98f,0),new Vector3(2.4f,h*.7f,2.4f),leaves);
                var obstacle=tree.gameObject.AddComponent<CapsuleCollider>(); obstacle.radius=.45f;obstacle.height=h;obstacle.center=Vector3.up*h*.5f;
            }
            for(int i=0;i<45;i++)
            {
                var p=UnityEngine.Random.insideUnitCircle*82;
                if(p.magnitude < 18 || ForestTerrain.PathWeight(p.x,p.y) > .3f) continue;
                var stone = Shape("Weathered stone",PrimitiveType.Sphere,world,Ground(new Vector3(p.x,0,p.y)),
                    new Vector3(1,.65f,.8f)*UnityEngine.Random.Range(.6f,2.4f),rocks);
                stone.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-12,12),UnityEngine.Random.Range(0,360),UnityEngine.Random.Range(-12,12));
            }
            var lamp=new GameObject("Sun");sun=lamp.AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.None;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.014f;
            var cameraObject = new GameObject("Follow Camera");view=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<AudioListener>();view.farClipPlane=160;view.fieldOfView=58;
            Physics.SyncTransforms();
            UnityEngine.Random.InitState(Environment.TickCount);
        }
        void BuildRider()
        {
            rider=new GameObject("Mounted player").transform;
            Vector3 savedPosition=new Vector3(Mathf.Clamp(data.x,-82,82),0,Mathf.Clamp(data.z,-82,82));
            Vector3 start=Ground(savedPosition,.08f);
            // Older flat-world saves can now land inside a newly placed tree trunk.
            for(int attempt=0;attempt<24;attempt++)
            {
                if(!Physics.CheckCapsule(start+Vector3.up*.6f,start+Vector3.up*1.5f,.7f,1<<ForestTerrain.SceneryLayer))break;
                float angle=attempt*2.4f, radius=1+attempt*.25f;
                start=Ground(savedPosition+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius),.08f);
            }
            start.x=Mathf.Clamp(start.x,-86,86);start.z=Mathf.Clamp(start.z,-86,86);
            rider.position=Ground(start,.08f);
            controller=rider.gameObject.AddComponent<CharacterController>();controller.center=new Vector3(0,1,0);controller.height=2;controller.radius=.55f;controller.stepOffset=.4f;controller.slopeLimit=45;
            donkey=new GameObject("Donkey").transform;donkey.SetParent(rider,false);
            var gray=Mat(new Color(.43f,.4f,.37f));var dark=Mat(new Color(.16f,.12f,.1f));var steel=Mat(new Color(.48f,.6f,.64f));var cloth=Mat(new Color(.55f,.18f,.11f));
            Shape("Donkey body",PrimitiveType.Capsule,donkey,new Vector3(0,.95f,0),new Vector3(.85f,.7f,1.5f),gray).transform.localRotation=Quaternion.Euler(90,0,0);
            Shape("Neck",PrimitiveType.Capsule,donkey,new Vector3(0,1.25f,.65f),new Vector3(.45f,.55f,.5f),gray);
            Shape("Head",PrimitiveType.Cube,donkey,new Vector3(0,1.65f,.85f),new Vector3(.43f,.43f,.7f),gray);
            for(int side=-1;side<=1;side+=2)
            {
                Shape("Long ear",PrimitiveType.Capsule,donkey,new Vector3(side*.16f,2,.68f),new Vector3(.12f,.35f,.12f),gray);
                Shape("Eye",PrimitiveType.Sphere,donkey,new Vector3(side*.225f,1.74f,.97f),Vector3.one*.075f,dark);
                for(int end=-1;end<=1;end+=2) Shape("Leg",PrimitiveType.Cube,donkey,new Vector3(side*.27f,.4f,end*.48f),new Vector3(.16f,.8f,.18f),dark);
                Shape("Donkey armor",PrimitiveType.Cube,donkey,new Vector3(side*.44f,1,0),new Vector3(.08f,.48f,.85f),steel);
            }
            Shape("Saddle",PrimitiveType.Cube,donkey,new Vector3(0,1.35f,-.12f),new Vector3(.65f,.15f,.65f),cloth);
            Shape("Rider breastplate",PrimitiveType.Capsule,donkey,new Vector3(0,1.86f,-.12f),new Vector3(.5f,.55f,.4f),steel);
            Shape("Helmet",PrimitiveType.Sphere,donkey,new Vector3(0,2.43f,-.12f),Vector3.one*.43f,steel);
            sword=new GameObject("Sword pivot").transform;sword.SetParent(donkey,false);sword.localPosition=new Vector3(.45f,1.95f,0);
            Shape("Sword",PrimitiveType.Cube,sword,new Vector3(0,0,.6f),new Vector3(.1f,.08f,1.2f),steel);
            Shape("Guard",PrimitiveType.Cube,sword,new Vector3(0,0,.12f),new Vector3(.4f,.12f,.1f),dark);
            view.transform.position=rider.position+new Vector3(0,7,-9);
            view.transform.LookAt(rider.position+Vector3.up*1.6f);
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape)) ToggleMenu();
            if(Input.GetKeyDown(KeyCode.F5)) Save();
            if(Input.GetKeyDown(KeyCode.Space)) Attack();
            if(Input.GetKeyDown(KeyCode.H)) Heal();
            if(Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift))Dodge();
            if(Input.GetKeyDown(KeyCode.E))Encounter.OfferGems();
            if(!Paused && !Dead)
            {
                float dt=Time.deltaTime;
                var input=moveInput.sqrMagnitude>.01f ? moveInput : new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
                input=Vector2.ClampMagnitude(input,1);
                var forward=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized;
                var direction=forward*input.y+view.transform.right*input.x;direction.y=0;
                dodgeCooldown=Mathf.Max(0,dodgeCooldown-dt);
                if(dodgeTime>0){direction=dodgeDirection;dodgeTime=Mathf.Max(0,dodgeTime-dt);}
                if(direction.sqrMagnitude>.01f) rider.rotation=Quaternion.Slerp(rider.rotation,Quaternion.LookRotation(direction),dt*10);
                controller.Move((direction*(dodgeTime>0?14f:6.5f)+Vector3.down*15)*dt);
                Vector3 pos=rider.position;pos.x=Mathf.Clamp(pos.x,-86,86);pos.z=Mathf.Clamp(pos.z,-86,86);rider.position=pos;
                donkey.localPosition=Vector3.up*(Mathf.Sin(Time.time*13)*.035f*input.magnitude);
                data.time=Mathf.Repeat(data.time+dt/360,1);
                spawnClock+=dt;if(spawnClock>4){spawnClock=0;if(enemies.Count<22&&!Encounter.Active)SpawnEnemy(Night);}
                autoSave+=dt;if(autoSave>30){autoSave=0;Save(false);}
                attackCooldown-=dt;swing=Mathf.Max(0,swing-dt);
            }
            sword.localRotation=Quaternion.Euler(0,swing>0?Mathf.Sin(swing/.3f*Mathf.PI)*-120:0,0);
            float daylight=Night?.12f:Mathf.Clamp01(Mathf.Sin((data.time-.22f)/.54f*Mathf.PI));
            sun.intensity=Mathf.Lerp(.12f,1.1f,daylight);sun.transform.rotation=Quaternion.Euler(data.time*360-90,35,0);
            RenderSettings.ambientLight=Color.Lerp(new Color(.18f,.22f,.34f),new Color(.65f,.68f,.57f),daylight);
            RenderSettings.fogColor=Color.Lerp(new Color(.055f,.075f,.13f),new Color(.48f,.59f,.5f),daylight);view.backgroundColor=RenderSettings.fogColor;
            status.text=$"FOREST RIDER  •  {(Night?"NIGHT — ZOMBIES":"DAY — FOREST HUNT")}\nHealth {Mathf.CeilToInt(data.health)} / {MaxHealth:0}    Coins {data.coins}    Kills {data.kills}\nSword {data.sword}  •  Armor {data.armor}  •  Donkey gear {data.barding}  •  Potions {data.potions}";
            objective.text=Encounter.Objective+$" • Dodge {(dodgeCooldown<=0?"READY":dodgeCooldown.ToString("0.0")+"s")}";
            messageClock-=Time.unscaledDeltaTime;message.text=Dead?"You fell. Open CAMP to revive.":messageClock>0?notice:"";
        }
        void LateUpdate()
        {
            if(!rider)return;
            Vector3 look = rider.position + Vector3.up * 1.6f;
            Vector3 target = rider.position + (Encounter && Encounter.Active?new Vector3(0,12,-16):new Vector3(0,7,-9));
            target.y = Mathf.Max(target.y, Ground(target).y + 2);
            var sight = target - look;
            int mask = (1 << ForestTerrain.GroundLayer) | (1 << ForestTerrain.SceneryLayer);
            if(Physics.SphereCast(look,.35f,sight.normalized,out var hit,sight.magnitude,mask))
                target = look + sight.normalized * Mathf.Max(.5f,hit.distance-.2f);
            Vector3 next=Vector3.Lerp(view.transform.position,target,1-Mathf.Exp(-8*Time.unscaledDeltaTime));
            next.y=Mathf.Max(next.y,Ground(next).y+1);
            view.transform.position=next;
            view.transform.LookAt(look);
        }
        public void Dodge()
        {
            if(Paused||Dead||dodgeCooldown>0)return;
            dodgeDirection=rider.forward;dodgeTime=.32f;dodgeCooldown=1.6f;
        }
        public void Attack()
        {
            if(Paused||Dead||attackCooldown>0)return;
            attackCooldown=.6f;swing=.3f;
            if(Encounter && Encounter.Boss)Encounter.Boss.Hit(17+data.sword*8);
            foreach(var enemy in enemies.ToArray())
            {
                if(!enemy)continue;
                var delta=enemy.transform.position-rider.position;
                if(Mathf.Abs(delta.y)>2.5f)continue;
                delta.y=0;
                if(delta.magnitude<3.4f && Vector3.Dot(rider.forward,delta.normalized)>-.15f)enemy.Hit(17+data.sword*8);
            }
        }
        public void Damage(float amount)
        {
            if(Dead||Paused||dodgeTime>0)return;
            data.health=Mathf.Max(0,data.health-Mathf.Max(1,amount*(1-Mathf.Min(.65f,(data.armor+data.barding)*.035f))));
            if(Dead){Show("You fell. Your equipment is safe. Revive at camp.");Save(false);}
        }
        void SpawnEnemy(bool zombie)
        {
            Vector3 pos = Vector3.zero;
            bool found = false;
            for(int attempt=0;attempt<16;attempt++)
            {
                Vector2 offset=UnityEngine.Random.insideUnitCircle.normalized*UnityEngine.Random.Range(16,34);
                pos=rider.position+new Vector3(offset.x,0,offset.y);
                pos.x=Mathf.Clamp(pos.x,-80,80);pos.z=Mathf.Clamp(pos.z,-80,80);
                pos=Ground(pos,.08f);
                if(Vector3.Distance(pos,rider.position)<12 || Landscape.Slope(pos)>32)continue;
                if(Physics.CheckCapsule(pos+Vector3.up*.6f,pos+Vector3.up*1.5f,.8f,1<<ForestTerrain.SceneryLayer))continue;
                found=true;break;
            }
            if(!found)return;
            string[] species={"Bear","Deer","Moose","Wolf","Fox"};
            var enemy=new GameObject(zombie?"Zombie":species[UnityEngine.Random.Range(0,species.Length)]).AddComponent<ForestEnemy>();
            enemy.transform.position=pos;enemy.Setup(this,enemy.name);enemies.Add(enemy);
        }
        public void Killed(ForestEnemy enemy)
        {
            enemies.Remove(enemy);data.kills++;data.coins+=enemy.kind=="Zombie"?12:8;
            if(UnityEngine.Random.value<.65f)
            {
                var drop=new GameObject("Loot — ride over to collect").AddComponent<ForestLoot>();
                drop.transform.position=Ground(enemy.transform.position,.6f);drop.reward=UnityEngine.Random.Range(0,5);
                Shape("Loot",PrimitiveType.Cube,drop.transform,Vector3.zero,Vector3.one*.45f,Mat(new Color(1,.74f,.15f)));
            }
        }
        public void Collect(int reward)
        {
            if(reward==0){data.potions++;Show("Found a healing potion.");}
            else if(reward==1){data.sword=Mathf.Min(10,data.sword+1);Show("Found a sword upgrade!");}
            else if(reward==2){data.armor=Mathf.Min(10,data.armor+1);Show("Found rider armor!");}
            else if(reward==3){data.barding=Mathf.Min(10,data.barding+1);Show("Found donkey armor!");}
            else{data.coins+=25;Show("Found 25 coins.");}
            Save(false);
        }
        public void Heal(){if(Paused||Dead)return;if(data.potions<=0){Show("No potions left.");return;}if(data.health>=MaxHealth)return;data.potions--;data.health=Mathf.Min(MaxHealth,data.health+50);Show("Recovered 50 health.");}
        public void Show(string text){notice=text;messageClock=6;}
        public void Save(bool notify=true)
        {
            if(rider){data.x=rider.position.x;data.z=rider.position.z;}
            string temp=SavePath+".tmp";
            try
            {
                File.WriteAllText(temp,JsonUtility.ToJson(data,true));
                if(File.Exists(SavePath)){File.Copy(SavePath,SavePath+".bak",true);File.Delete(SavePath);}
                File.Move(temp,SavePath);if(notify)Show("Progress saved on this device.");
            }
            catch(Exception e){Show("Save failed: device storage unavailable.");Debug.LogWarning(e.Message);}
        }
        void Load()
        {
            foreach(string path in new[]{SavePath,SavePath+".bak",SavePath+".tmp"})
            {
                if(!File.Exists(path))continue;
                try
                {
                    string json=File.ReadAllText(path);if(!json.TrimStart().StartsWith("{"))continue;
                    var loaded=JsonUtility.FromJson<Progress>(json);
                    if(loaded==null||loaded.version!=1||loaded.sword<1||float.IsNaN(loaded.health)||float.IsNaN(loaded.time)||float.IsInfinity(loaded.time)||float.IsNaN(loaded.x)||float.IsNaN(loaded.z)||float.IsInfinity(loaded.x)||float.IsInfinity(loaded.z))continue;
                    data=loaded;data.sword=Mathf.Clamp(data.sword,1,10);data.armor=Mathf.Clamp(data.armor,1,10);data.barding=Mathf.Clamp(data.barding,1,10);
                    data.health=Mathf.Clamp(data.health,0,MaxHealth);data.coins=Mathf.Max(0,data.coins);data.potions=Mathf.Max(0,data.potions);data.time=Mathf.Repeat(data.time,1);
                    data.gemMask &= GemRules.FullMask;
                    data.gemsPlaced=Mathf.Clamp(data.gemsPlaced,0,GemRules.Count(data.gemMask));
                    return;
                }catch(Exception){Debug.LogWarning("Could not read save; trying recovery copy.");}
            }
        }
        void OnApplicationPause(bool pause){if(pause)Save(false);}
        void OnApplicationQuit(){Save(false);}
        void OnDestroy(){foreach(var m in mats)if(m)Destroy(m);if(Instance==this)Instance=null;}
        void ToggleMenu(){Paused=!Paused;menu.SetActive(Paused);if(creditsPanel)creditsPanel.SetActive(false);moveInput=Vector2.zero;RefreshCamp();}
        void RefreshCamp(){inventory.text=$"CAMP & EQUIPMENT\nCoins: {data.coins}\nSword Lv.{data.sword}   Rider armor Lv.{data.armor}   Donkey armor Lv.{data.barding}\nUpgrades cost 40 × current level. Maximum level 10.\nLocal autosave every 30 seconds. One save per device.";}
        void Upgrade(int kind)
        {
            int level=kind==0?data.sword:kind==1?data.armor:data.barding;
            if(level>=10){Show("Equipment is already at maximum level.");return;}
            if(data.coins<level*40){Show("You need more coins.");return;}
            data.coins-=level*40;if(kind==0)data.sword++;else if(kind==1)data.armor++;else data.barding++;
            RefreshCamp();Save();
        }
        void Revive(){if(!Dead){Show("You can revive after falling in battle.");return;}data.health=MaxHealth;controller.enabled=false;rider.position=Ground(Vector3.zero,.08f);controller.enabled=true;foreach(var e in enemies.ToArray())if(e)Destroy(e.gameObject);enemies.Clear();dodgeTime=0;dodgeCooldown=0;Encounter.RestartBoss();Save();ToggleMenu();}
        Font font;
        RectTransform Panel(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image));obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=anchor;rect.anchoredPosition=pos;rect.sizeDelta=size;obj.GetComponent<Image>().color=color;return rect;
        }
        Text Label(Transform parent,string text,Vector2 anchor,Vector2 pos,Vector2 size,int fontsize)
        {
            var obj=new GameObject("Label",typeof(RectTransform),typeof(Text));obj.transform.SetParent(parent,false);
            var r=obj.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor;r.anchoredPosition=pos;r.sizeDelta=size;
            var t=obj.GetComponent<Text>();t.font=font;t.fontSize=fontsize;t.text=text;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;
        }
        void Button(Transform parent,string text,Vector2 anchor,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction click)
        {
            var rect=Panel(text,parent,anchor,pos,size,new Color(.18f,.25f,.23f,.95f));var b=rect.gameObject.AddComponent<Button>();b.targetGraphic=rect.GetComponent<Image>();b.onClick.AddListener(click);Label(rect,text,new Vector2(.5f,.5f),Vector2.zero,size,22);
        }
        void BuildUI()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root=new GameObject("Mobile HUD",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            if(!FindObjectOfType<EventSystem>())new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            var safe=new GameObject("Safe Area",typeof(RectTransform),typeof(SafeArea));safe.transform.SetParent(root.transform,false);
            var sr=safe.GetComponent<RectTransform>();sr.offsetMin=sr.offsetMax=Vector2.zero;
            var parent=safe.transform;
            status=Label(parent,"",new Vector2(.5f,1),new Vector2(0,-65),new Vector2(950,110),23);
            objective=Label(parent,"",new Vector2(.5f,1),new Vector2(0,-150),new Vector2(1120,70),20);
            message=Label(parent,"",new Vector2(.5f,.5f),new Vector2(0,125),new Vector2(950,80),25);
            Button(parent,"CAMP",new Vector2(1,1),new Vector2(-85,-55),new Vector2(140,60),ToggleMenu);
            Button(parent,"ATTACK",new Vector2(1,0),new Vector2(-110,115),new Vector2(180,110),Attack);
            Button(parent,"DODGE",new Vector2(1,0),new Vector2(-300,180),new Vector2(150,70),Dodge);
            Button(parent,"OFFER GEMS",new Vector2(0,1),new Vector2(120,-230),new Vector2(200,60),()=>Encounter.OfferGems());
            Button(parent,"HEAL",new Vector2(1,0),new Vector2(-280,80),new Vector2(120,70),Heal);
            var joystick=Panel("Movement",parent,Vector2.zero,new Vector2(135,135),new Vector2(190,190),new Color(.12f,.18f,.16f,.7f));
            var knob=Panel("Thumb",joystick,new Vector2(.5f,.5f),Vector2.zero,new Vector2(65,65),new Color(.7f,.78f,.6f,.9f));
            var stick=joystick.gameObject.AddComponent<MobileStick>();stick.knob=knob;stick.game=this;
            Label(parent,"Drag to ride",Vector2.zero,new Vector2(135,30),new Vector2(230,30),19);
            var panel=Panel("Camp",parent,new Vector2(.5f,.5f),Vector2.zero,new Vector2(880,530),new Color(.06f,.1f,.09f,.98f));menu=panel.gameObject;
            inventory=Label(panel,"",new Vector2(.5f,1),new Vector2(0,-100),new Vector2(840,180),24);
            Button(panel,"Sword",new Vector2(.5f,.5f),new Vector2(-270,0),new Vector2(240,65),()=>Upgrade(0));
            Button(panel,"Rider armor",new Vector2(.5f,.5f),Vector2.zero,new Vector2(240,65),()=>Upgrade(1));
            Button(panel,"Donkey armor",new Vector2(.5f,.5f),new Vector2(270,0),new Vector2(240,65),()=>Upgrade(2));
            Button(panel,"SAVE",new Vector2(.5f,.5f),new Vector2(-270,-100),new Vector2(240,65),()=>Save());
            Button(panel,"REVIVE",new Vector2(.5f,.5f),new Vector2(0,-100),new Vector2(240,65),Revive);
            Button(panel,"RESUME",new Vector2(.5f,.5f),new Vector2(270,-100),new Vector2(240,65),ToggleMenu);
            Label(panel,"WASD: ride • Space: attack • Shift: dodge • E: offer gems • H: heal • F5: save",new Vector2(.5f,0),new Vector2(0,50),new Vector2(850,60),20);
            Button(panel,"ASSET CREDITS",new Vector2(1,1),new Vector2(-115,-30),new Vector2(210,42),()=>creditsPanel.SetActive(true));
            var credits=Panel("Asset credits",panel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(880,530),new Color(.06f,.1f,.09f,1));
            creditsPanel=credits.gameObject;
            Label(credits,"FREE MODEL CREDITS\n\nBear and Elk (adapted as Moose): Poly by Google\nCreative Commons Attribution 3.0\nhttps://creativecommons.org/licenses/by/3.0/\n\nDeer, Wolf, Fox, Zombie and Dragon: Quaternius\nCC0 1.0 — https://creativecommons.org/publicdomain/zero/1.0/\n\nModels converted to FBX. Moose antlers adapted.\nOriginal model links and modification details are in THIRD_PARTY.md.",new Vector2(.5f,.5f),new Vector2(0,40),new Vector2(830,380),21);
            Button(credits,"MODEL SOURCES",new Vector2(.5f,0),new Vector2(-150,65),new Vector2(250,65),()=>Application.OpenURL("https://github.com/aizcrest/donkey-simulator-for-czach/blob/main/THIRD_PARTY.md"));
            Button(credits,"BACK",new Vector2(.5f,0),new Vector2(150,65),new Vector2(250,65),()=>creditsPanel.SetActive(false));
            creditsPanel.SetActive(false);
            menu.SetActive(false);
        }
    }
    public sealed class SafeArea:MonoBehaviour
    {
        void Update(){var r=GetComponent<RectTransform>();var s=Screen.safeArea;r.anchorMin=new Vector2(s.x/Screen.width,s.y/Screen.height);r.anchorMax=new Vector2(s.xMax/Screen.width,s.yMax/Screen.height);r.offsetMin=r.offsetMax=Vector2.zero;}
    }
    public sealed class MobileStick:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler
    {
        public RectTransform knob;public ForestGame game;int pointer=int.MinValue;
        public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue)return;pointer=e.pointerId;OnDrag(e);}
        public void OnDrag(PointerEventData e){if(e.pointerId!=pointer)return;Vector2 p;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out p);game.moveInput=Vector2.ClampMagnitude(p/75,1);knob.anchoredPosition=game.moveInput*60;}
        public void OnPointerUp(PointerEventData e){if(e.pointerId!=pointer)return;ResetStick();}
        void ResetStick(){pointer=int.MinValue;if(game)game.moveInput=Vector2.zero;if(knob)knob.anchoredPosition=Vector2.zero;}
        void OnDisable(){ResetStick();}
    }
}
