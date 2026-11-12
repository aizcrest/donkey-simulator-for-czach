"""Run with Blender: blender -b --python Tools/convert_models.py -- SOURCE_DIRECTORY.
Converts licensed GLB downloads to Unity-native FBX, preserving rig and actions.
Source URLs and licenses are recorded in AssetSources/models.json and THIRD_PARTY.md.
"""
import bpy, sys, json, math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
sources=Path(sys.argv[sys.argv.index('--')+1])
out=root/'Assets/Resources/Enemies'
for entry in json.loads((root/'Tools/AssetSources/models.json').read_text()):
    name=entry['kind']
    if '--only' in sys.argv and name not in sys.argv[sys.argv.index('--only')+1:]:continue
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    for collection in [bpy.data.actions,bpy.data.meshes,bpy.data.armatures,bpy.data.materials,bpy.data.images]:
        for block in list(collection):
            if block.users==0:collection.remove(block)
    bpy.ops.import_scene.gltf(filepath=str(sources/(name+'.glb')))
    if name=='Rider':
        sys.path.insert(0,str(root/'Tools'))
        from mounted_rider import prepare
        prepare()
    bpy.context.view_layer.update()
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
    lo=Vector(tuple(min(v[i] for v in points) for i in range(3)))
    hi=Vector(tuple(max(v[i] for v in points) for i in range(3)))
    print('SOURCE',name,'bounds',tuple(lo),tuple(hi),'actions',[a.name for a in bpy.data.actions])
    # The source is called Elk. Adapt the silhouette into a moose with broad palmate antlers.
    if name=='Moose':
        import bmesh
        mesh=meshes[0];bm=bmesh.new();bm.from_mesh(mesh.data)
        cut=lo.z+(hi.z-lo.z)*.72
        remove=[v for v in bm.verts if (mesh.matrix_world@v.co).z>cut]
        bmesh.ops.delete(bm,geom=remove,context='VERTS');bm.to_mesh(mesh.data);bm.free()
        material=bpy.data.materials.new('Moose antler');material.diffuse_color=(.38,.28,.14,1)
        # Flat, tapered hand-shaped antlers; source head is at negative Y in Blender.
        head_y=lo.y+(hi.y-lo.y)*.25
        base_z=cut-.04*(hi.z-lo.z)
        unit=(hi.z-lo.z)*.24
        for side in [-1,1]:
            silhouette=[(.1,0),(.4,.12),(.9,.1),(1.35,.3),(1.4,.65),(1.15,.45),(1.25,.95),(.98,.65),(.95,1.2),(.7,.8),(.5,1.05),(.48,.55),(.2,.25)]
            vs=[(side*x*unit,head_y+d*unit,base_z+z*unit) for d in [-.05,.05] for x,z in silhouette]
            n=len(silhouette);fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
            data=bpy.data.meshes.new('Palmate antler');data.from_pydata(vs,[],fs);data.materials.append(material)
            obj=bpy.data.objects.new('Moose palmate antler',data);bpy.context.collection.objects.link(obj)
    for action in bpy.data.actions:
        short=action.name.split('|')[-1]
        for suffix in ['_AnimalArmature','_CharacterArmature','_DragonArmature']:
            short=short.replace(suffix,'')
        action.name=short
    textures=sources/'converted-textures'/name;textures.mkdir(parents=True,exist_ok=True)
    for img in bpy.data.images:
        if img.type!='IMAGE' or img.name in ['Render Result','Viewer Node']:continue
        if img.packed_file or img.has_data:
            img.filepath_raw=str(textures/(name+'_'+Path(img.name).stem+'.png'))
            img.file_format='PNG';img.save()
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,
        object_types={'ARMATURE','MESH','EMPTY'},add_leaf_bones=False,
        bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,
        bake_anim_simplify_factor=.2,axis_forward='-Z',axis_up='Y',path_mode='COPY')
