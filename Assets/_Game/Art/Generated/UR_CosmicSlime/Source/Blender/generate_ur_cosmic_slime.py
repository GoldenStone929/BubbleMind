import math
import os
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(os.environ["BUBBLEMIND_PROJECT_ROOT"])
BLEND_PATH = ROOT / "Assets/_Game/Art/Generated/UR_CosmicSlime/Source/Blender/UR_CosmicSlime.blend"
FBX_PATH = ROOT / "Assets/_Game/Art/Generated/UR_CosmicSlime/Runtime/UR_CosmicSlime.fbx"
PREVIEW_PATH = ROOT / "Artifacts/UR_CosmicSlime/Blender/UR_CosmicSlime_preview.png"


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def material(name, base, metallic=0.0, roughness=0.35, emission=None, emission_strength=0.0, alpha=1.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*base, alpha)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*base, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Alpha"].default_value = alpha
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
        bsdf.inputs["Emission Strength"].default_value = emission_strength
    if alpha < 1.0:
        mat.surface_render_method = "DITHERED"
    return mat


def assign(obj, mat):
    obj.data.materials.append(mat)


def smooth(obj):
    if obj.type == "MESH":
        for polygon in obj.data.polygons:
            polygon.use_smooth = True


def ico(name, location, scale, mat, subdivisions=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign(obj, mat)
    smooth(obj)
    return obj


def uv_sphere(name, location, scale, mat, segments=24, rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign(obj, mat)
    smooth(obj)
    return obj


def curve_object(name, points, bevel, mat, cyclic=False, resolution=1):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = resolution
    curve.bevel_depth = bevel
    curve.bevel_resolution = 2
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for point, co in zip(spline.bezier_points, points):
        point.co = co
        point.handle_left_type = "AUTO"
        point.handle_right_type = "AUTO"
    spline.use_cyclic_u = cyclic
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    assign(obj, mat)
    return obj


def horn(name, base, height, lean, radius, mat):
    bx, by, bz = base
    points = [
        (bx, by, bz),
        (bx + lean * 0.10, by, bz + height * 0.32),
        (bx + lean * 0.42, by + 0.01, bz + height * 0.68),
        (bx + lean, by + 0.03, bz + height),
    ]
    obj = curve_object(name, points, radius, mat, resolution=2)
    obj.data.bevel_factor_start = 0.0
    obj.data.bevel_factor_end = 1.0
    taper = bpy.data.curves.new(f"{name}_Taper", "CURVE")
    taper.dimensions = "2D"
    spline = taper.splines.new("POLY")
    spline.points.add(2)
    for p, co in zip(spline.points, [(0, 0.95, 0, 1), (0.55, 0.65, 0, 1), (1, 0.03, 0, 1)]):
        p.co = co
    taper_obj = bpy.data.objects.new(f"{name}_Taper", taper)
    bpy.context.collection.objects.link(taper_obj)
    obj.data.taper_object = taper_obj
    taper_obj.hide_render = True
    taper_obj.hide_viewport = True
    return obj


def orbit_arc(name, radius_x, radius_y, z, start_deg, end_deg, tilt, mat, bevel=0.035, y_offset=0.0):
    steps = max(8, int(abs(end_deg - start_deg) / 9))
    points = []
    tilt_r = math.radians(tilt)
    for i in range(steps + 1):
        angle = math.radians(start_deg + (end_deg - start_deg) * i / steps)
        x = radius_x * math.cos(angle)
        y = radius_y * math.sin(angle) + y_offset
        local_z = z + math.sin(angle) * math.sin(tilt_r) * 0.18
        points.append((x, y, local_z))
    return curve_object(name, points, bevel, mat, resolution=1)


def cube_fragment(name, location, rotation, scale, mat):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign(obj, mat)
    bevel = obj.modifiers.new("EdgeBevel", "BEVEL")
    bevel.width = 0.025
    bevel.segments = 2
    return obj


def eye_prism(name, points, front_y, depth, mat):
    vertices = [(x, front_y, z) for x, z in points] + [(x, front_y + depth, z) for x, z in points]
    faces = [(0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)]
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    assign(obj, mat)
    bevel = obj.modifiers.new("SoftEyeEdge", "BEVEL")
    bevel.width = 0.018
    bevel.segments = 2
    return obj


def build_character():
    shell = material("MAT_Shell", (0.025, 0.004, 0.075), roughness=0.16, emission=(0.08, 0.005, 0.22), emission_strength=0.12)
    core = material("MAT_Core", (0.36, 0.025, 0.85), roughness=0.17, emission=(0.58, 0.08, 1.0), emission_strength=2.8)
    orbit = material("MAT_Orbit", (0.17, 0.075, 0.22), metallic=0.78, roughness=0.28, emission=(0.14, 0.01, 0.30), emission_strength=0.28)

    body = ico("SlimeBody", (0, 0, 0.72), (0.84, 0.61, 0.64), shell, subdivisions=3)
    body.scale.z = 1.02
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    for i in range(9):
        angle = 2 * math.pi * i / 9
        radius = 0.68 + (0.04 if i % 2 == 0 else 0.0)
        x, y = radius * math.cos(angle), radius * 0.72 * math.sin(angle)
        lobe = ico(f"SlimeSkirt_{i + 1:02d}", (x, y, 0.15), (0.37, 0.30, 0.14), shell, subdivisions=2)
        lobe.rotation_euler.z = angle

    horn("Horn_L", (-0.42, -0.02, 1.25), 0.52, -0.16, 0.115, shell)
    horn("Horn_R", (0.36, -0.01, 1.28), 0.34, 0.06, 0.09, shell)

    left_eye = [(-0.49, 0.98), (-0.13, 0.91), (-0.19, 0.78), (-0.42, 0.82)]
    right_eye = [(-x, z) for x, z in reversed(left_eye)]
    eye_prism("Eye_L", left_eye, -0.595, 0.055, core)
    eye_prism("Eye_R", right_eye, -0.595, 0.055, core)

    diamond = cube_fragment("ForeheadSigil", (0, -0.594, 1.22), (math.radians(45), 0, math.radians(45)), (0.105, 0.025, 0.105), core)
    inner = cube_fragment("ForeheadSigilCut", (0, -0.625, 1.22), (math.radians(45), 0, math.radians(45)), (0.052, 0.018, 0.052), orbit)
    inner.name = "ForeheadSigilInner"

    uv_sphere("SingularityCore", (0, -0.64, 0.58), (0.13, 0.06, 0.13), orbit, segments=24, rings=12)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.22, minor_radius=0.045, major_segments=32, minor_segments=8, location=(0, -0.635, 0.58), rotation=(math.radians(90), 0, 0))
    accretion = bpy.context.object
    accretion.name = "SingularityAccretion"
    accretion.scale.x = 1.25
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign(accretion, core)
    smooth(accretion)

    arcs = [
        ("OrbitRing_A1", 1.00, 0.74, 0.46, 192, 350, -12),
        ("OrbitRing_A2", 1.00, 0.74, 0.46, 8, 156, -12),
        ("OrbitRing_B1", 0.88, 0.68, 1.08, 18, 142, 18),
        ("OrbitRing_B2", 0.88, 0.68, 1.08, 164, 294, 18),
    ]
    for args in arcs[:2]:
        orbit_arc(*args, orbit)
    for args in arcs[2:]:
        orbit_arc(*args, orbit, y_offset=0.30)

    fragments = [
        (-0.92, -0.12, 0.54, 0.15), (0.91, 0.08, 0.50, -0.20),
        (-0.74, 0.20, 1.18, -0.35), (0.78, -0.05, 1.22, 0.28),
        (-0.52, -0.46, 0.33, 0.42), (0.58, -0.43, 0.36, -0.38),
    ]
    for i, (x, y, z, rz) in enumerate(fragments, 1):
        cube_fragment(f"OrbitFragment_{i:02d}", (x, y, z), (0.12, -0.18, rz), (0.13, 0.045, 0.04), orbit)

    droplets = [(-0.67, -0.34, 0.55, 0.07), (0.64, -0.38, 0.72, 0.09), (0.74, 0.03, 0.35, 0.055), (-0.49, 0.20, 0.88, 0.045)]
    for i, (x, y, z, radius) in enumerate(droplets, 1):
        ico(f"CosmicDroplet_{i:02d}", (x, y, z), (radius, radius, radius), core, subdivisions=2)

    root = bpy.data.objects.new("CharacterRoot", None)
    bpy.context.collection.objects.link(root)
    for obj in list(bpy.context.scene.objects):
        if obj != root and not obj.name.endswith("_Taper"):
            obj.parent = root

    for name, location in {
        "RightHandSocket": (0.72, -0.12, 0.67),
        "LeftHandSocket": (-0.72, -0.12, 0.67),
        "SkillVfxSocket": (0, -0.70, 0.58),
        "ProjectileSocket": (0, -0.78, 0.72),
        "GroundVfxSocket": (0, 0, 0.03),
        "TargetSocket": (0, 0, 0.82),
        "HealthBarSocket": (0, 0, 1.66),
    }.items():
        socket = bpy.data.objects.new(name, None)
        bpy.context.collection.objects.link(socket)
        socket.location = location
        socket.empty_display_type = "SPHERE"
        socket.empty_display_size = 0.035
        socket.parent = root

    return root


def convert_curves():
    for obj in list(bpy.context.scene.objects):
        if obj.type == "CURVE" and not obj.name.endswith("_Taper"):
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)
            bpy.ops.object.convert(target="MESH")
            smooth(obj)
            obj.select_set(False)


