"""Create original mounted poses/actions on the CC BY Knight's existing rig."""
import bpy,math
from mathutils import Vector,Quaternion

def prepare():
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    bpy.context.view_layer.objects.active=rig
    def aim(name,direction):
        p=rig.pose.bones.get(name)
        if not p:return
        target=(rig.matrix_world.to_3x3().inverted()@Vector(direction)).normalized()
        rest=p.bone.matrix_local.to_quaternion()
        desired=(rest@Vector((0,1,0))).rotation_difference(target)@rest
        relative=p.bone.matrix_local if not p.parent else p.parent.bone.matrix_local.inverted()@p.bone.matrix_local
        base=relative.to_quaternion() if not p.parent else p.parent.matrix.to_quaternion()@relative.to_quaternion()
        p.rotation_mode='QUATERNION';p.rotation_quaternion=base.inverted()@desired
        bpy.context.view_layer.update()
    for side,s in [('L',1),('R',-1)]:
        aim('UpperLeg.'+side,(s*.5,-1,-.15));aim('LowerLeg.'+side,(s*.06,.1,-1))
        aim('Fot.'+side,(0,-1,-.1));aim('UpperArm.'+side,(s*.3,-.4,-1));aim('LowerArm.'+side,(0,-1,.1))
    # Bake the sitting geometry and update bind transforms together.
    graph=bpy.context.evaluated_depsgraph_get();positions=[]
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH':continue
        evaluated=obj.evaluated_get(graph);mesh=evaluated.to_mesh()
        positions.append((obj,[v.co.copy() for v in mesh.vertices]));evaluated.to_mesh_clear()
    for obj,coords in positions:
        for vertex,co in zip(obj.data.vertices,coords):vertex.co=co
    bpy.ops.object.mode_set(mode='POSE');bpy.ops.pose.armature_apply(selected=False);bpy.ops.object.mode_set(mode='OBJECT')
    scene=bpy.context.scene;scene.render.fps=30
    for name,duration in [('MountedIdle',60),('MountedRide',24),('MountedAttack',15),('MountedGuard',30)]:
        action=bpy.data.actions.new(name);rig.animation_data_create();rig.animation_data.action=action
        for frame in range(0,duration+1,3):
            phase=frame/duration*math.pi*2
            for p in rig.pose.bones:p.rotation_mode='XYZ';p.rotation_euler=(0,0,0)
            spine=rig.pose.bones.get('Spine1');arm=rig.pose.bones.get('UpperArm.R');forearm=rig.pose.bones.get('LowerArm.R')
            if spine:
                spine.rotation_euler.x=math.sin(phase)*(.035 if name=='MountedIdle' else .08)
                if name=='MountedAttack':spine.rotation_euler.y=math.sin(phase)*.25
            if name=='MountedAttack' and arm:
                arm.rotation_euler.x=-.75*math.sin(frame/duration*math.pi);arm.rotation_euler.z=.9*math.sin(phase)
                if forearm:forearm.rotation_euler.x=-.3*math.sin(phase)
            if name=='MountedGuard' and arm:arm.rotation_euler.x=-.55;arm.rotation_euler.z=-.25
            for p in rig.pose.bones:p.keyframe_insert('rotation_euler',frame=frame,group=p.name)
        action.use_fake_user=True
    rig.animation_data.action=None;scene.frame_set(0)
    for p in rig.pose.bones:p.rotation_euler=(0,0,0)
