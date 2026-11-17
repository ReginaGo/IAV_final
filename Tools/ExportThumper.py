"""Run with Blender --background <original.blend> --python Tools/ExportThumper.py.
Exports the actual Thumper meshes in their evaluated pose without modifying the source blend.
"""
import bpy
from pathlib import Path

project = Path(__file__).resolve().parents[1]
if bpy.context.object and bpy.context.object.mode != "OBJECT":
    bpy.ops.object.mode_set(mode="OBJECT")
output = project / "Assets/Mounstros/Thumper.fbx"
graph = bpy.context.evaluated_depsgraph_get()
exports = []
for name in ("Thumper", "Thumper_tong"):
    original = bpy.data.objects.get(name)
    if original is None or original.type != "MESH":
        raise RuntimeError("Missing original mesh: " + name)
    evaluated = original.evaluated_get(graph)
    mesh = bpy.data.meshes.new_from_object(evaluated)
    copy = bpy.data.objects.new(name + "_Export", mesh)
    bpy.context.collection.objects.link(copy)
    copy.matrix_world = original.matrix_world.copy()
    exports.append(copy)

bpy.ops.object.select_all(action="DESELECT")
for obj in exports:
    obj.select_set(True)
bpy.context.view_layer.objects.active = exports[0]
bpy.ops.export_scene.fbx(filepath=str(output), use_selection=True, object_types={"MESH"},
                         bake_anim=False, axis_forward="-Z", axis_up="Y", path_mode="COPY", embed_textures=True)
print("EXPORTED_THUMPER", output, sum(len(obj.data.vertices) for obj in exports))
