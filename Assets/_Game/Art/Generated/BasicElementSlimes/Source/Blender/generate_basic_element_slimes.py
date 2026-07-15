import math
import os
from pathlib import Path

import bpy
from mathutils import Vector


SOURCE_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = Path(os.environ.get("BUBBLEMIND_PROJECT_ROOT", SOURCE_DIR.parents[6])).resolve()
GENERATED_ROOT = PROJECT_ROOT / "Assets/_Game/Art/Generated/BasicElementSlimes"
RUNTIME_DIR = GENERATED_ROOT / "Runtime"
ARTIFACT_DIR = PROJECT_ROOT / "Artifacts/BasicElementSlimes/Blender"
BLEND_PATH = SOURCE_DIR / "BasicElementSlimes.blend"

ELEMENT_ORDER = ("Water", "Fire", "Earth", "Wind", "Lightning")
ELEMENT_SPECS = {
    "Water": {
        "body": (0.105, 0.535, 0.93),
        "accent": (0.61, 0.90, 1.00),
        "detail": (0.07, 0.30, 0.82),
        "height": 1.12,
        "width": 0.65,
        "depth": 0.53,
        "lean": -0.08,
    },
    "Fire": {
        "body": (1.00, 0.255, 0.105),
        "accent": (1.00, 0.78, 0.16),
        "detail": (0.72, 0.055, 0.035),
        "height": 1.08,
        "width": 0.66,
        "depth": 0.53,
        "lean": 0.03,
    },
    "Earth": {
        "body": (0.43, 0.66, 0.20),
        "accent": (0.43, 0.245, 0.095),
        "detail": (0.30, 0.58, 0.11),
        "height": 0.93,
        "width": 0.70,
        "depth": 0.56,
        "lean": 0.00,
    },
    "Wind": {
        "body": (0.49, 0.89, 0.73),
        "accent": (0.86, 1.00, 0.91),
        "detail": (0.10, 0.57, 0.49),
        "height": 1.06,
        "width": 0.64,
        "depth": 0.51,
        "lean": -0.02,
    },
    "Lightning": {
        "body": (1.00, 0.67, 0.085),
        "accent": (1.00, 0.94, 0.42),
        "detail": (0.48, 0.16, 0.88),
        "height": 1.04,
        "width": 0.64,
        "depth": 0.51,
        "lean": 0.04,
    },
}


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.materials,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        for datablock in list(collection):
            if datablock.users == 0:
                collection.remove(datablock)


def make_material(name, color, roughness=0.42, emission=None, emission_strength=0.0):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    material.diffuse_color = (*color, 1.0)
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = 0.0
    bsdf.inputs["Roughness"].default_value = roughness
    ior_level = bsdf.inputs.get("IOR Level") or bsdf.inputs.get("Specular IOR Level")
    if ior_level:
        ior_level.default_value = 0.30
    coat_weight = bsdf.inputs.get("Coat Weight")
    if coat_weight:
        coat_weight.default_value = 0.06
    if emission is not None:
        emission_input = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
        emission_strength_input = bsdf.inputs.get("Emission Strength")
        if emission_input:
            emission_input.default_value = (*emission, 1.0)
        if emission_strength_input:
            emission_strength_input.default_value = emission_strength
    return material


def assign_material(obj, material):
    if obj.type == "MESH":
        obj.data.materials.append(material)
    return obj


def smooth_mesh(obj):
    if obj.type == "MESH":
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    return obj


def tag_object(obj, element, export_name, part_kind):
    obj["bubble_mind_element"] = element
    obj["export_name"] = export_name
    obj["part_kind"] = part_kind
    return obj


def create_empty(element, export_name, parent=None, location=(0.0, 0.0, 0.0), part_kind="socket"):
    obj = bpy.data.objects.new(f"{element}_{export_name}", None)
    bpy.context.collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    obj.location = location
    tag_object(obj, element, export_name, part_kind)
    return obj


