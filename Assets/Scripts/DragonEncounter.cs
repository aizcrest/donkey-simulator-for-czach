using UnityEngine;
namespace DonkeyForest
{
    public sealed class DragonEncounter:MonoBehaviour
    {
        public DragonBoss Boss { get; private set; }
        public bool Active => Boss;
        public bool Unlocked => game.data.gemsPlaced>=GemRules.Required && !game.data.dragonDefeated;
        ForestGame game;
        readonly Vector3[] gemLocations=new Vector3[GemRules.Required];
        readonly GameObject[] gems=new GameObject[GemRules.Required];
        readonly GameObject[] offerings=new GameObject[GemRules.Required];
        bool wasDead;
        public void Setup(ForestGame owner)
        {
            game=owner;
            transform.position=Vector3.zero;
            Vector3 center=game.Ground(Vector3.zero);
            game.Shape("Summoning altar",PrimitiveType.Cylinder,transform,center+Vector3.up*.12f,new Vector3(3,.12f,3),game.Mat(new Color(.28f,.29f,.32f)));
            Ring(transform,center+Vector3.up*.27f,1.6f,new Color(.15f,.7f,.9f));
            for(int i=0;i<GemRules.Required;i++)
            {
                float angle=i*Mathf.PI*2/GemRules.Required;
                Vector3 slot=center+new Vector3(Mathf.Cos(angle)*1.2f,.5f,Mathf.Sin(angle)*1.2f);
                offerings[i]=Crystal(transform,slot,.28f);
                offerings[i].SetActive(i<game.data.gemsPlaced);
                // Stable IDs and fixed positions survive reloads; move aside from trunks deterministically.
                float radius=i%2==0?33:63;
                Vector3 point=new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                for(int attempt=0;attempt<12;attempt++)
                {
                    Vector3 grounded=game.Ground(point);
                    if(!Physics.CheckCapsule(grounded+Vector3.up*.5f,grounded+Vector3.up*1.8f,.9f,1<<ForestTerrain.SceneryLayer))break;
                    point+=new Vector3(Mathf.Cos(angle+.7f),0,Mathf.Sin(angle+.7f))*1.5f;
                }
                point=game.Ground(point,.9f);gemLocations[i]=point;
                if((game.data.gemMask&(1<<i))!=0)continue;
                var node=new GameObject("Ancient forest gem "+(i+1));node.transform.SetParent(transform,false);node.transform.position=point;
                gems[i]=node;
                Crystal(node.transform,Vector3.zero,.6f);
                Ring(node.transform,Vector3.down*.7f,1,new Color(.1f,.85f,.75f));
                var beacon=node.AddComponent<LineRenderer>();beacon.useWorldSpace=false;
                beacon.positionCount=2;beacon.SetPosition(0,Vector3.up*.2f);beacon.SetPosition(1,Vector3.up*5);
                beacon.startWidth=.06f;beacon.endWidth=.02f;beacon.sharedMaterial=game.Glow(new Color(.12f,.8f,.7f));
            }
            if(Unlocked&&!game.Dead)RestartBoss();
        }
        GameObject Crystal(Transform parent,Vector3 position,float size)
        {
            var crystal=game.Shape("Ancient gem",PrimitiveType.Cube,parent,position,new Vector3(size,size*1.4f,size),game.Glow(new Color(.1f,.9f,.8f)));
            crystal.transform.localRotation=Quaternion.Euler(0,45,45);return crystal;
        }
        public LineRenderer Ring(Transform parent,Vector3 worldPosition,float radius,Color color)
        {
            var obj=new GameObject("Attack / objective marker");obj.transform.SetParent(parent,false);
            obj.transform.localPosition=worldPosition;
            var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=64;
            line.startWidth=line.endWidth=.13f;line.sharedMaterial=game.Glow(color);
            for(int i=0;i<64;i++)
            {
                float angle=i*Mathf.PI*2/64;
                Vector3 world=obj.transform.position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                line.SetPosition(i,obj.transform.InverseTransformPoint(game.Ground(world,.12f)));
            }
            return line;
        }
        void Update()
        {
            if(!game)return;
            if(game.Dead){if(!wasDead)CancelBoss();wasDead=true;return;}
            wasDead=false;if(game.Paused)return;
            for(int i=0;i<gems.Length;i++)
            {
                if(!gems[i])continue;
                gems[i].transform.GetChild(0).Rotate(0,60*Time.deltaTime,0,Space.World);
                if(Vector3.Distance(game.rider.position+Vector3.up*.8f,gemLocations[i])>1.9f)continue;
                if(GemRules.TryCollect(ref game.data.gemMask,i))
                {
                    Destroy(gems[i]);gems[i]=null;game.Audio.Play("gem");game.GainXP(25);
                    game.Show($"Ancient gem found! {GemRules.Count(game.data.gemMask)}/10. Bring gems to the central altar.");game.Save(false);
                }
            }
        }
        public void OfferGems()
        {
            if(game.Paused||game.Dead)return;
            if(new Vector2(game.rider.position.x,game.rider.position.z).magnitude>4.5f){game.Show("Ride to the altar in the middle of the forest.");return;}
            if(game.data.dragonDefeated){game.Show("The dragon is defeated. The forest is yours!");return;}
            if(Active){game.Show("Dodge the fireballs and dive. Attack only when the dragon is stunned!");return;}
            int held=GemRules.Held(game.data.gemMask,game.data.gemsPlaced);
            game.data.gemsPlaced=GemRules.Deposit(game.data.gemMask,game.data.gemsPlaced);
            for(int i=0;i<offerings.Length;i++)offerings[i].SetActive(i<game.data.gemsPlaced);
            game.Save(false);
            if(Unlocked)RestartBoss();
            else game.Show(held>0?$"Placed {held} gems. Altar: {game.data.gemsPlaced}/10.":"Find the glowing gems scattered around the forest.");
        }
        public void RestartBoss()
        {
            CancelBoss();if(!Unlocked||game.Dead)return;
            foreach(var enemy in game.enemies.ToArray())if(enemy)Destroy(enemy.gameObject);
            game.enemies.Clear();
            Boss=new GameObject("Forest dragon").AddComponent<DragonBoss>();
            Boss.transform.SetParent(transform,false);Boss.Setup(game,this);
            game.Show($"{game.Difficulty.Name} dragon summoned! Dodge its fireballs and dive; attack during the {game.Difficulty.DragonStunSeconds:0.##}-second stun.");
        }
        void CancelBoss()
        {
            if(!Boss)return;
            Boss.gameObject.SetActive(false);Destroy(Boss.gameObject);Boss=null;
        }
        public string Objective
        {
            get
            {
                if(game.data.dragonDefeated)return "DRAGON DEFEATED — forest champion!";
                if(Boss)return Boss.Status;
                if(Unlocked)return "Return to the altar and OFFER GEMS to challenge the dragon again.";
                float distance=float.MaxValue;Vector3 nearest=Vector3.zero;
                for(int i=0;i<10;i++)if((game.data.gemMask&(1<<i))==0)
                {
                    Vector3 delta=gemLocations[i]-game.rider.position;
                    if(delta.sqrMagnitude<distance){distance=delta.sqrMagnitude;nearest=delta;}
                }
                string hint=GemRules.Count(game.data.gemMask)==10?"Return to the central altar":$"Nearest gem: {Mathf.Sqrt(distance):0}m {(nearest.z>5?"N":nearest.z< -5?"S":"")}{(nearest.x>5?"E":nearest.x< -5?"W":"")}";
                return $"GEMS {GemRules.Count(game.data.gemMask)}/10 • Carrying {GemRules.Held(game.data.gemMask,game.data.gemsPlaced)} • Altar {game.data.gemsPlaced}/10 • {hint}";
            }
        }
        public void Win()
        {
            game.data.dragonDefeated=true;game.data.coins+=500;game.data.potions+=3;
            game.GainXP(250);game.Audio.Play("level");
            game.Show("DRAGON DEFEATED! +500 coins and 3 healing potions.");game.Save(false);CancelBoss();
        }
    }
}