def save_and_export(root):
    BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)
    FBX_PATH.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for child in root.children_recursive:
        if not child.hide_viewport:
            child.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=True,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        path_mode="AUTO",
    )


def render_preview():
    PREVIEW_PATH.parent.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(PREVIEW_PATH)
    scene.render.film_transparent = False
    scene.world.color = (0.025, 0.025, 0.04)

    bpy.ops.object.camera_add(location=(2.15, -5.1, 1.85))
    camera = bpy.context.object
    camera.name = "PreviewCamera"
    direction = Vector((0, 0, 0.78)) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 58
    scene.camera = camera

    for name, location, energy, color, size in [
        ("Key", (-2.2, -3.0, 3.6), 620, (0.52, 0.26, 1.0), 3.0),
        ("Rim", (2.8, 1.2, 2.7), 760, (0.16, 0.28, 1.0), 2.2),
        ("Fill", (0.0, -1.0, 4.0), 320, (1.0, 0.62, 0.88), 2.0),
    ]:
        light_data = bpy.data.lights.new(name, "AREA")
        light_data.energy = energy
        light_data.color = color
        light_data.shape = "DISK"
        light_data.size = size
        light = bpy.data.objects.new(name, light_data)
        bpy.context.collection.objects.link(light)
        light.location = location
        light.rotation_euler = (Vector((0, 0, 0.7)) - light.location).to_track_quat("-Z", "Y").to_euler()

    bpy.ops.mesh.primitive_plane_add(size=10, location=(0, 0, -0.005))
    floor = bpy.context.object
    floor.name = "PreviewFloor"
    floor_mat = material("PreviewFloorMat", (0.018, 0.018, 0.03), metallic=0.05, roughness=0.3)
    assign(floor, floor_mat)
    bpy.ops.render.render(write_still=True)


reset_scene()
character_root = build_character()
convert_curves()
save_and_export(character_root)
render_preview()
print(f"BUBBLEMIND_BLEND={BLEND_PATH}")
print(f"BUBBLEMIND_FBX={FBX_PATH}")
print(f"BUBBLEMIND_PREVIEW={PREVIEW_PATH}")