def create_ico(
    element,
    export_name,
    location,
    scale,
    material,
    parent,
    subdivisions=1,
    part_kind="detail",
    rotation=(0.0, 0.0, 0.0),
):
    bpy.ops.mesh.primitive_ico_sphere_add(
        subdivisions=subdivisions,
        radius=1.0,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = f"{element}_{export_name}"
    obj.parent = parent
    obj.scale = scale
    assign_material(obj, material)
    smooth_mesh(obj)
    tag_object(obj, element, export_name, part_kind)
    return obj


def create_radial_body(element, spec, material, parent):
    height = spec["height"]
    radius_x = spec["width"]
    radius_y = spec["depth"]
    lean = spec["lean"]
    profile = (
        (0.035, radius_x * 0.82, radius_y * 0.84, 0.070, 0.00),
        (0.095, radius_x * 1.04, radius_y * 1.05, 0.050, 0.00),
        (height * 0.25, radius_x * 1.06, radius_y * 1.06, 0.020, lean * 0.03),
        (height * 0.48, radius_x * 0.98, radius_y * 1.00, 0.010, lean * 0.10),
        (height * 0.68, radius_x * 0.82, radius_y * 0.84, 0.005, lean * 0.28),
        (height * 0.84, radius_x * 0.61, radius_y * 0.62, 0.000, lean * 0.54),
        (height * 0.96, radius_x * 0.34, radius_y * 0.35, 0.000, lean * 0.80),
        (height, radius_x * 0.10, radius_y * 0.10, 0.000, lean),
    )
    segments = 32
    lobe_count = 8
    vertices = []
    faces = []
    for z, rx, ry, lobe_amount, center_x in profile:
        for index in range(segments):
            angle = math.tau * index / segments
            scallop = 1.0 + lobe_amount * math.cos(lobe_count * angle + 0.32)
            x = center_x + rx * scallop * math.cos(angle)
            y = ry * scallop * math.sin(angle)
            vertices.append((x, y, z))

    for ring in range(len(profile) - 1):
        for index in range(segments):
            next_index = (index + 1) % segments
            a = ring * segments + index
            b = ring * segments + next_index
            c = (ring + 1) * segments + next_index
            d = (ring + 1) * segments + index
            faces.append((a, b, c, d))

    bottom_index = len(vertices)
    vertices.append((0.0, 0.0, 0.025))
    for index in range(segments):
        faces.append((bottom_index, (index + 1) % segments, index))

    top_index = len(vertices)
    vertices.append((lean, 0.0, height + 0.004))
    last_ring = (len(profile) - 1) * segments
    for index in range(segments):
        faces.append((last_ring + index, last_ring + (index + 1) % segments, top_index))

    mesh = bpy.data.meshes.new(f"{element}_SlimeBody_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    body = bpy.data.objects.new(f"{element}_SlimeBody", mesh)
    bpy.context.collection.objects.link(body)
    body.parent = parent
    assign_material(body, material)
    smooth_mesh(body)
    tag_object(body, element, "SlimeBody", "body")
    return body


def create_tube(
    element,
    export_name,
    points,
    start_radius,
    end_radius,
    material,
    parent,
    radial_segments=7,
    part_kind="element_detail",
):
    centers = [Vector(point) for point in points]
    vertices = []
    faces = []
    total = max(1, len(centers) - 1)

    for index, center in enumerate(centers):
        if index == 0:
            tangent = centers[1] - center
        elif index == len(centers) - 1:
            tangent = center - centers[index - 1]
        else:
            tangent = centers[index + 1] - centers[index - 1]
        if tangent.length_squared < 0.000001:
            tangent = Vector((0.0, 0.0, 1.0))
        tangent.normalize()
        reference = Vector((0.0, 1.0, 0.0))
        if abs(tangent.dot(reference)) > 0.94:
            reference = Vector((1.0, 0.0, 0.0))
        side = tangent.cross(reference).normalized()
        binormal = tangent.cross(side).normalized()
        progress = index / total
        radius = start_radius + (end_radius - start_radius) * progress
        for ring_index in range(radial_segments):
            angle = math.tau * ring_index / radial_segments
            offset = side * (math.cos(angle) * radius)
            offset += binormal * (math.sin(angle) * radius)
            vertices.append(tuple(center + offset))

    for ring in range(len(centers) - 1):
        for index in range(radial_segments):
            next_index = (index + 1) % radial_segments
            a = ring * radial_segments + index
            b = ring * radial_segments + next_index
            c = (ring + 1) * radial_segments + next_index
            d = (ring + 1) * radial_segments + index
            faces.append((a, b, c, d))

    start_cap = len(vertices)
    vertices.append(tuple(centers[0]))
    end_cap = len(vertices)
    vertices.append(tuple(centers[-1]))
    last_ring = (len(centers) - 1) * radial_segments
    for index in range(radial_segments):
        next_index = (index + 1) % radial_segments
        faces.append((start_cap, index, next_index))
        faces.append((end_cap, last_ring + next_index, last_ring + index))

    mesh = bpy.data.meshes.new(f"{element}_{export_name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(f"{element}_{export_name}", mesh)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    assign_material(obj, material)
    smooth_mesh(obj)
    tag_object(obj, element, export_name, part_kind)
    return obj


def create_prism(
    element,
    export_name,
    points_xz,
    front_y,
    depth,
    material,
    parent,
    part_kind="element_detail",
):
    count = len(points_xz)
    vertices = [(x, front_y, z) for x, z in points_xz]
    vertices += [(x, front_y + depth, z) for x, z in points_xz]
    faces = [tuple(reversed(range(count))), tuple(range(count, count * 2))]
    for index in range(count):
        next_index = (index + 1) % count
        faces.append((index, index + count, next_index + count, next_index))
    mesh = bpy.data.meshes.new(f"{element}_{export_name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(f"{element}_{export_name}", mesh)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    assign_material(obj, material)
    tag_object(obj, element, export_name, part_kind)
    return obj


def create_face(element, spec, materials, model_root):
    height = spec["height"]
    front_y = -spec["depth"] * 0.96
    eye_z = min(0.67, height * 0.61)
    eye_x = spec["width"] * 0.31
    for side, sign in (("L", -1.0), ("R", 1.0)):
        x = sign * eye_x
        create_ico(
            element,
            f"EyeWhite_{side}",
            (x, front_y - 0.005, eye_z),
            (0.145, 0.047, 0.185),
            materials["eye_white"],
            model_root,
            subdivisions=2,
            part_kind="eye_white",
        )
        create_ico(
            element,
            f"EyeIris_{side}",
            (x + sign * 0.010, front_y - 0.050, eye_z - 0.005),
            (0.086, 0.030, 0.126),
            materials["detail"],
            model_root,
            subdivisions=1,
            part_kind="eye_iris",
        )
        create_ico(
            element,
            f"FaceDark_Pupil_{side}",
            (x + sign * 0.013, front_y - 0.073, eye_z - 0.010),
            (0.043, 0.018, 0.080),
            materials["face_dark"],
            model_root,
            subdivisions=1,
            part_kind="face_dark",
        )
        create_ico(
            element,
            f"EyeHighlight_{side}",
            (x - sign * 0.020, front_y - 0.092, eye_z + 0.055),
            (0.026, 0.012, 0.036),
            materials["highlight"],
            model_root,
            subdivisions=1,
            part_kind="eye_highlight",
        )
        eyebrow_points = (
            (x - sign * 0.060, front_y - 0.050, eye_z + 0.205),
            (x, front_y - 0.060, eye_z + 0.225),
            (x + sign * 0.060, front_y - 0.050, eye_z + 0.205),
        )
        create_tube(
            element,
            f"FaceDark_Brow_{side}",
            eyebrow_points,
            0.016,
            0.012,
            materials["face_dark"],
            model_root,
            radial_segments=5,
            part_kind="face_dark",
        )

    mouth_z = eye_z - 0.215
    create_tube(
        element,
        "FaceDark_Mouth",
        ((-0.045, front_y - 0.072, mouth_z), (0.0, front_y - 0.085, mouth_z - 0.012), (0.045, front_y - 0.072, mouth_z)),
        0.013,
        0.011,
        materials["face_dark"],
        model_root,
        radial_segments=5,
        part_kind="face_dark",
    )
    for side, sign in (("L", -1.0), ("R", 1.0)):
        create_ico(
            element,
            f"Cheek_{side}",
            (sign * spec["width"] * 0.54, front_y - 0.018, eye_z - 0.135),
            (0.070, 0.020, 0.035),
            materials["accent"],
            model_root,
            subdivisions=1,
            part_kind="face_accent",
        )


def build_water(element, spec, materials, element_root):
    height = spec["height"]
    front_y = -spec["depth"] * 0.90
    create_tube(
        element,
        "WaterCrest",
        (
            (-0.06, -0.01, height - 0.06),
            (-0.15, -0.01, height + 0.11),
            (-0.13, -0.01, height + 0.27),
            (-0.02, -0.01, height + 0.37),
            (0.12, -0.01, height + 0.32),
            (0.17, -0.01, height + 0.20),
            (0.09, -0.01, height + 0.12),
        ),
        0.105,
        0.030,
        materials["accent"],
        element_root,
        radial_segments=8,
    )
    create_tube(
        element,
        "WaterWave_Front",
        ((-0.46, front_y - 0.025, 0.18), (-0.22, front_y - 0.050, 0.14), (0.02, front_y - 0.055, 0.18), (0.26, front_y - 0.045, 0.13), (0.47, front_y - 0.020, 0.17)),
        0.034,
        0.026,
        materials["detail"],
        element_root,
        radial_segments=6,
    )
    bubbles = (
        ("Bubble_01", (-0.72, -0.06, 0.72), (0.095, 0.085, 0.105)),
        ("Bubble_02", (-0.78, -0.04, 0.50), (0.045, 0.040, 0.052)),
        ("Bubble_03", (0.70, -0.03, 0.83), (0.070, 0.062, 0.080)),
    )
    for name, location, scale in bubbles:
        create_ico(element, name, location, scale, materials["accent"], element_root, subdivisions=1, part_kind="element_detail")
        create_ico(
            element,
            name + "_Highlight",
            (location[0] - 0.018, location[1] - 0.070, location[2] + 0.025),
            tuple(value * 0.28 for value in scale),
            materials["highlight"],
            element_root,
            subdivisions=1,
            part_kind="element_highlight",
        )


def build_fire(element, spec, materials, element_root):
    height = spec["height"]
    flame_paths = (
        ("Flame_Center", ((0.00, 0.01, height - 0.07), (-0.08, 0.01, height + 0.14), (0.02, 0.01, height + 0.31), (0.00, 0.01, height + 0.48)), 0.145, materials["detail"]),
        ("Flame_Left", ((-0.16, 0.02, height - 0.03), (-0.28, 0.02, height + 0.11), (-0.25, 0.02, height + 0.30), (-0.15, 0.02, height + 0.39)), 0.115, materials["body"]),
        ("Flame_Right", ((0.15, 0.02, height - 0.04), (0.29, 0.02, height + 0.09), (0.25, 0.02, height + 0.24), (0.33, 0.02, height + 0.34)), 0.105, materials["body"]),
        ("Flame_Inner", ((-0.02, -0.09, height + 0.00), (0.05, -0.10, height + 0.13), (0.00, -0.10, height + 0.28)), 0.070, materials["accent"]),
    )
    for name, points, radius, material in flame_paths:
        create_tube(element, name, points, radius, 0.020, material, element_root, radial_segments=7)

    for index, (x, z, scale) in enumerate(((-0.72, 0.72, 0.045), (0.72, 0.62, 0.038), (0.61, 0.91, 0.030)), 1):
        create_prism(
            element,
            f"Flame_Spark_{index:02d}",
            ((x, z - scale), (x + scale * 0.55, z), (x, z + scale * 1.6), (x - scale * 0.55, z)),
            -0.08,
            0.055,
            materials["accent"],
            element_root,
        )
    create_tube(
        element,
        "Flame_BaseGlow",
        ((-0.48, -spec["depth"] * 0.94, 0.15), (-0.20, -spec["depth"] * 0.99, 0.12), (0.08, -spec["depth"] * 1.00, 0.15), (0.38, -spec["depth"] * 0.95, 0.13)),
        0.035,
        0.025,
        materials["accent"],
        element_root,
        radial_segments=6,
    )


def build_earth(element, spec, materials, element_root):
    rocks = (
        ("Rock_Crown_01", (-0.31, -0.01, 1.02), (0.22, 0.18, 0.18), (0.10, 0.23, -0.12)),
        ("Rock_Crown_02", (0.00, 0.00, 1.12), (0.25, 0.20, 0.25), (-0.08, 0.12, 0.05)),
        ("Rock_Crown_03", (0.30, 0.00, 1.01), (0.20, 0.17, 0.17), (0.16, -0.12, 0.08)),
        ("Rock_Base_L01", (-0.58, -0.19, 0.22), (0.17, 0.14, 0.12), (0.20, 0.10, 0.15)),
        ("Rock_Base_L02", (-0.43, -0.31, 0.13), (0.12, 0.10, 0.09), (-0.18, 0.03, -0.12)),
        ("Rock_Base_R01", (0.58, -0.19, 0.21), (0.17, 0.14, 0.13), (-0.10, 0.17, -0.08)),
        ("Rock_Base_R02", (0.42, -0.33, 0.12), (0.11, 0.09, 0.08), (0.12, -0.12, 0.05)),
    )
    for name, location, scale, rotation in rocks:
        create_ico(
            element,
            name,
            location,
            scale,
            materials["accent"],
            element_root,
            subdivisions=1,
            part_kind="element_detail",
            rotation=rotation,
        )

    create_tube(
        element,
        "Leaf_Stem",
        ((0.23, 0.00, 1.07), (0.33, 0.00, 1.22), (0.39, 0.00, 1.36)),
        0.026,
        0.018,
        materials["detail"],
        element_root,
        radial_segments=6,
    )
    create_prism(
        element,
        "Leaf_01",
        ((0.37, 1.27), (0.52, 1.36), (0.56, 1.49), (0.42, 1.45), (0.32, 1.35)),
        -0.05,
        0.075,
        materials["detail"],
        element_root,
    )
    create_prism(
        element,
        "Leaf_02",
        ((0.31, 1.20), (0.22, 1.30), (0.20, 1.40), (0.31, 1.34), (0.37, 1.26)),
        -0.04,
        0.065,
        materials["detail"],
        element_root,
    )


def build_wind(element, spec, materials, element_root):
    height = spec["height"]
    spiral_points = []
    for index in range(15):
        progress = index / 14.0
        angle = math.radians(220.0 - progress * 485.0)
        radius = 0.27 * (1.0 - progress * 0.74)
        spiral_points.append((radius * math.cos(angle), -0.01, height + 0.22 + radius * math.sin(angle)))
    create_tube(
        element,
        "Wind_Spiral",
        spiral_points,
        0.075,
        0.027,
        materials["accent"],
        element_root,
        radial_segments=7,
    )
    ribbons = (
        ("Wind_Ribbon_L01", ((-0.61, -0.03, 0.78), (-0.77, -0.02, 0.87), (-0.89, -0.01, 0.82))),
        ("Wind_Ribbon_L02", ((-0.55, -0.01, 0.58), (-0.73, 0.00, 0.65), (-0.84, 0.01, 0.58))),
        ("Wind_Ribbon_R01", ((0.58, -0.02, 0.74), (0.76, -0.01, 0.83), (0.90, 0.00, 0.76))),
        ("Wind_Ribbon_R02", ((0.54, 0.01, 0.52), (0.72, 0.02, 0.58), (0.84, 0.03, 0.51))),
    )
    for name, points in ribbons:
        create_tube(element, name, points, 0.035, 0.012, materials["detail"], element_root, radial_segments=6)
    create_tube(
        element,
        "Wind_BaseSwirl",
        ((-0.42, -spec["depth"] * 0.96, 0.15), (-0.16, -spec["depth"] * 1.01, 0.12), (0.12, -spec["depth"] * 1.01, 0.17), (0.38, -spec["depth"] * 0.96, 0.13)),
        0.036,
        0.024,
        materials["accent"],
        element_root,
        radial_segments=6,
    )


def lightning_shape(center_x, center_z, scale):
    points = (
        (-0.12, 0.34),
        (0.04, 0.34),
        (-0.02, 0.08),
        (0.15, 0.08),
        (-0.14, -0.38),
        (-0.05, -0.10),
        (-0.19, -0.10),
    )
    return tuple((center_x + x * scale, center_z + z * scale) for x, z in points)


def build_lightning(element, spec, materials, element_root):
    height = spec["height"]
    create_prism(
        element,
        "Spark_Crown",
        lightning_shape(0.04, height + 0.30, 0.80),
        -0.08,
        0.16,
        materials["accent"],
        element_root,
    )
    create_prism(
        element,
        "Spark_Left",
        lightning_shape(-0.73, 0.77, 0.33),
        -0.06,
        0.075,
        materials["detail"],
        element_root,
    )
    create_prism(
        element,
        "Spark_Right",
        lightning_shape(0.73, 0.84, 0.36),
        -0.06,
        0.075,
        materials["detail"],
        element_root,
    )
    create_tube(
        element,
        "Spark_Crack_L",
        ((-0.47, -spec["depth"] * 0.98, 0.18), (-0.33, -spec["depth"] * 1.01, 0.26), (-0.40, -spec["depth"] * 1.015, 0.36), (-0.28, -spec["depth"] * 0.99, 0.43)),
        0.025,
        0.015,
        materials["accent"],
        element_root,
        radial_segments=5,
    )
    create_tube(
        element,
        "Spark_Crack_R",
        ((0.48, -spec["depth"] * 0.98, 0.18), (0.34, -spec["depth"] * 1.01, 0.25), (0.40, -spec["depth"] * 1.015, 0.34)),
        0.024,
        0.014,
        materials["accent"],
        element_root,
        radial_segments=5,
    )


def build_character(element, shared_materials):
    spec = ELEMENT_SPECS[element]
    element_materials = {
        "body": make_material(f"MAT_BasicSlime_{element}_Body", spec["body"], roughness=0.40),
        "accent": make_material(f"MAT_BasicSlime_{element}_Accent", spec["accent"], roughness=0.34),
        "detail": make_material(f"MAT_BasicSlime_{element}_Detail", spec["detail"], roughness=0.46),
        **shared_materials,
    }
    root = create_empty(element, "CharacterRoot", part_kind="root")
    root["asset_family"] = "BasicElementSlimes"
    model_root = create_empty(element, "ModelRoot", root, part_kind="model_root")
    create_radial_body(element, spec, element_materials["body"], model_root)
    create_face(element, spec, element_materials, model_root)
    element_root = create_empty(element, f"Element_{element}", model_root, part_kind="element_root")

    if element == "Water":
        build_water(element, spec, element_materials, element_root)
    elif element == "Fire":
        build_fire(element, spec, element_materials, element_root)
    elif element == "Earth":
        build_earth(element, spec, element_materials, element_root)
    elif element == "Wind":
        build_wind(element, spec, element_materials, element_root)
    elif element == "Lightning":
        build_lightning(element, spec, element_materials, element_root)

    create_empty(element, "RightHandSocket", model_root, (spec["width"] * 0.72, -0.04, 0.53), "socket")
    create_empty(element, "LeftHandSocket", model_root, (-spec["width"] * 0.72, -0.04, 0.53), "socket")
    create_empty(element, "SkillVfxSocket", model_root, (0.0, -spec["depth"] * 1.05, 0.62), "socket")
    create_empty(element, "ProjectileSocket", model_root, (0.0, -spec["depth"] * 1.15, 0.68), "socket")
    create_empty(element, "GroundVfxSocket", root, (0.0, 0.0, 0.03), "socket")
    create_empty(element, "TargetSocket", root, (0.0, 0.0, 0.66), "socket")
    create_empty(element, "HealthBarSocket", root, (0.0, 0.0, 1.68), "socket")
    return root


def descendants_including(root):
    return [root] + list(root.children_recursive)


def save_source(roots):
    BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)
    lineup_spacing = 2.15
    center = (len(roots) - 1) * 0.5
    for index, root in enumerate(roots.values()):
        root.location.x = (index - center) * lineup_spacing
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))


def export_character(element, root):
    output_path = RUNTIME_DIR / f"BasicSlime_{element}.fbx"
    RUNTIME_DIR.mkdir(parents=True, exist_ok=True)
    objects = descendants_including(root)
    original_location = root.location.copy()
    original_names = {obj: obj.name for obj in objects}
    root.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()

    try:
        for obj in objects:
            export_name = obj.get("export_name")
            if export_name:
                obj.name = export_name
        bpy.ops.object.select_all(action="DESELECT")
        for obj in objects:
            obj.hide_set(False)
            obj.select_set(True)
        bpy.context.view_layer.objects.active = root
        bpy.ops.export_scene.fbx(
            filepath=str(output_path),
            use_selection=True,
            object_types={"EMPTY", "MESH"},
            apply_unit_scale=True,
            apply_scale_options="FBX_SCALE_ALL",
            bake_space_transform=True,
            axis_forward="-Z",
            axis_up="Y",
            add_leaf_bones=False,
            mesh_smooth_type="FACE",
            use_mesh_modifiers=True,
            use_custom_props=False,
            path_mode="AUTO",
        )
    finally:
        for obj, original_name in original_names.items():
            obj.name = original_name
        root.location = original_location
        bpy.context.view_layer.update()
    return output_path


def point_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_area_light(name, location, energy, size, color, target=(0.0, 0.0, 0.65)):
    light_data = bpy.data.lights.new(name, "AREA")
    light_data.energy = energy
    light_data.shape = "DISK"
    light_data.size = size
    light_data.color = color
    light_object = bpy.data.objects.new(name, light_data)
    bpy.context.collection.objects.link(light_object)
    light_object.location = location
    point_at(light_object, target)
    return light_object


def set_hierarchy_render(root, visible):
    for obj in descendants_including(root):
        obj.hide_render = not visible
        obj.hide_set(not visible)


def render_previews(roots, preview_floor):
    ARTIFACT_DIR.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 768
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGBA"

    world = bpy.data.worlds.get("World") or bpy.data.worlds.new("World")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.19, 0.20, 0.31, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.55

    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        pass

    camera_data = bpy.data.cameras.new("PreviewCamera")
    camera = bpy.data.objects.new("PreviewCamera", camera_data)
    bpy.context.collection.objects.link(camera)
    scene.camera = camera
    camera_data.lens = 58.0

    add_area_light("PreviewKey", (-3.0, -4.0, 4.2), 660.0, 3.2, (1.00, 0.84, 0.76))
    add_area_light("PreviewFill", (3.4, -2.3, 2.8), 430.0, 3.0, (0.68, 0.82, 1.00))
    add_area_light("PreviewRim", (0.4, 3.1, 3.4), 520.0, 2.4, (0.72, 1.00, 0.88))

    view_data = (
        ("front", (0.0, -3.75, 1.35), (0.0, 0.0, 0.72)),
        ("three_quarter", (2.55, -3.30, 1.62), (0.0, 0.0, 0.72)),
    )

    for root in roots.values():
        set_hierarchy_render(root, False)

    for element, root in roots.items():
        original_location = root.location.copy()
        root.location = (0.0, 0.0, 0.0)
        set_hierarchy_render(root, True)
        preview_floor.hide_render = False
        preview_floor.hide_set(False)
        bpy.context.view_layer.update()
        for suffix, camera_location, target in view_data:
            camera.location = camera_location
            point_at(camera, target)
            scene.render.filepath = str(ARTIFACT_DIR / f"BasicSlime_{element}_{suffix}.png")
            bpy.ops.render.render(write_still=True)
        set_hierarchy_render(root, False)
        root.location = original_location


def create_preview_floor():
    bpy.ops.mesh.primitive_plane_add(size=9.0, location=(0.0, 0.0, -0.012))
    floor = bpy.context.object
    floor.name = "PreviewFloor"
    floor_material = make_material("MAT_PreviewFloor", (0.24, 0.22, 0.32), roughness=0.72)
    assign_material(floor, floor_material)
    return floor


def main():
    reset_scene()
    shared_materials = {
        "eye_white": make_material("MAT_BasicSlime_EyeWhite", (0.955, 0.965, 1.00), roughness=0.28),
        "face_dark": make_material("MAT_BasicSlime_FaceDark", (0.035, 0.018, 0.085), roughness=0.36),
        "highlight": make_material(
            "MAT_BasicSlime_EyeHighlight",
            (1.0, 1.0, 1.0),
            roughness=0.22,
            emission=(1.0, 1.0, 1.0),
            emission_strength=0.22,
        ),
    }
    roots = {element: build_character(element, shared_materials) for element in ELEMENT_ORDER}
    save_source(roots)
    exported = {element: export_character(element, root) for element, root in roots.items()}
    preview_floor = create_preview_floor()
    render_previews(roots, preview_floor)

    print(f"BUBBLEMIND_BASIC_SLIMES_BLEND={BLEND_PATH}")
    for element in ELEMENT_ORDER:
        print(f"BUBBLEMIND_BASIC_SLIME_FBX_{element.upper()}={exported[element]}")
        print(f"BUBBLEMIND_BASIC_SLIME_PREVIEW_{element.upper()}_FRONT={ARTIFACT_DIR / f'BasicSlime_{element}_front.png'}")
        print(f"BUBBLEMIND_BASIC_SLIME_PREVIEW_{element.upper()}_THREE_QUARTER={ARTIFACT_DIR / f'BasicSlime_{element}_three_quarter.png'}")


main()
