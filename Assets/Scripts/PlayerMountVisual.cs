using UnityEngine;
namespace DonkeyForest
{
    public sealed class PlayerMountVisual:MonoBehaviour
    {
        ForestGame game;
        EnemyVisual mount,knight;
        Transform back,blade,saddle;
        Vector3 backRest,knightRest;
        float swing;
        bool moving,fast;
        public void Setup(ForestGame owner)
        {
            game=owner;mount=EnemyVisual.Create("Donkey",transform,1.9f);knight=EnemyVisual.Create("Rider",transform,1.75f);
            back=Find(mount.transform,"Torso2");var hips=Find(knight.transform,"Root");
            Sample(mount);Sample(knight);
            Vector3 seat=back?back.position+Vector3.up*.18f:transform.TransformPoint(new Vector3(0,1.15f,-.05f));
            if(hips)knight.transform.position+=seat-hips.position;
            else knight.transform.localPosition=new Vector3(0,.65f,-.05f);
            backRest=back?transform.InverseTransformPoint(back.position):Vector3.up;
            knightRest=knight.transform.localPosition;
            saddle=new GameObject("Saddle and donkey barding").transform;saddle.SetParent(transform,false);
            var leather=game.Mat(new Color(.35f,.13f,.06f));var steel=game.Mat(new Color(.45f,.52f,.56f));
            game.Shape("Saddle blanket",PrimitiveType.Cube,saddle,new Vector3(0,1.05f,-.1f),new Vector3(.65f,.1f,.8f),game.Mat(new Color(.1f,.25f,.4f)));
            game.Shape("Leather saddle",PrimitiveType.Cube,saddle,new Vector3(0,1.15f,-.1f),new Vector3(.55f,.14f,.6f),leather);
            for(int side=-1;side<=1;side+=2)
            {
                game.Shape("Barding plate",PrimitiveType.Cube,saddle,new Vector3(side*.35f,.85f,-.1f),new Vector3(.08f,.35f,.75f),steel);
                for(int bolt=0;bolt<3;bolt++)game.Shape("Armor rivet",PrimitiveType.Sphere,saddle,new Vector3(side*.4f,.98f,-.35f+bolt*.25f),Vector3.one*.07f,leather);
            }
            Transform hand=Find(knight.transform,"Hand.R");blade=new GameObject("Mounted sword").transform;
            blade.SetParent(hand?hand:transform,false);
            float inherited=Mathf.Max(.001f,Mathf.Abs(blade.lossyScale.x));blade.localScale=Vector3.one/inherited;
            if(!hand)blade.localPosition=new Vector3(.5f,1.8f,.1f);
            game.Shape("Blade",PrimitiveType.Cube,blade,new Vector3(0,0,.55f),new Vector3(.07f,.045f,1),steel);
            game.Shape("Blade tip",PrimitiveType.Cube,blade,new Vector3(0,0,1.06f),new Vector3(.055f,.04f,.14f),steel).transform.localRotation=Quaternion.Euler(0,0,45);
            game.Shape("Crossguard",PrimitiveType.Cube,blade,new Vector3(0,0,.08f),new Vector3(.32f,.08f,.08f),steel);
            game.Shape("Grip",PrimitiveType.Cylinder,blade,new Vector3(0,0,-.04f),new Vector3(.075f,.12f,.075f),leather).transform.localRotation=Quaternion.Euler(90,0,0);
            gameObject.AddComponent<MountedMeshPresentation>().Setup(game,mount,knight);
        }
        static Transform Find(Transform root,string bone)
        {
            foreach(var child in root.GetComponentsInChildren<Transform>())if(child.name==bone||child.name.EndsWith(":"+bone))return child;
            return null;
        }
        static void Sample(EnemyVisual visual){foreach(var clip in visual.GetComponentsInChildren<Animation>())clip.Sample();}
        public void Animate(bool riding,bool charging,float attack)
        {
            moving=riding;fast=charging;swing=attack;
            var book=game.Skills.Book;
            mount.Play(book.Active(SkillId.Kick)>0?"kick":moving?fast?"move":"walk":"idle");
            knight.Play(book.Active(SkillId.Guard)>0?"guard":swing>0||book.Active(SkillId.Whirlwind)>0?"attack":moving?"move":"idle");
        }
        void LateUpdate()
        {
            if(!game||game.Paused||game.Dead)return;
            Vector3 delta=back?transform.InverseTransformPoint(back.position)-backRest:Vector3.zero;
            knight.transform.localPosition=knightRest+delta;saddle.localPosition=delta;
            float angle=swing>0?Mathf.Sin(swing/.3f*Mathf.PI)*-100:0;
            if(game.Skills.Book.Active(SkillId.Whirlwind)>0)angle=Time.time*800;
            blade.rotation=game.rider.rotation*Quaternion.Euler(-30,angle,0);
        }
    }
}
