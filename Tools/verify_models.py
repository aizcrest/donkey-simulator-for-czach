"""Run: blender -b --python Tools/verify_models.py
Round-trip checks that the Unity-native FBX meshes, textures and clips survived conversion.
This checks asset files with Blender; it does not replace importing/running them in Unity.
"""
import bpy, json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
for entry in json.loads((root/'Tools/AssetSources/models.json').read_text()):
    name=entry['kind'];file=root/'Assets/Resources/Enemies'/(name+'.fbx')
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    bpy.ops.import_scene.fbx(filepath=str(file))
    bpy.context.view_layer.update();graph=bpy.context.evaluated_depsgraph_get()
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    assert meshes, 'Missing mesh: '+name
    assert sum(len(o.data.polygons) for o in meshes)>100, 'Unexpected empty model: '+name
    points=[o.evaluated_get(graph).matrix_world@Vector(c) for o in meshes for c in o.evaluated_get(graph).bound_box]
    dimensions=[max(v[i] for v in points)-min(v[i] for v in points) for i in range(3)]
    assert all(d>.1 for d in dimensions), 'Degenerate model: '+name
    actions=[a.name.lower() for a in bpy.data.actions]
    if name not in ['Bear','Moose']:
        assert actions, 'Animations lost: '+name
        expected='flying' if name=='Dragon' else 'walk'
        assert any(expected in a for a in actions), 'Missing movement clip: '+name
        assert any('attack' in a or 'punch' in a for a in actions), 'Missing attack clip: '+name
    if name in ['Bear','Moose','Zombie']:
        textures=list(file.with_suffix('.fbm').glob('*.png'))
        assert textures and all(p.stat().st_size>100 for p in textures), 'Missing textures: '+name
    print('PASS',name,len(meshes),'meshes,',len(actions),'animation clips')
print('All model round-trip checks passed. Unity import and gameplay validation still required.')
