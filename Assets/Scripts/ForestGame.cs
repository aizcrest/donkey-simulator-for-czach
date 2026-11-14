using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DonkeyForest
{
    public sealed class ForestGame : MonoBehaviour
    {
        public static ForestGame Instance;
        public readonly List<ForestEnemy> enemies = new List<ForestEnemy>();
        public Progress data = new Progress();
        public Transform rider;
        public ForestTerrain Landscape { get; private set; }
        public DragonEncounter Encounter { get; private set; }
        public MountedSkills Skills { get; private set; }
        public GameAudio Audio { get; private set; }
        public CampAdditions Additions { get; private set; }
        public PlayerMountVisual MountedModel { get; private set; }
        public DifficultyProfile Difficulty=>DifficultyRules.Get(data.difficulty);
        public Camera View=>view;
        public TouchControlSkin Controls { get; private set; }
        DayNightSky sky;
        public bool IsDodging=>dodgeTime>0;
        public float WeaponDamage=>(17+data.sword*8)*(1+(data.journey.level-1)*.01f);
        public Vector3 Ground(Vector3 position, float clearance = 0) => Landscape.Ground(position, clearance);
        public float MaxHealth => 90 + data.armor * 10 + data.barding * 10 + (data.journey==null?0:(Mathf.Clamp(data.journey.level,1,20)-1)*3);
        public bool Night => data.time < .22f || data.time > .76f;
        public bool Dead => data.health <= 0;
        public bool Paused { get; private set; }
        public Vector2 moveInput;
        CharacterController controller;
        Camera view;
        OrbitCameraInput orbitCamera;
        readonly List<Camera> previousCameras=new List<Camera>();
        readonly List<AudioListener> previousListeners=new List<AudioListener>();
        Light sun;
        Text status, message, inventory, objective;
        GameObject menu, creditsPanel, hudRoot;
        bool ready;
        float comboWindow,hudClock,frameTime=1f/30;
        static readonly string[] AnimalSpecies={"Bear","Deer","Moose","Wolf","Fox"};
        int comboStep;
        float attackCooldown, swing, spawnClock, autoSave, messageClock, dodgeCooldown, dodgeTime;
        Vector3 dodgeDirection;
        string notice = "Welcome, rider. Hunt for loot. Zombies arrive after dark.";
        readonly ForestPropMeshes propMeshes=new ForestPropMeshes();
        readonly List<Material> mats = new List<Material>();
        readonly Dictionary<Color, Material> palette = new Dictionary<Color, Material>();
        string SavePath => Path.Combine(Application.persistentDataPath, "forest-rider.json");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (!FindAnyObjectByType<ForestGame>()) new GameObject("Czach’s Donkey Journey launch").AddComponent<LaunchSplash>(); }
        void Awake()
        {
            Instance=this;Paused=true;
            try
            {
                InitializeGame();ready=true;Paused=false;SetHudEnabled(true);
            }
            catch(Exception error){FailInitialization(error);}
        }
        void FailInitialization(Exception error)
        {
            ready=false;Paused=true;SetHudEnabled(false);
            Debug.LogException(error,this);
            Debug.LogWarning("Czach’s Donkey Journey setup did not finish. See the original exception above; Camp and autosave are disabled to protect your save.",this);
        }
        void InitializeGame()
        {
            Application.targetFrameRate = 30;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Load();GemRules.NormalizeState(data);
            if(data.skills==null)data.skills=new SkillSave();if(data.journey==null)data.journey=new JourneySave();
            SkillRules.Normalize(data.skills);JourneyRules.Normalize(data.journey);data.health=Mathf.Clamp(data.health,0,MaxHealth);
            data.masterVolume=SafeVolume(data.masterVolume,.75f);data.musicVolume=SafeVolume(data.musicVolume,.2f);data.effectsVolume=SafeVolume(data.effectsVolume,.8f);
            data.quality=Mathf.Clamp(data.quality,0,2);
            data.difficulty=DifficultyRules.Normalize(data.difficulty);
            Audio=gameObject.AddComponent<GameAudio>();Audio.Setup(this);
            BuildWorld(); BuildRider();
            Skills=gameObject.AddComponent<MountedSkills>();Skills.Setup(this);
            Controls=gameObject.AddComponent<TouchControlSkin>();
            BuildUI();
            MobilePerformance.Apply(this);
            Encounter=new GameObject("Gems and summoning altar").AddComponent<DragonEncounter>();
            Encounter.Setup(this);
            if(!Encounter.Active)for (int i = 0; i < MobilePerformance.InitialEnemies(data.quality,data.difficulty); i++) SpawnEnemy(false);
            Show(notice);
        }
        static float SafeVolume(float value,float fallback)=>float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp01(value);
        public Material Mat(Color color)
        {
            if (palette.TryGetValue(color, out var existing)) return existing;
            var m = new Material(Shader.Find("Standard")); m.color = color;m.enableInstancing=true; mats.Add(m); palette.Add(color,m); return m;
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
            var c = g.GetComponent<Collider>(); if(c){c.enabled=false;Destroy(c);}
            return g;
        }
        GameObject Prop(string name,Mesh mesh,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);
            obj.transform.localPosition=position;obj.transform.localScale=scale;
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;return obj;
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
                Prop("Trunk",propMeshes.Trunk,tree,new Vector3(0,h*.35f,0),new Vector3(.45f,h*.35f,.45f),bark);
                Prop("Lower canopy",propMeshes.Cone,tree,new Vector3(0,h*.65f,0),new Vector3(3.5f,h*.6f,3.5f),leaves);
                Prop("Upper canopy",propMeshes.Cone,tree,new Vector3(.15f,h*.98f,0),new Vector3(2.4f,h*.7f,2.4f),leaves);
                var obstacle=tree.gameObject.AddComponent<CapsuleCollider>(); obstacle.radius=.45f;obstacle.height=h;obstacle.center=Vector3.up*h*.5f;
            }
            for(int i=0;i<45;i++)
            {
                var p=UnityEngine.Random.insideUnitCircle*82;
                if(p.magnitude < 18 || ForestTerrain.PathWeight(p.x,p.y) > .3f) continue;
                var stone = Prop("Weathered stone",propMeshes.Stone,world,Ground(new Vector3(p.x,0,p.y)),
                    new Vector3(1,.65f,.8f)*UnityEngine.Random.Range(.6f,2.4f),rocks);
                stone.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-12,12),UnityEngine.Random.Range(0,360),UnityEngine.Random.Range(-12,12));
            }
            var lamp=new GameObject("Sun");sun=lamp.AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.None;
            // Unity 6 no longer bakes an ambient probe automatically for a new scene.
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.014f;
            // Play may start in a template scene with its own Main Camera and listener.
            foreach(var existing in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if(existing.enabled&&existing.targetTexture==null&&existing.targetDisplay==0)
                {previousCameras.Add(existing);existing.enabled=false;}
            foreach(var existing in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if(existing.enabled){previousListeners.Add(existing);existing.enabled=false;}
            var cameraObject = new GameObject("Follow Camera");cameraObject.tag="MainCamera";view=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<AudioListener>();view.farClipPlane=160;view.fieldOfView=58;
            sky=gameObject.AddComponent<DayNightSky>();sky.Setup(this,sun);
            orbitCamera=gameObject.AddComponent<OrbitCameraInput>();orbitCamera.Setup(this);
            StaticBatchingUtility.Combine(world.gameObject);
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
            MountedModel=rider.gameObject.AddComponent<PlayerMountVisual>();MountedModel.Setup(this);
            view.transform.position=rider.position+new Vector3(0,7,-9);
            view.transform.LookAt(rider.position+Vector3.up*1.6f);
        }
        void Update()
        {
            if(!ready)return;
            if(Input.GetKeyDown(KeyCode.Escape)) ToggleMenu();
            if(!ready)return;
            if(Input.GetKeyDown(KeyCode.F5)) Save();
            if(Input.GetKeyDown(KeyCode.Space)) Attack();
            if(Input.GetKeyDown(KeyCode.H)) Heal();
            if(Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift))Dodge();
            if(Input.GetKeyDown(KeyCode.E))Encounter.OfferGems();
            if(Input.GetKeyDown(KeyCode.Alpha1))Skills.Use(SkillId.Whirlwind);
            if(Input.GetKeyDown(KeyCode.Alpha2))Skills.Use(SkillId.Guard);
            if(Input.GetKeyDown(KeyCode.Alpha3))Skills.Use(SkillId.Charge);
            if(Input.GetKeyDown(KeyCode.Alpha4))Skills.Use(SkillId.Kick);
            if(Dead)Skills.Tick(0);
            if(!Paused && !Dead)
            {
                float dt=Time.deltaTime;
                var input=moveInput.sqrMagnitude>.01f ? moveInput : new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
                input=Vector2.ClampMagnitude(input,1);
                var forward=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized;
                var direction=forward*input.y+view.transform.right*input.x;direction.y=0;
                comboWindow=Mathf.Max(0,comboWindow-dt);
                bool charging=Skills.Charging;
                if(charging)direction=Skills.ChargeDirection;
                dodgeCooldown=Mathf.Max(0,dodgeCooldown-dt);
                if(dodgeTime>0){direction=dodgeDirection;dodgeTime=Mathf.Max(0,dodgeTime-dt);}
                if(direction.sqrMagnitude>.01f) rider.rotation=Quaternion.Slerp(rider.rotation,Quaternion.LookRotation(direction),dt*10);
                Vector3 previous=rider.position;
                controller.Move((direction*(dodgeTime>0?14f:charging?12.5f:6.5f)+Vector3.down*15)*dt);
                Vector3 pos=rider.position;var correction=new Vector3(Mathf.Clamp(pos.x,-86,86)-pos.x,0,Mathf.Clamp(pos.z,-86,86)-pos.z);
                if(correction.sqrMagnitude>0)controller.Move(correction);
                Landscape.KeepAboveGround(controller);
                if(charging)Skills.CheckCharge(previous,rider.position);
                Skills.Tick(dt);
                MountedModel.Animate(direction.sqrMagnitude>.01f,charging||dodgeTime>0,swing);
                Audio.Hooves(dt,(rider.position-previous).sqrMagnitude>.0001f&&direction.sqrMagnitude>.01f,charging||dodgeTime>0);
                data.time=Mathf.Repeat(data.time+dt/360,1);
                spawnClock+=dt;if(spawnClock>4){spawnClock=0;if(enemies.Count<MobilePerformance.EnemyCap(data.quality,data.difficulty)&&!Encounter.Active)SpawnEnemy(Night);}
                autoSave+=dt;if(autoSave>30){autoSave=0;Save(false);}
                attackCooldown-=dt;swing=Mathf.Max(0,swing-dt);
            }
            sky.Tick();
            frameTime=Mathf.Lerp(frameTime,Mathf.Max(.001f,Time.unscaledDeltaTime),.05f);
            hudClock-=Time.unscaledDeltaTime;
            if(hudClock<=0)
            {
            hudClock=.2f;
            status.text=$"Czach’s Donkey Journey\n{(Night?"NIGHT — ZOMBIES":"DAY — FOREST HUNT")} • {(data.quality==0?"LOW":data.quality==1?"BALANCED":"HIGH")} / {Mathf.RoundToInt(1/frameTime)} FPS • {Difficulty.Name.ToUpperInvariant()}\nHealth {Mathf.CeilToInt(data.health)} / {MaxHealth:0}    Coins {data.coins}    Kills {data.kills}    Lv.{data.journey.level}\nSword {data.sword}  •  Armor {data.armor}  •  Donkey gear {data.barding}  •  Potions {data.potions}";
            objective.text=Encounter.Objective+$" • Dodge {(dodgeCooldown<=0?"READY":dodgeCooldown.ToString("0.0")+"s")}";
            Additions.Refresh();
            }
            messageClock-=Time.unscaledDeltaTime;message.text=Dead?"":messageClock>0?notice:"";
        }
        void LateUpdate()
        {
            if(!ready||!rider)return;
            orbitCamera.ReadInput();
            Vector3 look = rider.position + Vector3.up * 1.6f;
            Vector3 target = look + orbitCamera.Offset(Encounter && Encounter.Active?19:10.5f);
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
            if(Paused||Dead||dodgeCooldown>0||Skills.Charging)return;
            dodgeDirection=rider.forward;dodgeTime=.32f;dodgeCooldown=1.6f;Audio.Play("swing",.5f);
        }
        public void Attack()
        {
            if(Paused||Dead||attackCooldown>0)return;
            attackCooldown=.6f;swing=.3f;
            if(comboWindow<=0)comboStep=0;comboStep=comboStep%3+1;comboWindow=1.3f;
            bool heavy=comboStep==3;float damage=WeaponDamage*(heavy?1.4f:1);
            Audio.Play("swing",heavy?1:.7f);
            if(Encounter && Encounter.Boss)Encounter.Boss.Hit(damage);
            foreach(var enemy in enemies.ToArray())
            {
                if(!enemy)continue;
                var delta=enemy.transform.position-rider.position;
                if(Mathf.Abs(delta.y)>2.5f)continue;
                delta.y=0;
                if(delta.magnitude<3.4f && Vector3.Dot(rider.forward,delta.normalized)>-.15f)enemy.SkillHit(damage,heavy?.35f:0,heavy?delta.normalized*.8f:Vector3.zero);
            }
        }
        public void Damage(float amount)
        {
            if(Dead||Paused||dodgeTime>0)return;
            data.health=Mathf.Max(0,data.health-Mathf.Max(1,amount*Skills.GuardMultiplier*(1-Mathf.Min(.65f,(data.armor+data.barding)*.035f))));
            Audio.Play("hit",.55f);
            if(Dead){Skills.CancelEffects();moveInput=Vector2.zero;Audio.Play("death");Encounter.OnPlayerDeath();Save(false);}
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
            var enemy=new GameObject(zombie?"Zombie":AnimalSpecies[UnityEngine.Random.Range(0,AnimalSpecies.Length)]).AddComponent<ForestEnemy>();
            enemy.transform.position=pos;
            try{enemy.Setup(this,enemy.name,UnityEngine.Random.value<.12f);enemies.Add(enemy);}
            catch{enemy.gameObject.SetActive(false);Destroy(enemy.gameObject);throw;}
        }
        public void Killed(ForestEnemy enemy)
        {
            enemies.Remove(enemy);data.kills++;data.coins+=(enemy.kind=="Zombie"?12:8)*(enemy.Elite?2:1);
            if(enemy.kind=="Zombie")data.journey.zombies++;else data.journey.hunts++;
            GainXP(JourneyRules.XP(enemy.kind,enemy.Elite));
            if(UnityEngine.Random.value<(enemy.Elite?.9f:.65f))
            {
                var drop=new GameObject("Loot — ride over to collect").AddComponent<ForestLoot>();
                drop.transform.position=Ground(enemy.transform.position,.6f);drop.reward=UnityEngine.Random.Range(0,5);
                Shape("Loot",PrimitiveType.Cube,drop.transform,Vector3.zero,Vector3.one*.45f,Mat(new Color(1,.74f,.15f)));
            }
            Save(false);
        }
        public void GainXP(int amount)
        {
            int levels=JourneyRules.GainXP(data.journey,amount);
            if(levels>0){data.health=Mathf.Min(MaxHealth,data.health+levels*3);Show($"Level {data.journey.level}! +{levels} skill point(s). Train in CAMP → SKILLS.");Audio.Play("level");}
        }
        public void Collect(int reward)
        {
            Audio.Play("loot");
            if(reward==0){data.potions++;Show("Found a healing potion.");}
            else if(reward==1){data.sword=Mathf.Min(10,data.sword+1);Show("Found a sword upgrade!");}
            else if(reward==2){data.armor=Mathf.Min(10,data.armor+1);Show("Found rider armor!");}
            else if(reward==3){data.barding=Mathf.Min(10,data.barding+1);Show("Found donkey armor!");}
            else{data.coins+=25;Show("Found 25 coins.");}
            Save(false);
        }
        public void Heal(){if(Paused||Dead)return;if(data.potions<=0){Show("No potions left.");return;}if(data.health>=MaxHealth)return;data.potions--;data.health=Mathf.Min(MaxHealth,data.health+50);Show("Recovered 50 health.");Audio.Play("heal");}
        public void Show(string text){notice=text;messageClock=6;}
        public void Save(bool notify=true)
        {
            if(!ready)return;
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
                    if(!json.Contains("\"masterVolume\"")){loaded.masterVolume=.75f;loaded.musicVolume=.2f;loaded.effectsVolume=.8f;}
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
        void OnDestroy()
        {
            BakedEnemyAnimation.ReleaseAll();ImportedModelMaterials.ReleaseAll();
            propMeshes.Dispose();foreach(var m in mats)if(m)Destroy(m);
            if(view){view.enabled=false;var listener=view.GetComponent<AudioListener>();if(listener)listener.enabled=false;Destroy(view.gameObject);}
            foreach(var previous in previousCameras)if(previous)previous.enabled=true;
            foreach(var previous in previousListeners)if(previous)previous.enabled=true;
            if(Instance==this)Instance=null;
        }
        void ToggleMenu()
        {
            if(!ready)return;
            if(!hudRoot||!menu||!inventory||!Additions)
            {
                try{BuildUI();SetHudEnabled(true);}
                catch(Exception error){FailInitialization(error);return;}
            }
            Paused=!Paused;
            menu.SetActive(Paused);
            if(creditsPanel)creditsPanel.SetActive(false);
            moveInput=Vector2.zero;Additions.Close();RefreshCamp();Additions.Refresh();
            if(Audio)Audio.Play("ui");
            if(!Paused)Save(false);
        }
        void SetHudEnabled(bool value)
        {
            if(!hudRoot)return;
            var canvas=hudRoot.GetComponent<Canvas>();if(canvas)canvas.enabled=value;
            var raycaster=hudRoot.GetComponent<GraphicRaycaster>();if(raycaster)raycaster.enabled=value;
        }
        void RefreshCamp(){inventory.text=$"CAMP & EQUIPMENT\nCoins: {data.coins}\nSword Lv.{data.sword}   Rider armor Lv.{data.armor}   Donkey armor Lv.{data.barding}\nUpgrades cost 40 × current level. Maximum level 10.\nLocal autosave every 30 seconds. One save per device.";}
        void Upgrade(int kind)
        {
            int level=kind==0?data.sword:kind==1?data.armor:data.barding;
            if(level>=10){Show("Equipment is already at maximum level.");return;}
            if(data.coins<level*40){Show("You need more coins.");return;}
            data.coins-=level*40;if(kind==0)data.sword++;else if(kind==1)data.armor++;else data.barding++;
            RefreshCamp();Save();
        }
        public bool ResetProgress()
        {
            if(!Paused)return false;
            var fresh=Progress.NewAdventure(data);
            try{ProgressResetStore.Replace(SavePath,JsonUtility.ToJson(fresh,true));}
            catch(Exception e){Debug.LogWarning("Progress reset failed: "+e.Message);return false;}
            // Commit the new state only after the fresh save is installed successfully.
            Skills.CancelEffects();
            if(Encounter){Encounter.gameObject.SetActive(false);Destroy(Encounter.gameObject);}
            foreach(var enemy in enemies)if(enemy){enemy.gameObject.SetActive(false);Destroy(enemy.gameObject);}
            enemies.Clear();
            foreach(var loot in FindObjectsByType<ForestLoot>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {loot.gameObject.SetActive(false);Destroy(loot.gameObject);}
            data=fresh;Skills.ResetProgress();Audio.ResetCombat();
            controller.enabled=false;rider.position=Ground(Vector3.zero,.08f);rider.rotation=Quaternion.identity;controller.enabled=true;
            moveInput=Vector2.zero;dodgeDirection=Vector3.zero;comboStep=0;
            comboWindow=attackCooldown=swing=spawnClock=autoSave=dodgeCooldown=dodgeTime=0;
            foreach(var stick in FindObjectsByType<MobileStick>(FindObjectsSortMode.None))stick.ResetStick();
            MountedModel.Animate(false,false,0);
            Encounter=new GameObject("Gems and summoning altar").AddComponent<DragonEncounter>();Encounter.Setup(this);
            for(int i=0;i<MobilePerformance.InitialEnemies(data.quality,data.difficulty);i++)SpawnEnemy(false);
            sky.Tick();hudClock=0;ToggleMenu();
            Show("New adventure started! Find the ten gems and challenge the dragon again.");
            return true;
        }
        public void SetDifficulty(int value)
        {
            if(!Paused)return;
            value=DifficultyRules.Normalize(value);if(value==data.difficulty)return;
            bool restart=Encounter&&Encounter.Active;
            data.difficulty=value;
            foreach(var enemy in enemies)if(enemy)enemy.ApplyDifficulty();
            MobilePerformance.Apply(this);
            if(restart)Encounter.RestartBoss();
            else if(!Dead)for(int i=enemies.Count;i<MobilePerformance.InitialEnemies(data.quality,data.difficulty);i++)SpawnEnemy(Night);
            Show(Difficulty.Name+" difficulty applied."+(restart?" Dragon fight restarted at full health.":""));
            Save(false);
        }
        void Revive()
        {
            if(!ready||!Dead)return;
            data.health=MaxHealth;controller.enabled=false;rider.position=Ground(Vector3.zero,.08f);controller.enabled=true;
            foreach(var e in enemies.ToArray())if(e){e.gameObject.SetActive(false);Destroy(e.gameObject);}
            enemies.Clear();moveInput=Vector2.zero;dodgeTime=dodgeCooldown=comboWindow=attackCooldown=swing=spawnClock=0;comboStep=0;
            foreach(var stick in FindObjectsByType<MobileStick>(FindObjectsSortMode.None))stick.ResetStick();
            Skills.CancelEffects();Audio.ResetCombat();Paused=false;
            if(menu)menu.SetActive(false);if(creditsPanel)creditsPanel.SetActive(false);Additions.Close();
            Encounter.RestartBoss();
            if(!Encounter.Active)for(int i=0;i<MobilePerformance.InitialEnemies(data.quality,data.difficulty);i++)SpawnEnemy(Night);
            hudClock=0;Save(false);Show("Revived at camp. Your journey continues.");
        }

        Font font;
        public RectTransform Panel(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image));obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=anchor;rect.anchoredPosition=pos;rect.sizeDelta=size;obj.GetComponent<Image>().color=color;return rect;
        }
        public Text Label(Transform parent,string text,Vector2 anchor,Vector2 pos,Vector2 size,int fontsize)
        {
            var obj=new GameObject("Label",typeof(RectTransform),typeof(Text));obj.transform.SetParent(parent,false);
            var r=obj.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor;r.anchoredPosition=pos;r.sizeDelta=size;
            var t=obj.GetComponent<Text>();t.font=font;t.fontSize=fontsize;t.text=text;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;
        }
        public Button Button(Transform parent,string text,Vector2 anchor,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction click)
        {
            var rect=Panel(text,parent,anchor,pos,size,new Color(.18f,.25f,.23f,.95f));var b=rect.gameObject.AddComponent<Button>();b.targetGraphic=rect.GetComponent<Image>();b.onClick.AddListener(click);Label(rect,text,new Vector2(.5f,.5f),Vector2.zero,size,22);Controls.Button(b);return b;
        }
        void BuildUI()
        {
            if(hudRoot){hudRoot.SetActive(false);Destroy(hudRoot);}
            menu=creditsPanel=null;inventory=null;
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root=new GameObject("Mobile HUD",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            hudRoot=root;root.transform.SetParent(transform,false);SetHudEnabled(false);
            // Keep objects active for label lookup, but hide rendering/input until setup completes.
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            if(!FindAnyObjectByType<EventSystem>())new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            var safe=new GameObject("Safe Area",typeof(RectTransform),typeof(SafeArea));safe.transform.SetParent(root.transform,false);
            var sr=safe.GetComponent<RectTransform>();sr.offsetMin=sr.offsetMax=Vector2.zero;
            var parent=safe.transform;
            status=Label(parent,"",new Vector2(.5f,1),new Vector2(0,-70),new Vector2(950,125),22);
            objective=Label(parent,"",new Vector2(.5f,1),new Vector2(0,-150),new Vector2(1120,70),20);
            message=Label(parent,"",new Vector2(.5f,.5f),new Vector2(0,125),new Vector2(950,80),25);
            Button(parent,"CAMP",new Vector2(1,1),new Vector2(-85,-55),new Vector2(140,60),ToggleMenu);
            Controls.Icon(Button(parent,"ATTACK",new Vector2(1,0),new Vector2(-110,115),new Vector2(180,110),Attack),"attack");
            Controls.Icon(Button(parent,"DODGE",new Vector2(1,0),new Vector2(-300,180),new Vector2(150,70),Dodge),"dodge");
            Button(parent,"OFFER GEMS",new Vector2(0,1),new Vector2(120,-230),new Vector2(200,60),()=>Encounter.OfferGems());
            Controls.Icon(Button(parent,"HEAL",new Vector2(1,0),new Vector2(-280,80),new Vector2(120,70),Heal),"heal");
            var joystick=Panel("Movement",parent,Vector2.zero,new Vector2(135,135),new Vector2(190,190),new Color(.12f,.18f,.16f,.7f));
            var knob=Panel("Thumb",joystick,new Vector2(.5f,.5f),Vector2.zero,new Vector2(65,65),new Color(.7f,.78f,.6f,.9f));
            Controls.Stick(joystick.GetComponent<Image>(),false);Controls.Stick(knob.GetComponent<Image>(),true);
            var stick=joystick.gameObject.AddComponent<MobileStick>();stick.knob=knob;stick.game=this;
            Label(parent,"Drag to ride",Vector2.zero,new Vector2(135,30),new Vector2(230,30),19);
            Label(parent,"Swipe empty right side to look • Mouse: hold right button",new Vector2(.5f,0),new Vector2(0,16),new Vector2(660,28),16);
            var panel=Panel("Camp",parent,new Vector2(.5f,.5f),Vector2.zero,new Vector2(880,530),new Color(.06f,.1f,.09f,.98f));menu=panel.gameObject;
            inventory=Label(panel,"",new Vector2(.5f,1),new Vector2(0,-132),new Vector2(840,145),22);
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
            Label(credits,"FREE MODEL CREDITS\n\nBear / Elk (Moose adaptation): Poly by Google\nReference Knight: Dawid2K • both under CC BY 3.0\nhttps://creativecommons.org/licenses/by/3.0/\n\nDonkey, Deer, Wolf, Fox, Zombie and Dragon: Quaternius\nCC0 1.0 — https://creativecommons.org/publicdomain/zero/1.0/\n\nModels converted to FBX; moose and rider adapted.\nActive player models, animation and audio are original project work.\nOriginal model links and modification details are in THIRD_PARTY.md.",new Vector2(.5f,.5f),new Vector2(0,40),new Vector2(830,380),21);
            Button(credits,"MODEL SOURCES",new Vector2(.5f,0),new Vector2(-150,65),new Vector2(250,65),()=>Application.OpenURL("https://github.com/aizcrest/donkey-simulator-for-czach/blob/main/THIRD_PARTY.md"));
            Button(credits,"BACK",new Vector2(.5f,0),new Vector2(150,65),new Vector2(250,65),()=>creditsPanel.SetActive(false));
            creditsPanel.SetActive(false);
            if(!Additions)Additions=gameObject.AddComponent<CampAdditions>();
            Additions.Setup(this,panel,parent);
            menu.SetActive(false);
            var combatHud=GetComponent<CombatHud>();if(!combatHud)combatHud=gameObject.AddComponent<CombatHud>();
            combatHud.Setup(this,parent,Revive);
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
        public void ResetStick(){pointer=int.MinValue;if(game)game.moveInput=Vector2.zero;if(knob)knob.anchoredPosition=Vector2.zero;}
        void OnDisable(){ResetStick();}
    }
}
