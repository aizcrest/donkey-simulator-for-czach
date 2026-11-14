using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace DonkeyForest.Tests
{
    public sealed class TerrainCollisionTests
    {
        GameObject host,actor;
        Component landscape;
        bool previouslyIgnored;
        static Type TerrainType()
        {
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {var type=assembly.GetType("DonkeyForest.ForestTerrain");if(type!=null)return type;}
            throw new InvalidOperationException("ForestTerrain runtime type not found.");
        }
        [SetUp] public void Setup()
        {
            previouslyIgnored=Physics.GetIgnoreLayerCollision(0,9);
            Physics.IgnoreLayerCollision(0,9,true); // Generation must explicitly restore ground collisions.
            host=new GameObject("Terrain collision regression");landscape=host.AddComponent(TerrainType());
            landscape.GetType().GetMethod("Generate").Invoke(landscape,null);
        }
        [TearDown] public void Cleanup()
        {
            if(actor)UnityEngine.Object.DestroyImmediate(actor);
            if(host)UnityEngine.Object.DestroyImmediate(host);
            Physics.IgnoreLayerCollision(0,9,previouslyIgnored);
        }
        Terrain Surface=>(Terrain)landscape.GetType().GetProperty("Surface").GetValue(landscape);
        TerrainCollider Collider=>(TerrainCollider)landscape.GetType().GetProperty("GroundCollider").GetValue(landscape);
        Vector3 Ground(Vector3 position)=>(Vector3)landscape.GetType().GetMethod("Ground").Invoke(landscape,new object[]{position,0f});
        void Recover(CharacterController controller)=>landscape.GetType().GetMethod("KeepAboveGround").Invoke(landscape,new object[]{controller});
        CharacterController Actor(float height,Vector3 position)
        {
            actor=new GameObject("Test actor");actor.transform.position=position;
            var controller=actor.AddComponent<CharacterController>();controller.height=height;controller.center=Vector3.up*(height*.5f);controller.radius=.45f;controller.stepOffset=.4f;
            Physics.SyncTransforms();return controller;
        }
        [Test] public void ColliderUsesRenderedHeightmapAndCoversCampAndHills()
        {
            Assert.IsTrue(Collider.enabled);Assert.AreSame(Surface.terrainData,Collider.terrainData);
            Assert.IsFalse(Physics.GetIgnoreLayerCollision(0,9));
            foreach(var position in new[]{Vector3.zero,new Vector3(-38,0,27),new Vector3(46,0,-32),new Vector3(80,0,80)})
            {
                Assert.IsTrue(Collider.Raycast(new Ray(position+Vector3.up*40,Vector3.down),out var hit,50));
                Assert.AreEqual(Ground(position).y,hit.point.y,.15f);
                Assert.IsTrue(Physics.Raycast(position+Vector3.up*40,Vector3.down,out var worldHit,50,1<<9),"Terrain must be registered with physics before actors spawn.");
                Assert.AreSame(Collider,worldHit.collider);
            }
        }
        [TestCase(1.2f)] [TestCase(2f)] [TestCase(2.4f)]
        public void RepeatedGravityMovesAreStoppedByTerrainPhysics(float height)
        {
            var point=Ground(Vector3.zero);var controller=Actor(height,point+Vector3.up*.1f);
            for(int i=0;i<120;i++)controller.Move(Vector3.down*.5f);
            Assert.GreaterOrEqual(actor.transform.position.y,point.y-.15f,"Physics itself must stop the fall; this test does not invoke recovery.");
            Assert.IsTrue(controller.isGrounded);
        }
        [Test] public void BelowGroundRecoveryWorksEvenWithColliderDisabled()
        {
            var point=Ground(new Vector3(-38,0,27));Collider.enabled=false;
            var controller=Actor(2,point-Vector3.up*200);
            Recover(controller);
            Assert.AreEqual(point.y+.08f,actor.transform.position.y,.001f);Assert.IsTrue(controller.enabled);
            // Recover only penetration, not normal skin-width overlap or airborne movement.
            controller.enabled=false;actor.transform.position=point+Vector3.up*3;controller.enabled=true;
            Recover(controller);Assert.AreEqual(point.y+3,actor.transform.position.y,.001f);
        }
    }
}
